namespace ANLAbel.Project.SaveLoad;

/// <summary>
/// A template is valid JSON but was written by a newer or incompatible
/// document format.  It must not be replaced by an older backup silently.
/// </summary>
public sealed class UnsupportedProjectSchemaException : Exception
{
    public UnsupportedProjectSchemaException(string message)
        : base(message)
    {
    }

    public UnsupportedProjectSchemaException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
