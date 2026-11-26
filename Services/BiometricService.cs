#if ANDROID
using Android.Hardware.Biometrics;
using Android.OS;
using AndroidX.Core.Content;
using Microsoft.Maui.ApplicationModel;

namespace SafeAlert.Services;

/// <summary>
/// Autenticación biométrica real usando BiometricPrompt del sistema.
/// No requiere servicios en la nube ni claves externas.
/// </summary>
public static class BiometricService
{
    private static AuthenticationCallback? _activeCallback;
    private static CancellationSignal? _activeSignal;

    public static bool IsAvailable()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.P)
            return false;

        try
        {
            var activity = Platform.CurrentActivity;
            if (activity == null)
                return false;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
        {
            var biometricManager = activity.GetSystemService(Android.Content.Context.BiometricService)
                as Android.Hardware.Biometrics.BiometricManager;

            // 255 = BIOMETRIC_WEAK: huella, rostro o iris ya enrolados.
            return biometricManager?.CanAuthenticate(255) == 0;
        }

        var keyguard = activity.GetSystemService(Android.Content.Context.KeyguardService)
            as Android.App.KeyguardManager;

        return keyguard?.IsDeviceSecure == true;
        }
        catch
        {
            return false;
        }
    }

    public static async Task<bool> AuthenticateAsync(string reason)
    {
        if (!IsAvailable())
            return false;

        var activity = Platform.CurrentActivity;
        if (activity == null)
            return false;

        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        try
        {
            var executor = ContextCompat.GetMainExecutor(activity);
            _activeCallback = new AuthenticationCallback(completion);
            _activeSignal = new CancellationSignal();

            var prompt = new BiometricPrompt.Builder(activity)
                .SetTitle("SafeAlert")
                .SetSubtitle(reason)
                .SetNegativeButton("Usar contraseña", executor, new NegativeListener(completion))
                .Build();

            prompt.Authenticate(_activeSignal, executor, _activeCallback);
        }
        catch
        {
            completion.TrySetResult(false);
        }

        return await completion.Task;
    }

    private static void Finish(TaskCompletionSource<bool> completion, bool success)
    {
        try
        {
            _activeSignal?.Cancel();
        }
        catch
        {
            // La señal ya puede estar cerrada por el sistema.
        }

        _activeSignal = null;
        _activeCallback = null;
        completion.TrySetResult(success);
    }

    private sealed class AuthenticationCallback(TaskCompletionSource<bool> completion)
        : BiometricPrompt.AuthenticationCallback
    {
        public override void OnAuthenticationSucceeded(BiometricPrompt.AuthenticationResult result) =>
            Finish(completion, true);

        // Una huella no reconocida no cierra el diálogo: el sistema permite reintentar.
        public override void OnAuthenticationFailed()
        {
        }

        public override void OnAuthenticationError(BiometricErrorCode errorCode, Java.Lang.ICharSequence errString) =>
            Finish(completion, false);
    }

    private sealed class NegativeListener(TaskCompletionSource<bool> completion)
        : Java.Lang.Object, Android.Content.IDialogInterfaceOnClickListener
    {
        public void OnClick(Android.Content.IDialogInterface? dialog, int which) =>
            Finish(completion, false);
    }
}
#else
namespace SafeAlert.Services;

public static class BiometricService
{
    public static bool IsAvailable() => false;

    public static Task<bool> AuthenticateAsync(string reason) => Task.FromResult(false);
}
#endif
