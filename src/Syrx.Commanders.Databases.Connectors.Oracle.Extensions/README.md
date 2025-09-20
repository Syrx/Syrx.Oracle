# Syrx.Commanders.Databases.Connectors.Oracle.Extensions

Dependency injection extensions for Syrx Oracle database connectors.

## Overview

`Syrx.Commanders.Databases.Connectors.Oracle.Extensions` provides dependency injection and service registration extensions specifically for Oracle database connectors in the Syrx framework. This package enables easy registration of Oracle connectors with DI containers.

## Features

- **Service Registration**: Automatic registration of Oracle connector services
- **Lifecycle Management**: Configurable service lifetimes for connectors
- **DI Integration**: Seamless integration with Microsoft.Extensions.DependencyInjection
- **Builder Pattern**: Fluent configuration APIs
- **Extensibility**: Support for custom connector configurations

## Installation

> **Note**: This package is typically installed automatically as a dependency of `Syrx.Oracle.Extensions`.

```bash
dotnet add package Syrx.Commanders.Databases.Connectors.Oracle.Extensions
```

**Package Manager**
```bash
Install-Package Syrx.Commanders.Databases.Connectors.Oracle.Extensions
```

**PackageReference**
```xml
<PackageReference Include="Syrx.Commanders.Databases.Connectors.Oracle.Extensions" Version="2.4.5" />
```

## Key Extensions

### ServiceCollectionExtensions

Provides extension methods for `IServiceCollection`:

```csharp
public static class ServiceCollectionExtensions
{
    internal static IServiceCollection AddOracle(
        this IServiceCollection services, 
        ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        return services.TryAddToServiceCollection(
            typeof(IDatabaseConnector),
            typeof(OracleDatabaseConnector),
            lifetime);
    }
}
```

### OracleConnectorExtensions

Provides builder pattern extensions:

```csharp
public static class OracleConnectorExtensions
{
    public static SyrxBuilder UseOracle(
        this SyrxBuilder builder,
        Action<CommanderSettingsBuilder> factory,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        // Extension implementation
    }
}
```

## Usage

### Basic Registration

```csharp
using Syrx.Commanders.Databases.Connectors.Oracle.Extensions;

public void ConfigureServices(IServiceCollection services)
{
    services.UseSyrx(builder => builder
        .UseOracle(oracle => oracle
            .AddConnectionString("Default", connectionString)
            .AddCommand(/* command configuration */)));
}
```

### Custom Lifetime

```csharp
services.UseSyrx(builder => builder
    .UseOracle(
        oracle => oracle.AddConnectionString(/* config */),
        ServiceLifetime.Scoped));
```

### Advanced Configuration

```csharp
services.UseSyrx(builder => builder
    .UseOracle(oracle => oracle
        .AddConnectionString("Primary", "Data Source=localhost:1521/PROD;User Id=admin;Password=adminpass;")
        .AddConnectionString("ReadOnly", "Data Source=readonly:1521/PROD;User Id=reader;Password=readpass;")
        .AddCommand(types => types
            .ForType<EmployeeRepository>(methods => methods
                .ForMethod("GetEmployees", command => command
                    .UseConnectionAlias("ReadOnly")
                    .UseCommandText("SELECT * FROM employees")))),
        ServiceLifetime.Singleton));
```

## Service Registration Details

The extensions automatically register:

1. **ICommanderSettings**: The configuration settings instance
2. **IDatabaseCommandReader**: For reading command configurations  
3. **IDatabaseConnector**: The Oracle-specific connector
4. **DatabaseCommander<T>**: The generic database commander

## Service Lifetimes

| Lifetime | Use Case | Description |
|----------|----------|-------------|
| `Transient` | Default | New instance per injection |
| `Scoped` | Web Apps | Instance per request/scope |
| `Singleton` | Performance | Single instance for application |

### Lifetime Recommendations

- **Transient**: Default for most scenarios, minimal overhead
- **Scoped**: Web applications where you want request-scoped connections
- **Singleton**: High-performance scenarios with careful connection management

## Registration Process

When calling `.UseOracle()`, the following happens:

1. **Settings Registration**: CommanderSettings configured as transient
2. **Reader Registration**: DatabaseCommandReader registered
3. **Connector Registration**: OracleDatabaseConnector registered
4. **Commander Registration**: DatabaseCommander<T> registered

## Integration with Other Extensions

Works seamlessly with other Syrx extension packages:

