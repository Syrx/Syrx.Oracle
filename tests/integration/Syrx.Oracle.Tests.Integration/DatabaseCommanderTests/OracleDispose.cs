namespace Syrx.Oracle.Tests.Integration.DatabaseCommanderTests
{
    [Collection(nameof(OracleFixtureCollection))]
    public class OracleDispose(OracleFixture fixture) : Dispose(fixture) { }
}
