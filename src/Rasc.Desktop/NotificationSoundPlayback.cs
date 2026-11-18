using System.IO;
using System.Windows.Media;

namespace Rasc.Desktop;

/// <summary>
/// Nonblocking local-file-only WPF playback for a presentation cue.
/// Owned by the main window; never manipulates Steam or achievement state.
/// Individual players are disposed after end/failure and when app closes.
/// </summary>
internal sealed class NotificationSoundPlayback : IDisposable
{
    private readonly List<MediaPlayer> _players = [];
    private bool _disposed;

    public bool Play(string path)
    {
        if (_disposed || !File.Exists(path)) return false;
        MediaPlayer? player = null;
        try
        {
            // Bound resource use even during a sudden unlock burst.
            if (_players.Count >= 8) Close(_players[0]);
            player = new MediaPlayer { Volume = 1 };
            _players.Add(player);
            player.MediaOpened += (_, _) =>
            {
                if (!_disposed) player.Play();
            };
            player.MediaEnded += (_, _) => Close(player);
            player.MediaFailed += (_, _) => Close(player);
            player.Open(new Uri(Path.GetFullPath(path)));
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or InvalidOperationException or ArgumentException or NotSupportedException)
        {
            if (player is not null) Close(player);
            // Audio must never stop an achievement notification or mutate events.
            return false;
        }
    }

    private void Close(MediaPlayer player)
    {
        if (!_players.Remove(player)) return;
        try { player.Close(); }
        catch (InvalidOperationException) { /* best-effort audio only */ }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var player in _players.ToArray()) Close(player);
    }
}
