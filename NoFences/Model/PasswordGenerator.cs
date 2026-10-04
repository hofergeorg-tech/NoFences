using System.Security.Cryptography;

namespace NoFences.Model
{
    /// <summary>Random passwords from a cryptographic random source, with at least one character of each chosen kind.</summary>
    public static class PasswordGenerator
    {
        public const string Upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        public const string Lower = "abcdefghijklmnopqrstuvwxyz";
        public const string Digits = "0123456789";
        public const string Symbols = "!#$%&*+-=?@_~";

        /// <summary>Characters that are easy to mix up when reading or typing (I, l, 1, O, 0).</summary>
        public const string Ambiguous = "Il1O0";

        public const int MinLength = 6;
        public const int MaxLength = 64;

        public static string Generate(int length, bool upper = true, bool lower = true, bool digits = true, bool symbols = true, bool avoidAmbiguous = true)
        {
            length = Math.Clamp(length, MinLength, MaxLength);
            var sets = new List<string>();
            if (upper) sets.Add(Upper);
            if (lower) sets.Add(Lower);
            if (digits) sets.Add(Digits);
            if (symbols) sets.Add(Symbols);
            if (sets.Count == 0)
                sets.Add(Lower);
            if (avoidAmbiguous)
                sets = sets.Select(s => new string(s.Where(c => !Ambiguous.Contains(c)).ToArray())).ToList();

            var all = string.Concat(sets);
            var chars = new char[length];
            // One of each kind, the rest from everything, then shuffled
            for (var i = 0; i < length; i++)
            {
                var set = i < sets.Count ? sets[i] : all;
                chars[i] = set[RandomNumberGenerator.GetInt32(set.Length)];
            }
            for (var i = length - 1; i > 0; i--)
            {
                var j = RandomNumberGenerator.GetInt32(i + 1);
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }
            return new string(chars);
        }

        /// <summary>Rough strength in bits (length × log2 of the alphabet).</summary>
        public static int Bits(int length, bool upper, bool lower, bool digits, bool symbols)
        {
            var size = (upper ? 26 : 0) + (lower ? 26 : 0) + (digits ? 10 : 0) + (symbols ? Symbols.Length : 0);
            return size == 0 ? 0 : (int)(Math.Clamp(length, MinLength, MaxLength) * Math.Log2(size));
        }
    }
}
