// ReSharper disable InvalidXmlDocComment
/** \file
 
 *  \author Rick Runowski
 *  \date   3/1/2025
 *  \brief  The namespace Library contains general functions used throughout EAGLE Applications.
 */

#region Declarations
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using Dapper;
using Dapper.Contrib.Extensions;
using JetBrains.Annotations;
using static System.Threading.Tasks.Task;
using static EAGLE.Library.FileLogging;
using Microsoft.Extensions.Configuration;
#endregion Declarations

// ReSharper disable HeuristicUnreachableCode
#pragma warning disable CS8794 // The input always matches the provided pattern.

// ReSharper disable once HeuristicUnreachableCode
// ReSharper disable once InvalidXmlDocComment
/** 
 *  \brief The namespace EAGLE.Library contains general functions used throughout Applications.
 *
 */
namespace EAGLE.Library;

[PublicAPI]
/** 
 *  \short a class to use for connecting to our databases.
 */
public class DbConnections : IDisposable
{

    /*****************************  CHANGE for PROD/DEV ****************************************/

    /**
     * @brief Defines the possible application environments.
     *
     * This enumeration is used to distinguish between different deployment environments
     * for the application, which may affect configuration settings, logging behavior,
     * feature toggles, and other environment-specific logic.
     *
     * @enum Environments
     *
     * @var Production
     * The live environment used by end users. Typically has the highest level of stability and monitoring.
     *
     * @var Stage
     * The staging environment used for pre-production testing. Mirrors production as closely as possible.
     *
     * @var Development
     * The development environment used by developers for building and testing new features.
     *
     * @note This enum is declared as `private`, meaning it is only accessible within the containing class or file.
     */
    private enum Environments
    {
        Production,
        Stage,
        Development,
    }


    // private const Environments Environment = Environments.Development;
    private const Environments Environment = Environments.Stage;
    // private const Environments Environment = Environments.Production;

    /*****************************  CHANGE for PROD/DEV ****************************************/


    private static IConfigurationRoot _config = new ConfigurationBuilder()
        .AddUserSecrets<DbConnections>() // Load secrets
        .Build();

    #region SQL SETTINGS

    private const int MaxPoolSize = 50;

    #region EXTERNAL
    private static readonly string DevServer = _config["AppSettings:DevServer"];
    private static readonly string ProdServer = _config["AppSettings:ProdServer"];
    private static readonly string DatabaseProd = _config["AppSettings:DatabaseProd"];
    private static readonly string DatabaseStage = _config["AppSettings:DatabaseStage"];
    private static readonly string DatabaseDev = _config["AppSettings:DatabaseDev"];
    private static readonly string Id = _config["AppSettings:DatabaseId"];
    private static readonly string Pass = _config["AppSettings:DatabaseId"];

    #endregion EXTERNAL

    /**
     * @brief Represents the minimum valid SQL Server `DateTime` value.
     *
     * SQL Server does not support dates earlier than January 1, 1753. This constant is used to ensure
     * compatibility with SQL Server when working with date values.
     *
     * @var SqlMinDate
     *
     * @note This value is declared as `readonly` and `static`, meaning it is initialized once and cannot be changed.
     *
     * @remark Edge cases include scenarios where application logic attempts to store or compare dates earlier than this value,
     * which would result in SQL exceptions or data truncation errors.
     */
    public static readonly DateTime SqlMinDate = new(1753, 1, 1);


    /**
     *  This is the connection string for ProdExt (Prod/Dev/Stage)
     */
    public static readonly string ConnectionString = $"""
													  Data Source={Environment switch
    {
        Environments.Production => ProdServer,
        _ => DevServer
    }};Initial Catalog={Environment switch
    {
        Environments.Production => DatabaseProd,
        Environments.Stage => DatabaseStage,
        _ => DatabaseDev,
    }};Min Pool Size=1;Max Pool Size={MaxPoolSize};Persist Security Info=True;User ID={Id};Password={Pass};Application Name=EAGLE.APP;MultiSubnetFailover=True;
													  """;
    #endregion SQL SETTINGS

