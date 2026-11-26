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

        // Normaliza textos guardados por versiones anteriores.
        foreach (var user in _data.Users)
        {
            foreach (var contact in user.Contacts)
            {
                if (contact.ActionMode is "SMS + GPS en vivo" or "Verificado 24/7" or "SMS + ubicación" or "Llamada Inmediata")
                    contact.ActionMode = "WhatsApp + SMS con ubicación";

                contact.FooterNote = contact.FooterNote switch
                {
                    "Notificación en altavoz" => "Recibe WhatsApp con ubicación",
                    "Seguimiento 60 min" => "Respaldo por SMS",
                    "Control de acceso y rondín" => "Contacto institucional",
                    _ => contact.FooterNote
                };
            }
        }

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
                Phone = "+593 98765 4321",
                Email = "elena.rosales@safealert.app",
                BloodType = "O+",
                Allergies = "Penicilina",
                DoctorName = "Dr. Fernando Torres",
                DoctorPhone = "+593 98123 4567",
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
                Phone = "+593 99111 2233",
                ActionMode = "WhatsApp + SMS con ubicación",
                FooterNote = "Recibe WhatsApp con ubicación",
                AvatarColor = "#1F3864",
                Verified = true,
                Order = 1
            },
            new EmergencyContact
            {
                Name = "Alejandro Ruiz",
                Relation = "Hermano",
                Priority = "Secundaria",
                Phone = "+593 98888 7766",
                ActionMode = "WhatsApp + SMS con ubicación",
                FooterNote = "Respaldo por SMS",
                AvatarColor = "#0074D5",
                Verified = true,
                Order = 2
            },
            new EmergencyContact
            {
                Name = "Seguridad Residencial",
                Relation = "Cuerpo de Guardias",
                Priority = "Caseta",
                Phone = "+593 2 234 5678",
                ActionMode = "WhatsApp + SMS con ubicación",
                FooterNote = "Contacto institucional",
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
                Address = "Av. Amazonas N34-12 y Naciones Unidas, Quito",
                Latitude = -0.1814,
                Longitude = -78.4755,
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
                Address = "Parque La Carolina, Quito",
                Latitude = -0.1768,
                Longitude = -78.4707,
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
                Latitude = -0.1814,
                Longitude = -78.4755,
                CreatedAt = today.AddDays(-30).AddHours(10),
                NotifiedContacts = 0,
                AudioRecorded = false,
                Notes = "Latencia 0.3 s. Prueba exitosa."
            }
        });

        return account;
    }
}
