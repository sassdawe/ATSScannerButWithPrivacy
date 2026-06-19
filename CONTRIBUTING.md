# Contributing & Developer Guide

This guide is for developers who want to build, modify, or extend ATS Scanner.

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) or later
- Any editor: Visual Studio 2022+, VS Code with the C# Dev Kit, or JetBrains Rider
- Git

Verify your environment:

```bash
dotnet --version   # should be 10.x.x or higher
```

---

## Getting started

```bash
git clone https://github.com/your-username/ASTScannerButWithPrivacy.git
cd ASTScannerButWithPrivacy
dotnet build
dotnet test
```

---

## Project structure

```
ASTScannerButWithPrivacy/
├── src/
│   ├── AtsScanner.Core/           # All business logic (no UI dependencies)
│   │   ├── Models/                # ParsedResume, ScanResult, ScanIssue, enums
│   │   ├── Parsing/               # IResumeParser, PDF + DOCX parsers, text analyzer
│   │   ├── Profiles/              # IAtsPlatformProfile + one file per ATS platform
│   │   └── Analysis/              # ResumeAnalyzer — orchestrates parse → profile → result
│   └── AtsScanner.Cli/            # Console entry point (Spectre.Console)
│       └── Commands/              # ScanCommand, ScanSettings
└── tests/
    └── AtsScanner.Tests/
        ├── Parsing/               # ResumeTextAnalyzer unit tests
        ├── Profiles/              # Per-profile unit tests
        └── Analysis/              # ProfileRegistry tests
```

**Dependency rule:** `AtsScanner.Core` must never reference `AtsScanner.Cli`. The CLI references Core; Core knows nothing about the CLI.

---

## Build

```bash
dotnet build                        # debug, all projects
dotnet build -c Release             # release
```

---

## Testing

```bash
dotnet test                         # all tests
dotnet test --filter "FullyQualifiedName~WorkdayProfile"   # single class
dotnet test --filter "FullyQualifiedName~Analyze_MultiColumn"  # single test
dotnet test -v normal               # verbose output
```

Tests use **xUnit** with **FluentAssertions**. No test should touch the file system (use in-memory `ParsedResume` builders instead).

---

## How the scanning pipeline works

```
File path
  │
  ▼
ResumeParserFactory.GetParser(filePath)
  │  selects PdfResumeParser or DocxResumeParser
  ▼
IResumeParser.ParseAsync(filePath)
  │  returns ParsedResume (RawText, Contact, Sections, FormatFlags)
  ▼
IAtsPlatformProfile.Analyze(resume)    ← one call per platform
  │  returns ScanResult (Score 0-100, Issues, DetectedSections, MissingSections)
  ▼
ScanCommand renders results via Spectre.Console
```

`ResumeTextAnalyzer` (used by both parsers) extracts `ContactInfo` and `ResumeSection[]` from raw text using regex and header heuristics. It is the only place where text-extraction logic should live — parsers must not duplicate it.

---

## Adding a new ATS platform profile

1. **Add the enum value** to `src/AtsScanner.Core/Models/AtsPlatform.cs`:

```csharp
public enum AtsPlatform
{
    Workday,
    Greenhouse,
    Taleo,
    Lever,
    SuccessFactors,
    MyNewPlatform    // ← add here
}
```

2. **Create the profile class** in `src/AtsScanner.Core/Profiles/MyNewPlatformProfile.cs`:

```csharp
using AtsScanner.Core.Models;

namespace AtsScanner.Core.Profiles;

public sealed class MyNewPlatformProfile : BaseAtsPlatformProfile
{
    public override AtsPlatform Platform => AtsPlatform.MyNewPlatform;
    public override string DisplayName => "My New Platform";

    public override ScanResult Analyze(ParsedResume resume)
    {
        var issues = new List<ScanIssue>();

        // Add issues using the inherited helpers:
        // HasFormat(resume, ResumeFormatFlags.HasMultipleColumns)
        // HasSection(resume, SectionType.Experience)
        // HasContactInfo(resume)
        // HasValidDates(resume)
        //
        // Severity guide:
        //   Critical (-20 pts): parsing failure or total field loss
        //   Warning  (-10 pts): field likely misread or skipped
        //   Info     (- 5 pts): minor risk or optimisation

        return new ScanResult(
            Platform, CalculateScore(issues), issues,
            GetDetectedSections(resume), GetMissingSections(resume, CoreSections));
    }
}
```

3. **Register the profile** in `src/AtsScanner.Core/Profiles/ProfileRegistry.cs`:

