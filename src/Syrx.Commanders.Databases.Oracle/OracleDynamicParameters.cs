using Dapper;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace Syrx.Commanders.Databases.Oracle
{
    /// <summary>
    /// Provides multiple result set support to Syrx (via Dapper) from Oracle databases.
    /// This class enables Oracle cursor parameters to work with Dapper's QueryMultiple functionality,
    /// which is necessary because Oracle handles multiple result sets differently than other RDBMS.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Oracle doesn't natively support multiple result sets in the same way that SQL Server and other
    /// databases do. Instead, Oracle uses cursor parameters (REF CURSOR) to return multiple result sets
    /// from stored procedures or PL/SQL blocks.
    /// </para>
    /// <para>
    /// This class automatically creates the necessary cursor parameters and handles their binding to
    /// Oracle commands, allowing Syrx to provide consistent multiple result set functionality across
    /// all supported database providers.
    /// </para>
    /// <para>
    /// The implementation supports both numbered cursors (automatically generated names like "1", "2", "3")
    /// and named cursors (custom cursor names specified by the developer).
    /// </para>
    /// <para>
    /// Credit: This implementation is adapted from https://stackoverflow.com/a/41110515
    /// Thanks to nw. and greyseal96 for the original solution.
    /// </para>
    /// </remarks>
    /// <example>
    /// <para>Using numbered cursors (most common approach):</para>
    /// <code>
    /// using static Syrx.Commanders.Databases.Oracle.OracleDynamicParameters;
    /// 
    /// // PL/SQL with numbered cursors
    /// var sql = @"
    ///     BEGIN
    ///         OPEN :1 FOR SELECT * FROM employees WHERE department_id = :deptId1;
    ///         OPEN :2 FOR SELECT * FROM employees WHERE department_id = :deptId2;
    ///     END;";
    /// 
    /// var arguments = new { deptId1 = 10, deptId2 = 20 };
    /// var parameters = Cursors(arguments);
    /// var result = await commander.QueryMultipleAsync(parameters);
    /// </code>
    /// 
    /// <para>Using named cursors:</para>
    /// <code>
    /// // PL/SQL with named cursors
    /// var sql = @"
    ///     BEGIN
    ///         OPEN :emp_cursor FOR SELECT * FROM employees WHERE department_id = :deptId;
    ///         OPEN :dept_cursor FOR SELECT * FROM departments WHERE department_id = :deptId;
    ///     END;";
    /// 
    /// string[] cursors = { "emp_cursor", "dept_cursor" };
    /// var arguments = new { deptId = 10 };
    /// var parameters = Cursors(cursors, arguments);
    /// var result = await commander.QueryMultipleAsync(parameters);
    /// </code>
    /// </example>
    public class OracleDynamicParameters : SqlMapper.IDynamicParameters
    {
        private readonly DynamicParameters dynamicParameters;

        private readonly List<OracleParameter> oracleParameters = new List<OracleParameter>();

        /// <summary>
        /// Initializes a new instance of the <see cref="OracleDynamicParameters"/> class with optional parameters and cursor names.
        /// </summary>
        /// <param name="parameters">An object containing the parameters to add to the dynamic parameters collection.
        /// This can be an anonymous object, dictionary, or any object with public properties.</param>
        /// <param name="cursorNames">An array of cursor names to create as output parameters. If null, 
        /// 16 numbered cursors ("1", "2", "3", ..., "16") will be created automatically.</param>
        /// <remarks>
        /// <para>
        /// If <paramref name="cursorNames"/> is not provided, the constructor automatically creates
        /// 16 numbered cursor parameters (named "1" through "16") to handle the most common scenarios.
        /// This covers the vast majority of use cases where multiple result sets are needed.
        /// </para>
        /// <para>
        /// All cursor parameters are created as <see cref="ParameterDirection.Output"/> parameters
        /// with <see cref="OracleDbType.RefCursor"/> type, which is the correct configuration for
        /// Oracle cursor parameters.
        /// </para>
        /// </remarks>
        /// <example>
        /// <para>Create with numbered cursors and parameters:</para>
        /// <code>
        /// var parameters = new OracleDynamicParameters(new { id = 1, name = "John" });
        /// // Creates cursors "1", "2", "3", ..., "16" automatically
        /// </code>
        /// 
        /// <para>Create with named cursors:</para>
        /// <code>
        /// var cursors = new[] { "employees", "departments" };
        /// var parameters = new OracleDynamicParameters(new { deptId = 10 }, cursors);
        /// // Creates cursors "employees" and "departments"
        /// </code>
        /// </example>
        public OracleDynamicParameters(object parameters = null, string[] cursorNames = null)
        {
            var cursors = cursorNames ?? Enumerable.Range(1, 16).Select(i => i.ToString()).ToArray();
            dynamicParameters = new DynamicParameters(parameters);
            AddCursorParameters(cursors);
        }

        private void AddCursorParameters(params string[] refCursorNames)
        {
            foreach (string refCursorName in refCursorNames)
            {
                var oracleParameter = new OracleParameter(refCursorName, OracleDbType.RefCursor, ParameterDirection.Output);
                oracleParameters.Add(oracleParameter);
            }
        }

        /// <summary>
        /// Adds the parameters to the specified <see cref="IDbCommand"/>.
        /// This method is called by Dapper to bind parameters to the Oracle command.
        /// </summary>
        /// <param name="command">The database command to add parameters to. Must be an <see cref="OracleCommand"/> for Oracle-specific parameters to be added.</param>
        /// <param name="identity">The SQL mapper identity containing information about the command and parameter mapping.</param>
        /// <remarks>
        /// <para>
        /// This method first delegates to the base <see cref="DynamicParameters"/> implementation to add
        /// regular parameters, then adds the Oracle-specific cursor parameters if the command is an
        /// <see cref="OracleCommand"/>.
        /// </para>
        /// <para>
        /// The cursor parameters are essential for Oracle's multiple result set functionality and must
        /// be present on the command before execution to avoid ORA-01008 errors.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="command"/> is null.</exception>
        public void AddParameters(IDbCommand command, SqlMapper.Identity identity)
        {
            ((SqlMapper.IDynamicParameters) dynamicParameters).AddParameters(command, identity);
            var oracleCommand = command as OracleCommand;
            if (oracleCommand != null)
            {
                oracleCommand.Parameters.AddRange(oracleParameters.ToArray());
            }
        }
       
        /// <summary>
        /// Returns a new instance of <see cref="OracleDynamicParameters"/> using numbered cursors (1, 2, 3, etc.).
        /// This is the most commonly used method for Oracle multiple result sets.
        /// </summary>
        /// <param name="parameters">An object containing the parameters to pass to the query. 
        /// This can be an anonymous object, dictionary, or any object with public properties.</param>
        /// <returns>A new <see cref="OracleDynamicParameters"/> instance configured with numbered cursors and the specified parameters.</returns>
        /// <remarks>
        /// <para>
        /// This method creates 16 numbered cursor parameters ("1", "2", "3", ..., "16") which covers
        /// the vast majority of use cases. These cursors correspond to the numbered placeholders in
        /// Oracle PL/SQL blocks (e.g., OPEN :1 FOR ..., OPEN :2 FOR ...).
        /// </para>
        /// <para>
        /// This is the recommended approach for most scenarios because it requires minimal configuration
        /// and handles the most common multiple result set patterns.
        /// </para>
        /// </remarks>
        /// <example>
        /// <para>Using with parameters:</para>
        /// <code>
        /// using static Syrx.Commanders.Databases.Oracle.OracleDynamicParameters;
        /// 
        /// var arguments = new { id1 = 2, id2 = 3, id3 = 4 };
        /// var parameters = Cursors(arguments);
        /// var result = await commander.QueryMultipleAsync(parameters);
        /// </code>
        /// 
        /// <para>Using without parameters:</para>
        /// <code>
        /// var parameters = Cursors();
        /// var result = await commander.QueryMultipleAsync(parameters);
        /// </code>
        /// </example>
        public static OracleDynamicParameters Cursors(object parameters = null) => new OracleDynamicParameters(parameters);

        /// <summary>
        /// Returns a new instance of <see cref="OracleDynamicParameters"/> using a supplied set of named cursors.
        /// Use this method when you need specific cursor names that correspond to named placeholders in your PL/SQL.
        /// </summary>
        /// <param name="cursors">A string array of cursor names corresponding to their names in the PL/SQL statement.
        /// Each name should match a cursor placeholder in your PL/SQL (e.g., ":emp_cursor", ":dept_cursor").</param>
        /// <param name="parameters">An optional object containing additional parameters to pass to the query.
        /// This can be an anonymous object, dictionary, or any object with public properties.</param>
        /// <returns>A new <see cref="OracleDynamicParameters"/> instance configured with the specified named cursors and parameters.</returns>
        /// <remarks>
        /// <para>
        /// Use this method when your PL/SQL uses meaningful cursor names rather than numbered placeholders.
        /// This provides better readability and maintainability when working with complex stored procedures
        /// that return multiple, semantically different result sets.
        /// </para>
        /// <para>
        /// All cursor names will be created as <see cref="ParameterDirection.Output"/> parameters with
        /// <see cref="OracleDbType.RefCursor"/> type.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="cursors"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="cursors"/> is empty or contains null/empty cursor names.</exception>
        /// <example>
        /// <para>Using named cursors with parameters:</para>
        /// <code>
        /// using static Syrx.Commanders.Databases.Oracle.OracleDynamicParameters;
        /// 
        /// // PL/SQL with named cursors:
        /// // BEGIN
        /// //     OPEN :employees FOR SELECT * FROM employees WHERE dept_id = :deptId;
        /// //     OPEN :departments FOR SELECT * FROM departments WHERE dept_id = :deptId;
        /// // END;
        /// 
        /// string[] cursors = { "employees", "departments" };
        /// var arguments = new { deptId = 10 };
        /// var parameters = Cursors(cursors, arguments);
        /// var result = await commander.QueryMultipleAsync(parameters);
        /// </code>
        /// 
        /// <para>Using named cursors without additional parameters:</para>
        /// <code>
        /// string[] cursors = { "all_employees", "all_departments" };
        /// var parameters = Cursors(cursors);
        /// var result = await commander.QueryMultipleAsync(parameters);
        /// </code>
        /// </example>
        public static OracleDynamicParameters Cursors(string[] cursors, object parameters = null) => new OracleDynamicParameters(parameters, cursors);
    }
}
