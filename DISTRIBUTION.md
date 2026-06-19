# Distribution Plan

This document covers how to package and distribute ATS Scanner to end users across all major options — from the simplest (GitHub Releases) to the most polished (package managers). They are ordered from lowest to highest effort.

---

## Option 1 — GitHub Releases with self-contained binaries (recommended first step)

### What it gives users
A single executable file per platform that requires no runtime installation. Users download, unzip, and run.

### How to build

```bash
# Windows x64
dotnet publish src/AtsScanner.Cli -c Release -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:PublishTrimmed=true \
  -o ./dist/win-x64

# macOS (Apple Silicon)
dotnet publish src/AtsScanner.Cli -c Release -r osx-arm64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:PublishTrimmed=true \
  -o ./dist/osx-arm64

# macOS (Intel)
dotnet publish src/AtsScanner.Cli -c Release -r osx-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:PublishTrimmed=true \
  -o ./dist/osx-x64

# Linux x64
dotnet publish src/AtsScanner.Cli -c Release -r linux-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:PublishTrimmed=true \
  -o ./dist/linux-x64
```

### Csproj changes needed

Add to `src/AtsScanner.Cli/AtsScanner.Cli.csproj`:

```xml
<PropertyGroup>
  <AssemblyName>ats-scanner</AssemblyName>
  <Version>1.0.0</Version>
  <Description>Local, privacy-first ATS resume scanner</Description>
  <PublishReadyToRun>true</PublishReadyToRun>
</PropertyGroup>
```

`PublishTrimmed` requires verifying that PdfPig and DocumentFormat.OpenXml are trimmer-safe. If trimming breaks PDF/DOCX parsing at runtime, remove `-p:PublishTrimmed=true` and note that binaries will be ~80–100 MB instead of ~30 MB.

### GitHub Release workflow

Create `.github/workflows/release.yml` to build and attach all four binaries automatically when a version tag is pushed:

```yaml
name: Release

on:
  push:
    tags: ['v*']

jobs:
  build:
    strategy:
      matrix:
        include:
          - rid: win-x64
            os: windows-latest
            ext: .exe
          - rid: osx-arm64
            os: macos-latest
            ext: ''
          - rid: osx-x64
            os: macos-latest
            ext: ''
          - rid: linux-x64
            os: ubuntu-latest
            ext: ''
    runs-on: ${{ matrix.os }}
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.x'
      - name: Publish
        run: >
          dotnet publish src/AtsScanner.Cli -c Release -r ${{ matrix.rid }}
          --self-contained true
          -p:PublishSingleFile=true
          -o ./dist/${{ matrix.rid }}
      - name: Package
        shell: bash
        run: |
          cd dist/${{ matrix.rid }}
          zip -r ../../ats-scanner-${{ matrix.rid }}.zip .
      - uses: actions/upload-artifact@v4
        with:
          name: ats-scanner-${{ matrix.rid }}
          path: ats-scanner-${{ matrix.rid }}.zip

  release:
    needs: build
    runs-on: ubuntu-latest
    permissions:
      contents: write
    steps:
      - uses: actions/download-artifact@v4
      - uses: softprops/action-gh-release@v2
        with:
          files: '**/*.zip'
          generate_release_notes: true
```

### User installation (after release is published)

**Windows (PowerShell):**
```powershell
# Download, unzip, optionally add to PATH
Invoke-WebRequest https://github.com/your-username/ASTScannerButWithPrivacy/releases/latest/download/ats-scanner-win-x64.zip -OutFile ats-scanner.zip
Expand-Archive ats-scanner.zip -DestinationPath $env:LOCALAPPDATA\ats-scanner
# Add $env:LOCALAPPDATA\ats-scanner to $env:PATH in System settings
```

**macOS / Linux:**
```bash
curl -Lo ats-scanner.zip https://github.com/your-username/ASTScannerButWithPrivacy/releases/latest/download/ats-scanner-linux-x64.zip
unzip ats-scanner.zip -d ~/.local/bin/
chmod +x ~/.local/bin/ats-scanner
```

---

## Option 2 — dotnet global tool (NuGet.org)

Best for users who already have .NET installed. One command installs from anywhere.

### What it gives users

```bash
dotnet tool install -g AtsScanner
ats-scanner scan resume.pdf
```

