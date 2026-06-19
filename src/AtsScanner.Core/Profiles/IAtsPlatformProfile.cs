using AtsScanner.Core.Models;

namespace AtsScanner.Core.Profiles;

public interface IAtsPlatformProfile
{
    AtsPlatform Platform { get; }
    string DisplayName { get; }

    /// <summary>
    /// Analyses a parsed resume and returns a scored result with issues and recommendations.
    /// Score is 0-100.
    /// </summary>
    ScanResult Analyze(ParsedResume resume);
}
