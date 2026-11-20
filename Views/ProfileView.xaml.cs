using SafeAlert.Helpers;
using SafeAlert.Services;

namespace SafeAlert.Views;

public partial class ProfileView : ContentView, IRefreshable
{
    private bool _loading;

    public event Action? LogoutRequested;
    public event Action<bool>? ShakeSettingChanged;
    public event Action? SimulateShakeRequested;
    public event Action? ProfileUpdated;

    public ProfileView()
    {
        InitializeComponent();
    }

    public void RefreshView()
    {
        if (AppSession.Account is not { } account)
            return;

        _loading = true;

        ProfileAvatarLabel.Text = account.Profile.Initials;
        ProfileNameLabel.Text = account.Profile.FullName;
        ProfilePhoneLabel.Text = account.Profile.Phone;
        ProfileEmailLabel.Text = account.Profile.Email;
        MemberIdLabel.Text = $"ID #{account.Profile.MemberId}";

        BloodTypeLabel.Text = account.Profile.BloodTypeDisplay;
        BloodTypeDetailLabel.Text = account.Profile.BloodTypeDisplay.EndsWith("-", StringComparison.Ordinal)
            ? $"{account.Profile.BloodTypeDisplay} Negativo"
            : $"{account.Profile.BloodTypeDisplay} Positivo";
        AllergiesLabel.Text = account.Profile.AllergiesDisplay;
        DoctorNameLabel.Text = string.IsNullOrWhiteSpace(account.Profile.DoctorName)
            ? "Por definir"
            : account.Profile.DoctorName;

        SosLockSwitch.IsToggled = account.Settings.SosOnLockScreen;
        BackgroundSwitch.IsToggled = account.Settings.BackgroundLocation;
        SirenSwitch.IsToggled = account.Settings.Siren;
        ShakeSwitch.IsToggled = account.Settings.ShakeDetection;

        _loading = false;
    }

    private void Setting_Toggled(object sender, ToggledEventArgs e)
    {
        if (_loading || AppSession.Account is not { } account)
            return;

        if (sender == SosLockSwitch)
            account.Settings.SosOnLockScreen = e.Value;
        else if (sender == BackgroundSwitch)
            account.Settings.BackgroundLocation = e.Value;
        else if (sender == SirenSwitch)
            account.Settings.Siren = e.Value;

        DataStore.Save();
    }

    private void ShakeSwitch_Toggled(object sender, ToggledEventArgs e)
    {
        if (_loading || AppSession.Account is not { } account)
            return;

        account.Settings.ShakeDetection = e.Value;
        DataStore.Save();
        ShakeSettingChanged?.Invoke(e.Value);
    }

    private async void EditProfile_Clicked(object sender, EventArgs e) => await OpenEditor(false);

    private async void EditMedical_Clicked(object sender, EventArgs e) => await OpenEditor(true);

    private async Task OpenEditor(bool medicalFocus)
    {
        var page = new ProfileEditPage(medicalFocus);
        page.Saved += () =>
        {
            RefreshView();
            ProfileUpdated?.Invoke();
        };

        await Ui.PushModal(page);
    }

    private async void CallDoctor_Clicked(object sender, EventArgs e)
    {
        if (AppSession.Account is not { } account)
            return;

        if (string.IsNullOrWhiteSpace(account.Profile.DoctorPhone))
        {
            await Ui.Alert(
                "Contacto médico",
                "Aún no registras el teléfono de tu médico principal. Puedes agregarlo en la ficha médica de emergencia.",
                "Aceptar");
            return;
        }

        var call = await Ui.Confirm(
            "Contacto médico principal",
            $"Se marcará a {account.Profile.DoctorName} al {account.Profile.DoctorPhone}.\n\nEn esta demostración de Windows la llamada es simulada.",
            "Marcar",
            "Cancelar");

        if (call)
        {
            await Ui.Alert(
                "Llamada simulada",
                $"Llamada de prueba con {account.Profile.DoctorName}.\nLa ficha médica se compartió correctamente.",
                "Finalizar");
        }
    }

