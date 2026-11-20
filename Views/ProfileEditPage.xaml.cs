using SafeAlert.Helpers;
using SafeAlert.Services;

namespace SafeAlert.Views;

public partial class ProfileEditPage : ContentPage
{
    private readonly bool _medicalFocus;

    public event Action? Saved;

    public ProfileEditPage(bool medicalFocus)
    {
        InitializeComponent();
        _medicalFocus = medicalFocus;

        BloodPicker.ItemsSource = new List<string>
        {
            "O+", "O-", "A+", "A-", "B+", "B-", "AB+", "AB-"
        };

        if (AppSession.Account is not { } account)
            return;

        MemberChipLabel.Text = account.Profile.MemberId;
        NameEntry.Text = account.Profile.FullName;
        PhoneEntry.Text = account.Profile.Phone;
        EmailEntry.Text = account.Profile.Email;
        AllergiesEntry.Text = account.Profile.Allergies;
        DoctorEntry.Text = account.Profile.DoctorName;
        DoctorPhoneEntry.Text = account.Profile.DoctorPhone;

        BloodPicker.SelectedItem = account.Profile.BloodTypeDisplay;
        if (BloodPicker.SelectedIndex < 0)
        {
            var items = (BloodPicker.ItemsSource as List<string>)!;
            items.Add(account.Profile.BloodTypeDisplay);
            BloodPicker.ItemsSource = items;
            BloodPicker.SelectedItem = account.Profile.BloodTypeDisplay;
        }

        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, EventArgs e)
    {
        if (!_medicalFocus)
            return;

        await Task.Delay(120);
        await FormScroll.ScrollToAsync(MedicalSection, ScrollToPosition.Start, false);
    }

    private void Close_Clicked(object sender, EventArgs e) => _ = Navigation.PopModalAsync();

    private async void Save_Clicked(object sender, EventArgs e)
    {
        ClearErrors();

        if (AppSession.Account is not { } account)
        {
            await Ui.Alert("Sesión requerida", "Vuelve a iniciar sesión para editar tu perfil.", "Aceptar");
            return;
        }

        var name = NameEntry.Text?.Trim() ?? string.Empty;
        var phone = PhoneEntry.Text?.Trim() ?? string.Empty;
        var email = EmailEntry.Text?.Trim().ToLowerInvariant() ?? string.Empty;
        var blood = BloodPicker.SelectedItem as string ?? "O+";
        var allergies = AllergiesEntry.Text?.Trim() ?? string.Empty;
        var doctor = DoctorEntry.Text?.Trim() ?? string.Empty;
        var doctorPhone = DoctorPhoneEntry.Text?.Trim() ?? string.Empty;
        var hasError = false;

        if (!Validation.IsValidFullName(name))
        {
            ShowError(NameError, NameBorder, "Ingresa nombre y apellido (solo letras).");
            hasError = true;
        }

        if (!Validation.IsValidPhone(phone))
        {
            ShowError(PhoneError, PhoneBorder, "Ingresa un teléfono de 10 a 15 dígitos.");
            hasError = true;
        }

        if (!Validation.IsValidEmail(email))
        {
            ShowError(EmailError, EmailBorder, "Ingresa un correo electrónico válido.");
            hasError = true;
        }
        else if (DataStore.Data.Users.Any(user =>
                     user != account &&
                     user.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
        {
            ShowError(EmailError, EmailBorder, "Ese correo ya está asociado a otra cuenta SafeAlert.");
            hasError = true;
        }

        if (!string.IsNullOrWhiteSpace(doctorPhone) && !Validation.IsValidPhone(doctorPhone))
        {
            ShowError(DoctorPhoneError, DoctorPhoneBorder, "El teléfono del médico debe tener entre 10 y 15 dígitos.");
            hasError = true;
        }

        if (hasError)
            return;

        account.Profile.FullName = name;
        account.Profile.Phone = phone;
        account.Profile.Email = email;
        account.Profile.BloodType = blood;
        account.Profile.Allergies = string.IsNullOrWhiteSpace(allergies) ? "Ninguna" : allergies;
        account.Profile.DoctorName = string.IsNullOrWhiteSpace(doctor) ? "Por definir" : doctor;
        account.Profile.DoctorPhone = doctorPhone;
        account.Email = email;
        DataStore.Data.LastEmail = email;

        AppSession.Save();
        Saved?.Invoke();
        await Navigation.PopModalAsync();
    }

    private void ClearErrors()
    {
        foreach (var label in new[] { NameError, PhoneError, EmailError, DoctorPhoneError })
        {
            label.Text = string.Empty;
            label.IsVisible = false;
        }

        ResetBorder(NameBorder);
        ResetBorder(PhoneBorder);
        ResetBorder(EmailBorder);
        ResetBorder(DoctorPhoneBorder);
    }

    private static void ShowError(Label label, Border border, string message)
    {
        label.Text = message;
        label.IsVisible = true;
        border.Stroke = new SolidColorBrush(Color.FromArgb("#DC2626"));
        border.BackgroundColor = Color.FromArgb("#FEF2F2");
    }

    private static void ResetBorder(Border border)
    {
        border.Stroke = new SolidColorBrush(Color.FromArgb("#E5E7EB"));
        border.BackgroundColor = Color.FromArgb("#F3F4F6");
    }
}
