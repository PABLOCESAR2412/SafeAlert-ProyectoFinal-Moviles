using System.Text.Json;
using System.Text.Json.Serialization;
using SafeAlert.Models;
using SQLite;

namespace SafeAlert.Services;

internal static class LocalDatabase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static AppData LoadOrImport(string path, string legacyPath)
    {
        using var db = Open(path);
        var metadata = db.Find<AppState>(1);
        if (metadata is null)
        {
            if (!File.Exists(legacyPath))
                return new AppData();

            var legacy = Read<AppData>(File.ReadAllText(legacyPath));
            Save(db, legacy);
            return legacy;
        }

        var data = new AppData { Version = metadata.Version, LastEmail = metadata.LastEmail };
        foreach (var row in db.Table<AccountRow>().OrderBy(row => row.Id))
        {
            data.Users.Add(new UserAccount
            {
                Email = row.Email,
                PasswordHash = row.PasswordHash,
                Profile = Read<UserProfile>(row.Profile),
                Settings = Read<AppSettings>(row.Settings),
                Contacts = db.Table<ContactRow>().Where(item => item.AccountId == row.Id)
                    .OrderBy(item => item.Position).ToList()
                    .Select(item => Read<EmergencyContact>(item.Payload)).ToList(),
                Alerts = db.Table<AlertRow>().Where(item => item.AccountId == row.Id)
                    .OrderBy(item => item.Position).ToList()
                    .Select(item => Read<AlertRecord>(item.Payload)).ToList()
            });
        }
        return data;
    }

    public static void Save(string path, AppData data)
    {
        using var db = Open(path);
        Save(db, data);
    }

    private static void Save(SQLiteConnection db, AppData data)
    {
        db.RunInTransaction(() =>
        {
            db.DeleteAll<ContactRow>();
            db.DeleteAll<AlertRow>();
            db.DeleteAll<AccountRow>();

            for (var i = 0; i < data.Users.Count; i++)
            {
                var user = data.Users[i];
                var id = i + 1;
                db.Insert(new AccountRow
                {
                    Id = id,
                    Email = user.Email,
                    PasswordHash = user.PasswordHash,
                    Profile = Write(user.Profile),
                    Settings = Write(user.Settings)
                });
                db.InsertAll(user.Contacts.Select((contact, position) =>
                    new ContactRow { AccountId = id, Position = position, Payload = Write(contact) }),
                    runInTransaction: false);
                db.InsertAll(user.Alerts.Select((alert, position) =>
                    new AlertRow { AccountId = id, Position = position, Payload = Write(alert) }),
                    runInTransaction: false);
            }

            db.InsertOrReplace(new AppState { Id = 1, Version = data.Version, LastEmail = data.LastEmail });
        });
    }

    private static SQLiteConnection Open(string path)
    {
        var db = new SQLiteConnection(path);
        try
        {
            db.CreateTable<AppState>();
            db.CreateTable<AccountRow>();
            db.CreateTable<ContactRow>();
            db.CreateTable<AlertRow>();
            return db;
        }
        catch
        {
            db.Dispose();
            throw;
        }
    }

    private static string Write<T>(T value) => JsonSerializer.Serialize(value, JsonOptions);

    private static T Read<T>(string payload) where T : class =>
        JsonSerializer.Deserialize<T>(payload, JsonOptions)
        ?? throw new InvalidDataException("El registro guardado no contiene datos válidos.");

    [Table("app_state")]
    private sealed class AppState
    {
        [PrimaryKey]
        public int Id { get; set; }
        public int Version { get; set; }
        public string? LastEmail { get; set; }
    }

    [Table("accounts")]
    private sealed class AccountRow
    {
        [PrimaryKey]
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Profile { get; set; } = string.Empty;
        public string Settings { get; set; } = string.Empty;
    }

    [Table("contacts")]
    private sealed class ContactRow
    {
        [Indexed]
        public int AccountId { get; set; }
        public int Position { get; set; }
        public string Payload { get; set; } = string.Empty;
    }

    [Table("alerts")]
    private sealed class AlertRow
    {
        [Indexed]
        public int AccountId { get; set; }
        public int Position { get; set; }
        public string Payload { get; set; } = string.Empty;
    }
}
