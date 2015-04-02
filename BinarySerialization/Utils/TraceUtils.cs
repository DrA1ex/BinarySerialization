using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;

namespace BinarySerialization.Utils
{
    internal static class TraceUtils
    {
        [Conditional("TRACE")]
        internal static void WriteLineFormatted(string format, params object[] args)
        {
            Contract.Requires<ArgumentNullException>(format != null);
            Contract.Requires<ArgumentNullException>(args != null);

            Trace.WriteLine(String.Format(format, args));
        }
    }
}