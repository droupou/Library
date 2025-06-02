#region Declarations
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
// ReSharper disable InvalidXmlDocComment

#endregion Declarations

namespace EAGLE.Library;

[PublicAPI]
/**
 *  /brief Contains a number of useful functions that are used ubiquitously in Code.
 *
 *  This is the class that contains all of the Commonly used functions that are used or may be used across multiple other classes and namespaces.
 */
public class Common
{
    public static string AppGuid = "System"; /*! The Aplication's Identifying GUID. */

    /** 
     * This function provides the version of the current set of DLL's.
     */
    public string GetVersion() =>
        FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;

    /** 
     *
     * This function will strip all duplicate email addresses across the provided strings.  This string typical is comma (,) or semicolon (;) delimited.
     * @param toEmail a list of emails intended for the To line
     * @param ccEmail a list of emails intended for the CC line
     * @param bccEmail a list of emails intended for the BCC line
     * @param separators a character array containing the possible separators used in this list.
     *
     *Example Code:
     *  ~~~~~~~~~~~~~~~{.c#}
     *      string emailToList = "email@address.com, email@address2.com, email@address.com, someOtherEmail@address.com";
     *      string emailCCList = "email@address.com; email@address2.com; email@address.com; someOtherEmail@address.com";
     *      string emailBCCList = "email@address.com, email@address2.com, email@address.com, someOtherEmail@address.com";
     *
     *      StripDuplicateEmailAddresses(emailToList, emailCCList, emailBCCList, [',', ';'])
     *  ~~~~~~~~~~~~~~~
     */
    public static void StripDuplicateEmailAddresses(ref string toEmail, ref string ccEmail, ref string bccEmail,
        char[] separators)
    {
        List<string> emailList = [];


        #region Process To/CC/BCC for duplicate addresses
        List<string> addressList;
        if (!string.IsNullOrEmpty(toEmail))
        {
            addressList = toEmail.Split(separators).ToList();
            addressList = addressList.Select(a => a.Trim()).Distinct().ToList();
            //addressList.RemoveAll(a => emailList.Any(e => e == a));
            emailList.AddRange(addressList);
            toEmail = string.Join(",", addressList);
        }

        if (!string.IsNullOrEmpty(ccEmail))
        {
            addressList = ccEmail.Split(separators).ToList();
            addressList = addressList.Select(a => a.Trim()).Distinct().ToList();
            addressList.RemoveAll(a => emailList.Any(e => e == a));
            emailList.AddRange(addressList);
            ccEmail = string.Join(",", addressList);
        }

        if (string.IsNullOrEmpty(bccEmail)) return;

        addressList = bccEmail.Split(separators).ToList();
        addressList = addressList.Select(a => a.Trim()).Distinct().ToList();
        addressList.RemoveAll(a => emailList.Any(e => e == a));
        //emailList.AddRange(addressList);
        bccEmail = string.Join(",", addressList);
        #endregion Process To for duplicate addresses
    }

    /** 
     * This function will take an integer value, and add th, st, nd, rd to the value.
     *
     * @param number is the number to add the suffix to.
     *
     *Example Code:
     *  ~~~~~~~~~~~~~~~{.c#}
     *      string result = IntToTh(1);     // result = 1st
     *      result = IntToTh(2);            // result = 2nd
     *      result = IntToTh(3);            // result = 3rd
     *      ... 
     *  ~~~~~~~~~~~~~~~
     */
    public static string IntToTh(int number)
    {
        int ones = number % 10;
        double tens = Math.Floor(number / 10f) % 10;
        if (Math.Abs(tens - 1) < .000000001)
        {
            return number + "th";
        }

        return ones switch
        {
            1 => number + "st",
            2 => number + "nd",
            3 => number + "rd",
            _ => number + "th"
        };
    }

    public static string CamelCase(string name)
    {
        name = $"{name.First().ToString().ToUpper()}{name.Remove(0, 1)}";
        if (!name.Contains("_"))
            return name;

        string temp = name.Split('_').Aggregate<string, string>(null, (current, n) => current + $"{CamelCase(n)}_");

        return temp.Substring(0, temp.Length - 1);
    }
}
