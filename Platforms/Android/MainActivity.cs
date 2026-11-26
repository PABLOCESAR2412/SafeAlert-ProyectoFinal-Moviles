using Android.App;
using Android.Content.PM;
using Android.OS;
using SafeAlert.Services;

namespace SafeAlert;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges =
        ConfigChanges.ScreenSize |
        ConfigChanges.Orientation |
        ConfigChanges.UiMode |
        ConfigChanges.ScreenLayout |
        ConfigChanges.SmallestScreenSize |
        ConfigChanges.Density |
        ConfigChanges.LayoutDirection |
        ConfigChanges.Locale |
        ConfigChanges.Keyboard |
        ConfigChanges.KeyboardHidden |
        ConfigChanges.Navigation |
        ConfigChanges.FontScale)]
public class MainActivity : MauiAppCompatActivity
{
    private const int PickContactRequest = 9001;

    public void PickContact(Android.Content.Intent intent)
    {
        StartActivityForResult(intent, PickContactRequest);
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Android.Content.Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);

        if (requestCode == PickContactRequest)
        {
            AndroidActivityResult.Complete(
                resultCode == Result.Ok ? data?.Data : null);
        }
    }
}
