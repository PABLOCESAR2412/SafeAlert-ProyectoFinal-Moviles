using System.Diagnostics;
using SafeAlert.Models;
using SafeAlert.Helpers;

namespace SafeAlert.Services;

public static class SharingService
{
    public static string BuildLocationLink(double latitude, double longitude) =>
        FormattableString.Invariant($"https://www.google.com/maps/search/?api=1&query={latitude:0.000000}%2C{longitude:0.000000}");

    public static string BuildMessage(string message, double latitude, double longitude) =>
        $"{message.Trim()}\n\nMi ubicación: {BuildLocationLink(latitude, longitude)}";

    public static EmergencyContact? FindSmsContact(UserAccount account) =>
        account.Contacts
            .Where(contact => Validation.IsValidPhone(contact.Phone))
            .OrderByDescending(contact => contact.IsPrimary)
            .ThenBy(contact => contact.Order)
            .FirstOrDefault();

    public static async Task<bool> ComposeSmsAsync(EmergencyContact contact, string message)
    {
        try
        {
            var phone = Validation.NormalizePhone(contact.Phone);
            if (phone is null || !Sms.Default.IsComposeSupported)
            {
                Trace.TraceWarning("SafeAlert SMS: compositor no disponible.");
                return false;
            }

            await Sms.Default.ComposeAsync(new SmsMessage(message, phone));
            return true;
        }
        catch (Exception ex)
        {
            Trace.TraceWarning("SafeAlert SMS: no se abrió el compositor ({0}).", ex.GetType().Name);
            return false;
        }
    }
}
