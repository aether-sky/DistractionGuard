using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DistractionGuard
{
  static class Globals
  {
    [DllImport("kernel32.dll")]
    static extern IntPtr GetConsoleWindow();
    internal static void Debug(string message)
    {
      if (GetConsoleWindow() == IntPtr.Zero)
      {
        System.Diagnostics.Debug.WriteLine(message);
      }
      else
      {
        Console.WriteLine(message);
      }
    }
  }
}
