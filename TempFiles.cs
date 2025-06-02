using System;
using System.IO;
using JetBrains.Annotations;

// ReSharper disable InvalidXmlDocComment

namespace EAGLE.Library;

[PublicAPI]
/** \class TempFiles
 *  \brief Generates a path to use when generating a Temporary File
 */
public class TempFiles
{
    /** 
     *  \brief Gets a temporary path to store documents in that the system will automatically delete during a later GC.
     */
    public static string GetTempPath(string appName) =>
        $"{Path.GetTempPath()}_{appName}_{Randomizer.RandomString(20, (int)DateTime.Now.Ticks)}";

}