using ANLAbel.Core.Printing;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class PrinterDriverEvidenceContractTests
{
    [Fact]
    public void KnownQueueFaultWinsBeforePageEvidence()
    {
        var snapshot = Snapshot() with
        {
            QueueHealth = Health(PrinterQueueHealthState.Offline)
        };

        var evidence = PrinterDriverEvidenceContract.Evaluate(snapshot);

        Assert.Equal(PrinterDriverEvidenceState.QueueBlocked, evidence.State);
        Assert.False(evidence.IsFullyObserved);
    }

    [Fact]
    public void MissingQueueHealthDoesNotBecomeReady()
    {
        var evidence = PrinterDriverEvidenceContract.Evaluate(Snapshot() with { QueueHealth = null });

        Assert.Equal(PrinterDriverEvidenceState.QueueHealthUnknown, evidence.State);
    }

    [Fact]
    public void MissingTicketFingerprintDoesNotBecomeFullyObserved()
    {
        var evidence = PrinterDriverEvidenceContract.Evaluate(Snapshot() with { ObservedTicketHash = "" });

        Assert.Equal(PrinterDriverEvidenceState.ObservedTicketUnverified, evidence.State);
        Assert.False(evidence.IsFullyObserved);
    }

    [Theory]
    [InlineData("x")]
    [InlineData("zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz")]
    public void MalformedTicketFingerprintDoesNotBecomeFullyObserved(string fingerprint)
    {
        var evidence = PrinterDriverEvidenceContract.Evaluate(Snapshot() with
        {
            ObservedTicketHash = fingerprint
        });

        Assert.Equal(PrinterDriverEvidenceState.ObservedTicketUnverified, evidence.State);
    }

    [Fact]
    public void ReportedButImpossibleImageableAreaIsInvalid()
    {
        var snapshot = Snapshot() with
        {
            PageObservation = Page(imageableWidth: 30_978, imageableHeight: 0)
        };

        var evidence = PrinterDriverEvidenceContract.Evaluate(snapshot);

        Assert.Equal(PrinterDriverEvidenceState.ImageableAreaInvalid, evidence.State);
    }

    [Fact]
    public void NoImageableValuesRemainDistinctFromInvalidValues()
    {
        var snapshot = Snapshot() with
        {
            PageObservation = new PrinterPageObservation("media", 200, 100, null, null, null, null)
        };

        var evidence = PrinterDriverEvidenceContract.Evaluate(snapshot);

        Assert.Equal(PrinterDriverEvidenceState.ImageableAreaMissing, evidence.State);
    }

    [Fact]
    public void PartialCatalogDoesNotClaimFullyObserved()
    {
        var evidence = PrinterDriverEvidenceContract.Evaluate(Snapshot() with { IsComplete = false });

        Assert.Equal(PrinterDriverEvidenceState.CapabilityCatalogPartial, evidence.State);
        Assert.False(evidence.IsFullyObserved);
    }

    [Fact]
    public void CapabilityDiagnosticDoesNotClaimFullyObserved()
    {
        var evidence = PrinterDriverEvidenceContract.Evaluate(Snapshot() with
        {
            Diagnostic = "invalid custom paper size limits"
        });

        Assert.Equal(PrinterDriverEvidenceState.CapabilityCatalogPartial, evidence.State);
        Assert.Contains("invalid custom", evidence.Diagnostic, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CompleteValidObservationStillDisclaimsPhysicalProof()
    {
        var evidence = PrinterDriverEvidenceContract.Evaluate(Snapshot());

        Assert.Equal(PrinterDriverEvidenceState.FullyObserved, evidence.State);
        Assert.True(evidence.IsFullyObserved);
        Assert.Contains("not physical-output verification", evidence.Diagnostic, StringComparison.OrdinalIgnoreCase);
    }

    private static PrinterCapabilitySnapshot Snapshot() => new(
        "queue",
        "driver",
        DateTimeOffset.UnixEpoch,
        IsAvailable: true,
        IsComplete: true,
        SupportsCustomMedia: false,
        Array.Empty<PrinterMediaCandidate>(),
        PageObservation: Page(190, 90),
        QueueHealth: Health(PrinterQueueHealthState.Ready),
        ObservedTicketHash: new string('a', 64));

    private static PrinterPageObservation Page(double imageableWidth, double imageableHeight) =>
        new("media", 200, 100, 0, 0, imageableWidth, imageableHeight);

    private static PrinterQueueHealthObservation Health(PrinterQueueHealthState state) =>
        new(state, DateTimeOffset.UnixEpoch, state == PrinterQueueHealthState.Ready ? "ready" : "blocked");
}
