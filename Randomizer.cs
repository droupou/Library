using System;
using System.Linq;
using JetBrains.Annotations;

// ReSharper disable InvalidXmlDocComment

namespace EAGLE.Library;

[PublicAPI]
/** \class Randomizer
 *  \brief Generates Random strings of alphanumeric characters
 */
public class Randomizer
{
    #region Randomizers

    /** 
     *  \brief produces a random string of a given length.
     *
     *  @param  length      How many characters you want the string to be.
     *  @param  seed        A string to use as the seed for the randomization.  This defaults to null.
     */
    public static string RandomStringTxt(int length, string seed = null)
    {
        seed ??= ((int)DateTime.Now.Ticks & 0x0000FFFF).ToString();

        return RandomString(length, seed.Aggregate(0, (current, a) => current + a));
    }

    /** 
     *  \brief produces a random string of a given length.
     *
     *  This will return a string of alphanumeric characters (A-Z, a-z, 0-9)
     *
     *  @param length   The length of the returned string.
     *  @param seed     A seed value to help with randomization.  Default is -1.
     */
    public static string RandomString(int length, int seed = -1)
    {
        // ReSharper disable StringLiteralTypo
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        // ReSharper restore StringLiteralTypo
        char[] stringChars = new char[length];
        if (seed == -1)
            seed = (int)DateTime.Now.Ticks & 0x0000FFFF;
        Random random = new(seed);

        for (int i = 0; i < stringChars.Length; i++)
            stringChars[i] = chars[random.Next(chars.Length)];

        return new string(stringChars);
    }
    
    /** 
     *  \brief produces a random string of a given length.
     *
     *  This will return a string of alphanumeric and special characters (A-Z, a-z, 0-9, and -@$^&%!#*)
     *
     *  @param  length      How many characters you want the string to be.
     *  @param  seed        A string to use as the seed for the randomization.  This defaults to null.
     */
    public static string RandomPassword(int length, int seed = -1)
    {
        // ReSharper disable StringLiteralTypo
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-@$^&%!#*";
        // ReSharper restore StringLiteralTypo
        char[] stringChars = new char[length];
        if (seed == -1)
            seed = (int)DateTime.Now.Ticks & 0x0000FFFF;
        Random random = new(seed);

        for (int i = 0; i < stringChars.Length; i++)
            stringChars[i] = chars[random.Next(chars.Length)];

        return new string(stringChars);
    }

    /**
     *  \brief produces a random string of a given length.
     *
     *  This will return a string of alphanumeric and special characters (A-Z, a-z, 0-9, and -@$^&%!#*)
     *
     *  @param  length      How many characters you want the string to be.
     *  @param  seed        A DateTime to use as the seed for the randomization..
     */
    public static string RandomPassword(int length, DateTime seed) =>
        RandomPassword(length, int.Parse(seed.ToString("O").Replace("-", "")
            .Replace("T", "").Replace(".", "").Replace(":", "").Substring(0, 10)));
    #endregion Randomizers
}