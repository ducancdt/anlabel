using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ANLAbel.Core.Printing;

namespace ANLAbel.App;

/// <summary>
/// Rasterizes frozen preview drawings on one reusable STA worker. WPF's
/// RenderTargetBitmap call itself is synchronous, so cancellation cannot
/// interrupt one native render call; running it off the dispatcher keeps the
/// operator UI responsive. Only the newest queued request is retained so rapid
/// page changes cannot build an unbounded backlog of drawings/threads.
/// </summary>
internal static class PreviewRasterizer
{
    private const double PreviewDpi = 300;
    private const long MaxPreviewPixels = 32_000_000;

    private static readonly object QueueGate = new();
    private static readonly AutoResetEvent QueueSignal = new(false);
    private static RenderRequest? _pendingRequest;
    private static readonly Thread WorkerThread = StartWorker();
    private static int _workerThreadId;
    private static int _workerStartCount;
    private static long _requestCount;
    private static long _supersededRequestCount;
    private static long _renderStartedCount;
    private static long _renderCompletedCount;
    private static long _renderCanceledCount;
    private static long _peakPixelCount;
    private static int _maxPendingRequestCountObserved;

    // Diagnostics are intentionally internal: the application does not expose
    // thread details, while regression tests can prove that the worker is reused.
    internal static int WorkerThreadId => Volatile.Read(ref _workerThreadId);
    internal static int WorkerStartCount => Volatile.Read(ref _workerStartCount);
    internal static long RequestCount => Volatile.Read(ref _requestCount);
    internal static long SupersededRequestCount => Volatile.Read(ref _supersededRequestCount);
    internal static long RenderStartedCount => Volatile.Read(ref _renderStartedCount);
    internal static long RenderCompletedCount => Volatile.Read(ref _renderCompletedCount);
    internal static long RenderCanceledCount => Volatile.Read(ref _renderCanceledCount);
    internal static long PeakPixelCount => Volatile.Read(ref _peakPixelCount);
    internal static int MaxPendingRequestCountObserved => Volatile.Read(ref _maxPendingRequestCountObserved);
    internal static int PendingRequestCount
    {
        get
        {
            lock (QueueGate)
            {
                return _pendingRequest is null ? 0 : 1;
            }
        }
    }

    public static Task<ImageSource> RenderAsync(
        Drawing drawing,
        double widthDip,
        double heightDip,
        CancellationToken cancellationToken)
    {
        // Keep argument validation synchronous for the legacy API: callers use
        // it to reject unsafe dimensions before a request enters the bounded
        // worker queue. The projection itself remains asynchronous.
        var snapshotTask = RenderSnapshotAsync(drawing, widthDip, heightDip, cancellationToken);
        return ProjectImageAsync(snapshotTask);
    }

    private static async Task<ImageSource> ProjectImageAsync(Task<PreviewRasterResult> snapshotTask)
    {
        var result = await snapshotTask;
        return result.Image;
    }

