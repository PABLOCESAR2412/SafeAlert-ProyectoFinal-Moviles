namespace SafeAlert.Models;

public class UserAccount
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserProfile Profile { get; set; } = new();
    public List<EmergencyContact> Contacts { get; set; } = new();
    public List<AlertRecord> Alerts { get; set; } = new();
    public AppSettings Settings { get; set; } = new();
}
