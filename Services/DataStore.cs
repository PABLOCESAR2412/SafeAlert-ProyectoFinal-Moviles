using SafeAlert.Helpers;
using SafeAlert.Models;

namespace SafeAlert.Services;

public static class DataStore
{
    private static readonly object Gate = new();
    private static AppData? _data;

    public static string StoragePath { get; } = Path.Combine(BuildDirectory(), "safealert.db3");

    public static AppData Data
    {
        get
        {
            lock (Gate)
            {
                if (_data is not null)
                    return _data;

                var legacyPath = Path.ChangeExtension(StoragePath, ".json");
                var data = LocalDatabase.LoadOrImport(StoragePath, legacyPath);
                if (data.Users.Count == 0)
                {
                    data.Users.Add(CreateDemoAccount());
                    data.LastEmail = data.Users[0].Email;
                    LocalDatabase.Save(StoragePath, data);
                }

                _data = data;
                return data;
            }
        }
    }

    private static string BuildDirectory()
    {
        string directory;
        try
        {
            directory = Path.Combine(FileSystem.AppDataDirectory, "SafeAlert");
            Directory.CreateDirectory(directory);
        }
        catch
        {
            directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SafeAlert");
            Directory.CreateDirectory(directory);
        }
        return directory;
    }

    public static void Save()
    {
        lock (Gate)
        {
            if (_data is not null)
                LocalDatabase.Save(StoragePath, _data);
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
