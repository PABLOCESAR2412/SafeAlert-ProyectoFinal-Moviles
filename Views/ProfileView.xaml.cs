using Microsoft.Maui.ApplicationModel;
using SafeAlert.Helpers;
using SafeAlert.Services;

namespace SafeAlert.Views;

public partial class ProfileView : ContentView, IRefreshable
{

    public event Action? LogoutRequested;
    public event Action? ProfileUpdated;
    public event Action? MovementSettingChanged;
    private bool _updatingSensorSettings;

    public ProfileView()
    {
        InitializeComponent();
    }

    public void RefreshView()
    {
        if (AppSession.Account is not { } account)
            return;


        UpdateSensorSwitches();
        ProfileAvatarLabel.Text = account.Profile.Initials;
        ProfileNameLabel.Text = account.Profile.FullName;
        ProfilePhoneLabel.Text = Validation.NormalizePhone(account.Profile.Phone) ?? account.Profile.Phone;
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


        VersionLabel.Text = $"SafeAlert v{AppInfo.Current.VersionString} · Datos locales";

    }


    private void UpdateSensorSwitches()
    {
        _updatingSensorSettings = true;
        LocationSwitch.IsToggled = PermissionService.LocationEnabled;
        ContactsSwitch.IsToggled = PermissionService.ContactsEnabled;
        MovementSwitch.IsToggled = PermissionService.MovementEnabled;
        _updatingSensorSettings = false;
    }

    private async void SensorSetting_Toggled(object? sender, ToggledEventArgs e)
    {
        if (_updatingSensorSettings || sender is not Switch toggle)
            return;

        if (toggle == MovementSwitch)
        {
            PermissionService.MovementEnabled = e.Value
                && await PermissionService.SensorStatusAsync() == "Acelerómetro disponible";
            MovementSettingChanged?.Invoke();
            UpdateSensorSwitches();
            if (e.Value && !PermissionService.MovementEnabled)
                await Ui.Alert("Acelerómetro", "El sensor no está disponible en este dispositivo.", "Entendido");
            return;
        }

        LocationSwitch.IsEnabled = ContactsSwitch.IsEnabled = MovementSwitch.IsEnabled = false;
        try
        {
            bool granted;
            if (toggle == LocationSwitch)
            {
                granted = e.Value && await PermissionService.EnsureLocationAsync(request: true);
                PermissionService.LocationEnabled = granted;
                LocationService.ClearPosition();
                if (granted)
                    _ = LocationService.RefreshAsync();
            }
            else
            {
                granted = e.Value && await PermissionService.EnsureContactsAsync(request: true);
                PermissionService.ContactsEnabled = granted;
            }

            UpdateSensorSwitches();
            if (e.Value && !granted)
                await Ui.Alert("Permiso no concedido", "Puedes revisar el permiso de SafeAlert en los ajustes del dispositivo.", "Entendido");
        }
        finally
        {
            LocationSwitch.IsEnabled = ContactsSwitch.IsEnabled = MovementSwitch.IsEnabled = true;
        }
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

    private async void CallDoctor_Clicked(object? sender, EventArgs e)
    {
        var phone = Validation.NormalizePhone(AppSession.Account?.Profile.DoctorPhone);
        if (string.IsNullOrWhiteSpace(phone))
        {
            await Ui.Alert(
                "Contacto médico",
                "Agrega o corrige el teléfono del médico principal en la ficha médica.",
                "Aceptar");
            return;
        }

        try
        {
            PhoneDialer.Default.Open(phone);
        }
        catch
        {
            await Ui.Alert(
                "Marcador no disponible",
                $"Abre la app Teléfono y marca el número guardado: {phone}.",
                "Aceptar");
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

    private async void Privacy_Clicked(object? sender, EventArgs e) =>
        await Ui.PushModal(new LegalPage());

    private void Logout_Clicked(object sender, EventArgs e) => LogoutRequested?.Invoke();
}
