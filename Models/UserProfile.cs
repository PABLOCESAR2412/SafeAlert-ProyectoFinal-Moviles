using System.Text.Json.Serialization;

namespace SafeAlert.Models;

public class UserProfile
{
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string BloodType { get; set; } = "O+";
    public string Allergies { get; set; } = "Ninguna";
    public string DoctorName { get; set; } = string.Empty;
    public string DoctorPhone { get; set; } = string.Empty;
    public string MemberId { get; set; } = "SA-0000";
    public bool IsPro { get; set; } = true;

    [JsonIgnore]
    public string Initials
    {
        get
        {
            var parts = FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return "SA";

            if (parts.Length == 1)
                return parts[0].Length > 1 ? parts[0][..2].ToUpperInvariant() : parts[0].ToUpperInvariant();

            return string.Concat(parts[0][0], parts[1][0]).ToUpperInvariant();
        }
    }

    [JsonIgnore]
    public string FirstName
    {
        get
        {
            var parts = FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length == 0 ? "Usuario" : parts[0];
        }
    }

    [JsonIgnore]
    public string BloodTypeDisplay => string.IsNullOrWhiteSpace(BloodType) ? "O+" : BloodType;

    [JsonIgnore]
    public string AllergiesDisplay => string.IsNullOrWhiteSpace(Allergies) ? "Ninguna" : Allergies;
}