```csharp
services.UseSyrx(builder => builder
    .UseOracle(/* Oracle config */)
    .UseSqlServer(/* SQL Server config */)    // If needed
    .UseNpgsql(/* PostgreSQL config */));     // If needed
```

## Oracle-Specific Configuration

### Connection Pool Management
```csharp
services.UseSyrx(builder => builder
    .UseOracle(oracle => oracle
        .AddConnectionString("Pooled", 
            "Data Source=localhost:1521/XE;User Id=user;Password=pass;" +
            "Min Pool Size=10;Max Pool Size=200;Connection Lifetime=300;")
        .AddCommand(/* commands */)));
```

### Primary/Standby Configuration
```csharp
services.UseSyrx(builder => builder
    .UseOracle(oracle => oracle
        .AddConnectionString("Primary", primaryConnectionString)
        .AddConnectionString("Standby", standbyConnectionString)
        .AddCommand(types => types
            .ForType<EmployeeRepository>(methods => methods
                .ForMethod("GetEmployees", command => command
                    .UseConnectionAlias("Standby"))      // Read operations
                .ForMethod("CreateEmployee", command => command
                    .UseConnectionAlias("Primary"))))));  // Write operations
```

### Oracle Wallet Configuration
```csharp
services.UseSyrx(builder => builder
    .UseOracle(oracle => oracle
        .AddConnectionString("Wallet", 
            "Data Source=mydb_high;" +
            "TNS_ADMIN=/opt/oracle/wallet/;" +
            "Wallet_Location=/opt/oracle/wallet/;")
        .AddCommand(/* commands */)));
```

### PL/SQL and Advanced Features
```csharp
services.UseSyrx(builder => builder
    .UseOracle(oracle => oracle
        .AddConnectionString("Default", connectionString)
        .AddCommand(types => types
            .ForType<EmployeeRepository>(methods => methods
                // PL/SQL procedure call
                .ForMethod("UpdateEmployeeSalary", command => command
                    .UseConnectionAlias("Default")
                    .UseCommandText("BEGIN update_employee_salary(:employeeId, :newSalary); END;"))
                
                // Oracle hierarchical queries
                .ForMethod("GetEmployeeHierarchy", command => command
                    .UseConnectionAlias("Default")
                    .UseCommandText(@"
                        SELECT employee_id, first_name, last_name, manager_id, LEVEL
                        FROM employees
                        START WITH manager_id IS NULL
                        CONNECT BY PRIOR employee_id = manager_id
                        ORDER SIBLINGS BY last_name"))
                
                // Oracle analytical functions
                .ForMethod("GetEmployeeRanking", command => command
                    .UseConnectionAlias("Default")
                    .UseCommandText(@"
                        SELECT 
                            employee_id, first_name, last_name, salary,
                            RANK() OVER (ORDER BY salary DESC) as salary_rank,
                            RATIO_TO_REPORT(salary) OVER () as salary_percentage
                        FROM employees"))))));
```

## Error Handling

The extensions provide proper error handling for:
- Invalid configuration scenarios
- Missing dependencies
- Circular dependency issues
- Service registration conflicts
- Oracle-specific connection errors

## Testing Support

The extensions support testing scenarios:

```csharp
// Test service collection
var services = new ServiceCollection();
services.UseSyrx(builder => builder
    .UseOracle(oracle => oracle
        .AddConnectionString("Test", testConnectionString)
        .AddCommand(/* test commands */)));

var provider = services.BuildServiceProvider();
var connector = provider.GetService<IDatabaseConnector>();
```

## Performance Optimizations

### Connection String Optimization
```csharp
services.UseSyrx(builder => builder
    .UseOracle(oracle => oracle
        .AddConnectionString("Optimized", 
            "Data Source=localhost:1521/XE;User Id=user;Password=pass;" +
            "Min Pool Size=10;Max Pool Size=100;" +
            "Connection Lifetime=300;Connection Timeout=30;" +
            "Command Timeout=60;Statement Cache Size=50;" +
            "Self Tuning=true;Validate Connection=true;")
        .AddCommand(/* commands */)));
```

### Bulk Operation Configuration
```csharp
.ForMethod("BulkInsert", command => command
    .UseConnectionAlias("BulkWrite")
    .UseCommandText("INSERT INTO employees (first_name, last_name, email) VALUES (:FirstName, :LastName, :Email)")
    .SetCommandTimeout(300))  // Longer timeout for bulk operations
```

