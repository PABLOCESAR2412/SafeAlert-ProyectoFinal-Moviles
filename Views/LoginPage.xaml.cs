using SafeAlert.Helpers;
using SafeAlert.Services;

namespace SafeAlert.Views;

public partial class LoginPage : ContentPage
{
    private bool _loginMode = true;

    public LoginPage()
    {
        InitializeComponent();
        SetMode(true);
    }

    private void SetMode(bool login)
    {
        _loginMode = login;
        LoginPanel.IsVisible = login;
        RegisterPanel.IsVisible = !login;

        LoginTab.BackgroundColor = login ? Colors.White : Colors.Transparent;
        RegisterTab.BackgroundColor = login ? Colors.Transparent : Colors.White;

        LoginTabLabel.TextColor = login ? Color.FromArgb("#1F3864") : Color.FromArgb("#4B5563");
        RegisterTabLabel.TextColor = login ? Color.FromArgb("#4B5563") : Color.FromArgb("#1F3864");

        LoginTabLabel.FontFamily = login ? "InterBold" : "InterSemiBold";
        RegisterTabLabel.FontFamily = login ? "InterSemiBold" : "InterBold";

        ClearErrors();
        _ = RootScroll.ScrollToAsync(0, 0, true);
    }

    private void ClearErrors()
    {
        foreach (var label in new[]
                 {
                     LoginUserError, LoginPassError, LoginGeneralError,
                     RegNameError, RegPhoneError, RegEmailError,
                     RegPassError, RegConfirmError, RegGeneralError
                 })
        {
            label.IsVisible = false;
            label.Text = string.Empty;
        }
    }

    private static void ShowError(Label label, string message)
    {
        label.Text = message;
        label.IsVisible = true;
    }

    private void LoginTab_Tapped(object sender, TappedEventArgs e) => SetMode(true);

    private void RegisterTab_Tapped(object sender, TappedEventArgs e) => SetMode(false);

    private void RegisterLink_Tapped(object sender, TappedEventArgs e) => SetMode(false);

    private void LoginLink_Tapped(object sender, TappedEventArgs e) => SetMode(true);

    private void ToggleLoginPassword_Clicked(object sender, EventArgs e)
    {
        LoginPassEntry.IsPassword = !LoginPassEntry.IsPassword;
        LoginEyeButton.Text = LoginPassEntry.IsPassword ? Icons.Visibility : Icons.VisibilityOff;
    }

    private void ToggleRegisterPassword_Clicked(object sender, EventArgs e)
    {
        RegPassEntry.IsPassword = !RegPassEntry.IsPassword;
        RegEyeButton.Text = RegPassEntry.IsPassword ? Icons.Visibility : Icons.VisibilityOff;
    }

    private void ToggleRegisterConfirmPassword_Clicked(object sender, EventArgs e)
    {
        RegConfirmEntry.IsPassword = !RegConfirmEntry.IsPassword;
        RegConfirmEyeButton.Text = RegConfirmEntry.IsPassword ? Icons.Visibility : Icons.VisibilityOff;
    }

    private void LoginPass_Completed(object sender, EventArgs e) => TryLogin();

    private void LoginButton_Clicked(object sender, EventArgs e) => TryLogin();

    private void TryLogin()
    {
        if (!_loginMode)
            SetMode(true);

        ClearErrors();
        var identifier = LoginUserEntry.Text?.Trim() ?? string.Empty;
        var password = LoginPassEntry.Text ?? string.Empty;
        var hasError = false;

        if (string.IsNullOrWhiteSpace(identifier))
        {
            ShowError(LoginUserError, "Ingresa tu correo o teléfono.");
            hasError = true;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            ShowError(LoginPassError, "Ingresa tu contraseña.");
            hasError = true;
        }

        if (hasError)
            return;

        if (!AppSession.TryLogin(identifier, password, out var error))
        {
            ShowError(LoginGeneralError, error);
            return;
        }

        OpenMain();
    }

