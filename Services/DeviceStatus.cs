using Microsoft.Maui.Devices;

namespace SafeAlert.Services;

public static class DeviceStatus
{
    public static string Network { get; private set; } = "Con internet";
    public static string Gps { get; private set; } = "GPS Activo";
    public static string BatteryLevel { get; private set; } = "No disponible";

    public static void Refresh()
    {
        try
        {
            Network = Connectivity.Current.NetworkAccess switch
            {
                NetworkAccess.Internet => "Con internet",
                NetworkAccess.Local => "Red local",
                NetworkAccess.ConstrainedInternet => "Red limitada",
                _ => "Sin red"
            };
        }
        catch
        {
            Network = "Estado de red no disponible";
        }

        try
        {
            var level = ReadBatteryLevel();
            BatteryLevel = level is >= 0 and <= 1
                ? $"{Math.Round(level * 100)}%"
                : "No disponible";
        }
        catch
        {
            BatteryLevel = "No disponible";
        }

        Gps = !PermissionService.LocationEnabled ? "GPS desactivado en SafeAlert"
            : LocationService.UsingSimulation ? "Ubicación sin verificar" : "Posición obtenida";
    }
    private static double ReadBatteryLevel()
    {
#if ANDROID
        using var filter = new Android.Content.IntentFilter(Android.Content.Intent.ActionBatteryChanged);
        using var battery = Android.App.Application.Context.RegisterReceiver(null, filter);
        if (battery is not null)
        {
            var level = battery.GetIntExtra(Android.OS.BatteryManager.ExtraLevel, -1);
            var scale = battery.GetIntExtra(Android.OS.BatteryManager.ExtraScale, -1);
            if (scale > 0 && level >= 0 && level <= scale)
                return (double)level / scale;
        }
#endif
        return Battery.Default.ChargeLevel;
    }
}
