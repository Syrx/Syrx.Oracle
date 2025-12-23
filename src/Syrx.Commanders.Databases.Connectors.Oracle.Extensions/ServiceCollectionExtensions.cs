namespace Syrx.Commanders.Databases.Connectors.Oracle.Extensions
{
    /// <summary>
    /// Provides extension methods for registering Oracle database connector services with the dependency injection container.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class contains internal extension methods used by the Syrx framework to register Oracle-specific
    /// database connector services. These methods are primarily used by the higher-level configuration APIs
    /// and are not intended for direct use by application developers.
    /// </para>
    /// <para>
    /// The extensions handle the registration of <see cref="OracleDatabaseConnector"/> as an implementation
    /// of <see cref="IDatabaseConnector"/>, ensuring proper service lifetime management and avoiding
    /// duplicate registrations.
    /// </para>
    /// </remarks>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the Oracle database connector with the specified service lifetime.
        /// This method is used internally by the Syrx configuration system.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to register services with.</param>
        /// <param name="lifetime">The service lifetime for the Oracle connector. Defaults to <see cref="ServiceLifetime.Transient"/>.</param>
        /// <returns>The same <see cref="IServiceCollection"/> instance for method chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method registers <see cref="OracleDatabaseConnector"/> as an implementation of
        /// <see cref="IDatabaseConnector"/> using the specified service lifetime. It uses the
        /// TryAdd pattern to avoid duplicate registrations if the service is already registered.
        /// </para>
        /// <para>
        /// Service lifetime considerations:
        /// <list type="bullet">
        /// <item><description><strong>Transient</strong>: New instance per injection, minimal memory footprint</description></item>
        /// <item><description><strong>Scoped</strong>: Instance per request/scope, good for web applications</description></item>
        /// <item><description><strong>Singleton</strong>: Single instance for application lifetime, best performance</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// This method is marked as internal because it's primarily used by the Syrx configuration
        /// system. Application developers should use the higher-level <c>UseOracle</c> extension
        /// methods instead.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
        internal static IServiceCollection AddOracle(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Transient)
        {
            return services.TryAddToServiceCollection(
                typeof(IDatabaseConnector),
                typeof(OracleDatabaseConnector),
                lifetime);
        }
    }
}
