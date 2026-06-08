using System.Runtime.InteropServices;
using Tmds.DBus.Protocol;

namespace beampdf;

public static class SleepInhibitor
{
    [DllImport("Kernel32.dll")]
    static extern uint SetThreadExecutionState(uint esFlags);

    static System.Timers.Timer timer;

    static bool active;

    static void InhibitWindows()
    {
        // Windows requires us to repeatedly call a function to avoid sleep
        timer = new(1000.0);
        timer.Elapsed += delegate
        {
            // ES_DISPLAY_REQUIRED | ES_SYSTEM_REQUIRED
            SetThreadExecutionState(0x00000002 | 0x00000001);
        };
        timer.Start();
    }

    static void InhibitLinux()
    {
        // On Linux, we inhibit sleep via a desktop portal
        // (so it also works in a sandboxed environment)
        var connection = new DBusConnection(DBusAddress.Session);
        Task.Run(async () => {
            // FIXME  this will work again once a new version of Avalonia (>12.0.4) is released with the Tmds.DBus bugfix for codegen
            // await connection.ConnectAsync();
            // var inhibit = new DBus.Inhibit(
            //     connection,
            //     "org.freedesktop.portal.Desktop",
            //     "/org/freedesktop/portal/desktop"
            // );
            // await inhibit.InhibitAsync("", 4 | 8, new() { ["reason"] = "Presenting slides" });
        });
    }

    public static void Inhibit()
    {
        if (active)
            return;

        if (OperatingSystem.IsWindows())
            InhibitWindows();
        else if (OperatingSystem.IsLinux())
            InhibitLinux();

        // TODO OSX will snooze happily

        active = true;
    }
}
