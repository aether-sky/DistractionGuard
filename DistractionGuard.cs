
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace DistractionGuard
{
  internal class DistractionGuard
  {

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    static extern int GetWindowTextLength(IntPtr hWnd);


    private static bool Active = true;
    private static bool Running = true;
    private static bool Debounce = false;
    internal static void Close()
    {
      SetActive(false);
      Running = false;
    }
    internal static void SetActive(bool b)
    {
      Active = b;
      Debounce = b; //no need to debounce if b is false
    }
   
    internal static void WatchWindows()
    {
      //var config = DistractionGuard.LoadConfig();
      var hwnd = GetForegroundWindow();
      string hwndName = GetWindowTitle(hwnd);
      while (Running)
      {

        var patterns = Model.GetPatterns();
        var otherSec = Model.GetOtherSecsInt();

        Thread.Sleep(10);
        if (!Active)
        {
          Thread.Sleep(500);
          continue;
        }

        var newHwnd = GetForegroundWindow();
        string newHwndName = GetWindowTitle(newHwnd);
        if (hwnd != newHwnd && !string.IsNullOrWhiteSpace(newHwndName) &&
          newHwnd != IntPtr.Zero)
        {
          Globals.Debug($"HWND switched from");
          Globals.Debug($"    {hwndName} ({hwnd})");
          Globals.Debug("    to");
          Globals.Debug($"    {newHwndName} ({newHwnd})");


          var pauseSeconds = 0;
          foreach (var i in patterns)
          {
            var windowName = i.Key;
            var pauseLength = i.Value;
            if (Regex.Match(newHwndName, windowName).Success)
            {
              pauseSeconds = pauseLength;
            }
          }
          if (pauseSeconds == 0 && otherSec > 0)
          {
            pauseSeconds = otherSec;
          }

          //avoids start menu or DistractionGuard from triggering it
          if (newHwndName == "Search" || newHwndName == "Task Switching" || newHwndName == "DistractionGuard")
          {
            pauseSeconds = 0;
          }
          if (pauseSeconds > 0 && !Debounce)
          {
            SecureDesktop.SwitchToNewDesktopFor(TimeSpan.FromSeconds(pauseSeconds));
          }
          Debounce = false;
          hwnd = newHwnd;
          hwndName = newHwndName;
        }
      }
    }

    private static string GetWindowTitle(IntPtr hwnd)
    {
      int textLength = GetWindowTextLength(hwnd);
      var sb = new StringBuilder(textLength + 1);
      GetWindowText(hwnd, sb, textLength + 1);
      return sb.ToString();
    }
  }
}