### Changes needed

1. **Change the project SDK and properties** in `src/AtsScanner.Cli/AtsScanner.Cli.csproj`:

```xml
<PropertyGroup>
  <OutputType>Exe</OutputType>
  <TargetFramework>net10.0</TargetFramework>
  <PackAsTool>true</PackAsTool>
  <ToolCommandName>ats-scanner</ToolCommandName>
  <PackageId>AtsScanner</PackageId>
  <Version>1.0.0</Version>
  <Authors>Your Name</Authors>
  <Description>Local, privacy-first ATS resume scanner for Workday, Greenhouse, Taleo, Lever, and SuccessFactors</Description>
  <PackageTags>ats;resume;cv;scanner;career</PackageTags>
  <PackageProjectUrl>https://github.com/your-username/ASTScannerButWithPrivacy</PackageProjectUrl>
  <PackageLicenseExpression>MIT</PackageLicenseExpression>
  <RepositoryUrl>https://github.com/your-username/ASTScannerButWithPrivacy</RepositoryUrl>
  <RepositoryType>git</RepositoryType>
</PropertyGroup>
```

2. **Pack and push:**

```bash
dotnet pack src/AtsScanner.Cli -c Release -o ./nupkg
dotnet nuget push ./nupkg/AtsScanner.1.0.0.nupkg --api-key $NUGET_API_KEY --source https://api.nuget.org/v3/index.json
```

3. **GitHub Actions — automate on tag push** (add to the release workflow above):

```yaml
  publish-nuget:
    needs: build
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.x'
      - run: dotnet pack src/AtsScanner.Cli -c Release -o ./nupkg
      - run: dotnet nuget push ./nupkg/*.nupkg --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json
```

Store `NUGET_API_KEY` as a GitHub Actions secret.

