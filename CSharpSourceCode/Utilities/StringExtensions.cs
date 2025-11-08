using System;

namespace TOR_Core.Utilities
{
    public static class StringExtensions
    {
        public static string UnderscoreFirstCharToUpper(this string input)
        {
            switch (input)
            {
                case null: throw new ArgumentNullException(nameof(input));
                case "": throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input));
                default: return input[1].ToString().ToUpper() + input.Substring(2);
            }
        }
    }
}