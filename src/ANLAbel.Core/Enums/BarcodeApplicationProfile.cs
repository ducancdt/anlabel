namespace ANLAbel.Core.Enums;

/// <summary>
/// Declares the application contract a barcode must satisfy before a production
/// job may be dispatched.  <see cref="General"/> preserves the permissive legacy
/// authoring behavior; the other profiles opt into industrial checks explicitly.
/// This is a software preflight policy, not a GS1 verifier grade.
/// </summary>
public enum BarcodeApplicationProfile
{
    General,
    Industrial,
    Gs1
}
