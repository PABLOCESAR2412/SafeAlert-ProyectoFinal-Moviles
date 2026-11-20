namespace SafeAlert.Models;

public class AppData
{
    public int Version { get; set; } = 1;
    public List<UserAccount> Users { get; set; } = new();
    public string? LastEmail { get; set; }
}
