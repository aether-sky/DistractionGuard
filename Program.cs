namespace DistractionGuard
{
  internal static class Program
  {
    [STAThread]
    static void Main()
    {
      try
      {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        var watchThread = new Thread(DistractionGuard.WatchWindows);
        watchThread.Start();
        Application.Run(new MainForm());
      }
      finally
      {
        DistractionGuard.Close();
      }
    }
  }
}