    #region Check Environment
    // ReSharper disable once ConditionIsAlwaysTrueOrFalse
#pragma warning disable CS8519 // The given expression never matches the provided pattern.
    /**
 * @brief Determines if the system is set up for the Development environment.
 *
 * This function checks whether the current environment is either Development Testing.
 *
 * @return `true` if the system is in Development Testing; `false` otherwise.
 *
 * @exception None This function does not throw exceptions.
 *
 * @note Edge Cases:
 * - If `Environment` is `null`, this function may cause a runtime error unless properly handled elsewhere.
 * - If `Environment` is set to an unexpected value, the function will return `false`.
 */
    public static bool IsDevSet() =>
        Environment is Environments.Development;
#pragma warning restore CS8519 // The given expression never matches the provided pattern.

    /**
     * @brief Determines if the system is set up for the Stage environment.
     *
     * This function checks whether the current environment is explicitly set to Stage.
     *
     * @return `true` if the system is in the Stage environment; `false` otherwise.
     *
     * @exception None This function does not throw exceptions.
     *
     * @note Edge Cases:
     * - If `Environment` is `null`, this function may cause a runtime error unless properly handled elsewhere.
     * - If `Environment` is set to an unexpected value, the function will return `false`.
     */
    public static bool IsStgSet() =>
        Environment == Environments.Stage;

    /**
     * @brief Determines if the system is set up for the Production environment.
     *
     * This function checks whether the current environment is neither Development, Stage Testing.
     *
     * @return `true` if the system is in the Production environment; `false` otherwise.
     *
     * @exception None This function does not throw exceptions.
     *
     * @note Edge Cases:
     * - If `Environment` is `null`, this function may cause a runtime error unless properly handled elsewhere.
     * - If `Environment` is set to an unexpected value, the function will return `true`, assuming it is not explicitly Development, Stage Testing.
     */
    public static bool IsProduction() =>
        !IsStgSet() && !IsDevSet();

    #endregion Check Environment

    #region Standard SQL Connections

    /** 
     *  \short Inserts or Updates data using a parmeter list to pass data.
     *
     *  @param query            This is an SQL query.  It must be an Insert or an Update query.
     *  @param parameters       A list of SqlParameter objects containing the data to be inserted or updated.
     *
     *  Example Code:
     *  ~~~~~~~~~~~~~~~{.c#}
     *      string query = """
     *                  INSERT INTO {TableName} (Col1, Col2, ...)
     *                  VALUES (@Value1, @Value2, ...)
     *                  """;
     *      List<SqlParameter> parameters =
     *      [
     *          new SqlParameter("@Value1", somevalue),
     *          new SqlParameter("@Value2", SomeOtherValue),
     *          ... 
     *      ];
     *      int cnt = DbExt_Paramaterized_InsertUpdate(query, parameters);
     *
     *  ~~~~~~~~~~~~~~~
     */
    private static int DbSend(string query, object parameters, int timeout, string connectionString)
    {
        SqlConnection webDataConnection = new(connectionString);
        try
        {
            webDataConnection.Open();
            return timeout >= 0
                ? webDataConnection.Execute(query, parameters, commandTimeout: timeout)
                : webDataConnection.Execute(query, parameters);
        }
        catch (Exception ex)
        {
            if (ex.Message.ToLower().Contains("chosen as the deadlock victim")
            //     || ex.Message.ToLower().Contains("thread was being aborted")
                || ex.Message.ToLower().Contains("was deadlocked on lock")
            || ex.Message.ToLower().Contains("a connection was successfully established with the server, but then an error occurred during the pre-login handshake"))
                return DbSend(query, parameters, timeout, connectionString);
            throw;
        }
        finally
        {
            webDataConnection.Close();
            webDataConnection.Dispose();
        }
    }

    #endregion Standard SQL Connections
    /**
     * @brief Represents the minimum valid date for database operations.
     *
     * This value is set to January 1, 1753 at 00:00:00, which is the minimum `DateTime` value supported by SQL Server.
     * It is used to ensure compatibility with SQL Server when working with date values in database operations.
     *
     * @var DbMinDate
     *
     * @note This field is marked with `[PublicAPI]`, indicating it is intended for use by external consumers of the API.
     *
     * @remark Edge cases include attempts to insert or compare dates earlier than this value in SQL Server, which will result in exceptions or data truncation errors.
     *
     * @warning Modifying this value may lead to inconsistencies or runtime errors in systems relying on SQL Server's date constraints.
     */
    [PublicAPI]
    public static DateTime DbMinDate = new(1753, 1, 1, 0, 0, 0);


