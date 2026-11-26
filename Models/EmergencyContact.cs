using System.Text.Json.Serialization;
using SafeAlert.Helpers;

namespace SafeAlert.Models;

public class EmergencyContact
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Name { get; set; } = string.Empty;
    public string Relation { get; set; } = "Familiar";
    public string Priority { get; set; } = "Secundaria";
    public string Phone { get; set; } = string.Empty;
    public string ActionMode { get; set; } = "WhatsApp + SMS con ubicación";
    public string FooterNote { get; set; } = "Notificación en altavoz";
    public string AvatarColor { get; set; } = "#1F3864";
    public bool Verified { get; set; }
    public int Order { get; set; }

    [JsonIgnore]
    public string Initials
    {
        get
        {
            var parts = Name.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
            if (parts.Count > 0 && (parts[0].Equals("Dra.", StringComparison.OrdinalIgnoreCase) ||
                                    parts[0].Equals("Dr.", StringComparison.OrdinalIgnoreCase)))
            {
                parts.RemoveAt(0);
            }

            if (parts.Count == 0)
                return "SA";

            if (parts.Count == 1)
                return parts[0].Length > 1 ? parts[0][..2].ToUpperInvariant() : parts[0].ToUpperInvariant();

            return string.Concat(parts[0][0], parts[1][0]).ToUpperInvariant();
        }
    }

    [JsonIgnore]
    public string RelationLine => Priority is "Primaria" or "Secundaria"
        ? $"{Relation} • Prioridad {Priority}"
        : $"{Relation} • {Priority}";

    [JsonIgnore]
    public string ActionBackground => ActionMode switch
    {
        var mode when mode.Contains("Llamada", StringComparison.OrdinalIgnoreCase) => "#1F3864",
        var mode when mode.Contains("SMS", StringComparison.OrdinalIgnoreCase) => "#DBEAFE",
        var mode when mode.Contains("Verificado", StringComparison.OrdinalIgnoreCase) => "#DCFCE7",
        _ => "#F3F4F6"
    };

    [JsonIgnore]
    public string ActionTextColor => ActionMode switch
    {
        var mode when mode.Contains("Llamada", StringComparison.OrdinalIgnoreCase) => "#FFFFFF",
        var mode when mode.Contains("SMS", StringComparison.OrdinalIgnoreCase) => "#1D4ED8",
        var mode when mode.Contains("Verificado", StringComparison.OrdinalIgnoreCase) => "#15803D",
        _ => "#4B5563"
    };

    [JsonIgnore]
    public string FooterIcon => FooterNote switch
    {
        var note when note.Contains("WhatsApp", StringComparison.OrdinalIgnoreCase) => Icons.Chat,
        var note when note.Contains("SMS", StringComparison.OrdinalIgnoreCase) => Icons.Chat,
        var note when note.Contains("Notificación", StringComparison.OrdinalIgnoreCase) => Icons.NotificationsActive,
        var note when note.Contains("Seguimiento", StringComparison.OrdinalIgnoreCase) => Icons.LocationOn,
        var note when note.Contains("Control", StringComparison.OrdinalIgnoreCase) => Icons.Shield,
        _ => Icons.Info
    };

    [JsonIgnore]
    public bool IsPrimary => Priority.Equals("Primaria", StringComparison.OrdinalIgnoreCase);
}
