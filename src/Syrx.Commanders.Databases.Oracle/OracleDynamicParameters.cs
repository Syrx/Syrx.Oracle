using Dapper;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace Syrx.Commanders.Databases.Oracle
{
    /// <summary>
    /// Provides multiple result set support to Syrx (via Dapper) from Oracle. 
    /// This is necessary so that all implementations of the Syrx database commander
    /// provide support for multiple result sets. 
    /// </summary>
    /// <notes>
    /// Lifted (almost) entirely from https://stackoverflow.com/a/41110515
    /// Thank you to nw. and greyseal96
    /// </notes>

    public class OracleDynamicParameters : SqlMapper.IDynamicParameters
    {
        private readonly DynamicParameters dynamicParameters;

        private readonly List<OracleParameter> oracleParameters = new List<OracleParameter>();

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
        /// Returns a new instance of <see cref="OracleDynamicParameters"/> using an incrementing value as a default cursor name (starting at 1). 
        /// </summary>
        /// <param name="parameters">Parameters to pass to the query.</param>
        /// <returns></returns>
        public static OracleDynamicParameters Cursors(object parameters = null) => new OracleDynamicParameters(parameters);

        /// <summary>
        /// Returns a new instance of <see cref="OracleDynamicParameters"/> using a supplied set of named cursors. 
        /// </summary>
        /// <param name="cursors">A string array of cursor names corresponding to the their names on the PL/SQL statement.</param>
        /// <param name="parameters">An optional set of parameters to pass to the query.</param>
        /// <returns></returns>
        public static OracleDynamicParameters Cursors(string[] cursors, object parameters = null) => new OracleDynamicParameters(parameters, cursors);
    }
}
