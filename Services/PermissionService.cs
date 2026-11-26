namespace SafeAlert.Services;

public static class PermissionService
{
    public static async Task<bool> EnsureLocationAsync()
    {
        try
        {
#if ANDROID
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
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

    public static async Task<bool> EnsureContactsAsync()
    {
        try
        {
#if ANDROID
            var status = await Permissions.CheckStatusAsync<Permissions.ContactsRead>();
            if (status != PermissionStatus.Granted)
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
