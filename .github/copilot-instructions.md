# Copilot Instructions

## Project Overview

**ATS Scanner with Privacy** is a local, privacy-first resume scanner that simulates how applicant tracking systems (ATS) parse and score resumes. Users run this tool entirely on their own machine — no resume data is ever sent to external services. It supports analysis against the parsing behaviors of Workday, Greenhouse, Taleo, Lever, and SuccessFactors.

## Tech Stack

- **Language / Runtime:** C# / .NET 10
- **Target:** Cross-platform CLI (and/or desktop GUI if added later)

## Build & Run Commands

```bash
# Build
dotnet build

# Run
dotnet run --project src/AtsScanner

# Run tests
dotnet test

# Run a single test class or method
dotnet test --filter "FullyQualifiedName~ClassName.MethodName"

# Run tests for a specific project
dotnet test tests/AtsScanner.Tests
```

## High-Level Architecture

The scanner is structured around three core concerns:

1. **Document Parsing** — Extracts raw text, structure (headings, bullet points, tables), and metadata from resume files (PDF, DOCX). Lives in a dedicated parsing layer that returns a normalized `ParsedResume` model.

2. **ATS Platform Profiles** — Each supported platform (Workday, Greenhouse, Taleo, Lever, SuccessFactors) has its own scoring/analysis profile that encodes known quirks: field detection heuristics, keyword parsing rules, section recognition patterns, and formatting penalties. Profiles implement a shared `IAtsPlatformProfile` interface.

3. **Analysis & Reporting** — The scanner runs a `ParsedResume` through one or more platform profiles and produces a structured `ScanResult` with per-field scores, warnings, and improvement suggestions.

**Privacy boundary:** All processing is in-process. No HTTP calls to external services. No telemetry. No file uploads.

## Key Conventions

- **Privacy by design:** Never add any network calls, telemetry, analytics, or external API dependencies. All dependencies must be local/offline-capable NuGet packages.
- **Platform profiles are isolated:** Each ATS profile is self-contained. Cross-profile logic belongs in the shared analysis engine, not inside a profile.
- **Normalized model first:** Document parsers always output a `ParsedResume` model — parsers never talk directly to platform profiles.
- **No hardcoded paths:** Use `Path.Combine` and environment-relative paths; the tool must work on Windows, macOS, and Linux.
- **xUnit** for tests. Test projects mirror the source project structure under `tests/`.
- **Target .NET 10** — use the latest C# language features where they improve clarity (e.g., primary constructors, collection expressions, pattern matching).
