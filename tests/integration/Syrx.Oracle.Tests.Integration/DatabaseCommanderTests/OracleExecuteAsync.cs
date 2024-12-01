using System.Transactions;

namespace Syrx.Oracle.Tests.Integration.DatabaseCommanderTests
{
    [Collection(nameof(OracleFixtureCollection))]
    public class OracleExecuteAsync(OracleFixture fixture) : ExecuteAsync(fixture) 
    {
        [Theory(Skip = "Not supported by Oracle")]
        [MemberData(nameof(TransactionScopeOptions))] // TransactionScopeOptions is taken from base Execute
        public override Task SupportsEnlistingInAmbientTransactions(TransactionScopeOption scopeOption)
        {
            return base.SupportsEnlistingInAmbientTransactions(scopeOption);
        }
    }
}
