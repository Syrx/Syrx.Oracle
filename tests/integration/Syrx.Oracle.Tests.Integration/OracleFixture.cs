namespace Syrx.Oracle.Tests.Integration
{
    public class OracleFixture : Fixture, IAsyncLifetime
    {
        private readonly OracleContainer _container;

        public OracleFixture()
        {
            var _logger = LoggerFactory.Create(b => b
                .AddConsole()
                .AddSystemdConsole()
                .AddSimpleConsole()).CreateLogger<OracleFixture>();

            _container = new OracleBuilder()
             .WithImage("gvenzl/oracle-xe:21.3.0-slim-faststart")
             .WithReuse(true)
             .WithLogger(_logger)
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
ConnectionString . : {container.GetConnectionString()}
{new string('=', 150)}
";
                 container.Logger.LogInformation(message);
                 return Task.CompletedTask;
             })
             .Build();

            // start
            _container.StartAsync().Wait();
        }

        public async Task DisposeAsync()
        {
            await Task.Run(() => Console.WriteLine("Done"));
        }

        public async Task InitializeAsync()
        {
            // line up
            var connectionString = _container.GetConnectionString();
            var alias = "Syrx.Sql";

            var provider = Installer.Install(alias, connectionString);

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
