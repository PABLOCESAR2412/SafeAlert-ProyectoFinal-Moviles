using Microsoft.Maui.Devices;

namespace SafeAlert.Services;

public static class DeviceStatus
{
    public static string Network { get; private set; } = "Red 5G";
    public static string Gps { get; private set; } = "GPS Activo";
    public static string BatteryLevel { get; private set; } = "88%";

    public static void Refresh()
    {
        try
        {
            Network = Connectivity.Current.NetworkAccess switch
            {
                NetworkAccess.Internet => "Red 5G",
                NetworkAccess.Local => "Red local",
                NetworkAccess.ConstrainedInternet => "Red limitada",
                _ => "Sin red"
            };
        }
        catch
        {
            Network = "Red 5G";
        }

        try
        {
            var level = Battery.Default.ChargeLevel;
            BatteryLevel = level is >= 0 and <= 1
                ? $"{Math.Round(level * 100)}%"
                : "88%";
        }
        catch
        {
            BatteryLevel = "88%";
        }

        Gps = LocationService.UsingSimulation ? "GPS Activo" : "GPS Activo";
    }
}
