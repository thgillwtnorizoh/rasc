using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Rasc.Desktop;

/// <summary>
/// Nonactivating, click-through WPF preview window. Physical scaling, native raster,
/// animation path and stacking position are unverified against historical Steam.
/// </summary>
public partial class ClassicToastWindow : Window
{
    private const int GwlExstyle = -20;
    private const long WsExTransparent = 0x00000020L;
    private const long WsExToolwindow = 0x00000080L;
    private const long WsExNoactivate = 0x08000000L;

    public ClassicToastWindow(string achievementId)
    {
        InitializeComponent();
        var text = (achievementId ?? string.Empty)
            .Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ').Trim();
        AchievementName.Text = text.Length > 160 ? text[..160] : text;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        if (!OperatingSystem.IsWindows()) return;
        var handle = new WindowInteropHelper(this).Handle;
        var flags = GetWindowLongPtr(handle, GwlExstyle).ToInt64();
        _ = SetWindowLongPtr(handle, GwlExstyle, new IntPtr(
            flags | WsExTransparent | WsExToolwindow | WsExNoactivate));
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr window, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr window, int index, IntPtr value);
}
