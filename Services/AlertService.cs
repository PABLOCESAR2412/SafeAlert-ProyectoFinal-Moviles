using SafeAlert.Helpers;
using SafeAlert.Models;

namespace SafeAlert.Services;

public static class AlertService
{
    public static AlertRecord Send(
        EmergencyType type,
        string? message,
        string address,
        double latitude,
        double longitude,
        AlertStatus status = AlertStatus.InProgress,
        int? notifiedContacts = null,
        int? responseMinutes = null,
        string? notes = null,
        bool? hasVerifiedLocation = null)
    {
        var account = AppSession.Account;
        if (account == null)
            throw new InvalidOperationException("No hay una sesión activa.");

        var alert = new AlertRecord
        {
            Title = EmergencyCatalog.HistoryTitle(type),
            Type = type,
            Status = status,
            Message = string.IsNullOrWhiteSpace(message)
                ? EmergencyCatalog.DefaultMessage(type)
                : message.Trim(),
            Address = address,
            Latitude = latitude,
            Longitude = longitude,
            HasVerifiedLocation = hasVerifiedLocation,
            CreatedAt = DateTime.Now,
            NotifiedContacts = notifiedContacts ?? 0,
            ResponseMinutes = responseMinutes,
            AudioRecorded = false,
            Notes = notes ?? string.Empty
        };

        account.Alerts.Insert(0, alert);
        DataStore.Save();
        return alert;
    }

    public static void ChangeStatus(AlertRecord alert, AlertStatus status)
    {
        alert.Status = status;

        DataStore.Save();
    }
}
