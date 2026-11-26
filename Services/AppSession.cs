using SafeAlert.Helpers;
using SafeAlert.Models;

namespace SafeAlert.Services;

public static class AppSession
{
    private const string SessionKey = "safealert.session.email";

    public static UserAccount? Account { get; private set; }

    public static bool IsLoggedIn => Account != null;

    public static bool TryRestoreSession()
    {
        if (Account != null)
            return true;

        try
        {
            var email = Preferences.Default.Get(SessionKey, string.Empty);
            if (string.IsNullOrWhiteSpace(email))
                return false;

            var user = DataStore.Data.Users.FirstOrDefault(account =>
                account.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

            if (user == null)
            {
                Preferences.Default.Remove(SessionKey);
                return false;
            }

            Account = user;
            DataStore.Data.LastEmail = user.Email;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void RememberSession(string email)
    {
        try
        {
            Preferences.Default.Set(SessionKey, email);
        }
        catch
        {
            // La sesión persistente es opcional si las preferencias no están disponibles.
        }
    }

    public static bool TryLogin(string identifier, string password, out string error)
    {
        error = string.Empty;
        identifier = (identifier ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(password))
        {
            error = "Ingresa tus credenciales para continuar.";
            return false;
        }

        var digits = Validation.DigitsOnly(identifier);
        var user = DataStore.Data.Users.FirstOrDefault(u =>
            u.Email.Equals(identifier, StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrWhiteSpace(digits) &&
             Validation.DigitsOnly(u.Profile.Phone).EndsWith(digits, StringComparison.Ordinal)));

        if (user == null)
        {
            error = "No encontramos una cuenta con ese correo o teléfono.";
            return false;
        }

        if (!PasswordHasher.Verify(password, user.PasswordHash))
        {
            error = "La contraseña no es correcta. Inténtalo de nuevo.";
            return false;
        }

        Account = user;
        DataStore.Data.LastEmail = user.Email;
        RememberSession(user.Email);
        DataStore.Save();
        return true;
    }

    public static bool TryLoginDemo(out string error) =>
        TryLogin("elena.rosales@safealert.app", "123456", out error);

    /// <summary>
    /// Acceso rápido tras una autenticación biométrica correcta. Solo reabre la
    /// última cuenta usada en este dispositivo; no acepta credenciales nuevas.
    /// </summary>
    public static bool TryBiometricLogin(out string error)
    {
        error = string.Empty;

        var email = DataStore.Data.LastEmail;
        var user = DataStore.Data.Users.FirstOrDefault(account =>
            account.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

        if (user == null)
        {
            error = "Primero inicia sesión con tu contraseña para habilitar el acceso biométrico.";
            return false;
        }

        Account = user;
        RememberSession(user.Email);
        DataStore.Save();
        return true;
    }

    public static bool TryRegister(
        string fullName,
        string phone,
        string email,
        string password,
        out string error)
    {
        error = string.Empty;
        var normalizedEmail = (email ?? string.Empty).Trim().ToLowerInvariant();

        if (DataStore.Data.Users.Any(u => u.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase)))
        {
            error = "Ya existe una cuenta registrada con ese correo.";
            return false;
        }

        var user = new UserAccount
        {
            Email = normalizedEmail,
            PasswordHash = PasswordHasher.Hash(password),
            Profile = new UserProfile
            {
                FullName = (fullName ?? string.Empty).Trim(),
                Phone = (phone ?? string.Empty).Trim(),
                Email = normalizedEmail,
                BloodType = "O+",
                Allergies = "Ninguna",
                DoctorName = "Por definir",
                DoctorPhone = string.Empty,
                MemberId = $"SA-{Random.Shared.Next(1000, 9999)}",
                IsPro = true
            },
            Settings = new AppSettings()
        };

        DataStore.Data.Users.Add(user);
        DataStore.Data.LastEmail = user.Email;
        Account = user;
        RememberSession(user.Email);
        DataStore.Save();
        return true;
    }

    public static void Logout()
    {
        Account = null;

        try
        {
            Preferences.Default.Remove(SessionKey);
        }
        catch
        {
            // Sin preferencias disponibles la sesión simplemente no se restaura.
        }

        DataStore.Save();
    }

    public static void Save()
    {
        if (Account != null)
            RememberSession(Account.Email);

        DataStore.Save();
    }
}
