using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace Rasc.Desktop;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Capture the exact next WPF exception on the affected user's machine.
        // Never suppress a real crash or report a recovery that did not occur.
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnProcessUnhandledException;
        base.OnStartup(e);
    }

    private static void OnDispatcherUnhandledException(object sender,
        DispatcherUnhandledExceptionEventArgs e)
    {
        SaveLastUnhandled("UI Dispatcher", e.Exception);
        // e.Handled remains false; this is diagnostic evidence, not a blanket
        // exception-swallowing workaround.
    }

    private static void OnProcessUnhandledException(object sender,
        UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception error)
            SaveLastUnhandled("Process", error);
    }

    private static void SaveLastUnhandled(string source, Exception error)
    {
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RASC");
            Directory.CreateDirectory(dir);
            var file = Path.Combine(dir, "last-unhandled-error.txt");
            File.WriteAllText(file,
                "RASC " + source + " exception at " +
                DateTimeOffset.UtcNow.ToString("O") + Environment.NewLine +
                error.ToString()[..Math.Min(error.ToString().Length, 64000)]);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        DispatcherUnhandledException -= OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException -= OnProcessUnhandledException;
        ShutdownTrace.Mark("Application.Exit");
        ShutdownTrace.Flush();
        base.OnExit(e);
    }
}
