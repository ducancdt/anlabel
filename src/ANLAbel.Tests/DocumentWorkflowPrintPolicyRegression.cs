using ANLAbel.Core.Workflow;

internal static class DocumentWorkflowPrintPolicyRegression
{
    public static Task Run()
    {
        Require(DocumentWorkflowPrintPolicy.Evaluate(DocumentWorkflowPrintPolicyMode.Off, false, false, false, DocumentWorkflowState.Unknown).Allowed, "Off policy must remain informational.");
        Require(!DocumentWorkflowPrintPolicy.Evaluate(DocumentWorkflowPrintPolicyMode.RequirePublished, false, true, true, DocumentWorkflowState.Published).Allowed, "Published policy must require a saved document.");
        Require(!DocumentWorkflowPrintPolicy.Evaluate(DocumentWorkflowPrintPolicyMode.RequirePublished, true, false, true, DocumentWorkflowState.Published).Allowed, "Published policy must fail closed on an invalid audit.");
        Require(!DocumentWorkflowPrintPolicy.Evaluate(DocumentWorkflowPrintPolicyMode.RequirePublished, true, true, false, DocumentWorkflowState.Published).Allowed, "Published policy must fail closed on a changed document hash.");
        Require(!DocumentWorkflowPrintPolicy.Evaluate(DocumentWorkflowPrintPolicyMode.RequirePublished, true, true, true, DocumentWorkflowState.Approved).Allowed, "Approved must not be silently equivalent to Published.");
        Require(DocumentWorkflowPrintPolicy.Evaluate(DocumentWorkflowPrintPolicyMode.RequirePublished, true, true, true, DocumentWorkflowState.Published).Allowed, "Published policy may only permit the exact current Published revision.");
        Require(!DocumentWorkflowPrintPolicy.Evaluate((DocumentWorkflowPrintPolicyMode)99, true, true, true, DocumentWorkflowState.Published).Allowed, "Unknown policy configuration must fail closed.");
        return Task.CompletedTask;
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
