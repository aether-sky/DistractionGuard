using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace DistractionGuard
{
  internal class SecureDesktop
  {
    [DllImport("user32.dll")]
    public static extern IntPtr CreateDesktop(string lpszDesktop, IntPtr lpszDevice, IntPtr pDevmode, int dwFlags, uint dwDesiredAccess, IntPtr lpsa);

    [DllImport("user32.dll")]
    private static extern bool SwitchDesktop(IntPtr hDesktop);

    [DllImport("user32.dll")]
    public static extern bool CloseDesktop(IntPtr handle);

    [DllImport("user32.dll")]
    public static extern bool SetThreadDesktop(IntPtr hDesktop);

    [DllImport("user32.dll")]
    public static extern IntPtr GetThreadDesktop(int dwThreadId);

    [DllImport("kernel32.dll")]
    public static extern int GetCurrentThreadId();

    enum DESKTOP_ACCESS : uint
    {
      DESKTOP_NONE = 0,
      DESKTOP_READOBJECTS = 0x0001,
      DESKTOP_CREATEWINDOW = 0x0002,
      DESKTOP_CREATEMENU = 0x0004,
      DESKTOP_HOOKCONTROL = 0x0008,
      DESKTOP_JOURNALRECORD = 0x0010,
      DESKTOP_JOURNALPLAYBACK = 0x0020,
      DESKTOP_ENUMERATE = 0x0040,
      DESKTOP_WRITEOBJECTS = 0x0080,
      DESKTOP_SWITCHDESKTOP = 0x0100,

      GENERIC_ALL = (
        DESKTOP_READOBJECTS |
        DESKTOP_CREATEWINDOW |
        DESKTOP_CREATEMENU |
        DESKTOP_HOOKCONTROL |
        DESKTOP_JOURNALRECORD |
        DESKTOP_JOURNALPLAYBACK |
        DESKTOP_ENUMERATE |
        DESKTOP_WRITEOBJECTS |
        DESKTOP_SWITCHDESKTOP
        ),
    }

    public static void SwitchToNewDesktopFor(TimeSpan ts)
    {
      var oldDesktop = GetThreadDesktop(GetCurrentThreadId());
      var newDesktop = CreateDesktop("DistractionGuardDesktop", IntPtr.Zero, IntPtr.Zero, 0,
        (uint)DESKTOP_ACCESS.GENERIC_ALL, IntPtr.Zero);

      SwitchDesktop(newDesktop);

      Thread.Sleep(ts);

      SwitchDesktop(oldDesktop);
      CloseDesktop(newDesktop);
    }
  }
}
