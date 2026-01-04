using System;
using System.Text;

namespace CodeCop.Sharp.Utilities
{
    /// <summary>
    /// Provides utility methods for converting between naming conventions.
    /// Used by naming convention analyzers (CCS0001-CCS0005).
    /// </summary>
    public static class NamingUtilities
    {
        /// <summary>
        /// Converts a string to PascalCase by capitalizing the first character.
        /// </summary>
        /// <param name="name">The identifier name to convert.</param>
        /// <returns>The name with the first character uppercased, or the original if null/empty.</returns>
        /// <example>
        /// ToPascalCase("myMethod") returns "MyMethod"
        /// ToPascalCase("MyMethod") returns "MyMethod"
        /// ToPascalCase("m") returns "M"
        /// </example>
        public static string ToPascalCase(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }

            if (char.IsUpper(name[0]))
            {
                return name;
            }

            return char.ToUpperInvariant(name[0]) + name.Substring(1);
        }

        /// <summary>
        /// Converts a string to camelCase by lowercasing the first character.
        /// </summary>
        /// <param name="name">The identifier name to convert.</param>
        /// <returns>The name with the first character lowercased, or the original if null/empty.</returns>
        /// <example>
        /// ToCamelCase("MyField") returns "myField"
        /// ToCamelCase("myField") returns "myField"
        /// ToCamelCase("M") returns "m"
        /// </example>
        public static string ToCamelCase(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }

            if (char.IsLower(name[0]))
            {
                return name;
            }

            return char.ToLowerInvariant(name[0]) + name.Substring(1);
        }

        /// <summary>
        /// Converts a string to UPPER_SNAKE_CASE.
        /// Inserts underscores before uppercase letters (except at the start)
        /// and converts all characters to uppercase.
        /// </summary>
        /// <param name="name">The identifier name to convert.</param>
        /// <returns>The name in UPPER_SNAKE_CASE format, or the original if null/empty.</returns>
        /// <example>
        /// ToUpperSnakeCase("maxSize") returns "MAX_SIZE"
        /// ToUpperSnakeCase("MaxSize") returns "MAX_SIZE"
        /// ToUpperSnakeCase("apiKey") returns "API_KEY"
        /// ToUpperSnakeCase("HTTPClient") returns "HTTPCLIENT"
        /// </example>
        public static string ToUpperSnakeCase(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }

            var result = new StringBuilder(name.Length + 5);

            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];

                // Insert underscore before uppercase letters (except first char)
                // but not if the previous character was already uppercase (handles acronyms)
                if (i > 0 && char.IsUpper(c))
                {
                    char prev = name[i - 1];
                    if (!char.IsUpper(prev) && prev != '_')
                    {
                        result.Append('_');
                    }
                }

                result.Append(char.ToUpperInvariant(c));
            }

            return result.ToString();
        }

        /// <summary>
        /// Checks if a string starts with an uppercase letter.
        /// </summary>
        /// <param name="name">The identifier name to check.</param>
        /// <returns>True if the first character is uppercase; otherwise, false.</returns>
        /// <example>
        /// StartsWithUpperCase("MyClass") returns true
        /// StartsWithUpperCase("myClass") returns false
        /// StartsWithUpperCase("_myClass") returns false
        /// </example>
        public static bool StartsWithUpperCase(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            return char.IsUpper(name[0]);
        }

        /// <summary>
        /// Checks if a string starts with a lowercase letter.
        /// </summary>
        /// <param name="name">The identifier name to check.</param>
        /// <returns>True if the first character is lowercase; otherwise, false.</returns>
        /// <example>
        /// StartsWithLowerCase("myClass") returns true
        /// StartsWithLowerCase("MyClass") returns false
        /// StartsWithLowerCase("_myClass") returns false
        /// </example>
        public static bool StartsWithLowerCase(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            return char.IsLower(name[0]);
        }
    }
}
