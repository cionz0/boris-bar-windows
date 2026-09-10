using Windows.Media.Core;
using Windows.Media.Playback;

namespace BorisBar;

internal sealed class AudioPlayer : IDisposable
{
    private const int MaxClipMs = 30_000;

    private readonly MediaPlayer _player = new();
    private readonly object _gate = new();
    private string? _currentPath;
    private System.Threading.Timer? _limitTimer;
    private bool _disposed;

    public event Action<string>? Failed;

    public AudioPlayer()
    {
        _player.MediaEnded += (_, _) => Stop();
        _player.MediaFailed += (_, args) =>
        {
            Stop();
            var message = args.ErrorMessage;
            Failed?.Invoke(string.IsNullOrWhiteSpace(message)
                ? "Formato audio non supportato da Windows Media Foundation."
                : message);
        };
    }

    public void Play(string path)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_gate)
        {
            if (string.Equals(_currentPath, path, StringComparison.OrdinalIgnoreCase)
                && _player.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
            {
                StopUnlocked();
                return;
            }

            StopUnlocked();
            _currentPath = path;
            _player.Source = MediaSource.CreateFromUri(new Uri(Path.GetFullPath(path)));
            _player.Play();
            _limitTimer = new System.Threading.Timer(_ => Stop(), null, MaxClipMs, Timeout.Infinite);
        }
    }

    public void Stop()
    {
        lock (_gate)
        {
            StopUnlocked();
        }
    }

    private void StopUnlocked()
    {
        if (_limitTimer is not null)
        {
            _limitTimer.Dispose();
            _limitTimer = null;
        }

        _player.Pause();
        _player.Source = null;
        _currentPath = null;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Stop();
        _player.Dispose();
        _disposed = true;
    }
}
