using System.Diagnostics;

namespace VfdMeter;

internal readonly record struct NetworkSpeed(
    double ReceivedBytesPerSecond,
    double SentBytesPerSecond);

internal sealed class NetworkMonitor : IDisposable
{
    private Dictionary<Guid, NetworkInterfaceCounters>? _previousCounters;
    private long _previousTimestamp;
    private bool _disposed;

    public NetworkSpeed GetSpeed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!NativeNetworkApi.TryGetActiveInterfaceCounters(out var currentCounters))
        {
            return default;
        }

        var currentTimestamp = Stopwatch.GetTimestamp();
        if (_previousCounters is null)
        {
            _previousCounters = currentCounters;
            _previousTimestamp = currentTimestamp;
            return default;
        }

        var elapsedSeconds = Stopwatch.GetElapsedTime(_previousTimestamp, currentTimestamp).TotalSeconds;
        ulong receivedDelta = 0;
        ulong sentDelta = 0;

        foreach (var (id, current) in currentCounters)
        {
            if (!_previousCounters.TryGetValue(id, out var previous))
            {
                continue;
            }

            if (current.ReceivedBytes >= previous.ReceivedBytes)
            {
                receivedDelta += current.ReceivedBytes - previous.ReceivedBytes;
            }

            if (current.SentBytes >= previous.SentBytes)
            {
                sentDelta += current.SentBytes - previous.SentBytes;
            }
        }

        _previousCounters = currentCounters;
        _previousTimestamp = currentTimestamp;

        return elapsedSeconds > 0
            ? new NetworkSpeed(receivedDelta / elapsedSeconds, sentDelta / elapsedSeconds)
            : default;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _previousCounters?.Clear();
        _previousCounters = null;
    }
}
