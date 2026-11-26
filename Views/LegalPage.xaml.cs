namespace SafeAlert.Views;

public partial class LegalPage : ContentPage
{
    public LegalPage()
    {
        InitializeComponent();
        VersionLabel.Text = $"Versión de la aplicación: {AppInfo.Current.VersionString}";
    }

    private async void Close_Clicked(object sender, EventArgs e) =>
        await Navigation.PopModalAsync();
}
