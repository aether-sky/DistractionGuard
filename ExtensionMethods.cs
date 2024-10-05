using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DistractionGuard
{
  public static class ExtensionMethods
  {
    public static string ToSingleLine(this string str)
    {
      var split = str.Split(Environment.NewLine).Select(x => x.Trim());
      return string.Join(" ", split);
    }
  }
}
