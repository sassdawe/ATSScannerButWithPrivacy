namespace AtsScanner.Core.Models;

public enum IssueSeverity
{
    /// <summary>Likely to cause parsing failure or significant data loss.</summary>
    Critical,
    /// <summary>May cause field misread or data to be skipped.</summary>
    Warning,
    /// <summary>Minor risk; optimisation suggestion.</summary>
    Info
}
