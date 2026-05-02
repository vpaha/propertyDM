using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

public sealed class DamageEntrySection
{
    public int Id { get; set; }
    // FK → damage_entries.id
    public int DamageEntryId { get; set; }
    // FK → damage_section_types.id
    public int DamageSectionId { get; set; }

    public string? Entry { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public DamageSectionType? DamageSectionType { get; set; }
}

public sealed class DamageSectionType
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public bool IsEmergency { get; set; }
}

public class DamageEntry : IValidatableObject
{
    public int Id { get; set; }
    public int StatusId { get; set; }

    [Required(ErrorMessage = "What's the property address? (Street, city, state, or ZIP code)")]
    public string? AddressEntry { get; set; }

    public string? Street { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }

    public string? Placename { get; set; }
    public string? Region { get; set; }

    public string? GoogleId { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public string? InsuranceEntry { get; set; }
    public string? InsuranceCarrier { get; set; }
    public string? PolicyNumber { get; set; }
    public string? ClaimNumber { get; set; }

    [Required(ErrorMessage = "What's the best way to contact you?")]
    public string? ContactEntry { get; set; }

    public string? FullName { get; set; }
    public string? Phone { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    [DateGreaterThan(2000, 1, 1, ErrorMessage = "Date must be after 01/01/2000")]
    public DateTimeOffset DateOfLoss { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public int? UserId { get; set; }
    [JsonIgnore]
    public AppUser? User { get; set; }

    public int? VendorId { get; set; }
    [JsonIgnore]
    public VendorModel? Vendor { get; set; }

    public List<DamageEntrySection> Sections { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Sections.Count == 0 || Sections.All(x => string.IsNullOrWhiteSpace(x.Entry)))
        {
            yield return new ValidationResult(
                "Select a damage type to describe the damage.",
                [nameof(Sections)]);
        }
    }

    public string? BuildCombinedDescription()
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(AddressEntry)) parts.Add(AddressEntry);
        if (!string.IsNullOrWhiteSpace(ContactEntry)) parts.Add(ContactEntry);
        if (!string.IsNullOrWhiteSpace(InsuranceEntry)) parts.Add(InsuranceEntry);

        if (Sections.Count == 0 || Sections.All(x => string.IsNullOrWhiteSpace(x.Entry)))
        {
            parts.AddRange(
                Sections.Where(d => !string.IsNullOrWhiteSpace(d.Entry))
                    .Select(d => string.Join(".", new[] { d.Entry, d.DamageSectionType?.Name }
                    .Where(v => !string.IsNullOrWhiteSpace(v)))));
        }
        if (parts.Any() == true) return string.Join(". ", parts);
        return null;
    }
}

public sealed class DateGreaterThanAttribute : ValidationAttribute
{
    private readonly DateTimeOffset _minDate;

    public DateGreaterThanAttribute(int year, int month, int day)
    {
        _minDate = new DateTimeOffset(year, month, day, 0, 0, 0, TimeSpan.Zero);
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        if (value is DateTimeOffset date && date <= _minDate)
        {
            return new ValidationResult(
                ErrorMessage ?? $"Date must be greater than {_minDate:MM/dd/yyyy}.");
        }

        return ValidationResult.Success;
    }
}