    private async void RegisterButton_Clicked(object sender, EventArgs e)
    {
        ClearErrors();
        var name = RegNameEntry.Text?.Trim() ?? string.Empty;
        var phone = RegPhoneEntry.Text?.Trim() ?? string.Empty;
        var email = RegEmailEntry.Text?.Trim() ?? string.Empty;
        var password = RegPassEntry.Text ?? string.Empty;
        var confirm = RegConfirmEntry.Text ?? string.Empty;
        var hasError = false;

        if (!Validation.IsValidFullName(name))
        {
            ShowError(RegNameError, "Ingresa nombre y apellido (solo letras).");
            hasError = true;
        }

        if (!Validation.IsValidPhone(phone))
        {
            ShowError(RegPhoneError, "Ingresa un teléfono de 10 a 15 dígitos.");
            hasError = true;
        }

        if (!Validation.IsValidEmail(email))
        {
            ShowError(RegEmailError, "Ingresa un correo electrónico válido.");
            hasError = true;
        }

        if (!Validation.IsStrongEnough(password))
        {
            ShowError(RegPassError, "La contraseña debe tener al menos 6 caracteres.");
            hasError = true;
        }

        if (!string.Equals(password, confirm, StringComparison.Ordinal))
        {
            ShowError(RegConfirmError, "Las contraseñas no coinciden.");
            hasError = true;
        }

        if (hasError)
            return;

        if (!AppSession.TryRegister(name, phone, email, password, out var error))
        {
            if (error.Contains("correo", StringComparison.OrdinalIgnoreCase))
                ShowError(RegEmailError, error);
            else
                ShowError(RegGeneralError, error);
            return;
        }

        await DisplayAlert(
            "Cuenta creada",
            $"Bienvenido/a, {AppSession.Account?.Profile.FirstName}. Tu cuenta quedó guardada en este dispositivo.",
            "Continuar");

        OpenMain();
    }

    private async void ForgotPassword_Tapped(object sender, TappedEventArgs e)
    {
        var email = await DisplayPromptAsync(
            "Recuperar contraseña",
            "Ingresa el correo registrado en tu cuenta:",
            "Enviar enlace",
            "Cancelar",
            "tu@correo.com",
            120,
            Keyboard.Email);

        if (string.IsNullOrWhiteSpace(email))
            return;

        var exists = DataStore.Data.Users.Any(user =>
            user.Email.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase));

        await DisplayAlert(
            "Recuperación de contraseña",
            exists
                ? "Enlace simulado enviado. Revisa tu correo para restablecer la contraseña."
                : "No encontramos una cuenta registrada con ese correo.",
            "Aceptar");
    }

    private async void BiometricButton_Clicked(object sender, EventArgs e)
    {
        if (BiometricService.IsAvailable())
        {
            var authenticated = await BiometricService.AuthenticateAsync(
                "Confirma tu huella para ingresar a SafeAlert");

            if (!authenticated)
            {
                await DisplayAlert(
                    "Biometría",
                    "No se pudo verificar tu identidad. Puedes ingresar con tu contraseña.",
                    "Aceptar");
                return;
            }

            if (AppSession.TryBiometricLogin(out var biometricError))
                OpenMain();
            else
                await DisplayAlert("Ingreso rápido", biometricError, "Aceptar");

            return;
        }

        var useDemo = await DisplayAlert(
            "Ingreso rápido no disponible",
            "Este dispositivo no tiene huella digital configurada o el lector no está disponible.\n\nPuedes entrar con la cuenta de demostración o usar tu contraseña.",
            "Usar cuenta de prueba",
            "Cancelar");

        if (!useDemo)
            return;

        if (AppSession.TryLoginDemo(out var error))
            OpenMain();
        else
            await DisplayAlert("No disponible", error, "Aceptar");
    }

    private void DemoAccount_Tapped(object sender, TappedEventArgs e)
    {
        LoginUserEntry.Text = "elena.rosales@safealert.app";
        LoginPassEntry.Text = "123456";
        SetMode(true);
    }

    private void OpenMain()
    {
        var window = Application.Current?.Windows.FirstOrDefault();
        if (window != null)
            window.Page = new MainPage();
    }
}
