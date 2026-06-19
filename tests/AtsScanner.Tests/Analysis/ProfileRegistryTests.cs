using AtsScanner.Core.Models;
using AtsScanner.Core.Profiles;
using FluentAssertions;

namespace AtsScanner.Tests.Analysis;

public class ProfileRegistryTests
{
    [Theory]
    [InlineData("workday", AtsPlatform.Workday)]
    [InlineData("greenhouse", AtsPlatform.Greenhouse)]
    [InlineData("taleo", AtsPlatform.Taleo)]
    [InlineData("lever", AtsPlatform.Lever)]
    [InlineData("successfactors", AtsPlatform.SuccessFactors)]
    [InlineData("sap", AtsPlatform.SuccessFactors)]
    public void TryParse_KnownName_Succeeds(string name, AtsPlatform expected)
    {
        var result = ProfileRegistry.TryParse(name, out var platform);

        result.Should().BeTrue();
        platform.Should().Be(expected);
    }

    [Fact]
    public void TryParse_UnknownName_ReturnsFalse()
    {
        var result = ProfileRegistry.TryParse("unknown-ats", out _);
        result.Should().BeFalse();
    }

    [Fact]
    public void GetAll_ReturnsFiveProfiles()
    {
        var profiles = ProfileRegistry.GetAll();
        profiles.Should().HaveCount(5);
    }

    [Theory]
    [InlineData(AtsPlatform.Workday)]
    [InlineData(AtsPlatform.Greenhouse)]
    [InlineData(AtsPlatform.Taleo)]
    [InlineData(AtsPlatform.Lever)]
    [InlineData(AtsPlatform.SuccessFactors)]
    public void Get_EachPlatform_ReturnsCorrectProfile(AtsPlatform platform)
    {
        var profile = ProfileRegistry.Get(platform);
        profile.Platform.Should().Be(platform);
    }

    [Fact]
    public void AllProfiles_ScoreRange_IsZeroToHundred()
    {
        var worstCaseResume = new ParsedResume(
            FileName: "bad.pdf",
            FileFormat: "pdf",
            RawText: "",
            Contact: new ContactInfo(null, null, null, null, null, null, null),
            Sections: [],
            Format: ResumeFormatFlags.HasMultipleColumns | ResumeFormatFlags.HasTables |
                    ResumeFormatFlags.HasImages | ResumeFormatFlags.HasHeaders | ResumeFormatFlags.HasFooters);

        foreach (var profile in ProfileRegistry.GetAll())
        {
            var result = profile.Analyze(worstCaseResume);
            result.Score.Should().BeInRange(0, 100,
                because: $"{profile.DisplayName} must keep score within 0-100");
        }
    }
}