    /// <summary>
    /// Renders one frozen drawing and returns the exact device-frame identity
    /// captured on the STA worker. The identity is intentionally separate from
    /// the WPF bitmap so preview code cannot claim that a visual match is proven
    /// merely because a BitmapSource was produced.
    /// </summary>
    internal static Task<PreviewRasterResult> RenderSnapshotAsync(
        Drawing drawing,
        double widthDip,
        double heightDip,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(drawing);
        cancellationToken.ThrowIfCancellationRequested();

        var pixelWidth = GetPixelDimension(widthDip);
        var pixelHeight = GetPixelDimension(heightDip);
        var pixelCount = (long)pixelWidth * pixelHeight;
        if (pixelCount > MaxPreviewPixels)
        {
            throw new InvalidOperationException(
                $"Preview label is too large to rasterize safely at {PreviewDpi:0} DPI ({pixelWidth:n0} x {pixelHeight:n0} pixels).");
        }

        var request = new RenderRequest(drawing, pixelWidth, pixelHeight, cancellationToken);
        Interlocked.Increment(ref _requestCount);
        lock (QueueGate)
        {
            // There is no value in rendering a page that the UI has already
            // superseded. The active native call cannot be preempted, but the
            // pending queue is always bounded to one newest request.
            if (_pendingRequest is not null)
            {
                Interlocked.Increment(ref _supersededRequestCount);
                _pendingRequest.CancelAsSuperseded();
            }

            _pendingRequest = request;
            Interlocked.Exchange(ref _maxPendingRequestCountObserved, 1);
        }

        QueueSignal.Set();
        var registration = cancellationToken.Register(static state =>
        {
            ((RenderRequest)state!).CancelFromToken();
        }, request);
        _ = request.Completion.Task.ContinueWith(
            _ => registration.Dispose(),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
        return request.Completion.Task;
    }

    private static int GetPixelDimension(double dimensionDip)
    {
        if (!double.IsFinite(dimensionDip) || dimensionDip <= 0)
        {
            throw new InvalidOperationException("Preview dimensions must be finite and greater than zero.");
        }

        var scaled = dimensionDip * (PreviewDpi / 96.0);
        if (!double.IsFinite(scaled) || scaled > int.MaxValue)
        {
            throw new InvalidOperationException("Preview dimensions exceed the safe raster size supported by this application.");
        }

        return Math.Max(1, (int)Math.Ceiling(scaled));
    }

    private static Thread StartWorker()
    {
        var thread = new Thread(WorkerLoop)
        {
            IsBackground = true,
            Name = "ANLAbel preview raster"
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return thread;
    }

    private static void WorkerLoop()
    {
        Interlocked.Exchange(ref _workerThreadId, Environment.CurrentManagedThreadId);
        Interlocked.Increment(ref _workerStartCount);

        while (true)
        {
            QueueSignal.WaitOne();
            RenderRequest? request;
            lock (QueueGate)
            {
                request = _pendingRequest;
                _pendingRequest = null;
            }

            if (request is null || request.IsCanceled)
            {
                continue;
            }

            RenderOne(request);
        }
    }

    private static void RenderOne(RenderRequest request)
    {
        Interlocked.Increment(ref _renderStartedCount);
        UpdatePeak(ref _peakPixelCount, (long)request.PixelWidth * request.PixelHeight);
        try
        {
            request.ThrowIfCanceled();
            var target = new RenderTargetBitmap(
                request.PixelWidth,
                request.PixelHeight,
                PreviewDpi,
                PreviewDpi,
                PixelFormats.Pbgra32);
            var visual = new DrawingVisual();
            using (var context = visual.RenderOpen())
            {
                context.DrawDrawing(request.Drawing);
            }

            request.ThrowIfCanceled();
            target.Render(visual);
            target.Freeze();
            request.ThrowIfCanceled();
            var stride = checked(request.PixelWidth * 4);
            var pixels = new byte[checked(stride * request.PixelHeight)];
            target.CopyPixels(pixels, stride, 0);
            var identity = RasterGoldenContract.Describe(
                target.PixelWidth,
                target.PixelHeight,
                checked((int)Math.Round(target.DpiX)),
                checked((int)Math.Round(target.DpiY)),
                stride,
                "Pbgra32",
                pixels);
            if (!identity.IsValid)
            {
                throw new InvalidOperationException("Preview raster identity could not be computed for the rendered frame.");
            }

            request.Completion.TrySetResult(new PreviewRasterResult(target, identity));
            Interlocked.Increment(ref _renderCompletedCount);
        }
        catch (OperationCanceledException)
        {
            request.CancelFromWorker();
        }
        catch (Exception ex)
        {
            request.Completion.TrySetException(ex);
        }
    }

    private static void UpdatePeak(ref long target, long value)
    {
        while (true)
        {
            var current = Volatile.Read(ref target);
            if (value <= current || Interlocked.CompareExchange(ref target, value, current) == current)
            {
                return;
            }
        }
    }

    private sealed class RenderRequest
    {
        private int _canceled;
        private int _cancellationCounted;

        public RenderRequest(Drawing drawing, int pixelWidth, int pixelHeight, CancellationToken cancellationToken)
        {
            Drawing = drawing;
            PixelWidth = pixelWidth;
            PixelHeight = pixelHeight;
            CancellationToken = cancellationToken;
            Completion = new TaskCompletionSource<PreviewRasterResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public Drawing Drawing { get; }
        public int PixelWidth { get; }
        public int PixelHeight { get; }
        public CancellationToken CancellationToken { get; }
        public TaskCompletionSource<PreviewRasterResult> Completion { get; }
        public bool IsCanceled => Volatile.Read(ref _canceled) != 0 || CancellationToken.IsCancellationRequested;

        public void ThrowIfCanceled()
        {
            if (IsCanceled)
            {
                throw new OperationCanceledException(CancellationToken);
            }
        }

        public void CancelAsSuperseded()
        {
            if (Interlocked.Exchange(ref _canceled, 1) == 0)
            {
                Completion.TrySetCanceled();
            }

            RecordCancellation();
        }

        public void CancelFromToken()
        {
            if (Interlocked.Exchange(ref _canceled, 1) == 0)
            {
                Completion.TrySetCanceled(CancellationToken);
            }

            RecordCancellation();

            lock (QueueGate)
            {
                if (ReferenceEquals(_pendingRequest, this))
                {
                    _pendingRequest = null;
                    QueueSignal.Set();
                }
            }
        }

        public void CancelFromWorker()
        {
            Interlocked.Exchange(ref _canceled, 1);
            Completion.TrySetCanceled(CancellationToken);
            RecordCancellation();
        }

        private void RecordCancellation()
        {
            // A token can race with a successful native render. Count only
            // requests that did not complete successfully, and guard the
            // counter so cancellation observed by the callback and the STA
            // worker is recorded exactly once.
            if (!Completion.Task.IsCompletedSuccessfully
                && Interlocked.Exchange(ref _cancellationCounted, 1) == 0)
            {
                Interlocked.Increment(ref _renderCanceledCount);
            }
        }
    }
}

internal sealed record PreviewRasterResult(
    ImageSource Image,
    RasterGoldenIdentity RasterIdentity)
{
    public bool IsValid => Image is BitmapSource { IsFrozen: true }
        && RasterIdentity.IsValid;
}
