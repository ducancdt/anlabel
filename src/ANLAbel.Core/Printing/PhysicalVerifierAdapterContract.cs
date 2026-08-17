using System.Text;

namespace ANLAbel.Core.Printing;

/// <summary>
/// SDK-neutral observation returned by a scanner/verifier adapter before it is
/// converted into durable evidence. Raw barcode text or image bytes must never
/// enter this record; adapters provide the canonical observed content digest.
/// </summary>
public sealed record PhysicalVerifierAdapterObservation(
    string AdapterId,
    string AdapterVersion,
    string CorrelationToken,
    PhysicalVerificationMethod Method,
    PhysicalVerificationOutcome Outcome,
    string ObservedContentFingerprint,
    string DeviceId,
    string Grade,
    DateTimeOffset VerifiedAtUtc);

public sealed record PhysicalVerifierAdapterMapping(
    bool IsAccepted,
    string Code,
    string Message,
    PhysicalOutputVerificationEvidence? Evidence)
{
    public static PhysicalVerifierAdapterMapping Pass(PhysicalOutputVerificationEvidence evidence)
        => new(true, "accepted", "The SDK observation was normalized into hash-only physical evidence.", evidence);

    public static PhysicalVerifierAdapterMapping Fail(string code, string message)
        => new(false, code, message, null);
}

/// <summary>
/// Small async surface implemented by a vendor SDK adapter. Implementations
/// may perform I/O here, but return only normalized metadata and fingerprints.
/// </summary>
public interface IPhysicalVerifierPayloadAdapter
{
    ValueTask<PhysicalVerifierAdapterObservation?> ObserveAsync(
        PhysicalOutputVerificationRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Runtime guardrails for a physical verifier adapter. A thermal scanner or
/// barcode verifier is a single hardware channel, so a second observation is
/// rejected while the first one is still in flight. The timeout is finite and
/// deliberately bounded: a vendor SDK that ignores cancellation remains marked
/// busy until its task actually finishes, rather than allowing overlapping
/// reads against the same device.
/// </summary>
public sealed record PhysicalVerifierAdapterOptions
{
    public static readonly TimeSpan MaximumTimeout = TimeSpan.FromMinutes(5);

    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);

    public bool IsValid
        => Timeout > TimeSpan.Zero && Timeout <= MaximumTimeout;
}

/// <summary>
/// Adapts the payload-only SDK surface to the existing evidence verifier. A
/// mapping failure is explicit and reaches the coordinator as a fail-closed
/// diagnostic rather than as a null/success ambiguity.
/// </summary>
public sealed class PhysicalVerifierAdapter : IPhysicalOutputVerifier
{
    private readonly IPhysicalVerifierPayloadAdapter _adapter;
    private readonly PhysicalVerifierAdapterOptions _options;
    private readonly object _sync = new();
    private Task<PhysicalVerifierAdapterObservation?>? _inFlight;

    public PhysicalVerifierAdapter(
        IPhysicalVerifierPayloadAdapter adapter,
        PhysicalVerifierAdapterOptions? options = null)
    {
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        _options = options ?? new PhysicalVerifierAdapterOptions();
        if (!_options.IsValid)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                _options.Timeout,
                $"The physical verifier timeout must be greater than zero and no more than {PhysicalVerifierAdapterOptions.MaximumTimeout.TotalMinutes:0.#} minutes.");
        }
    }

    public async ValueTask<PhysicalOutputVerificationEvidence?> VerifyAsync(
        PhysicalOutputVerificationRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(_options.Timeout);

        var observationTask = StartObservation(request, timeoutCts.Token);
        PhysicalVerifierAdapterObservation? observation;
        try
        {
            // WaitAsync gives the caller a hard upper bound even when a vendor
            // SDK returns a task that does not promptly observe cancellation.
            // The in-flight task is retained until it completes, so a timed-out
            // non-cooperative SDK cannot be called concurrently.
            observation = await observationTask
                .WaitAsync(timeoutCts.Token)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (
            !cancellationToken.IsCancellationRequested
            && timeoutCts.IsCancellationRequested)
        {
            throw new PhysicalVerifierAdapterException(
                "adapter-timeout",
                $"The physical verifier adapter did not respond within {_options.Timeout.TotalSeconds:0.#} seconds; output remains unverified.");
        }

        var mapping = PhysicalVerifierAdapterContract.Map(request, observation);
        if (!mapping.IsAccepted)
        {
            throw new PhysicalVerifierAdapterException(mapping.Code, mapping.Message);
        }

        return mapping.Evidence;
    }

    private Task<PhysicalVerifierAdapterObservation?> StartObservation(
        PhysicalOutputVerificationRequest request,
        CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            if (_inFlight is { IsCompleted: false })
            {
                throw new PhysicalVerifierAdapterException(
                    "adapter-busy",
                    "The physical verifier adapter is still completing a previous observation; retry only after it is idle.");
            }

            var observationTask = _adapter.ObserveAsync(request, cancellationToken).AsTask();
            _inFlight = observationTask;
            _ = observationTask.ContinueWith(
                completed =>
                {
                    // Observe a late SDK exception after a timeout so it cannot
                    // become an unobserved task fault, then release the channel.
                    _ = completed.Exception;
                    lock (_sync)
                    {
                        if (ReferenceEquals(_inFlight, completed))
                        {
                            _inFlight = null;
                        }
                    }
                },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
            return observationTask;
        }
    }
}

