using System.Runtime.InteropServices;
using Desktop.DBus;
using Tmds.DBus.Protocol;

namespace beampdf;

public static class SleepInhibitor
{
    [DllImport("Kernel32.dll")]
    static extern uint SetThreadExecutionState(uint esFlags);

    static System.Timers.Timer timer;

    static void InhibitWindows()
    {
        // Windows requires us to repeatedly call a function to avoid sleep
        timer = new(1000.0);
        timer.Elapsed += delegate {
            // ES_DISPLAY_REQUIRED | ES_SYSTEM_REQUIRED
            SetThreadExecutionState(0x00000002 | 0x00000001);
        };
        timer.Start();
    }

    static void InhibitLinux()
    {
        // On Linux, we inhibit sleep via a desktop portal
        // (so it also works in a sandboxed environment)
        Connection connection = new(Address.Session);
        Task.Run(async () =>
        {
            await connection.ConnectAsync();
            DesktopService desktop = new(connection, "org.freedesktop.portal.Desktop");
            var inhibit = desktop.CreateInhibit("/org/freedesktop/portal/desktop");
            await inhibit.InhibitAsync("", 4 | 8, new() { ["reason"] = "Presenting slides"});
        });
    }

    public static void Inhibit()
    {
        if (OperatingSystem.IsWindows())
            InhibitWindows();
        else if (OperatingSystem.IsLinux())
            InhibitLinux();
        // TODO OSX will snooze happily
    }
}
