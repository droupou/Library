using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using static System.IO.File;
using static EAGLE.Library.Common;

// ReSharper disable InvalidXmlDocComment

namespace EAGLE.Library;

[PublicAPI]
/** \class FileLogging
 *  \brief Writes logging inforamtion to a local text file.
 *
 *  This will write a file to C:\\inetpub\\logs\\weblogs\\Service_Logging.log on the computer it is run from.  This is highly
 *  useful if the database connection goes down, we can still get logging information.
 */
public class FileLogging
{
    private static readonly object LogLock = new();
    //static ReaderWriterLock _locker = new();
    //private static Mutex _mutex;

    //public const string LoggingTable = "Logs";
    private const long MaxLength = 1073741824 / 2; // 1 Gig is 1,073,741,824     /*! The maximum length of the file on the Server. */

    /** \enum LogType
     *
     */
    [PublicAPI]
    public enum LogType
    {
        Critical,   /**< This is for serious bugs that are not caused by the Users. */
        Error,      /**< This is for Errors that users generate by entering bad data, or doing things they shouldn't */
        Info,       /**< This is for Inforamtion you want to track in your system. */
        Debug       /**< This is for debugging and should be removed (or commented) from your code before moving to production. */
    }

    private static void WriteFileLog(string message)
    {
        //bool createdNew = false;
        try
        {
            // ReSharper disable StringLiteralTypo
            const string destPath = @"C:\inetpub\logs\weblogs\Service_Logging.log";
            // ReSharper restore StringLiteralTypo
            lock (LogLock)
            {
                #region Keep total file size under MaxFileSize * 2

                FileInfo fi = new(destPath);
                if (fi.Exists && MaxLength <= fi.Length)
                {
                    string newDestPath = $"{destPath.Split('.')[0]}_2.log";
                    if (Exists(newDestPath))
                        Delete(newDestPath);
                    Move(destPath, newDestPath);
                }

                #endregion Keep total file size under MaxFileSize * 2

                #region Write to log file

                AppendAllText(destPath,
                    "*************************************************************************************\r\n");
                AppendAllText(destPath, $"""
                                         Logged on {DateTime.Now:G}
                                                         
                                             
                                         """);
                AppendAllText(destPath, $"""
                                         {message}
                                             
                                         """);
                AppendAllText(destPath, "\r\n");

                #endregion Write to log file
            }
        }
#pragma warning disable CS0168
        catch (Exception ex)
#pragma warning restore CS0168
        {
            // ReSharper disable once EmptyStatement
            ;
        }
    }

    /** 
     *  \brief Writes the message to the log file, then throws an exception.
     *
     *  Writes the message to the log file, then throws an exception to be handled by your code later.  This is a standard exception.
     * 
     *  @param msg              The message to write to the log file.
     *  @param dataDictionary   A list of [Data State](#DataState) values containing the inforamtion currently in memory when this occurred.
     *  @param sessionId        The Unique ID of the session to help track which records go with which events.
     *  @param ex               An exception to make the inner exception of the current exception being thrown.
     *  @param origPage         The file from which this function is being called.
     *
     *  Example Code:
     *  ~~~~~~~~~~~~~~~{.c#}
     *      if ("good thing happens")
     *          DoSomething();
     *      else    // Ooops
     *          ThrowMe("Invalid data returned from database.", dataDictionary, sessionId,
     *              origPage: $"{PageName}:{GetCurrentMethod()?.Name}");
     *  ~~~~~~~~~~~~~~~
     */
    public static void ThrowMe(string msg, List<DataState> dataDictionary, string sessionId,
        Exception ex = null, string origPage = null)
    {
        FsWriteLog(AppGuid, LogType.Critical, "Exception thrown: " + msg, origPage ?? "Libraries:FileLogging.cs:ThrowMe",
            dataDictionary, ex?.StackTrace, sessionId);
        throw new Exception(msg, ex);
    }

    /** 
     *  \brief Produces a string from the dataDictionary
     *
     *  This function will produce a string that displays the data dictionary.  One entry per line.  This will be formated
     *      like:
     *          Name: {Data}
     *
     *  @param dataDictionary   A list of DataState objects containing the data intended for printing.
     */
    private static string DictionaryToString(IEnumerable<DataState> dataDictionary) =>
        dataDictionary == null ? "No Data Provided." : dataDictionary.Aggregate("", (current, pair) => current + $"""
             
                 {pair.Param}: {pair.Data}
             """);

