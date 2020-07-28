using System.Text;
using System.Linq;
using System.Threading;
using System.Security.Cryptography;
using System;

namespace XamlX.Helpers
{
    public class DeterministicName
    {
        private static long _counter;
        private static object _lockobj = new object();

        public static string GetString()
        {
            lock (_lockobj)
            {
                Interlocked.Increment(ref _counter);
            }
            return $"C{_counter}";
        }
    }
}
