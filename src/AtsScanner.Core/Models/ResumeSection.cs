namespace AtsScanner.Core.Models;

public sealed record ResumeSection(
    string Title,
    SectionType Type,
    string RawContent,
    IReadOnlyList<string> BulletPoints
);
