using Microsoft.Maui.Devices.Sensors;

namespace SafeAlert.Services;

/// <summary>
/// Detector de sacudidas basado en el acelerómetro. En Windows puede no existir
/// hardware, pero la misma implementación funciona en Android/iOS.
/// </summary>
public sealed class ShakeDetector : IDisposable
{
    private const double ThresholdInG = 2.2;
    private static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(2);

    private bool _running;
    private DateTime _lastDetection = DateTime.MinValue;

    public bool IsSupported => Accelerometer.Default.IsSupported;

    public event EventHandler? Detected;

    public bool Start()
    {
        if (!IsSupported)
            return false;

        if (_running)
            return true;

        try
        {
            Accelerometer.Default.ReadingChanged += OnReadingChanged;
            Accelerometer.Default.Start(SensorSpeed.Game);
            _running = true;
            return true;
        }
        catch
        {
            Accelerometer.Default.ReadingChanged -= OnReadingChanged;
            return false;
        }
    }

    public void Stop()
    {
        if (!_running)
            return;

        try
        {
            Accelerometer.Default.Stop();
        }
        catch
        {
            // Ignorado: el sensor pudo haberse detenido por el sistema.
        }

        Accelerometer.Default.ReadingChanged -= OnReadingChanged;
        _running = false;
    }

    private void OnReadingChanged(object? sender, AccelerometerChangedEventArgs e)
    {
        var x = e.Reading.Acceleration.X;
        var y = e.Reading.Acceleration.Y;
        var z = e.Reading.Acceleration.Z;
        var magnitude = Math.Sqrt((x * x) + (y * y) + (z * z));

        if (magnitude < ThresholdInG)
            return;

        var now = DateTime.UtcNow;
        if (now - _lastDetection < Cooldown)
            return;

        _lastDetection = now;
        MainThread.BeginInvokeOnMainThread(() => Detected?.Invoke(this, EventArgs.Empty));
    }

    public void Dispose() => Stop();
}