    private async void ProCard_Tapped(object sender, TappedEventArgs e)
    {
        var account = AppSession.Account;
        var expiry = DateTime.Today.AddYears(1).ToString("dd 'de' MMMM 'de' yyyy");

        await Ui.Alert(
            "SafeAlert Pro",
            $"Protección 24/7 monitoreada.\n\nTitular: {account?.Profile.FullName}\nID: {account?.Profile.MemberId}\nRenovación: {expiry}\n\nIncluye despacho prioritario 911/C5, ficha médica vital y respaldo cifrado.",
            "Aceptar");
    }

    private async void TestShake_Tapped(object sender, TappedEventArgs e)
    {
        if (AppSession.Account?.Settings.ShakeDetection != true)
        {
            var enable = await Ui.Confirm(
                "Detección de sacudidas",
                "La detección está desactivada. ¿Deseas activarla para probar el sensor?",
                "Activar",
                "Cancelar");

            if (!enable)
                return;

            ShakeSwitch.IsToggled = true;
        }

        SimulateShakeRequested?.Invoke();
    }

    private async void TripleTap_Tapped(object sender, TappedEventArgs e)
    {
        if (AppSession.Account is not { } account)
            return;

        var action = await Ui.ActionSheet(
            "Triple pulsación del botón físico",
            "Cancelar",
            account.Settings.TripleTap ? "Desactivar triple pulsación" : "Activar triple pulsación",
            "Cómo funciona");

        if (action == "Activar triple pulsación" || action == "Desactivar triple pulsación")
        {
            account.Settings.TripleTap = !account.Settings.TripleTap;
            DataStore.Save();
            await Ui.Alert(
                "Configuración actualizada",
                account.Settings.TripleTap
                    ? "La triple pulsación del botón de encendido activará una alerta SOS."
                    : "La triple pulsación quedó desactivada.",
                "Aceptar");
        }
        else if (action == "Cómo funciona")
        {
            await Ui.Alert(
                "Triple pulsación",
                "Pulsa tres veces seguidas el botón de encendido del teléfono. SafeAlert emitirá una alerta SOS silenciosa con tu ubicación, incluso con la pantalla bloqueada.",
                "Entendido");
        }
    }

    private async void FallDetection_Tapped(object sender, TappedEventArgs e)
    {
        await Ui.Alert(
            "Detección de caídas e impactos",
            "SafeAlert usa el acelerómetro para detectar caídas o impactos fuertes.\n\nSensibilidad actual: media.\nSi se detecta un impacto y no confirmas que estás bien en 30 segundos, se enviará una alerta automática.",
            "Aceptar");
    }

    private async void Devices_Tapped(object sender, TappedEventArgs e)
    {
        var sync = await Ui.Confirm(
            "Dispositivos vinculados",
            "Dispositivos activos:\n• Smartwatch SafeAlert (conectado)\n• Llavero SOS Bluetooth (conectado)\n\n¿Deseas sincronizar ahora?",
            "Sincronizar",
            "Cerrar");

        if (sync)
        {
            await Ui.Alert(
                "Dispositivos sincronizados",
                $"Se sincronizaron 2 dispositivos correctamente.\nÚltima conexión: {DateTime.Now:HH:mm} hrs",
                "Aceptar");
        }
    }

    private async void Privacy_Tapped(object sender, TappedEventArgs e)
    {
        await Ui.Alert(
            "Privacidad y Términos Legales",
            "SafeAlert protege tus datos con cifrado de extremo a extremo.\n\n• Tu ubicación solo se comparte durante una alerta.\n• Los contactos son verificados por ti.\n• Puedes exportar o eliminar tu historial en cualquier momento.\n\nVersión de política: 2026-01",
            "Aceptar");
    }

    private void Logout_Clicked(object sender, EventArgs e) => LogoutRequested?.Invoke();
}
