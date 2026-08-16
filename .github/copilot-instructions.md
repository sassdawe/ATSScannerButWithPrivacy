# Copilot Instructions

## Project Overview

**ATS Scanner with Privacy** is a local, privacy-first resume scanner that simulates how applicant tracking systems (ATS) parse and score resumes. Users run this tool entirely on their own machine — no resume data is ever sent to external services. It supports analysis against the parsing behaviors of Workday, Greenhouse, Taleo, Lever, SuccessFactors, Haufe-umantis, digitalent.ch, SmartRecruiters, Avature, Personio, prospective.ch, and refline.io.

## Tech Stack

- **Language / Runtime:** C# / .NET 10
- **Target:** Cross-platform CLI (and/or desktop GUI if added later)

## Build & Run Commands

```bash
# Build
dotnet build

# Run the CLI
dotnet run --project src/AtsScanner.Cli -- scan resume.pdf

# Run the desktop GUI (Avalonia UI)
dotnet run --project src/AtsScanner.Gui

# Run tests
dotnet test

# Run a single test class or method
dotnet test --filter "FullyQualifiedName~ClassName.MethodName"

# Run tests for a specific project
dotnet test tests/AtsScanner.Tests

# Publish a self-contained single-file CLI binary (see DISTRIBUTION.md for other RIDs)
dotnet publish src/AtsScanner.Cli -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./dist/win-x64
```

## High-Level Architecture

The repository has three projects under `src/`:

- **`AtsScanner.Core`** — the engine. Contains `Parsing` (PDF/DOCX → `ParsedResume`), `Profiles` (one class per ATS platform), `Models` (`ParsedResume`, `ScanResult`, `AtsPlatform`, etc.), and `Analysis` (runs a resume through one or more profiles).
- **`AtsScanner.Cli`** — Spectre.Console-based command line app (`ats-scanner scan <resume> [options]`).
- **`AtsScanner.Gui`** — Avalonia UI desktop app, built on the same `AtsScanner.Core` engine.

The scanner is structured around three core concerns:

1. **Document Parsing** — Extracts raw text, structure (headings, bullet points, tables), and metadata from resume files (PDF, DOCX). Lives in a dedicated parsing layer that returns a normalized `ParsedResume` model.

2. **ATS Platform Profiles** — Each supported platform has its own scoring/analysis profile that encodes known quirks: field detection heuristics, keyword parsing rules, section recognition patterns, and formatting penalties. Profiles implement a shared `IAtsPlatformProfile` interface and extend `BaseAtsPlatformProfile` for common helpers (`HasFormat`, `HasSection`, `HasContactInfo`, `HasValidDates`, `CalculateScore`, etc.).

3. **Analysis & Reporting** — The scanner runs a `ParsedResume` through one or more platform profiles and produces a structured `ScanResult` with per-field scores, warnings, and improvement suggestions.

**Privacy boundary:** All processing is in-process. No HTTP calls to external services. No telemetry. No file uploads.

## Adding a New ATS Platform Profile

1. Add the platform to the `AtsPlatform` enum (`src/AtsScanner.Core/Models/AtsPlatform.cs`).
2. Create `src/AtsScanner.Core/Profiles/<Name>Profile.cs` extending `BaseAtsPlatformProfile`, documenting the platform's real-world parsing quirks in an XML doc comment and encoding them as `ScanIssue`s in `Analyze`.
3. Register the new profile instance in `ProfileRegistry.All`, and add lowercase name/domain aliases to `ProfileRegistry.TryParse`.
4. Update the CLI `--platform` option description (`src/AtsScanner.Cli/Commands/ScanSettings.cs`) and the platform table / options table in `README.md`.
5. Add `tests/AtsScanner.Tests/Profiles/<Name>ProfileTests.cs` (mirror an existing profile's tests) and extend `ProfileRegistryTests` (`TryParse`, `GetAll` count, `Get_EachPlatform_ReturnsCorrectProfile`).

## Key Conventions

- **Privacy by design:** Never add any network calls, telemetry, analytics, or external API dependencies. All dependencies must be local/offline-capable NuGet packages.
- **Platform profiles are isolated:** Each ATS profile is self-contained. Cross-profile logic belongs in the shared analysis engine, not inside a profile.
- **Normalized model first:** Document parsers always output a `ParsedResume` model — parsers never talk directly to platform profiles.
- **No hardcoded paths:** Use `Path.Combine` and environment-relative paths; the tool must work on Windows, macOS, and Linux.
- **xUnit** for tests. Test projects mirror the source project structure under `tests/`.
- **Target .NET 10** — use the latest C# language features where they improve clarity (e.g., primary constructors, collection expressions, pattern matching).
