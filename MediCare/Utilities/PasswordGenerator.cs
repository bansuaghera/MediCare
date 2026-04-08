using System;
using System.Linq;

namespace MediCare.Utilities
{
    public static class PasswordGenerator
    {
        private static readonly Random _random = new Random();
        private const string Chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*";

        public static string Generate(int length = 12)
        {
            return new string(Enumerable.Repeat(Chars, length)
                .Select(s => s[_random.Next(s.Length)]).ToArray());
        }
    }
}
