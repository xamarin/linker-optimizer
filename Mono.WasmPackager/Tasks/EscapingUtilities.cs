﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Copied from msbuild/src/Shared/EscapingUtilities.cs and slightly adjusted.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Microsoft.Build.Shared
{
    /// <summary>
    /// This class implements static methods to assist with unescaping of %XX codes
    /// in the MSBuild file format.
    /// </summary>
    /// <remarks>
    /// PERF: since we escape and unescape relatively frequently, it may be worth caching
    /// the last N strings that were (un)escaped
    /// </remarks>
    static internal class EscapingUtilities
    {
        /// <summary>
        /// Optional cache of escaped strings for use when needing to escape in performance-critical scenarios with significant
        /// expected string reuse.
        /// </summary>
        private static Dictionary<string, string> s_unescapedToEscapedStrings = new Dictionary<string, string>(StringComparer.Ordinal);

        private static bool IsHexDigit(char character)
        {
            return ((character >= '0') && (character <= '9'))
                || ((character >= 'A') && (character <= 'F'))
                || ((character >= 'a') && (character <= 'f'));
        }

        /// <summary>
        /// Adds instances of %XX in the input string where the char to be escaped appears
        /// XX is the hex value of the ASCII code for the char.
        /// </summary>
        /// <param name="unescapedString">The string to escape.</param>
        /// <returns>escaped string</returns>
        internal static string Escape(string unescapedString)
        {
            return EscapeWithOptionalCaching(unescapedString, cache: false);
        }

        /// <summary>
        /// Adds instances of %XX in the input string where the char to be escaped appears
        /// XX is the hex value of the ASCII code for the char.  Caches if requested.
        /// </summary>
        /// <param name="unescapedString">The string to escape.</param>
        /// <param name="cache">
        /// True if the cache should be checked, and if the resultant string
        /// should be cached.
        /// </param>
        private static string EscapeWithOptionalCaching(string unescapedString, bool cache)
        {
            // If there are no special chars, just return the original string immediately.
            // Don't even instantiate the StringBuilder.
            if (String.IsNullOrEmpty(unescapedString) || !ContainsReservedCharacters(unescapedString))
            {
                return unescapedString;
            }

            // next, if we're caching, check to see if it's already there.
            if (cache)
            {
                string cachedEscapedString = null;
                lock (s_unescapedToEscapedStrings)
                {
                    if (s_unescapedToEscapedStrings.TryGetValue(unescapedString, out cachedEscapedString))
                    {
                        return cachedEscapedString;
                    }
                }
            }

            // This is where we're going to build up the final string to return to the caller.
            StringBuilder escapedStringBuilder = new StringBuilder(unescapedString.Length * 2);

            AppendEscapedString(escapedStringBuilder, unescapedString);

            if (!cache)
            {
                return escapedStringBuilder.ToString();
            }

            string escapedString = escapedStringBuilder.ToString();

            lock (s_unescapedToEscapedStrings)
            {
                s_unescapedToEscapedStrings[unescapedString] = escapedString;
            }

            return escapedString;
        }

        /// <summary>
        /// Before trying to actually escape the string, it can be useful to call this method to determine
        /// if escaping is necessary at all.  This can save lots of calls to copy around item metadata
        /// that is really the same whether escaped or not.
        /// </summary>
        /// <param name="unescapedString"></param>
        /// <returns></returns>
        private static bool ContainsReservedCharacters
            (
            string unescapedString
            )
        {
            return (-1 != unescapedString.IndexOfAny(s_charsToEscape));
        }

        /// <summary>
        /// Determines whether the string contains the escaped form of '*' or '?'.
        /// </summary>
        /// <param name="escapedString"></param>
        /// <returns></returns>
        internal static bool ContainsEscapedWildcards
            (
            string escapedString
            )
        {
            if (-1 != escapedString.IndexOf('%'))
            {
                // It has a '%' sign.  We have promise.
                if (
                        (-1 != escapedString.IndexOf("%2", StringComparison.Ordinal)) ||
                        (-1 != escapedString.IndexOf("%3", StringComparison.Ordinal))
                    )
                {
                    // It has either a '%2' or a '%3'.  This is looking very promising.
                    return
                        (
                            (-1 != escapedString.IndexOf("%2a", StringComparison.Ordinal)) ||
                            (-1 != escapedString.IndexOf("%2A", StringComparison.Ordinal)) ||
                            (-1 != escapedString.IndexOf("%3f", StringComparison.Ordinal)) ||
                            (-1 != escapedString.IndexOf("%3F", StringComparison.Ordinal))
                        );
                }
            }
            return false;
        }

        /// <summary>
        /// Convert the given integer into its hexadecimal representation.
        /// </summary>
        /// <param name="x">The number to convert, which must be non-negative and less than 16</param>
        /// <returns>The character which is the hexadecimal representation of <paramref name="x"/>.</returns>
        private static char HexDigitChar(int x)
        {
            return (char)(x + (x < 10 ? '0' : ('a' - 10)));
        }

        /// <summary>
        /// Append the escaped version of the given character to a <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="sb">The <see cref="StringBuilder"/> to which to append.</param>
        /// <param name="ch">The character to escape.</param>
        private static void AppendEscapedChar(StringBuilder sb, char ch)
        {
            // Append the escaped version which is a percent sign followed by two hexadecimal digits
            sb.Append('%');
            sb.Append(HexDigitChar(ch / 0x10));
            sb.Append(HexDigitChar(ch & 0x0F));
        }

        /// <summary>
        /// Append the escaped version of the given string to a <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="sb">The <see cref="StringBuilder"/> to which to append.</param>
        /// <param name="unescapedString">The unescaped string.</param>
        private static void AppendEscapedString(StringBuilder sb, string unescapedString)
        {
            // Replace each unescaped special character with an escape sequence one
            for (int idx = 0; ;)
            {
                int nextIdx = unescapedString.IndexOfAny(s_charsToEscape, idx);
                if (nextIdx == -1)
                {
                    sb.Append(unescapedString, idx, unescapedString.Length - idx);
                    break;
                }

                sb.Append(unescapedString, idx, nextIdx - idx);
                AppendEscapedChar(sb, unescapedString[nextIdx]);
                idx = nextIdx + 1;
            }
        }

        /// <summary>
        /// Special characters that need escaping.
        /// It's VERY important that the percent character is the FIRST on the list - since it's both a character
        /// we escape and use in escape sequences, we can unintentionally escape other escape sequences if we
        /// don't process it first. Of course we'll have a similar problem if we ever decide to escape hex digits
        /// (that would require rewriting the algorithm) but since it seems unlikely that we ever do, this should
        /// be good enough to avoid complicating the algorithm at this point.
        /// </summary>
        // MARTIN 05/12/2020: added the double-quote '"' to the list.
        private static readonly char[] s_charsToEscape = { '%', '*', '?', '@', '$', '(', ')', ';', '\'', '"' };
    }
}
