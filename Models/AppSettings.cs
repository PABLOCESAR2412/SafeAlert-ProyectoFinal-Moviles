namespace SafeAlert.Models;

public class AppSettings
{
    public bool SosOnLockScreen { get; set; } = true;
    public bool BackgroundLocation { get; set; } = true;
    public bool Siren { get; set; }
    public bool TripleTap { get; set; } = true;
    public bool ShakeDetection { get; set; }
    public bool NotifyOnSpeaker { get; set; } = true;
}
