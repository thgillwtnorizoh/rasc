using Rasc.Core;

namespace Rasc.Desktop;

/// <summary>
/// Application-scope local diagnostics. No startup dialog or network upload.
/// Log content may contain exception stack/message details; inspect before
/// sharing it publicly. Recording must never crash the achievement overlay.
/// </summary>
internal static class AppLog
{
    private static readonly Lazy<DiagnosticLog> Current =
        new(() => new DiagnosticLog(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RASC", "logs")));

    internal static string? CurrentFile => Current.Value.FilePath;

    internal static void Info(string category, string message) =>
        Current.Value.Info(category, message);

    internal static void Warning(string category, string message) =>
        Current.Value.Warning(category, message);

    internal static void Error(string category, string message) =>
        Current.Value.Error(category, message);

    internal static void Fatal(string category, Exception ex) =>
        Current.Value.Fatal(category, ex);

    internal static void Finish()
    {
        if (!Current.IsValueCreated) return;
        Current.Value.Info("session", "normal application exit");
        Current.Value.Dispose();
    }
}
