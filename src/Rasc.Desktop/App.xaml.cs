using System.Windows;

namespace Rasc.Desktop;
public partial class App : Application
{
    protected override void OnExit(ExitEventArgs e)
    {
        ShutdownTrace.Mark("Application.Exit");
        ShutdownTrace.Flush();
        base.OnExit(e);
    }
}
