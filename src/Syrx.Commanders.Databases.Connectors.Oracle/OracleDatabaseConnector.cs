using Oracle.ManagedDataAccess.Client;
using Syrx.Commanders.Databases.Settings;

namespace Syrx.Commanders.Databases.Connectors.Oracle
{
    /// <summary>
    /// Provides Oracle database connectivity for the Syrx framework.
    /// This connector implements Oracle-specific database connection and command execution capabilities
    /// using Oracle.ManagedDataAccess.Core as the underlying provider.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The OracleDatabaseConnector provides native Oracle database support through the official
    /// Oracle.ManagedDataAccess.Core provider. It handles Oracle-specific connection pooling,
    /// parameter binding, and transaction management.
    /// </para>
    /// <para>
    /// Key features include:
    /// <list type="bullet">
    /// <item><description>Native Oracle connection pooling and management</description></item>
    /// <item><description>Oracle-specific data type handling (NUMBER, VARCHAR2, CLOB, BLOB, etc.)</description></item>
    /// <item><description>PL/SQL stored procedure and function support</description></item>
    /// <item><description>Oracle advanced features (hierarchical queries, analytical functions)</description></item>
    /// <item><description>Transaction support with proper rollback capabilities</description></item>
    /// <item><description>Async/await pattern support for all operations</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// This connector is designed to work seamlessly with Oracle databases version 12c and later,
    /// including Oracle Cloud Autonomous Database and Oracle Cloud Infrastructure (OCI) deployments.
    /// </para>
    /// </remarks>
    /// <param name="settings">The commander settings containing connection strings and command configurations.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="settings"/> is null.</exception>
    /// <example>
    /// <para>Basic usage through dependency injection:</para>
    /// <code>
    /// // Register the connector
    /// services.AddScoped&lt;IDatabaseConnector, OracleDatabaseConnector&gt;();
    /// 
    /// // Use in a repository
    /// public class EmployeeRepository
    /// {
    ///     private readonly IDatabaseConnector _connector;
    ///     
    ///     public EmployeeRepository(IDatabaseConnector connector)
    ///     {
    ///         _connector = connector;
    ///     }
    ///     
    ///     public async Task&lt;Employee&gt; GetEmployeeAsync(int id)
    ///     {
    ///         var commandSetting = new CommandSetting
    ///         {
    ///             CommandText = "SELECT employee_id, first_name, last_name FROM employees WHERE employee_id = :id",
    ///             ConnectionAlias = "Default"
    ///         };
    ///         
    ///         var employees = await _connector.QueryAsync&lt;Employee&gt;(
    ///             connectionString, 
    ///             commandSetting, 
    ///             new { id });
    ///         return employees.FirstOrDefault();
    ///     }
    /// }
    /// </code>
    /// </example>
    public class OracleDatabaseConnector(ICommanderSettings settings) : DatabaseConnector(settings, () => OracleClientFactory.Instance)
    {
    }
}
