using System.Text.Json;
using System.Text.Json.Serialization;
using SafeAlert.Helpers;
using SafeAlert.Models;

namespace SafeAlert.Services;

public static class DataStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private static AppData? _data;

    public static string StoragePath { get; } = BuildPath();

    public static AppData Data
    {
        get
        {
            EnsureLoaded();
            return _data!;
        }
    }

    private static string BuildPath()
    {
        try
        {
            var dir = Path.Combine(FileSystem.AppDataDirectory, "SafeAlert");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "safealert.json");
        }
        catch
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SafeAlert");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "safealert.json");
        }
    }

    private static void EnsureLoaded()
    {
        if (_data != null)
            return;

        try
        {
            if (File.Exists(StoragePath))
            {
                var json = File.ReadAllText(StoragePath);
                _data = JsonSerializer.Deserialize<AppData>(json, JsonOptions);
            }
        }
        catch
        {
            try
            {
                var backup = StoragePath + ".bak";
                if (File.Exists(StoragePath))
                    File.Copy(StoragePath, backup, true);
            }
            catch
            {
                // La copia de seguridad es opcional.
            }
        }

        _data ??= new AppData();

        if (_data.Users.Count == 0)
        {
            _data.Users.Add(CreateDemoAccount());
            _data.LastEmail = _data.Users[0].Email;
            Save();
        }
    }

    public static void Save()
    {
        if (_data == null)
            return;

        try
        {
            var directory = Path.GetDirectoryName(StoragePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            var temp = StoragePath + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(_data, JsonOptions));
            File.Move(temp, StoragePath, true);
        }
        catch
        {
            // En una demo no interrumpimos la experiencia si falla el guardado.
        }
    }

    private static UserAccount CreateDemoAccount()
    {
        var today = DateTime.Today;
        var medicalTime = today.AddHours(14).AddMinutes(32);
        if (medicalTime > DateTime.Now)
            medicalTime = DateTime.Now.AddMinutes(-90);

        var account = new UserAccount
        {
            Email = "elena.rosales@safealert.app",
            PasswordHash = PasswordHasher.Hash("123456"),
            Profile = new UserProfile
            {
                FullName = "Elena Rosales Martínez",
                Phone = "+52 55 4321 8765",
                Email = "elena.rosales@safealert.app",
                BloodType = "O+",
                Allergies = "Penicilina",
                DoctorName = "Dr. Fernando Torres",
                DoctorPhone = "+52 55 1122 3344",
                MemberId = "SA-8492",
                IsPro = true
            },
            Settings = new AppSettings()
        };

        account.Contacts.AddRange(new[]
        {
            new EmergencyContact
            {
                Name = "Dra. Carmen Morales",
                Relation = "Mamá",
                Priority = "Primaria",
                Phone = "+52 55 1234 5678",
                ActionMode = "Llamada Inmediata",
                FooterNote = "Notificación en altavoz",
                AvatarColor = "#1F3864",
                Verified = true,
                Order = 1
            },
            new EmergencyContact
            {
                Name = "Alejandro Ruiz",
                Relation = "Hermano",
                Priority = "Secundaria",
                Phone = "+52 55 9876 5432",
                ActionMode = "SMS + GPS en vivo",
                FooterNote = "Seguimiento 60 min",
                AvatarColor = "#0074D5",
                Verified = true,
                Order = 2
            },
            new EmergencyContact
            {
                Name = "Seguridad Residencial",
                Relation = "Cuerpo de Guardias",
                Priority = "Caseta",
                Phone = "+52 55 4433 2211",
                ActionMode = "Verificado 24/7",
                FooterNote = "Control de acceso y rondín",
                AvatarColor = "#0F766E",
                Verified = true,
                Order = 3
            }
        });

        account.Alerts.AddRange(new[]
        {
            new AlertRecord
            {
                Title = "Alerta Médica SOS",
                Type = EmergencyType.Medical,
                Status = AlertStatus.Resolved,
                Message = "Emergencia médica. Dolor en el pecho y dificultad para respirar.",
                Address = "Calle Liverpool 88, Col. Juárez",
                Latitude = 19.4270,
                Longitude = -99.1676,
                CreatedAt = medicalTime,
                NotifiedContacts = 3,
                ResponseMinutes = 8,
                AudioRecorded = true,
                Notes = "Ambulancia y contactos notificados. La usuaria fue atendida."
            },
            new AlertRecord
            {
                Title = "Pánico / Alerta Silenciosa",
                Type = EmergencyType.Silent,
                Status = AlertStatus.Resolved,
                Message = "Alerta silenciosa activada. Compartan mi ubicación con mis contactos.",
                Address = "Estación Metro Insurgentes",
                Latitude = 19.4247,
                Longitude = -99.1630,
                CreatedAt = today.AddDays(-23).AddHours(22).AddMinutes(15),
                NotifiedContacts = 2,
                AudioRecorded = false,
                Notes = "Ubicación en vivo compartida por 25 min con 2 contactos. Confirmaron a salvo."
            },
            new AlertRecord
            {
                Title = "Prueba de Sistema y Sirena",
                Type = EmergencyType.Test,
                Status = AlertStatus.Resolved,
                Message = "Prueba del sistema SafeAlert. No es una emergencia real.",
                Address = "Hogar (Zona Segura)",
                Latitude = 19.4270,
                Longitude = -99.1676,
                CreatedAt = today.AddDays(-30).AddHours(10),
                NotifiedContacts = 0,
                AudioRecorded = false,
                Notes = "Latencia 0.3 s. Prueba exitosa."
            }
        });

        return account;
    }
}
