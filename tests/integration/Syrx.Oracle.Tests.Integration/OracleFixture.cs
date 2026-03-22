namespace Syrx.Oracle.Tests.Integration
{
    public class OracleFixture : Fixture, IAsyncLifetime
    {
        private const string OracleImage = "gvenzl/oracle-xe:21.3.0-slim-faststart@sha256:f82bccdf6020d27373fdf0e93046b63eb3f777a0289e329d9839feebaf4555de";

        private readonly OracleContainer _container;

        public OracleFixture()
        {
            var logger = LoggerFactory.Create(builder => builder
                .AddConsole()
                .AddSystemdConsole()
                .AddSimpleConsole()).CreateLogger<OracleFixture>();

            _container = new OracleBuilder(OracleImage)
             .WithReuse(false)
             .WithLogger(logger)
             .WithStartupCallback((container, token) =>
             {
                 var message = @$"{new string('=', 150)}
Syrx: {nameof(OracleContainer)} startup callback. Container details:
{new string('=', 150)}
Name ............. : {container.Name}
Id ............... : {container.Id}
State ............ : {container.State}
Health ........... : {container.Health}
CreatedTime ...... : {container.CreatedTime}
StartedTime ...... : {container.StartedTime}
Hostname ......... : {container.Hostname}
Image.Digest ..... : {container.Image.Digest}
Image.FullName ... : {container.Image.FullName}
Image.Registry ... : {container.Image.Registry}
Image.Repository . : {container.Image.Repository}
Image.Tag ........ : {container.Image.Tag}
IpAddress ........ : {container.IpAddress}
MacAddress ....... : {container.MacAddress}
{new string('=', 150)}
";
                 container.Logger.LogInformation(message);
                 return Task.CompletedTask;
             })
             .Build();
        }

        public async Task DisposeAsync()
        {
            await Task.Run(() => Console.WriteLine("Done"));
        }

        public async Task InitializeAsync()
        {
            // line up
            await _container.StartAsync();

            var connectionString = _container.GetConnectionString();
            var alias = "Syrx.Sql";

            // call Install() on the base type. 
            Install(() => Installer.Install(alias, connectionString));
            Installer.SetupDatabase(base.ResolveCommander<DatabaseBuilder>());

            // set assertion messages for those that change between RDBMS implementations. 
            AssertionMessages.Add<Execute>(nameof(Execute.SupportsTransactionRollback), 
                OracleCommandStrings.Assertions.Execute.SupportsTransactionRollback);
            AssertionMessages.Add<Execute>(nameof(Execute.ExceptionsAreReturnedToCaller), 
                OracleCommandStrings.Assertions.Execute.ExceptionsAreReturnedToCaller);
            AssertionMessages.Add<Execute>(nameof(Execute.SupportsRollbackOnParameterlessCalls), 
                OracleCommandStrings.Assertions.Execute.SupportsRollbackOnParameterlessCalls);

            AssertionMessages.Add<ExecuteAsync>(nameof(ExecuteAsync.SupportsTransactionRollback), 
                OracleCommandStrings.Assertions.Execute.SupportsTransactionRollback);
            AssertionMessages.Add<ExecuteAsync>(nameof(ExecuteAsync.ExceptionsAreReturnedToCaller), 
                OracleCommandStrings.Assertions.Execute.ExceptionsAreReturnedToCaller);
            AssertionMessages.Add<ExecuteAsync>(nameof(ExecuteAsync.SupportsRollbackOnParameterlessCalls), 
                OracleCommandStrings.Assertions.Execute.SupportsRollbackOnParameterlessCalls);


            AssertionMessages.Add<Query>(nameof(Query.ExceptionsAreReturnedToCaller), 
                OracleCommandStrings.Assertions.Query.ExceptionsAreReturnedToCaller);
            AssertionMessages.Add<QueryAsync>(nameof(QueryAsync.ExceptionsAreReturnedToCaller), 
                OracleCommandStrings.Assertions.Query.ExceptionsAreReturnedToCaller);

            await Task.CompletedTask;
        }

    }
}
