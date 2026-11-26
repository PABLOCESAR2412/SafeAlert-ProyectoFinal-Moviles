using Microsoft.Maui.Devices.Sensors;

namespace SafeAlert.Services;

public static class LocationService
{
    private static bool _initialized;

    public static double Latitude { get; private set; } = -0.1814;
    public static double Longitude { get; private set; } = -78.4755;
    public static string Address { get; private set; } = "Av. Amazonas N34-12 y Naciones Unidas, Quito";
    public static string Nearby { get; private set; } = "Cerca de Parque La Carolina";
    public static string Precision { get; private set; } = "±15 m";
    public static bool UsingSimulation { get; private set; } = true;

    public static async Task<bool> RefreshAsync()
    {
        try
        {
            var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(6));
            var location = await Geolocation.Default.GetLocationAsync(request);

            if (location == null)
                return false;

            Latitude = location.Latitude;
            Longitude = location.Longitude;
            Precision = location.Accuracy.HasValue
                ? $"±{Math.Round(location.Accuracy.Value)} m"
                : "±15 m";
            UsingSimulation = false;

            try
            {
                var placemarks = await Geocoding.Default.GetPlacemarksAsync(Latitude, Longitude);
                var place = placemarks?.FirstOrDefault();

                if (place != null)
                {
                    var parts = new[]
                    {
                        place.Thoroughfare,
                        place.SubThoroughfare,
                        place.Locality,
                        place.AdminArea
                    }.Where(part => !string.IsNullOrWhiteSpace(part));

                    var address = string.Join(", ", parts);
                    if (!string.IsNullOrWhiteSpace(address))
                        Address = address;

                    if (!string.IsNullOrWhiteSpace(place.Locality))
                        Nearby = $"Cerca de {place.Locality}";
                }
            }
            catch
            {
                // La dirección de demostración se conserva si el geocodificador no está disponible.
            }

            _initialized = true;
            return true;
        }
        catch
        {
            _initialized = true;
            return false;
        }
    }

    public static string CoordinatesText => $"Lat: {Latitude:0.0000}, Lon: {Longitude:0.0000}";

    public static string PrecisionText => $"Precisión GPS: {Precision}";

    public static bool HasAttempted => _initialized;
}