    /**
     * @brief Writes a formatted log entry to the file system.
     * 
     * This method constructs a detailed log message including the log type, message content, session ID,
     * data dictionary, optional stack trace, GUID, and calling page. It then writes the message to a log file.
     * 
     * @param guid A unique identifier for the user or system context. If null or "JS", it defaults to `AppGuid`.
     * @param logType The severity or category of the log (e.g., Info, Warning, Error, Critical).
     * @param message The main message content to be logged.
     * @param caller The name of the calling page or method.
     * @param dataDictionary A list of `DataState` objects representing contextual data to include in the log.
     * @param sessionId The session identifier associated with the current request.
     * @param stackTrace (Optional) A stack trace string to include in the log for debugging purposes.
     * 
     * @return void
     * 
     * @remark Edge cases include:
     * - `guid` being null or "JS", which triggers fallback to `AppGuid`.
     * - `stackTrace` being null or whitespace, in which case it is omitted from the log.
     * 
     * @note 
     * - The method uses `DictionaryToString` to serialize the `dataDictionary`.
     * - The final log message is written using `WriteFileLog`.
     * 
     * @see DictionaryToString, WriteFileLog
     * 
     * @code
     * // Example usage:
     * var data = new List<DataState>
     * {
     *     new DataState("UserId", "12345"),
     *     new DataState("Action", "LoginAttempt")
     * };
     * 
     * FsWriteLog(
     *     guid: "abc-123-guid",
     *     logType: LogType.Warning,
     *     message: "User login failed due to invalid credentials.",
     *     caller: "LoginPage",
     *     dataDictionary: data,
     *     sessionId: "session-xyz-789",
     *     stackTrace: Environment.StackTrace
     * );
     * @endcode
     */
    public static void FsWriteLog(string guid, LogType logType, string message, string caller, List<DataState> dataDictionary, string sessionId,
        string stackTrace = null)
    {
        if (guid is null or "JS")
            guid = AppGuid;

        message = $"""
               {logType} Event:

               {message}
               
               {sessionId}

               -------------------------------------
               Data:
               """;
        message += DictionaryToString(dataDictionary);
        if (!string.IsNullOrWhiteSpace(stackTrace))
            message += $"""

                    -------------------------------------

                    Stack Trace:

                    {stackTrace}
                    """;

        message += $"""

                Guid: {guid}
                Calling Page: {caller}
                """;

        WriteFileLog(message);
    }


    /** 
     *  \brief Writes the message to the log file without throwing an exception.
     *
     *  Writes the message to the log file.
     *
     *  @param msg              The message to write to the log file.
     *  @param caller           The Page Name and Function Name of the calling function.
     *  @param dataDictionary   A list of DataState objects containing the data intended for printing.
     *  @param sessionId        The Unique ID of the session to help track which records go with which events.
     *
     *  Example Code:
     *  ~~~~~~~~~~~~~~~{.c#}
     *      SInfo("Something you want to see in the log table without actually causing an error.", caller, dataDictionary, sessionId);
     *  ~~~~~~~~~~~~~~~
     */
    public static void SInfo(string msg, string caller, List<DataState> dataDictionary, string sessionId)
    {
        string debugMsg = $"""
                           
                           DEBUG: {msg}
                   
                               Common.throwMe
                           """;
        FsWriteLog(AppGuid, LogType.Info, debugMsg, caller, dataDictionary, null, sessionId);
    }

    /** \class DataState
     *  \brief This is a data element that holds the name and value of data pieces for debugging issues caused in production environment.
     *
     */
    public sealed class DataState : IDisposable
    {
        #region Declarations
        public string Param { get; set; }   /**< The name of the parameter. */
        public string Data { get; set; }    /**< The value of the parameter. */
        #endregion Declarations


        public DataState(string param, string data)
        {
            Param = param;
            Data = data;
        }

        public DataState()
        {
        }

        #region Dispose
        // ReSharper disable once UnusedMember.Global
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        ~DataState()
        {
            // Finalizer calls Dispose(false)
            Dispose(false);
        }
        // The bulk of the clean-up code is implemented in Dispose(bool)
        private static void Dispose(bool disposing)
        {
        }
        #endregion Dispose
    }
}