namespace Syrx.Commanders.Databases.Connectors.Oracle.Extensions.Tests.Unit.OracleDatabaseConnectorExtensionsTests
{
    public class UseOracle
    {
        private IServiceCollection _services;

        public UseOracle()
        {
            _services = new ServiceCollection();
        }

        [Fact]
        public void Successful()
        {
            _services.UseSyrx(a => a
                .UseOracle(b => b
                    .AddCommand(c => c
                        .ForType<UseOracle>(d => d
                            .ForMethod(nameof(Successful), e => e.UseCommandText("test-command").UseConnectionAlias("test-aliase"))))));

            var provider = _services.BuildServiceProvider();
            var connector = provider.GetService<IDatabaseConnector>();
            IsType<OracleDatabaseConnector>(connector);
        }
    }
}