### High-Performance OLTP
```csharp
services.UseSyrx(builder => builder
    .UseOracle(oracle => oracle
        .AddConnectionString("OLTP", 
            "Data Source=oltp.oracle.com:1521/PROD;User Id=app;Password=secret;" +
            "Min Pool Size=50;Max Pool Size=500;" +
            "Connection Lifetime=600;" +
            "Connection Timeout=5;Command Timeout=30;" +
            "Statement Cache Size=100;" +
            "Self Tuning=true;")
        .AddCommand(/* OLTP commands */)));
```

### Data Warehouse Workload
```csharp
services.UseSyrx(builder => builder
    .UseOracle(oracle => oracle
        .AddConnectionString("DW", 
            "Data Source=warehouse.oracle.com:1521/DW;User Id=analyst;Password=secret;" +
            "Min Pool Size=5;Max Pool Size=50;" +
            "Connection Lifetime=1800;" +
            "Connection Timeout=60;Command Timeout=600;" +
            "Statement Cache Size=200;" +
            "Statement Cache Purge=false;")  // Keep statements cached
        .AddCommand(/* DW commands */)));
```

## Oracle Advanced Features

### Partitioning Support
```csharp
.ForMethod("GetSalesByPartition", command => command
    .UseConnectionAlias("DW")
    .UseCommandText(@"
        SELECT /*+ PARTITION_WISE_JOIN */ 
            sale_id, customer_id, sale_date, amount
        FROM sales PARTITION(:partitionName)
        WHERE sale_date BETWEEN :startDate AND :endDate"))
```

### Parallel Execution
```csharp
.ForMethod("ProcessLargeDataset", command => command
    .UseConnectionAlias("Analytics")
    .UseCommandText(@"
        SELECT /*+ PARALLEL(emp, 8) */ 
            department_id, COUNT(*) as employee_count, AVG(salary) as avg_salary
        FROM employees emp
        GROUP BY department_id"))
```

### Advanced Compression
```csharp
.ForMethod("ArchiveOldData", command => command
    .UseConnectionAlias("Archive")
    .UseCommandText("ALTER TABLE sales_archive COMPRESS FOR QUERY HIGH"))
```

## Configuration from Environment

### Environment-Specific Setup

```csharp
public static class OracleEnvironmentExtensions
{
    public static SyrxBuilder ConfigureOracleForEnvironment(
        this SyrxBuilder builder, 
        IConfiguration configuration)
    {
        var environment = configuration["ENVIRONMENT"] ?? "Development";
        
        return environment switch
        {
            "Development" => builder.UseOracle(oracle => oracle.ConfigureDevelopment(configuration)),
            "Staging" => builder.UseOracle(oracle => oracle.ConfigureStaging(configuration)),
            "Production" => builder.UseOracle(oracle => oracle.ConfigureProduction(configuration)),
            _ => throw new InvalidOperationException($"Unknown environment: {environment}")
        };
    }
    
    private static OracleBuilder ConfigureDevelopment(this OracleBuilder oracle, IConfiguration config)
    {
        return oracle
            .AddConnectionString("Default", config.GetConnectionString("Development"))
            .AddCommand(/* development-specific commands with detailed logging */);
    }
    
    private static OracleBuilder ConfigureProduction(this OracleBuilder oracle, IConfiguration config)
    {
        return oracle
            .AddConnectionString("Primary", config.GetConnectionString("ProductionPrimary"))
            .AddConnectionString("Standby", config.GetConnectionString("ProductionStandby"))
            .AddCommand(/* production-optimized commands */);
    }
}

// Usage
services.UseSyrx(builder => builder.ConfigureOracleForEnvironment(Configuration));
```

### Oracle Cloud Integration

```csharp
public static class OracleCloudExtensions
{
    public static SyrxBuilder UseOracleCloud(this SyrxBuilder builder, IConfiguration configuration)
    {
        var walletPath = configuration["ORACLE_WALLET_PATH"] ?? "/app/wallet";
        var serviceName = configuration["ORACLE_SERVICE_NAME"] ?? "mydb_high";
        
        return builder.UseOracle(oracle => oracle
            .AddConnectionString("Cloud", 
                $"Data Source={serviceName};" +
                $"User Id={configuration["ORACLE_USER"]};" +
                $"Password={configuration["ORACLE_PASSWORD"]};" +
                $"TNS_ADMIN={walletPath};" +
                $"Wallet_Location={walletPath};" +
                "Connection Timeout=60;")
            .AddCommand(/* cloud-optimized commands */));
    }
}

// docker-compose.yml with Oracle Wallet
services:
  app:
    build: .
    environment:
      - ORACLE_SERVICE_NAME=mydb_high
      - ORACLE_USER=${ORACLE_USER}
      - ORACLE_PASSWORD=${ORACLE_PASSWORD}
      - ORACLE_WALLET_PATH=/app/wallet
    volumes:
      - ./wallet:/app/wallet:ro
```