### Notes
- NuGet package IDs are globally unique — check [nuget.org](https://nuget.org) that `AtsScanner` is available before committing to it.
- The tool runs on any platform where .NET 10 is installed, so no per-platform builds are needed.
- `TargetFramework` can be a list (`net10.0;net9.0`) to support older runtimes.

---

## Option 3 — Windows Package Manager (WinGet)

Lets Windows users install with `winget install AtsScanner`. Best reached through the [winget-pkgs](https://github.com/microsoft/winget-pkgs) community repository.

### Prerequisites
- A published GitHub Release with a `.zip` or `.exe` installer (Option 1 must be done first)
- The release asset URL and its SHA256 hash

### Manifest files

Create three files locally, then submit via PR to [microsoft/winget-pkgs](https://github.com/microsoft/winget-pkgs):

**`manifests/y/YourName/AtsScanner/1.0.0/YourName.AtsScanner.installer.yaml`**
```yaml
PackageIdentifier: YourName.AtsScanner
PackageVersion: 1.0.0
MinimumOSVersion: '10.0.0.0'
InstallerType: zip
NestedInstallerType: portable
NestedInstallerFiles:
  - RelativeFilePath: ats-scanner.exe
    PortableCommandAlias: ats-scanner
Installers:
  - Architecture: x64
    InstallerUrl: https://github.com/your-username/ASTScannerButWithPrivacy/releases/download/v1.0.0/ats-scanner-win-x64.zip
    InstallerSha256: <SHA256_OF_ZIP>
ManifestType: installer
ManifestVersion: 1.6.0
```

**`manifests/y/YourName/AtsScanner/1.0.0/YourName.AtsScanner.locale.en-US.yaml`**
```yaml
PackageIdentifier: YourName.AtsScanner
PackageVersion: 1.0.0
PackageLocale: en-US
Publisher: Your Name
PackageName: ATS Scanner
ShortDescription: Local, privacy-first ATS resume scanner
License: MIT
PackageUrl: https://github.com/your-username/ASTScannerButWithPrivacy
ManifestType: defaultLocale
ManifestVersion: 1.6.0
```

**`manifests/y/YourName/AtsScanner/1.0.0/YourName.AtsScanner.yaml`**
```yaml
PackageIdentifier: YourName.AtsScanner
PackageVersion: 1.0.0
DefaultLocale: en-US
ManifestType: version
ManifestVersion: 1.6.0
```

### User installation (once accepted)
```powershell
winget install YourName.AtsScanner
ats-scanner scan resume.pdf
```

---

## Option 4 — Homebrew (macOS and Linux)

Lets macOS and Linux users install with `brew install`. The easiest path is a personal tap first, then applying to homebrew-core once the project has traction.

### Personal tap (no review needed)

1. Create a new GitHub repository named `homebrew-ats-scanner`
2. Add a formula file `Formula/ats-scanner.rb`:

```ruby
class AtsScanner < Formula
  desc "Local, privacy-first ATS resume scanner"
  homepage "https://github.com/your-username/ASTScannerButWithPrivacy"
  version "1.0.0"
  license "MIT"

  on_macos do
    on_arm do
      url "https://github.com/your-username/ASTScannerButWithPrivacy/releases/download/v1.0.0/ats-scanner-osx-arm64.zip"
      sha256 "<SHA256_OF_OSX_ARM64_ZIP>"
    end
    on_intel do
      url "https://github.com/your-username/ASTScannerButWithPrivacy/releases/download/v1.0.0/ats-scanner-osx-x64.zip"
      sha256 "<SHA256_OF_OSX_X64_ZIP>"
    end
  end

  on_linux do
    url "https://github.com/your-username/ASTScannerButWithPrivacy/releases/download/v1.0.0/ats-scanner-linux-x64.zip"
    sha256 "<SHA256_OF_LINUX_ZIP>"
  end

  def install
    bin.install "ats-scanner"
  end

  test do
    assert_match "ATS Scanner", shell_output("#{bin}/ats-scanner --version")
  end
end
```

### User installation (personal tap)
```bash
brew tap your-username/ats-scanner
brew install ats-scanner
```

### homebrew-core (optional, higher visibility)
Submit a PR to [Homebrew/homebrew-core](https://github.com/Homebrew/homebrew-core) once the project has at least 75 stars and a stable release. Run `brew audit --new ats-scanner` locally first to validate the formula meets their requirements.

---

## Option 5 — Scoop (Windows)

Similar to Homebrew but for Windows. No review required for a personal bucket.

1. Create a GitHub repository named `scoop-ats-scanner`
2. Add `bucket/ats-scanner.json`:

```json
{
  "version": "1.0.0",
  "description": "Local, privacy-first ATS resume scanner",
  "homepage": "https://github.com/your-username/ASTScannerButWithPrivacy",
  "license": "MIT",
  "architecture": {
    "64bit": {
      "url": "https://github.com/your-username/ASTScannerButWithPrivacy/releases/download/v1.0.0/ats-scanner-win-x64.zip",
      "hash": "<SHA256>"
    }
  },
  "bin": "ats-scanner.exe",
  "checkver": {
    "github": "https://github.com/your-username/ASTScannerButWithPrivacy"
  },
  "autoupdate": {
    "architecture": {
      "64bit": {
        "url": "https://github.com/your-username/ASTScannerButWithPrivacy/releases/download/v$version/ats-scanner-win-x64.zip"
      }
    }
  }
}
```

### User installation
```powershell
scoop bucket add ats-scanner https://github.com/your-username/scoop-ats-scanner
scoop install ats-scanner
```

---

## Recommended rollout order

| Phase | Action | Effort | Audience |
|---|---|---|---|
| 1 | Set up GitHub Actions release workflow (Option 1) | Low | Technical users comfortable with downloading zips |
| 2 | Publish as dotnet global tool on NuGet.org (Option 2) | Low | .NET developers |
| 3 | Submit to WinGet (Option 3) | Medium | Windows users |
| 4 | Create personal Homebrew tap (Option 4) | Medium | macOS / Linux users |
| 5 | Submit to homebrew-core / Scoop main | High | Broad general audience |

---

## Versioning

Use [Semantic Versioning](https://semver.org): `MAJOR.MINOR.PATCH`

- Bump **PATCH** for bug fixes and updated ATS profile rules
- Bump **MINOR** for new platforms or new CLI features
- Bump **MAJOR** for breaking changes to output format or options

Tag releases as `v1.0.0`, `v1.1.0`, etc. The GitHub Actions workflow triggers on any `v*` tag.

To release:
```bash
git tag v1.0.0
git push origin v1.0.0
```
