using Microsoft.Maui.Controls;

namespace SafeAlert.Helpers;

public static class Ui
{
    public static Page? RootPage => Application.Current?.Windows.FirstOrDefault()?.Page;

    public static Task Alert(string title, string message, string accept = "Aceptar") =>
        RootPage?.DisplayAlertAsync(title, message, accept) ?? Task.CompletedTask;

    public static Task<bool> Confirm(string title, string message, string accept = "Aceptar", string cancel = "Cancelar") =>
        RootPage?.DisplayAlertAsync(title, message, accept, cancel) ?? Task.FromResult(false);

    public static Task<string?> Ask(string title, string message, string initialValue = "", string placeholder = "") =>
        RootPage?.DisplayPromptAsync(title, message, "Aceptar", "Cancelar", placeholder, 160, Keyboard.Text, initialValue)
        ?? Task.FromResult<string?>(null);

    public static Task<string?> ActionSheet(string title, string cancel, params string[] buttons) =>
        RootPage?.DisplayActionSheetAsync(title, cancel, null, buttons)
        ?? Task.FromResult<string?>(null);

    public static async Task PushModal(Page page)
    {
        if (RootPage is { } root)
            await root.Navigation.PushModalAsync(page);
    }

    public static async Task PopModal()
    {
        if (RootPage is { } root)
            await root.Navigation.PopModalAsync();
    }

    public static Color Color(string hex) => Microsoft.Maui.Graphics.Color.FromArgb(hex);
}
