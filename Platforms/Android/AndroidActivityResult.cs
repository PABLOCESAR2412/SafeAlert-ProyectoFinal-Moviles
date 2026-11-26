#if ANDROID
using Android.App;
using Android.Content;

namespace SafeAlert.Services;

public static class AndroidActivityResult
{
    private static Action<Android.Net.Uri?>? _callback;

    public static void Register(Intent intent, Activity activity, Action<Android.Net.Uri?> callback)
    {
        _callback = callback;

        if (activity is MainActivity mainActivity)
        {
            mainActivity.PickContact(intent);
        }
        else
        {
            callback(null);
        }
    }

    public static void Complete(Android.Net.Uri? uri)
    {
        var callback = _callback;
        _callback = null;
        callback?.Invoke(uri);
    }
}
#endif
