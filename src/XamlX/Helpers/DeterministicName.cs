using System.Text;
using System.Linq;
using System.Threading;
using System.Security.Cryptography;
using System;

namespace XamlX.Helpers
{
    public class DeterministicName
    {
        private static SHA256 _hasher = SHA256.Create();
        private static byte[] _seed = new byte[] { 1, 0, 1, 0 };
        private static object _global_lock = new object();

        public static Guid Get()
        {
            lock (_global_lock)
                _seed = _hasher.ComputeHash(_seed);

            var newGuid = new byte[16];
            Array.Copy(_seed, 0, newGuid, 0, 16);

            SwapByteOrder(newGuid);
            return new Guid(newGuid);
        }

        public static string GetString()
        {
            var x = Get().ToString("N"); ;
            Console.WriteLine(x);
            return x;
        }

        internal static void SwapByteOrder(byte[] guid)
        {
            SwapBytes(guid, 0, 3);
            SwapBytes(guid, 1, 2);
            SwapBytes(guid, 4, 5);
            SwapBytes(guid, 6, 7);
        }

        private static void SwapBytes(byte[] guid, int left, int right)
        {
            byte temp = guid[left];
            guid[left] = guid[right];
            guid[right] = temp;
        }
    }
}
