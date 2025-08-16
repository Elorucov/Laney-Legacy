using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Elorucov.Laney.Helpers.Security
{
    public static class Encryptor
    {
        public static string ToSHA1(string plain)
        {
            string shash = null;
            using (var sha1 = SHA1.Create())
            {
                var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(plain));
                shash = String.Concat(hash.Select(b => b.ToString("x2")));
            }
            return shash;
        }
    }
}
