using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;

namespace SafeAlert;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        builder.ConfigureFonts(fonts =>
        {
            fonts.AddFont("Inter-Regular.ttf", "Inter");
            fonts.AddFont("Inter-Medium.ttf", "InterMedium");
            fonts.AddFont("Inter-SemiBold.ttf", "InterSemiBold");
            fonts.AddFont("Inter-Bold.ttf", "InterBold");
            fonts.AddFont("Inter-ExtraBold.ttf", "InterExtraBold");
            fonts.AddFont("MaterialSymbolsOutlined.ttf", "MaterialIcons");
        });

#if WINDOWS
        EntryHandler.Mapper.AppendToMapping("SafeAlertBorderless", (handler, view) =>
        {
            handler.PlatformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
            handler.PlatformView.Padding = new Microsoft.UI.Xaml.Thickness(0);
            handler.PlatformView.Background = null;
        });

        EditorHandler.Mapper.AppendToMapping("SafeAlertBorderless", (handler, view) =>
        {
            handler.PlatformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
            handler.PlatformView.Padding = new Microsoft.UI.Xaml.Thickness(0);
            handler.PlatformView.Background = null;
        });
#endif

        builder.Logging.AddDebug();
        return builder.Build();
    }
}
