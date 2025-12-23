namespace Syrx.Commanders.Databases.Connectors.Oracle.Extensions
{
    /// <summary>
    /// Provides extension methods for configuring Oracle database connectivity in the Syrx framework.
    /// These extensions enable fluent configuration of Oracle-specific services and settings.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class contains extension methods that integrate Oracle database support into the Syrx
    /// dependency injection and configuration system. It provides a fluent API for configuring
    /// Oracle connections, commands, and service lifetimes.
    /// </para>
    /// <para>
    /// The extensions automatically register all necessary services including:
    /// <list type="bullet">
    /// <item><description>Commander settings with Oracle-specific configuration</description></item>
    /// <item><description>Database command reader for Oracle command resolution</description></item>
    /// <item><description>Oracle database connector implementation</description></item>
    /// <item><description>Database commander for executing Oracle commands</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public static class OracleConnectorExtensions
    {
        /// <summary>
        /// Configures the Syrx builder to use Oracle database connectivity with the specified settings and service lifetime.
        /// </summary>
        /// <param name="builder">The <see cref="SyrxBuilder"/> instance to configure.</param>
        /// <param name="factory">An action delegate that configures the Oracle-specific command settings using the <see cref="CommanderSettingsBuilder"/>.</param>
        /// <param name="lifetime">The service lifetime for the registered Oracle services. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
        /// <returns>The same <see cref="SyrxBuilder"/> instance for method chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method registers all the necessary services for Oracle database operations:
        /// <list type="number">
        /// <item><description><see cref="ICommanderSettings"/> - Configuration settings for database operations</description></item>
        /// <item><description><see cref="IDatabaseCommandReader"/> - Service for reading command configurations</description></item>
        /// <item><description><see cref="IDatabaseConnector"/> - Oracle-specific database connector</description></item>
        /// <item><description><see cref="DatabaseCommander{T}"/> - Generic database commander for repositories</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// The <paramref name="factory"/> delegate provides access to a fluent configuration API where you can:
        /// <list type="bullet">
        /// <item><description>Add connection strings with aliases</description></item>
        /// <item><description>Configure commands for repository methods</description></item>
        /// <item><description>Set Oracle-specific options and timeouts</description></item>
        /// <item><description>Configure multiple database connections</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// Service lifetime considerations:
        /// <list type="bullet">
        /// <item><description><strong>Singleton</strong>: Best for high-performance scenarios, single configuration</description></item>
        /// <item><description><strong>Scoped</strong>: Suitable for web applications, per-request lifetime</description></item>
        /// <item><description><strong>Transient</strong>: Creates new instances each time, highest flexibility</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="factory"/> is null.</exception>
        /// <example>
        /// <para>Basic Oracle configuration:</para>
        /// <code>
        /// services.UseSyrx(builder => builder
        ///     .UseOracle(oracle => oracle
        ///         .AddConnectionString("Default", "Data Source=localhost:1521/XE;User Id=hr;Password=password;")
        ///         .AddCommand(types => types
        ///             .ForType&lt;EmployeeRepository&gt;(methods => methods
        ///                 .ForMethod(nameof(EmployeeRepository.GetAllAsync), command => command
        ///                     .UseConnectionAlias("Default")
        ///                     .UseCommandText("SELECT employee_id, first_name, last_name FROM employees"))))));
        /// </code>
        /// 
        /// <para>Multiple connection strings with different service lifetime:</para>
        /// <code>
        /// services.UseSyrx(builder => builder
        ///     .UseOracle(oracle => oracle
        ///         .AddConnectionString("Primary", primaryConnectionString)
        ///         .AddConnectionString("ReadOnly", readOnlyConnectionString)
        ///         .AddCommand(types => types
        ///             .ForType&lt;UserRepository&gt;(methods => methods
        ///                 .ForMethod("GetUsers", command => command
        ///                     .UseConnectionAlias("ReadOnly"))
        ///                 .ForMethod("CreateUser", command => command
        ///                     .UseConnectionAlias("Primary")))),
        ///         ServiceLifetime.Scoped));
        /// </code>
        /// 
        /// <para>Oracle Cloud Autonomous Database configuration:</para>
        /// <code>
        /// services.UseSyrx(builder => builder
        ///     .UseOracle(oracle => oracle
        ///         .AddConnectionString("ADB", 
        ///             "Data Source=mydb_high;User Id=admin;Password=password;" +
        ///             "TNS_ADMIN=/app/wallet/;Connection Timeout=60;")
        ///         .AddCommand(/* command configuration */)));
        /// </code>
        /// </example>
        public static SyrxBuilder UseOracle(
            this SyrxBuilder builder,
            Action<CommanderSettingsBuilder> factory,
            ServiceLifetime lifetime = ServiceLifetime.Singleton)
        {
            var options = CommanderSettingsBuilderExtensions.Build(factory);
            builder.ServiceCollection
                .AddSingleton<ICommanderSettings, CommanderSettings>(a => options)
                .AddReader(lifetime) // add reader
                .AddOracle(lifetime) // add connector
                .AddDatabaseCommander(lifetime);

            return builder;
        }

    }
}
