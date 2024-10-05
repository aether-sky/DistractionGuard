
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace DistractionGuard
{
  internal class DistractionGuard
  {

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

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    static extern int GetWindowTextLength(IntPtr hWnd);

    internal static Dictionary<string,int> LoadConfig()
    {
      Globals.Debug("HERE:" + System.Reflection.Assembly.GetExecutingAssembly().Location);
      var path = "";
      var dtConfig = "C:/Users/sky/Desktop/config.txt";
      var defaultConfig = "config.txt";
      if (File.Exists(dtConfig))
      {
        path = dtConfig;
      }
      else
      {
        path = defaultConfig;
      }
      Globals.Debug($"Using config {path}");
      var contents = File.ReadAllText(path);

      var file = Regex.Split(contents, "\n|\r\n");

      var config = new Dictionary<string, int>();
      foreach (var f in file)
      {
        if (f.Length > 0 && f[0] == ';')
        {
          Globals.Debug("Skipping " + f);
          continue;
        }
        var pair = f.Split("=");
        if (pair.Length < 2)
        {
          if (f.Trim().Length > 0) Globals.Debug("Skipping line: " + f);
          continue;
        }
        var i = int.Parse(pair[1]);
        config[pair[0]] = i;
      }
      foreach (var c in config)
      {

        Globals.Debug($"{c.Key}<->{c.Value}");
      }
      return config;
    }
    internal static void WatchWindows()
    {
      var config = DistractionGuard.LoadConfig();
      var hwnd = GetForegroundWindow();
      string hwndName = GetWindowTitle(hwnd);
      while (Running)
      {
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
          foreach (var i in config)
          {
            var windowName = i.Key;
            if (windowName == "other")
            {
              continue;
            }
            var pauseLength = i.Value;
            if (Regex.Match(newHwndName, ".*" + windowName + ".*").Success)
            {
              pauseSeconds = pauseLength;
            }
          }
          if (pauseSeconds == 0 && config.ContainsKey("other"))
          {
            pauseSeconds = config["other"];
          }

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