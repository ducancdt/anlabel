namespace ANLAbel.Core.Enums;

/// <summary>How Code 128 data is split into code sets A, B and C.</summary>
public enum Code128CodeSet
{
    /// <summary>The encoder picks the shortest code-set sequence (default).</summary>
    Auto = 0,

    /// <summary>Whole symbol in code set A (upper case and control characters).</summary>
    A = 1,

    /// <summary>Whole symbol in code set B (printable ASCII).</summary>
    B = 2,

    /// <summary>Whole symbol in code set C (digit pairs).</summary>
    C = 3,

    /// <summary>The data switches sets itself with &lt;MODEA&gt;, &lt;MODEB&gt; and &lt;MODEC&gt;.</summary>
    Manual = 4
}