    #region Standard SQL Connections
    /**
     * @brief Executes a parameterized SQL insert or update command.
     *
     * This method sends a SQL query with parameters to the database using the default connection string.
     * It is typically used for executing insert or update operations where SQL injection protection is required.
     *
     * @param query The SQL query string to be executed. It should contain parameter placeholders (e.g., `@paramName`).
     * @param parameters An object containing the parameter values to be bound to the query.
     *
     * @return int The result of the `DbSend` operation, typically indicating the number of affected rows or a status code.
     *
     * @exception std::runtime_error Thrown if the database operation fails due to connection issues, malformed SQL, or parameter mismatches.
     *
     * @remark Edge cases include:
     * - Null or empty `query` string.
     * - `parameters` object being null or missing required fields.
     * - SQL syntax errors or constraint violations.
     *
     * @note This method uses a default timeout of `-1`, which may indicate no timeout or a system-defined default.
     *
     * @see DbSend
     */
    public static int DbExt_Parameterized_InsertUpdate(string query, object parameters) =>
        DbSend(query, parameters, -1, ConnectionString);

    /**
     * @brief Executes a parameterized SQL query and maps the result to a list of objects of type `T`.
     * 
     * This method opens a SQL connection using the default `ConnectionString`, executes the provided query with parameters,
     * and maps the result set to a list of objects of type `T` using Dapper. It includes retry logic for specific transient
     * connection errors.
     * 
     * @tparam T The type to which the result set rows will be mapped.
     * 
     * @param query The SQL query string to execute.
     * @param pageName The name of the page or context from which the call originates (used for logging).
     * @param functionName The name of the function making the call (used for logging).
     * @param parameters An object containing the parameters to bind to the SQL query.
     * 
     * @return List<T> A list of objects of type `T` representing the query result. Returns `null` if an unhandled exception occurs.
     * 
     * @exception System.Exception Catches and logs any exceptions that occur during query execution. Retries the operation for specific transient errors.
     * 
     * @remark Edge cases include:
     * - SQL connection timeouts or network-related errors, which trigger a retry.
     * - Query returning no results, resulting in an empty list.
     * - Mapping failures if the result set does not match the structure of type `T`.
     * 
     * @note 
     * - Uses a command timeout of 600 seconds.
     * - The connection is explicitly closed in the `finally` block.
     * - Logging and error handling are performed via the `ThrowMe` method.
     * 
     * @see ThrowMe
     */
    [PublicAPI]
    public static List<T> DbExt_ObjCall<T>(string query, string pageName, string functionName, object parameters)
    {
        using SqlConnection webDataConnection = new(ConnectionString);
        try
        {
            return webDataConnection.Query<T>(query, parameters, commandTimeout: 600).ToList();
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains(
                    "Connection Timeout Expired.  The timeout period elapsed while attempting to consume the pre-login handshake acknowledgement.  This could be because the pre-login handshake failed or the server was unable to respond back in time."))
                return DbExt_ObjCall<T>(query, pageName, functionName, parameters);
            if (ex.InnerException != null && ex.InnerException.Message.Contains("A network-related or instance-specific error occurred while establishing a connection to SQL Server. The server was not found or was not accessible."))
                return DbExt_ObjCall<T>(query, pageName, functionName, parameters);

            ThrowMe($"Errors in DbConnection.cs on line {new StackFrame(0, true).GetFileLineNumber()}.",
            [
                new("Query", query),
            new("parameters", parameters?.ToString())
            ], "System", ex);
            return null;
        }
        finally
        {
            webDataConnection.Close();
            // webDataConnection.Dispose();
        }
    }


    /**
     * @brief Saves an object to the database using Dapper.Contrib, performing an update or insert as needed.
     * 
     * This method attempts to update the given object in the database. If the update fails (returns false),
     * it falls back to inserting the object. If the object lacks a `[Key]` or `[ExplicitKey]` attribute,
     * it will directly attempt an insert.
     * 
     * @tparam T The type of the object being saved. Must be a reference type (`class`).
     * 
     * @param guid A string identifier for the operation, typically used for logging or tracking.
     * @param obj The object to be saved to the database.
     * @param traceBack A string used for tracing the origin of the call, useful for debugging or logging.
     * 
     * @return long Returns `1` if the update succeeds, or the result of the insert operation (typically the new record ID).
     * 
     * @exception System.Exception Thrown if the database connection fails or if an unexpected error occurs during update/insert.
     * 
     * @remark Edge cases include:
     * - The object not having a `[Key]` or `[ExplicitKey]` property, which causes the update to fail and triggers an insert.
     * - Insert or update failing due to constraint violations or missing required fields.
     * 
     * @note 
     * - Uses Dapper.Contrib's `Update` and `Insert` methods.
     * - The SQL connection is explicitly opened and closed within the method.
     * - Exceptions are wrapped with file and line number information for easier debugging.
     * 
     * @see Dapper.Contrib.Extensions.Update, Dapper.Contrib.Extensions.Insert, StackFrame
     */
    public static long DbExt_ObjSave<T>(string guid, T obj, string traceBack) where T : class
    {
        using SqlConnection webDataConnection = new(ConnectionString);
        try
        {
            webDataConnection.Open();
            try
            {
                return webDataConnection.Update(obj) ? 1 : webDataConnection.Insert(obj);
            }
            catch (Exception e)
            {
                if (e.Message == "Entity must have at least one [Key] or [ExplicitKey] property")
                    return webDataConnection.Insert(obj);

                throw;
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Errors in DbConnection.asmx.cs on line {new StackFrame(0, true).GetFileLineNumber()}.", ex);
        }
        finally
        {
            webDataConnection.Close();
            //webDataConnection.Dispose();
        }
    }


    /**
     * @brief Executes a parameterized SQL query that returns a single `Guid` value.
     * 
     * This method opens a SQL connection using the default `ConnectionString`, executes the provided query with parameters,
     * and returns the resulting `Guid`. It includes nested exception handling for query execution and connection issues.
     * 
     * @param guid A string representation of a GUID, typically used for tracking or logging (not used in logic here).
     * @param query The SQL query expected to return a single `Guid` value.
     * @param traceBack A string used for tracing the origin of the call, useful for debugging or logging.
     * @param parameters An object containing the parameters to bind to the SQL query.
     * 
     * @return Guid The `Guid` returned by the query.
     * 
     * @exception System.Exception Thrown if the connection fails or if the query does not return exactly one result.
     * 
     * @remark Edge cases include:
     * - Query returning zero or multiple results, which will cause `Single()` to throw.
     * - SQL connection issues or malformed queries.
     * - `parameters` being null or not matching the query's expected inputs.
     * 
     * @note 
     * - The method uses `Dapper` for query execution and object mapping.
     * - The SQL connection is explicitly opened and closed within the method.
     * - Exceptions are wrapped with file and line number information for easier debugging.
     * 
     * @see SqlConnection, Dapper.Query, StackFrame
     */
    public static Guid DbExt_CreateGuid(string guid, string query, string traceBack, object parameters)
    {
        using SqlConnection webDataConnection = new(ConnectionString);
        try
        {
            webDataConnection.Open();
            try
            {
                return webDataConnection.Query<Guid>(query, parameters).Single();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Errors in DbConnection.asmx.cs on line {new StackFrame(0, true).GetFileLineNumber()}.", ex);
        }
        finally
        {
            webDataConnection.Close();
            //webDataConnection.Dispose();
        }
    }

    /**
     * @brief Saves an object to the database and returns a boolean indicating success.
     *
     * This method wraps `DbExt_ObjSave` and returns `true` if the save operation (insert or update) was successful,
     * i.e., if the result is greater than or equal to 0.
     *
     * @tparam T The type of the object being saved. Must be a reference type (`class`).
     *
     * @param guid A string identifier for the operation, typically used for logging or tracking.
     * @param obj The object to be saved to the database.
     * @param traceBack A string used for tracing the origin of the call, useful for debugging or logging.
     *
     * @return bool `true` if the object was saved successfully (inserted or updated), otherwise `false`.
     *
     * @exception System.Exception Any exceptions thrown by `DbExt_ObjSave` will propagate through this method.
     *
     * @remark Edge cases include:
     * - The object not having a `[Key]` or `[ExplicitKey]` property, which may cause an update to fail and trigger an insert.
     * - Insert or update failing due to constraint violations or missing required fields.
     *
     * @note This method is a convenience wrapper for `DbExt_ObjSave` when only a success/failure result is needed.
     *
     * @see DbExt_ObjSave
     */
    public static bool DbExt_ObjSaveBool<T>(string guid, T obj, string traceBack) where T : class =>
        DbExt_ObjSave(guid, obj, traceBack) >= 0;

    /**
     * @brief Executes a parameterized SQL command, optionally asynchronously, and returns a success status.
     * 
     * This method sends a SQL command to the database using the default connection string. It supports both
     * synchronous and asynchronous execution. The result is considered successful if at least one record is affected,
     * or if `zeroRec` is set to `true`.
     * 
     * @param query The SQL query string to execute.
     * @param parameters An object containing the parameters to bind to the SQL query.
     * @param isAsync If `true`, the query is executed asynchronously using a background thread. Defaults to `false`.
     * @param timeout The command timeout in seconds. A value of `-1` uses the default timeout. Defaults to `-1`.
     * @param zeroRec If `true`, the method returns `true` even if no records are affected. Defaults to `false`.
     * 
     * @return bool `true` if the operation is successful (based on affected rows or `zeroRec`), otherwise `false`.
     * 
     * @exception System.Exception Thrown if the database operation fails. If the exception message indicates a thread abort,
     * the method retries the operation once recursively.
     * 
     * @remark Edge cases include:
     * - SQL command affecting zero rows when `zeroRec` is `false`, resulting in `false`.
     * - Thread abort exceptions, which trigger a retry.
     * - Asynchronous execution errors may not be caught immediately.
     * 
     * @note 
     * - Uses `DbSend` for actual database interaction.
     * - Asynchronous execution is handled via a `Run` delegate or method (assumed to be defined elsewhere).
     * - Recursive retry on thread abort should be used cautiously to avoid infinite loops.
     * 
     * @see DbSend, Run
     */
    public static bool DbExt_Send(string query, object parameters, bool isAsync = false, int timeout = -1, bool zeroRec = false)
    {
        try
        {
            if (!isAsync)
                return DbSend(query, parameters, timeout, ConnectionString) >= 1 || zeroRec;

            Run(() => DbSend(query, parameters, timeout, ConnectionString) >= 1 || zeroRec);
            return true;
        }
        catch (Exception ex)
        {
            if (ex.Message.ToLower().Contains("thread was being aborted"))
                DbExt_Send(query, parameters, isAsync, timeout, zeroRec);
            throw;
        }
    }

    #endregion Standard SQL Connections

    #region helpers
    /**
     * @brief Ensures that a given `DateTime` value is not earlier than the minimum SQL Server date.
     *
     * This method checks if the provided `dateValue` is earlier than `SqlMinDate` (January 1, 1753).
     * If it is, the method returns `SqlMinDate`; otherwise, it returns the original `dateValue`.
     *
     * @param dateValue The `DateTime` value to validate.
     *
     * @return DateTime The original `dateValue` if it is valid for SQL Server; otherwise, `SqlMinDate`.
     *
     * @remark Edge cases include:
     * - Passing `DateTime.MinValue`, which will be corrected to `SqlMinDate`.
     * - Using this method before inserting dates into SQL Server to avoid runtime exceptions.
     *
     * @note This method helps prevent SQL Server exceptions caused by unsupported date values.
     *
     * @see SqlMinDate
     */
    public static DateTime FixDate(DateTime dateValue) =>
        dateValue < SqlMinDate ? SqlMinDate : dateValue;


    /**
     * @brief Ensures the provided nullable DateTime value is valid within SQL constraints.
     *
     * This function checks if the given nullable DateTime value is earlier than the SQL Server
     * minimum allowed date (`SqlMinDate`). If so, it returns `null`; otherwise, it returns the
     * original value.
     *
     * @param dateValue The nullable DateTime value to validate.
     * @return A nullable DateTime that is either `null` (if the input is below `SqlMinDate`)
     *         or the original value.
     *
     * @exception None This function does not throw exceptions.
     *
     * @note Edge Cases:
     * - If `dateValue` is `null`, the function simply returns `null`.
     * - If `dateValue` is exactly `SqlMinDate`, it is considered valid and returned as-is.
     * - If `dateValue` is slightly below `SqlMinDate`, it is replaced with `null`, which may
     *   have implications for database operations expecting non-null values.
     */
    public static DateTime? FixDateNullable(DateTime? dateValue) =>
        dateValue < SqlMinDate ? null : dateValue;

    #endregion helpers

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}

