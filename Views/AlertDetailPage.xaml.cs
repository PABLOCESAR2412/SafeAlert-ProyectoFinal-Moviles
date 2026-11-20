using SafeAlert.Helpers;
using SafeAlert.Models;
using SafeAlert.Services;

namespace SafeAlert.Views;

public partial class AlertDetailPage : ContentPage
{
    private readonly AlertRecord _alert;

    public event Action? Changed;

    public AlertDetailPage(AlertRecord alert)
    {
        InitializeComponent();
        _alert = alert;
        RefreshDetails();
    }

    private void RefreshDetails()
    {
        TypeIconLabel.Text = _alert.Icon;
        TypeIconBorder.BackgroundColor = Color.FromArgb(_alert.IconBackground);
        TypeIconLabel.TextColor = Color.FromArgb(_alert.IconColor);
        TypeTitleLabel.Text = _alert.TypeTitle;
        DateLabel.Text = _alert.DateText;
        AlertIdLabel.Text = $"ID: {_alert.Id}";
        AddressLabel.Text = _alert.Address;
        MapAddressLabel.Text = _alert.Address;
        CoordinatesLabel.Text = _alert.CoordinatesText;
        MessageLabel.Text = _alert.Message;
        ContactsLabel.Text = _alert.ContactsLine;
        ResponseLabel.Text = _alert.ResponseLine;
        AudioLabel.Text = _alert.AudioRecorded
            ? "Grabación de audio disponible"
            : "Sin audio grabado";
        NotesLabel.Text = _alert.Notes;
        NotesCard.IsVisible = !string.IsNullOrWhiteSpace(_alert.Notes);
        MapCard.IsVisible = _alert.ShowMap;

        StatusLabel.Text = _alert.StatusText;
        StatusLabel.TextColor = Color.FromArgb(_alert.StatusTextColor);

        HeaderStatusLabel.Text = _alert.StatusText;
        HeaderStatusBadge.BackgroundColor = Color.FromArgb(_alert.StatusBackgroundColor);
        HeaderStatusLabel.TextColor = Color.FromArgb(_alert.StatusTextColor);

        HeaderSubtitle.Text = _alert.Status switch
        {
            AlertStatus.InProgress => "Evento en curso • seguimiento activo",
            AlertStatus.FalseAlarm => "Evento cerrado como falsa alarma",
            _ => "Reporte cifrado y trazable"
        };

        var isOpen = _alert.Status == AlertStatus.InProgress;
        ResolveButton.IsVisible = isOpen;
        FalseAlarmButton.IsVisible = isOpen;
        ClosedLabel.IsVisible = !isOpen;
    }

    private void Close_Clicked(object sender, EventArgs e) => _ = Navigation.PopModalAsync();

    private async void Resolve_Clicked(object sender, EventArgs e)
    {
        var confirm = await Ui.Confirm(
            "Cerrar alerta",
            "¿Confirmas que la emergencia fue atendida y deseas cerrar este evento como resuelto?",
            "Marcar resuelta",
            "Cancelar");

        if (!confirm)
            return;

        AlertService.ChangeStatus(_alert, AlertStatus.Resolved);
        RefreshDetails();
        Changed?.Invoke();

        await Ui.Alert(
            "Alerta resuelta",
            "El evento se cerró correctamente y se conserva en el historial cifrado.",
            "Aceptar");
    }

    private async void FalseAlarm_Clicked(object sender, EventArgs e)
    {
        var confirm = await Ui.Confirm(
            "Falsa alarma",
            "¿Deseas cerrar este evento como falsa alarma? Se notificará a tus contactos que estás a salvo.",
            "Marcar falsa alarma",
            "Cancelar");

        if (!confirm)
            return;

        AlertService.ChangeStatus(_alert, AlertStatus.FalseAlarm);
        RefreshDetails();
        Changed?.Invoke();

        await Ui.Alert(
            "Evento cerrado",
            "La alerta se marcó como falsa alarma. Tus contactos recibieron la confirmación de que estás a salvo.",
            "Aceptar");
    }
}
