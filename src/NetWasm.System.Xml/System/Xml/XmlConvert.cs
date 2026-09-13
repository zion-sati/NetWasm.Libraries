// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Name and date conversion follows the pinned XML facade contract. Culture and
// runtime discovery are intentionally not used by this profile.

using System.Globalization;
using System.Text;

namespace System.Xml;

public static class XmlConvert
{
    public static string EncodeName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        var builder = new StringBuilder(name.Length);
        for (var index = 0; index < name.Length; index++)
        {
            var character = name[index];
            if (IsNameCharacter(character, index))
            {
                builder.Append(character);
            }
            else
            {
                builder.Append("_x");
                builder.Append(((int)character).ToString("X4", CultureInfo.InvariantCulture));
                builder.Append('_');
            }
        }

        return builder.ToString();
    }

    public static string DecodeName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        var builder = new StringBuilder(name.Length);
        for (var index = 0; index < name.Length; index++)
        {
            if (index + 6 < name.Length && name[index] == '_' && name[index + 1] == 'x' && name[index + 6] == '_')
            {
                builder.Append((char)Convert.ToInt32(name.Substring(index + 2, 4), 16));
                index += 6;
            }
            else
            {
                builder.Append(name[index]);
            }
        }

        return builder.ToString();
    }

    public static string ToString(DateTime value) => value.ToString("O", CultureInfo.InvariantCulture);

    public static DateTime ToDateTime(string s)
    {
        ArgumentNullException.ThrowIfNull(s);
        return DateTime.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }

    private static bool IsNameCharacter(char value, int index) =>
        value is '_' or '-' or '.' or ':' ||
        char.IsLetter(value) ||
        (index > 0 && char.IsDigit(value));
}
