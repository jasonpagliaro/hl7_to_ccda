using System.Diagnostics;

namespace Microsoft.Health.MeasurementUtility;

public interface ITimed : IDisposable
{
}

public static class Performance
{
    public static ITimed TrackDuration(Action<double> onCompleted)
    {
        return new TimedMeasurement(onCompleted);
    }

    private sealed class TimedMeasurement : ITimed
    {
        private readonly Action<double> _onCompleted;
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private bool _disposed;

        public TimedMeasurement(Action<double> onCompleted)
        {
            _onCompleted = onCompleted;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _stopwatch.Stop();
            _onCompleted(_stopwatch.Elapsed.TotalMilliseconds);
        }
    }
}