public sealed class PhysicalVerifierAdapterException : InvalidOperationException
{
    public PhysicalVerifierAdapterException(string code, string message)
        : base(message)
    {
        Code = string.IsNullOrWhiteSpace(code) ? "adapter-mapping-failed" : code;
    }

    public string Code { get; }
}

/// <summary>
/// Converts vendor-neutral adapter observations into the existing durable
/// evidence contract. This is the only place where adapter identity is folded
/// into the persisted device identity; the coordinator still performs the
/// final manifest/content/grade checks.
/// </summary>
public static class PhysicalVerifierAdapterContract
{
    public static PhysicalVerifierAdapterMapping Map(
        PhysicalOutputVerificationRequest request,
        PhysicalVerifierAdapterObservation? observation)
    {
        if (request is null || !request.IsValid)
        {
            return PhysicalVerifierAdapterMapping.Fail(
                "request-invalid",
                "A valid physical-verification request is required before mapping SDK output.");
        }

        if (observation is null)
        {
            return PhysicalVerifierAdapterMapping.Fail(
                "observation-missing",
                "The scanner/verifier adapter returned no observation.");
        }

        if (string.IsNullOrWhiteSpace(observation.AdapterId)
            || string.IsNullOrWhiteSpace(observation.AdapterVersion))
        {
            return PhysicalVerifierAdapterMapping.Fail(
                "adapter-identity-missing",
                "Adapter name and version are required for SDK evidence.");
        }

        if (request.Method != observation.Method)
        {
            return PhysicalVerifierAdapterMapping.Fail(
                "method-mismatch",
                "The SDK observation method does not match the reviewed verification request.");
        }

        if (!string.IsNullOrWhiteSpace(request.CorrelationToken)
            && !string.Equals(
                Normalize(request.CorrelationToken),
                Normalize(observation.CorrelationToken),
                StringComparison.Ordinal))
        {
            return PhysicalVerifierAdapterMapping.Fail(
                "correlation-mismatch",
                "The SDK observation was not produced for the reviewed correlation token.");
        }

        if (string.IsNullOrWhiteSpace(observation.DeviceId))
        {
            return PhysicalVerifierAdapterMapping.Fail(
                "device-identity-missing",
                "A physical scanner/verifier device identity is required.");
        }

        if (string.IsNullOrWhiteSpace(observation.ObservedContentFingerprint))
        {
            return PhysicalVerifierAdapterMapping.Fail(
                "observed-content-missing",
                "The adapter must return a canonical observed content fingerprint.");
        }

        if (request.Method == PhysicalVerificationMethod.BarcodeVerifier
            && !IsSha256Fingerprint(observation.ObservedContentFingerprint))
        {
            return PhysicalVerifierAdapterMapping.Fail(
                "observed-content-invalid",
                "Barcode-verifier observations must use a 64-character SHA-256 content fingerprint.");
        }

        var adapterIdentity = $"{Normalize(observation.AdapterId)}@{Normalize(observation.AdapterVersion)}";
        var deviceIdentity = $"{adapterIdentity}/{Normalize(observation.DeviceId)}";
        var evidence = PhysicalOutputVerificationEvidence.Create(
            request.JobId,
            request.Manifest.Fingerprint,
            observation.Method,
            observation.Outcome,
            request.ExpectedContentFingerprint,
            NormalizeFingerprint(observation.ObservedContentFingerprint),
            deviceIdentity,
            observation.Grade,
            observation.VerifiedAtUtc);
        return PhysicalVerifierAdapterMapping.Pass(evidence);
    }

    private static string Normalize(string? value)
        => (value ?? string.Empty).Normalize(NormalizationForm.FormC).Trim();

    private static string NormalizeFingerprint(string? value)
        => Normalize(value).ToUpperInvariant();

    private static bool IsSha256Fingerprint(string value)
        => value.Length == 64 && value.All(Uri.IsHexDigit);
}
