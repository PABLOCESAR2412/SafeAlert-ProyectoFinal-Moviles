using Microsoft.Maui.ApplicationModel;
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
    public event Action? UpdateCheckRequested;

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


        ShakeSwitch.IsToggled = account.Settings.ShakeDetection;
        UpdateSubtitleLabel.Text = $"Versión {AppInfo.Current.VersionString} instalada";
        VersionLabel.Text = $"SafeAlert v{AppInfo.Current.VersionString} · Datos locales";

        _loading = false;
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
            $"Perfil SafeAlert activo.\n\nTitular: {account?.Profile.FullName}\nID: {account?.Profile.MemberId}\nRenovación: {expiry}\n\nIncluye ficha médica, perfil de emergencia y historial local. Esta demo no realiza despacho ECU911 ni respaldo remoto.",
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

    private async void FallDetection_Tapped(object sender, TappedEventArgs e)
    {
        await Ui.Alert(
            "Sensor de movimiento",
            "SafeAlert usa el acelerómetro del teléfono para detectar sacudidas fuertes (por ejemplo, agitar el equipo) y ofrecer el envío de la alerta.\n\nLa detección automática de caídas con cuenta regresiva es una mejora pendiente; hoy la activación siempre la confirmas tú.",
            "Aceptar");
    }

    private async void Privacy_Tapped(object sender, TappedEventArgs e)
    {
        await Ui.PushModal(new LegalPage());
    }

    private void Update_Tapped(object sender, TappedEventArgs e)
    {
        _ = ShowUpdateOptionsAsync();
    }

    private async Task ShowUpdateOptionsAsync()
    {
        var preview = UpdateService.Preview();
        var enable = await Ui.Confirm(
            "Actualizaciones de SafeAlert",
            $"Versión instalada: {AppInfo.Current.VersionString}\n\nSafeAlert compara su versión con un archivo update.json publicado por el administrador y muestra un aviso discreto cuando hay una versión nueva. También puedes ver el aviso de ejemplo para comprobar cómo se verá.",
            "Ver aviso de ejemplo",
            "Cerrar");

        if (!enable || preview == null)
            return;

        UpdateService.EnablePreview();
        await Ui.Alert(
            "Aviso de ejemplo activado",
            $"Se mostrará el aviso de la versión {preview.LatestVersion} al volver al Inicio. No descarga nada hasta que publiques la APK.",
            "Entendido");

        UpdateCheckRequested?.Invoke();
    }

    private void Logout_Clicked(object sender, EventArgs e) => LogoutRequested?.Invoke();
}
