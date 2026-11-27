namespace SafeAlert.Services;

public static class PermissionService
{
    public static bool LocationEnabled
    {
        get => Preferences.Default.Get("safealert.location.enabled", false);
        set => Preferences.Default.Set("safealert.location.enabled", value);
    }

    public static bool ContactsEnabled
    {
        get => Preferences.Default.Get("safealert.contacts.enabled", false);
        set => Preferences.Default.Set("safealert.contacts.enabled", value);
    }

    public static bool MovementEnabled
    {
        get => Preferences.Default.Get("safealert.movement.enabled", false);
        set => Preferences.Default.Set("safealert.movement.enabled", value);
    }

    public static async Task<bool> EnsureLocationAsync(bool request = false)
    {
        if (!request && !LocationEnabled)
            return false;

        try
        {
#if ANDROID
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (request && status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            return status == PermissionStatus.Granted;
#else
            return true;
#endif
        }
        catch
        {
            return false;
        }
    }

    public static async Task<bool> EnsureContactsAsync(bool request = false)
    {
        if (!request && !ContactsEnabled)
            return false;

        try
        {
#if ANDROID
            var status = await Permissions.CheckStatusAsync<Permissions.ContactsRead>();
            if (request && status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<Permissions.ContactsRead>();
            return status == PermissionStatus.Granted;
#else
            return false;
#endif
        }
        catch
        {
            return false;
        }
    }

    public static async Task<string> SensorStatusAsync()
    {
        try
        {
            var supported = Microsoft.Maui.Devices.Sensors.Accelerometer.Default.IsSupported;
            return supported
                ? "Acelerómetro disponible"
                : "Acelerómetro no disponible en este dispositivo";
        }
        catch
        {
            return "Acelerómetro no disponible";
        }
    }
}
