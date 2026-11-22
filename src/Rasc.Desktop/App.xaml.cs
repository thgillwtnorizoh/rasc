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
        AppLog.Info("app", "startup; initializing WPF exception handlers");
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnProcessUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        base.OnStartup(e);
    }

    private static void OnDispatcherUnhandledException(object sender,
        DispatcherUnhandledExceptionEventArgs e)
    {
        AppLog.Fatal("crash.dispatcher", e.Exception);
        SaveLastUnhandled("UI Dispatcher", e.Exception);
        // e.Handled remains false; this is diagnostic evidence, not a blanket
        // exception-swallowing workaround.
    }

    private static void OnProcessUnhandledException(object sender,
        UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception error)
        {
            AppLog.Fatal("crash.process", error);
            SaveLastUnhandled("Process", error);
        }
        else AppLog.Error("crash.process", "Non-Exception unhandled object");
    }

    private static void OnUnobservedTaskException(object? sender,
        UnobservedTaskExceptionEventArgs e)
    {
        AppLog.Fatal("task.unobserved", e.Exception);
        // Do not mutate exception-handling behavior via SetObserved().
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
        TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
        AppLog.Info("app", $"WPF exit; code={e.ApplicationExitCode}");
        ShutdownTrace.Mark("Application.Exit");
        ShutdownTrace.Flush();
        AppLog.Finish();
        base.OnExit(e);
    }
}
