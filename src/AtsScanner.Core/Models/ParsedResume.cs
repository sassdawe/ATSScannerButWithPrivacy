namespace AtsScanner.Core.Models;

public sealed record ParsedResume(
    string FileName,
    string FileFormat,
    string RawText,
    ContactInfo Contact,
    IReadOnlyList<ResumeSection> Sections,
    ResumeFormatFlags Format
);

/// <summary>Structural characteristics detected in the source document.</summary>
[Flags]
public enum ResumeFormatFlags
{
    None = 0,
    HasMultipleColumns = 1 << 0,
    HasTables = 1 << 1,
    HasImages = 1 << 2,
    HasHeaders = 1 << 3,
    HasFooters = 1 << 4,
    HasSpecialCharacters = 1 << 5
}
