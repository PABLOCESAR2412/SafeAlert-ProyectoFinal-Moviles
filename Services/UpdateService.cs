namespace SafeAlert.Services;

public sealed record UpdateInfo(string LatestVersion, string? DownloadUrl, string Message)
{
    public bool IsUpdateAvailable => Version.TryParse(LatestVersion, out var latest)
        && Version.TryParse(AppInfo.Current.VersionString, out var current)
        && latest > current;
}

/// <summary>
/// Comprobación de actualizaciones sin tienda ni backend propio.
/// El administrador publica un archivo update.json junto al APK (GitHub Releases,
/// Drive o cualquier URL HTTPS) y la app compara esa versión con la instalada.
/// </summary>
public static class UpdateService
{
    // Pegar aquí la URL HTTPS del update.json cuando se publique una versión.
    // Ejemplo: https://raw.githubusercontent.com/PABLOCESAR2412/SafeAlert-ProyectoFinal-Moviles/main/update.json
    private const string ManifestUrl = "";

    private const string PreviewVersionKey = "safealert.update.preview";
    private const string SeenVersionKey = "safealert.update.seen";

    public static async Task<UpdateInfo?> CheckAsync()
    {
        var info = ReadPreview();

        if (info == null && !string.IsNullOrWhiteSpace(ManifestUrl))
            info = await ReadRemoteAsync(ManifestUrl);

        if (info == null || !info.IsUpdateAvailable)
            return null;

        return Preferences.Default.Get(SeenVersionKey, string.Empty) == info.LatestVersion
            ? null
            : info;
    }

    /// <summary>Vista previa local del aviso, útil para demostraciones sin publicar una APK.</summary>
    public static UpdateInfo? Preview()
    {
        var version = AppInfo.Current.VersionString;
        var next = Version.TryParse(version, out var parsed)
            ? $"{parsed.Major}.{parsed.Minor + 1}"
            : "1.1";

        return new UpdateInfo(
            next,
            null,
            "Mejoras de ubicación, contactos y avisos de emergencia.");
    }

    public static void EnablePreview()
    {
        var preview = Preview();
        if (preview != null)
            Preferences.Default.Set(PreviewVersionKey, preview.LatestVersion);
    }

    public static void MarkSeen(string version) =>
        Preferences.Default.Set(SeenVersionKey, version);

    private static UpdateInfo? ReadPreview()
    {
        try
        {
            var version = Preferences.Default.Get(PreviewVersionKey, string.Empty);
            if (string.IsNullOrWhiteSpace(version))
                return null;

            return new UpdateInfo(
                version,
                null,
                "Mejoras de ubicación, contactos y avisos de emergencia.");
        }
        catch
        {
            return null;
        }
    }

    private static async Task<UpdateInfo?> ReadRemoteAsync(string url)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var json = await client.GetStringAsync(url);
            var manifest = System.Text.Json.JsonSerializer.Deserialize<UpdateManifest>(json);

            if (manifest == null || string.IsNullOrWhiteSpace(manifest.Version))
                return null;

            return new UpdateInfo(
                manifest.Version.Trim(),
                manifest.Url,
                string.IsNullOrWhiteSpace(manifest.Notes)
                    ? "Hay una versión nueva disponible."
                    : manifest.Notes.Trim());
        }
        catch
        {
            return null;
        }
    }

    private sealed class UpdateManifest
    {
        public string Version { get; set; } = string.Empty;
        public string? Url { get; set; }
        public string? Notes { get; set; }
    }
}
