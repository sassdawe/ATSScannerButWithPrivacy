namespace AtsScanner.Core.Models;

public sealed record ContactInfo(
    string? Name,
    string? Email,
    string? Phone,
    string? Location,
    string? LinkedIn,
    string? GitHub,
    string? Website
);