## Health Checks Integration

```csharp
services.UseSyrx(builder => builder
    .UseOracle(oracle => oracle
        .AddConnectionString("Default", connectionString)
        .AddCommand(/* commands */)));

// Add health checks for Oracle
services.AddHealthChecks()
    .AddOracle(connectionString, name: "oracle");

// Custom health check using Syrx
services.AddHealthChecks()
    .AddTypeActivatedCheck<SyrxOracleHealthCheck>(
        "syrx-oracle", 
        args: new object[] { "Default" });

public class SyrxOracleHealthCheck : IHealthCheck
{
    private readonly ICommander<SyrxOracleHealthCheck> _commander;
    private readonly string _connectionAlias;

    public SyrxOracleHealthCheck(ICommander<SyrxOracleHealthCheck> commander, string connectionAlias)
    {
        _commander = commander;
        _connectionAlias = connectionAlias;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _commander.QueryAsync<int>("SELECT 1 FROM dual");
            return HealthCheckResult.Healthy("Oracle is responding");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Oracle is not responding", ex);
        }
    }
}
```

## Oracle Enterprise Features

### Real Application Clusters (RAC)
```csharp
services.UseSyrx(builder => builder
    .UseOracle(oracle => oracle
        .AddConnectionString("RAC", 
            "Data Source=(DESCRIPTION=" +
            "(ADDRESS_LIST=" +
            "(ADDRESS=(PROTOCOL=TCP)(HOST=rac1)(PORT=1521))" +
            "(ADDRESS=(PROTOCOL=TCP)(HOST=rac2)(PORT=1521))" +
            "(LOAD_BALANCE=yes)(FAILOVER=yes))" +
            "(CONNECT_DATA=(SERVICE_NAME=myservice)(FAILOVER_MODE=(TYPE=SELECT)(METHOD=BASIC))))" +
            ";User Id=app;Password=secret;" +
            "Connection Timeout=30;HA Events=true;")
        .AddCommand(/* RAC-optimized commands */)));
```

### Autonomous Database
```csharp
services.UseSyrx(builder => builder
    .UseOracle(oracle => oracle
        .AddConnectionString("ADB", 
            "Data Source=mydb_high;" +
            "User Id=admin;Password=complexPassword123!;" +
            "TNS_ADMIN=/app/wallet/;" +
            "Connection Timeout=60;" +
            "Statement Cache Size=200;")
        .AddCommand(/* autonomous database commands */)));
```

### Oracle Multitenant (PDBs)
```csharp
services.UseSyrx(builder => builder
    .UseOracle(oracle => oracle
        .AddConnectionString("PDB1", "Data Source=localhost:1521/PDB1;User Id=app;Password=secret;")
        .AddConnectionString("PDB2", "Data Source=localhost:1521/PDB2;User Id=app;Password=secret;")
        .AddCommand(types => types
            .ForType<TenantARepository>(methods => methods
                .ForMethod("GetData", command => command
                    .UseConnectionAlias("PDB1")))
            .ForType<TenantBRepository>(methods => methods
                .ForMethod("GetData", command => command
                    .UseConnectionAlias("PDB2"))))));
```

## Related Packages

- **[Syrx.Oracle.Extensions](https://www.nuget.org/packages/Syrx.Oracle.Extensions/)**: High-level Oracle extensions
- **[Syrx.Commanders.Databases.Connectors.Oracle](https://www.nuget.org/packages/Syrx.Commanders.Databases.Connectors.Oracle/)**: Core Oracle connector
- **[Syrx.Commanders.Databases.Extensions](https://www.nuget.org/packages/Syrx.Commanders.Databases.Extensions/)**: Base database extensions

## License

This project is licensed under the [MIT License](https://github.com/Syrx/Syrx/blob/main/LICENSE).

## Credits

- Built on top of [Microsoft.Extensions.DependencyInjection](https://github.com/dotnet/extensions)
- Oracle support provided by [Oracle.ManagedDataAccess.Core](https://www.nuget.org/packages/Oracle.ManagedDataAccess.Core/)
- Follows [Dapper](https://github.com/DapperLib/Dapper) performance patterns