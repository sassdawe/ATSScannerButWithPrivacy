using AtsScanner.Core.Models;

namespace AtsScanner.Core.Profiles;

/// <summary>Provides all registered ATS platform profiles.</summary>
public static class ProfileRegistry
{
    private static readonly IReadOnlyList<IAtsPlatformProfile> All =
    [
        new WorkdayProfile(),
        new GreenhouseProfile(),
        new TaleoProfile(),
        new LeverProfile(),
        new SuccessFactorsProfile(),
        new UmantisProfile()
    ];

    public static IReadOnlyList<IAtsPlatformProfile> GetAll() => All;

    public static IAtsPlatformProfile Get(AtsPlatform platform) =>
        All.First(p => p.Platform == platform);

    public static bool TryParse(string name, out AtsPlatform platform)
    {
        platform = name.ToLowerInvariant() switch
        {
            "workday" => AtsPlatform.Workday,
            "greenhouse" => AtsPlatform.Greenhouse,
            "taleo" => AtsPlatform.Taleo,
            "lever" => AtsPlatform.Lever,
            "successfactors" or "sap" => AtsPlatform.SuccessFactors,
            "umantis" or "haufe-umantis" or "haufe" => AtsPlatform.Umantis,
            _ => (AtsPlatform)(-1)
        };
        return (int)platform >= 0;
    }
}
