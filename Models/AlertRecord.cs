using System.Globalization;
using System.Text.Json.Serialization;
using SafeAlert.Helpers;

namespace SafeAlert.Models;

public class AlertRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
    public string Title { get; set; } = string.Empty;
    public EmergencyType Type { get; set; } = EmergencyType.Panic;
    public AlertStatus Status { get; set; } = AlertStatus.InProgress;
    public string Message { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool? HasVerifiedLocation { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int NotifiedContacts { get; set; }
    public int? ResponseMinutes { get; set; }
    public bool AudioRecorded { get; set; }
    public string Notes { get; set; } = string.Empty;

    [JsonIgnore]
    public string TypeTitle => string.IsNullOrWhiteSpace(Title)
        ? EmergencyCatalog.HistoryTitle(Type)
        : Title;

    [JsonIgnore]
    public string Icon => EmergencyCatalog.Icon(Type);

    [JsonIgnore]
    public string IconColor => Type switch
    {
        EmergencyType.Medical => "#B91C1C",
        EmergencyType.Harassment => "#B45309",
        EmergencyType.Robbery or EmergencyType.Police => "#1D4ED8",
        EmergencyType.Test => "#1D4ED8",
        EmergencyType.WalkSafe => "#047857",
        _ => "#374151"
    };

    [JsonIgnore]
    public string IconBackground => Type switch
    {
        EmergencyType.Medical => "#FEE2E2",
        EmergencyType.Harassment => "#FEF3C7",
        EmergencyType.Robbery or EmergencyType.Police => "#DBEAFE",
        EmergencyType.Test => "#DBEAFE",
        EmergencyType.WalkSafe => "#D1FAE5",
        _ => "#F3F4F6"
    };

    [JsonIgnore]
    public string StatusText => Status switch
    {
        AlertStatus.Resolved when Type == EmergencyType.Test => "Prueba exitosa",
        AlertStatus.Resolved => "Finalizada",
        AlertStatus.FalseAlarm => "Falsa alarma",
        _ => "En curso"
    };

    [JsonIgnore]
    public string StatusTextColor => Status switch
    {
        AlertStatus.Resolved => "#1E40AF",
        AlertStatus.FalseAlarm => "#4B5563",
        _ => "#B45309"
    };

    [JsonIgnore]
    public string StatusBackgroundColor => Status switch
    {
        AlertStatus.Resolved => "#DBEAFE",
        AlertStatus.FalseAlarm => "#F3F4F6",
        _ => "#FEF3C7"
    };

    [JsonIgnore]
    public string DateText
    {
        get
        {
            var culture = CultureInfo.GetCultureInfo("es-ES");
            if (CreatedAt.Date == DateTime.Today)
                return $"Hoy, {CreatedAt:HH:mm} hrs";

            if (CreatedAt.Date == DateTime.Today.AddDays(-1))
                return $"Ayer, {CreatedAt:HH:mm} hrs";

            return CreatedAt.ToString("dd 'de' MMMM, HH:mm 'hrs'", culture);
        }
    }

    [JsonIgnore]
    public string ContactsLine => NotifiedContacts switch
    {
        0 => "Entrega sin confirmar",
        1 => "1 contacto incluido en la alerta",
        _ => $"{NotifiedContacts} contactos incluidos en la alerta"
    };

    [JsonIgnore]
    public string ResponseLine
    {
        get
        {
            if (Type == EmergencyType.Test)
                return "Latencia de prueba: 0.3 s";

            return ResponseMinutes.HasValue
                ? $"Tiempo de respuesta: {ResponseMinutes} min"
                : "Sin respuesta registrada";
        }
    }

    [JsonIgnore]
    public string ResponseIcon => ResponseMinutes.HasValue ? Icons.Ambulance : Icons.Schedule;

    [JsonIgnore]
    public bool HasResponse => ResponseMinutes.HasValue || Type == EmergencyType.Test;

    [JsonIgnore]
    public bool ShowMap => HasVerifiedLocation != false && Type is EmergencyType.Medical or EmergencyType.Harassment
        or EmergencyType.Robbery or EmergencyType.Police or EmergencyType.Panic
        or EmergencyType.Silent or EmergencyType.WalkSafe;

    [JsonIgnore]
    public bool IsResolved => Status == AlertStatus.Resolved;

    [JsonIgnore]
    public bool IsInProgress => Status == AlertStatus.InProgress;

    [JsonIgnore]
    public string ActionText => "Ver detalle";

    [JsonIgnore]
    public string ActionBackgroundColor => Type == EmergencyType.Medical ? "#0074D5" : "#F3F4F6";

    [JsonIgnore]
    public string ActionTextColor => Type == EmergencyType.Medical ? "#FFFFFF" : "#1F3864";

    [JsonIgnore]
    public string CoordinatesText => HasVerifiedLocation == false
        ? "Ubicación no disponible"
        : $"Lat: {Latitude:0.0000}, Lon: {Longitude:0.0000}";
}
