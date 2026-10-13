namespace ANLAbel.Core.Automation;

/// <summary>
/// Immutable, payload-free identity of a prepared local automation batch.
/// <para>
/// The file-drop automation was removed from the product (plan 13 QĐ-5; the
/// code lives on branch <c>archive/automation-2026-10</c>). This record stays
/// because <see cref="Printing.PrintJobManifest"/> keeps optional automation
/// provenance fields in its fingerprint, and manifests already written as job
/// evidence must remain verifiable.
/// </para>
/// </summary>
public sealed record FileDropPreparedBatchIdentity(
    string PreparedBatchId,
    string EventId,
    string TriggerId,
    string ConfigurationFingerprint,
    string SourceFingerprint,
    string TemplateFingerprint,
    string DataFingerprint,
    int RecordCount);
