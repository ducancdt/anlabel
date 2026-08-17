namespace ANLAbel.Core.Workflow;

public enum DocumentWorkflowPrintPolicyMode
{
    Off,
    RequirePublished
}

public sealed record DocumentWorkflowPrintPolicyResult(bool Allowed, string Diagnostic);

/// <summary>
/// Pure publication-policy evaluator. It composes before normal preflight and
/// never changes print geometry, queue selection, or physical-output claims.
/// </summary>
public static class DocumentWorkflowPrintPolicy
{
    public static DocumentWorkflowPrintPolicyResult Evaluate(
        DocumentWorkflowPrintPolicyMode mode,
        bool hasSavedDocument,
        bool auditHealthy,
        bool hashMatchesCurrentDocument,
        DocumentWorkflowState state)
    {
        if (!Enum.IsDefined(mode)) return new(false, "Document workflow print policy configuration is invalid.");
        if (mode == DocumentWorkflowPrintPolicyMode.Off) return new(true, "Document workflow publication policy is informational (Off).");
        if (!hasSavedDocument) return new(false, "Published policy requires a saved document with a local workflow audit.");
        if (!auditHealthy) return new(false, "Document workflow audit requires repair before print preparation.");
        if (!hashMatchesCurrentDocument) return new(false, "Document changed since its workflow decision; create and publish a new revision.");
        if (state != DocumentWorkflowState.Published) return new(false, $"Published policy blocks {state}; publish the exact current revision before print preparation.");
        return new(true, "Exact current document revision is Published; normal preflight, output-contract and queue checks still apply.");
    }
}
