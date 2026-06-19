# ATS Scanner

A local, **privacy-first** resume scanner that shows you how your resume will perform on the most popular Applicant Tracking Systems (ATS) — without uploading your document anywhere.

Your resume never leaves your machine.

---

## Supported ATS Platforms

| Platform | Notes |
|---|---|
| **Workday** | XML-based parser; dislikes columns and tables |
| **Greenhouse** | Modern parser; LinkedIn-aware |
| **Taleo** (Oracle) | Oldest and strictest; plain text fares best |
| **Lever** | Most lenient; recognises GitHub profiles |
| **SAP SuccessFactors** | Similar strictness to Workday |

---

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A resume in `.pdf` or `.docx` format

---

## Installation

Clone the repository and build:

```bash
git clone https://github.com/your-username/ASTScannerButWithPrivacy.git
cd ASTScannerButWithPrivacy
dotnet build
```

---

## Usage

```
ats-scanner scan <resume> [options]
```

### Arguments

| Argument | Description |
|---|---|
| `<resume>` | Path to your resume file (`.pdf` or `.docx`) |

### Options

| Option | Values | Default | Description |
|---|---|---|---|
| `-p`, `--platform` | `all`, `workday`, `greenhouse`, `taleo`, `lever`, `successfactors` | `all` | ATS platform to check against |
| `-o`, `--output` | `text`, `json` | `text` | Output format |
| `-v`, `--verbose` | — | off | Show expanded results with document analysis, positive checklist, issues grouped by category, and top priority actions |

---

## Examples

### Scan against all platforms

```bash
dotnet run --project src/AtsScanner.Cli -- scan "C:\Users\me\Documents\resume.pdf"
```

```bash
dotnet run --project src/AtsScanner.Cli -- scan ~/documents/resume.pdf
```

### Scan against a single platform

```bash
dotnet run --project src/AtsScanner.Cli -- scan resume.pdf --platform taleo
```

```bash
dotnet run --project src/AtsScanner.Cli -- scan resume.pdf -p workday
```

### Export results as JSON

Useful for scripting or saving results:

```bash
dotnet run --project src/AtsScanner.Cli -- scan resume.pdf --output json > results.json
```

```bash
dotnet run --project src/AtsScanner.Cli -- scan resume.pdf -p greenhouse -o json
```

### Get detailed feedback

Add `-v` / `--verbose` for the full breakdown:

```bash
dotnet run --project src/AtsScanner.Cli -- scan resume.pdf --verbose
```

```bash
dotnet run --project src/AtsScanner.Cli -- scan resume.pdf -p taleo -v
```

Verbose mode adds:
- **Document Analysis** — detected contact info, format flags (columns, tables, images), and a full section inventory
- **What looks good** — a positive checklist of things each platform will handle correctly
- **Issues grouped by category** — Formatting, Structure, and Content issues listed separately with detailed `What to do` guidance
- **Top Priority Actions** — the critical issues that affect the most platforms, ranked so you know what to fix first

---

## Reading the Output

Running a scan produces a **summary table** followed by per-platform details.

### Summary table

```
╭─────────────────────┬──────────┬───────────┬───────────────────────────────╮
│ Platform            │ Score    │ Rating    │ Issues                        │
├─────────────────────┼──────────┼───────────┼───────────────────────────────┤
│ Lever               │ 85/100   │ Good      │ 1 warning  2 info             │
│ Greenhouse          │ 75/100   │ Good      │ 2 warning  1 info             │
│ Workday             │ 55/100   │ Fair      │ 1 critical  2 warning         │
│ SAP SuccessFactors  │ 55/100   │ Fair      │ 1 critical  2 warning         │
│ Taleo (Oracle)      │ 35/100   │ Poor      │ 2 critical  3 warning  1 info │
╰─────────────────────┴──────────┴───────────┴───────────────────────────────╯
```

### Score ratings

| Score | Rating |
|---|---|
| 90 – 100 | Excellent |
| 75 – 89 | Good |
| 55 – 74 | Fair |
| 35 – 54 | Poor |
| 0 – 34 | Very Poor |

### Issue severities

| Icon | Severity | Meaning |
|---|---|---|
| ✖ | **Critical** | Likely to cause parsing failure or significant data loss. Fix before applying. |
| ⚠ | **Warning** | May cause a field to be misread or skipped. Strongly recommended to fix. |
| ℹ | **Info** | Minor risk or optimisation suggestion. |

Each issue includes a **suggestion** (indented below with `→`) explaining exactly what to change.

---

## Common Issues & How to Fix Them

### Multi-column layout (Critical on Workday, Taleo, SuccessFactors)

Most ATS parsers read text left-to-right, top-to-bottom in a single pass. A two-column resume causes your skills column and your job history column to be interleaved nonsensically.

**Fix:** Use a single-column layout. Save a plain single-column version specifically for ATS submissions.

### Tables (Critical on Taleo, Warning on Workday)

Table cells are read in unpredictable order by many parsers.

**Fix:** Replace skill tables and education tables with simple bullet lists or plain text.

### Images and graphics (Warning on most platforms)

Profile photos, icons, and decorative dividers are invisible to ATS parsers. Text placed inside or next to images may be lost.

**Fix:** Remove all images. Use plain text dividers (e.g. a horizontal rule made of dashes) if needed.

### Contact info in headers/footers (Warning on Workday, Taleo, SuccessFactors)

Many ATS platforms ignore the document header and footer sections entirely.

**Fix:** Copy your name, email, and phone number into the main body of the first page.

### Missing standard sections (Warning on all platforms)

Parsers map content to structured fields (Job History, Education, etc.) using section header names. Non-standard names like "Where I've Worked" may not be recognised.

**Fix:** Use conventional headers: `Work Experience`, `Education`, `Skills`, `Summary`, `Certifications`.

---

## Tips for Better ATS Scores

1. **Keep a plain version** — Maintain a single-column, no-table `.docx` alongside your designed PDF resume. Submit the plain version to ATS portals.
2. **Check Taleo first** — If your resume scores well on Taleo, it will score well everywhere. Taleo is the strictest parser.
3. **Date formats matter** — Use `Jan 2021 – Mar 2024` or `01/2021 – 03/2024`. Avoid relative dates like "3 years ago".
4. **LinkedIn URL** — Include your full LinkedIn profile URL (`linkedin.com/in/yourname`) in plain text. Greenhouse and Lever auto-populate fields from it.
5. **Keyword placement** — ATS systems do keyword matching. Make sure relevant skills appear in a dedicated `Skills` section *and* in your job descriptions.

---

## Privacy

- **No network calls.** The tool is fully offline. No resume data is transmitted anywhere.
- **No telemetry.** Nothing is logged or tracked.
- **No file copies.** Your resume is read once, in memory, and discarded when the scan completes.
