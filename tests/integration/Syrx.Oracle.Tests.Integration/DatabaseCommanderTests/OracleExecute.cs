using System.Transactions;

namespace Syrx.Oracle.Tests.Integration.DatabaseCommanderTests
{
    [Collection(nameof(OracleFixtureCollection))]
    public class OracleExecute(OracleFixture fixture) : Execute(fixture) 
    {
        [Theory(Skip = "Not supported by Oracle")]
        [MemberData(nameof(TransactionScopeOptions))] // TransactionScopeOptions is taken from base Execute
        public override void SupportsEnlistingInAmbientTransactions(TransactionScopeOption scopeOption)
        {
            base.SupportsEnlistingInAmbientTransactions(scopeOption);
        }
    }
}
