using System.Text;

namespace MessagingApp
{
    internal static class Encoder
    {
        private static Encoding encoder => Encoding.UTF8;

        public static byte[] GetBytes(string @string) => encoder.GetBytes(@string);

        public static string GetString(byte[] bytes, int index, int count) => encoder.GetString(bytes, index, count);
    }
}
