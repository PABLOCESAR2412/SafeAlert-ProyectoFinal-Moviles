using SafeAlert.Helpers;
using SafeAlert.Models;
using SafeAlert.Services;

namespace SafeAlert.Views;

public partial class ContactFormPage : ContentPage
{
    private static readonly string[] AvatarColors =
    {
        "#1F3864", "#0074D5", "#0F766E", "#7C3AED", "#B45309", "#BE123C", "#0369A1"
    };

    private readonly EmergencyContact? _contact;
    private readonly bool _isEditing;
    private readonly List<string> _relations = new()
    {
        "Mamá", "Papá", "Hermano", "Hermana", "Esposo/a", "Hijo/a",
        "Familiar", "Amigo/a", "Vecino/a", "Colega", "Cuerpo de Guardias", "Otro"
    };

    public event Action? Saved;

    public ContactFormPage(EmergencyContact? contact)
    {
        InitializeComponent();

        _contact = contact;
        _isEditing = contact != null;

        RelationPicker.ItemsSource = _relations;
        PriorityPicker.ItemsSource = new List<string> { "Primaria", "Secundaria", "Caseta", "Vecino", "Colega" };
        ActionPicker.ItemsSource = new List<string> { "WhatsApp + SMS con ubicación" };

        if (_isEditing)
        {
            TitleLabel.Text = "Editar contacto";
            DeleteButton.IsVisible = true;

            NameEntry.Text = contact!.Name;
            PhoneEntry.Text = Validation.NormalizePhone(contact.Phone) ?? contact.Phone;

            if (!_relations.Contains(contact.Relation))
                _relations.Add(contact.Relation);

            RelationPicker.SelectedItem = contact.Relation;
            PriorityPicker.SelectedItem = contact.Priority;
            ActionPicker.SelectedItem = "WhatsApp + SMS con ubicación";
        }
        else
        {
            RelationPicker.SelectedItem = "Familiar";
            PriorityPicker.SelectedItem = "Secundaria";
            ActionPicker.SelectedItem = "WhatsApp + SMS con ubicación";
        }
    }

    private void Close_Clicked(object sender, EventArgs e)
    {
        _ = Navigation.PopModalAsync();
    }

    private async void PickPhoneContact_Clicked(object sender, EventArgs e)
    {
        if (!PermissionService.ContactsEnabled)
        {
            await Ui.Alert("Agenda desactivada", "Actívala en Perfil → Sensores y permisos o escribe el número manualmente.", "Entendido");
            return;
        }

        var contact = await DeviceContactService.PickAsync();

        if (contact == null)
        {
            await Ui.Alert(
                "Contactos",
                "No se pudo seleccionar un contacto. Comprueba el permiso o ingresa los datos manualmente.",
                "Entendido");
            return;
        }

        NameEntry.Text = contact.Name;
        PhoneEntry.Text = Validation.NormalizePhone(contact.Phone) ?? contact.Phone;
    }

    private async void Save_Clicked(object sender, EventArgs e)
    {
        ClearErrors();

        var account = AppSession.Account;
        if (account == null)
        {
            await Ui.Alert("Sesión requerida", "Vuelve a iniciar sesión para guardar contactos.", "Aceptar");
            return;
        }

        var name = NameEntry.Text?.Trim() ?? string.Empty;
        var phone = Validation.NormalizePhone(PhoneEntry.Text) ?? string.Empty;
        var relation = RelationPicker.SelectedItem as string ?? string.Empty;
        var priority = PriorityPicker.SelectedItem as string ?? "Secundaria";
        var action = ActionPicker.SelectedItem as string ?? "WhatsApp + SMS con ubicación";
        var footer = action == "Llamada Inmediata" ? "Llamada configurada" : "Acción configurada";
        var hasError = false;

        if (name.Length < 3 || !Validation.IsLetters(name))
        {
            ShowError(NameError, NameBorder, "Ingresa un nombre válido (solo letras, mínimo 3 caracteres).");
            hasError = true;
        }

        if (!Validation.IsValidPhone(phone))
        {
            ShowError(PhoneError, PhoneBorder, "Ingresa un celular como 0991234567 o un fijo como 022345678.");
            hasError = true;
        }

        if (string.IsNullOrWhiteSpace(relation))
        {
            ShowError(RelationError, RelationBorder, "Selecciona la relación con este contacto.");
            hasError = true;
        }

        if (account.Contacts.Any(contact =>
                contact.Id != _contact?.Id &&
                Validation.DigitsOnly(contact.Phone) == Validation.DigitsOnly(phone)))
        {
            ShowError(PhoneError, PhoneBorder, "Ya tienes un contacto registrado con ese teléfono.");
            hasError = true;
        }

        if (hasError)
            return;

        if (_isEditing && _contact != null)
        {
            _contact.Name = name;
            _contact.Phone = phone;
            _contact.Relation = relation;
            _contact.Priority = priority;
            _contact.ActionMode = action;
            _contact.FooterNote = footer;
            _contact.Verified = true;
        }
        else
        {
            var newContact = new EmergencyContact
            {
                Name = name,
                Phone = phone,
                Relation = relation,
                Priority = priority,
                ActionMode = action,
                FooterNote = footer,
                AvatarColor = AvatarColors[account.Contacts.Count % AvatarColors.Length],
                Verified = true,
                Order = account.Contacts.Count == 0
                    ? 1
                    : account.Contacts.Max(contact => contact.Order) + 1
            };

            account.Contacts.Add(newContact);
        }

        DataStore.Save();
        Saved?.Invoke();
        await Navigation.PopModalAsync();
    }

    private async void Delete_Clicked(object sender, EventArgs e)
    {
        if (_contact == null)
        {
            await Navigation.PopModalAsync();
            return;
        }

        var confirm = await Ui.Confirm(
            "Eliminar contacto",
            $"¿Deseas eliminar a {_contact.Name} de tu red de apoyo?",
            "Eliminar",
            "Cancelar");

        if (!confirm)
            return;

        var account = AppSession.Account;
        if (account == null)
            return;

        account.Contacts.Remove(_contact);

        var ordered = account.Contacts.OrderBy(contact => contact.Order).ToList();
        for (var i = 0; i < ordered.Count; i++)
            ordered[i].Order = i + 1;

        DataStore.Save();
        Saved?.Invoke();
        await Navigation.PopModalAsync();
    }

    private void ClearErrors()
    {
        foreach (var label in new[] { NameError, PhoneError, RelationError })
        {
            label.Text = string.Empty;
            label.IsVisible = false;
        }

        ResetBorder(NameBorder);
        ResetBorder(PhoneBorder);
        ResetBorder(RelationBorder);
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