```csharp
private static readonly IReadOnlyList<IAtsPlatformProfile> All =
[
    new WorkdayProfile(),
    new GreenhouseProfile(),
    new TaleoProfile(),
    new LeverProfile(),
    new SuccessFactorsProfile(),
    new MyNewPlatformProfile()   // ← add here
];
```

4. **Add the name mapping** in `ProfileRegistry.TryParse`:

```csharp
platform = name.ToLowerInvariant() switch
{
    "workday" => AtsPlatform.Workday,
    // ... existing entries ...
    "mynewplatform" or "mnp" => AtsPlatform.MyNewPlatform,
    _ => (AtsPlatform)(-1)
};
```

5. **Write tests** in `tests/AtsScanner.Tests/Profiles/MyNewPlatformProfileTests.cs`. Use the `BuildResume` helper pattern from existing profile test files.

That's it — the CLI picks up the new profile automatically.

---

## Adding support for a new document format

1. Create `src/AtsScanner.Core/Parsing/MyFormatResumeParser.cs` implementing `IResumeParser`.
2. The parser must only extract raw text and set `ResumeFormatFlags` — call `ResumeTextAnalyzer.ExtractContactInfo` and `ResumeTextAnalyzer.ExtractSections` for the structured data.
3. Register the parser in `ResumeParserFactory`:

```csharp
private static readonly IReadOnlyList<IResumeParser> Parsers =
[
    new PdfResumeParser(),
    new DocxResumeParser(),
    new MyFormatResumeParser()   // ← add here
];
```

4. Update the file extension validation in `ScanCommand.ExecuteAsync`.

---

## Code conventions

- **C# 13 / .NET 10** — use primary constructors, collection expressions `[...]`, and pattern matching where they improve clarity.
- **Records for data** — `ParsedResume`, `ResumeSection`, `ScanResult`, `ScanIssue`, `ContactInfo` are all `sealed record` or `sealed record` types. Don't add mutable state to them.
- **No network calls, ever.** The privacy guarantee is absolute. CI has no network allowlist, but code review must reject any `HttpClient`, `WebClient`, or external service dependency in `AtsScanner.Core`.
- **`internal` by default** for helpers not needed outside their namespace (e.g. `ResumeTextAnalyzer` was made `public` only to enable direct unit testing — keep that in mind when adding new helpers).
- **xUnit + FluentAssertions** for all tests. Assert behaviour, not implementation detail.
- **No test file I/O** — build `ParsedResume` in memory in test helpers; don't read files from disk in unit tests.
- **XML doc comments** on all `public` types and members in `AtsScanner.Core`.

---

## Updating ATS profile rules

ATS platform behaviour changes over time. When updating scoring rules:

- Source the change from a credible reference (vendor documentation, verified community research, or reproducible testing with a real ATS).
- Add or update the `Suggestion` text in the relevant `ScanIssue` to explain the updated guidance.
- Update or add a test that asserts the new rule fires (or no longer fires) correctly.
- Note the change in the PR description so it can be tracked across versions.

---

## Building release binaries

```bash
# Windows
dotnet publish src/AtsScanner.Cli -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./dist/win-x64

# macOS (Apple Silicon)
dotnet publish src/AtsScanner.Cli -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -o ./dist/osx-arm64

# Linux
dotnet publish src/AtsScanner.Cli -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -o ./dist/linux-x64
```

See [DISTRIBUTION.md](DISTRIBUTION.md) for packaging, WinGet, Homebrew, and NuGet publishing instructions.

---

## Dependency notes

| Package | Used in | Purpose | License |
|---|---|---|---|
| `UglyToad.PdfPig` | Core | PDF text and layout extraction | MIT |
| `DocumentFormat.OpenXml` | Core | DOCX parsing | MIT |
| `Spectre.Console.Cli` | CLI | Terminal rendering and argument parsing | MIT |
| `xUnit` | Tests | Test framework | Apache 2.0 |
| `FluentAssertions` | Tests | Readable test assertions | Community license (non-commercial free) |
| `coverlet.collector` | Tests | Code coverage collection | MIT |

See [sbom.json](sbom.json) for the full machine-readable bill of materials.

> **FluentAssertions note:** Version 8.x requires a paid license for commercial use. If this project becomes commercial, replace it with `Shouldly` or plain xUnit assertions.

---

## Reporting issues

Please open a GitHub issue with:
- The ATS platform affected
- What the scanner reported vs. what you expected
- If possible, a minimal `.docx` or redacted `.pdf` that reproduces the issue (ensure no personal data is included)
