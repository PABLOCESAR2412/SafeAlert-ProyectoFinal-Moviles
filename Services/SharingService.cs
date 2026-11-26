using SafeAlert.Models;

namespace SafeAlert.Services;

public sealed record ShareResult(
    int WhatsAppCount,
    IReadOnlyList<EmergencyContact> SmsPending,
    string PrimaryName,
    string? Error)
{
    public bool WhatsAppSent => WhatsAppCount > 0;

    public bool HasPendingSms => SmsPending.Count > 0;
}

/// <summary>
/// Envío sin costo ni API de WhatsApp: abre el chat del contacto principal con un
/// enlace de Google Maps y deja preparado un SMS para el resto de contactos.
/// El envío final lo confirma el usuario en WhatsApp o en la app de mensajes,
/// que es la única forma permitida sin usar la API de negocio de WhatsApp.
/// </summary>
public static class SharingService
{
    public static string BuildLocationLink(double latitude, double longitude) =>
        $"https://www.google.com/maps?q={latitude:0.000000},{longitude:0.000000}";

    public static string BuildMessage(string message, double latitude, double longitude) =>
        $"{message.Trim()}\n\nMi ubicación: {BuildLocationLink(latitude, longitude)}";

    public static async Task<ShareResult> DispatchAsync(
        UserAccount account,
        string message,
        double latitude,
        double longitude)
    {
        var contacts = account.Contacts
            .Where(c => !string.IsNullOrWhiteSpace(c.Phone))
            .OrderBy(c => c.Order)
            .ToList();

        if (contacts.Count == 0)
            return new ShareResult(0, [], string.Empty, "No tienes contactos de emergencia registrados.");

        var primary = contacts.FirstOrDefault(c => c.IsPrimary) ?? contacts[0];
        var others = contacts.Where(c => c.Id != primary.Id).ToList();
        var primaryText = BuildMessage(message, latitude, longitude);
        var sent = 0;

        try
        {
            var digits = NormalizePhone(primary.Phone);

            if (!string.IsNullOrWhiteSpace(digits))
            {
                var appUri = new Uri($"whatsapp://send?phone={digits}&text={Uri.EscapeDataString(primaryText)}");

                if (await Launcher.Default.CanOpenAsync(appUri))
                {
                    await Launcher.Default.OpenAsync(appUri);
                    sent = 1;
                }
                else
                {
                    var webUri = new Uri($"https://wa.me/{digits}?text={Uri.EscapeDataString(primaryText)}");
                    if (await Launcher.Default.CanOpenAsync(webUri))
                    {
                        await Launcher.Default.OpenAsync(webUri);
                        sent = 1;
                    }
                }
            }
        }
        catch
        {
            sent = 0;
        }

        // Si WhatsApp no está instalado, el contacto principal pasa a la lista de SMS.
        var pending = sent == 1
            ? others
            : new[] { primary }.Concat(others).ToList();

        return new ShareResult(sent, pending, primary.Name, sent == 1 ? null : "WhatsApp no está disponible en este dispositivo.");
    }

    public static async Task<bool> ComposeSmsAsync(IReadOnlyList<EmergencyContact> contacts, string message)
    {
        var numbers = contacts
            .Select(c => c.Phone.Trim())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct()
            .ToArray();

        if (numbers.Length == 0)
            return false;

        try
        {
            await Sms.Default.ComposeAsync(new SmsMessage(message.Trim(), numbers));
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string NormalizePhone(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());

        // Ecuador: 09XXXXXXXX -> 5939XXXXXXXX
        if (digits.Length == 10 && digits.StartsWith('0'))
            return $"593{digits[1..]}";

        return digits;
    }
}
