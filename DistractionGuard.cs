
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace DistractionGuard
{
  internal class DistractionGuard
  {
    private static bool Running = true;
    internal static void Close()
    {
      Running = false;
    }
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    static extern int GetWindowTextLength(IntPtr hWnd);

    internal static Dictionary<string,int> LoadConfig()
    {
      Console.WriteLine("HERE:" + System.Reflection.Assembly.GetExecutingAssembly().Location);
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
      Console.WriteLine($"Using config {path}");
      var contents = File.ReadAllText(path);

      var file = Regex.Split(contents, "\n|\r\n");

      var config = new Dictionary<string, int>();
      foreach (var f in file)
      {
        if (f.Length > 0 && f[0] == ';')
        {
          Console.WriteLine("Skipping " + f);
          continue;
        }
        var pair = f.Split("=");
        if (pair.Length < 2)
        {
          if (f.Trim().Length > 0) Console.WriteLine("Skipping line: " + f);
          continue;
        }
        var i = int.Parse(pair[1]);
        config[pair[0]] = i;
      }
      foreach (var c in config)
      {

        Console.WriteLine($"{c.Key}<->{c.Value}");
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
        var newHwnd = GetForegroundWindow();
        string newHwndName = GetWindowTitle(newHwnd);
        if (hwnd != newHwnd && !string.IsNullOrWhiteSpace(newHwndName) &&
          newHwnd != IntPtr.Zero)
        {
          Console.WriteLine($"HWND switched from");
          Console.WriteLine($"    {hwndName} ({hwnd})");
          Console.WriteLine("    to");
          Console.WriteLine($"    {newHwndName} ({newHwnd})");


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
          if (pauseSeconds > 0)
          {
            SecureDesktop.SwitchToNewDesktopFor(TimeSpan.FromSeconds(pauseSeconds));
          }
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