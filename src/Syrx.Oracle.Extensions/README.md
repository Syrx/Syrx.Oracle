# Syrx.Oracle.Extensions

Dependency injection extensions for Syrx Oracle integration.

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Configuration Examples](#configuration-examples)
- [Advanced Configuration](#advanced-configuration)
- [Oracle-Specific Features](#oracle-specific-features)
- [Service Lifetimes](#service-lifetimes)
- [Multiple Database Support](#multiple-database-support)
- [Environment-Specific Settings](#environment-specific-settings)
- [Performance Optimization](#performance-optimization)
- [Multiple Result Sets](#multiple-result-sets)
- [Error Handling](#error-handling)
- [Testing Integration](#testing-integration)
- [Migration Scenarios](#migration-scenarios)
- [Related Packages](#related-packages)
- [Requirements](#requirements)
- [License](#license)
- [Credits](#credits)

## Overview

`Syrx.Oracle.Extensions` provides seamless dependency injection integration for Oracle database data access in the Syrx framework. This package simplifies the registration and configuration of Oracle-specific services in .NET applications using Microsoft's dependency injection container.

## Features

- **Easy Registration**: Simple service registration with `UseSyrx()` extension
- **Fluent Configuration**: Builder pattern for clean configuration syntax
- **Service Lifetime Management**: Configurable service lifetimes
- **Oracle Optimization**: Oracle-specific configuration options
- **Multiple Connections**: Support for multiple named connections
- **Environment Configuration**: Easy switching between development/production settings

## Installation

```bash
dotnet add package Syrx.Oracle.Extensions
```

**Package Manager**
```bash
Install-Package Syrx.Oracle.Extensions
```

**PackageReference**
```xml
<PackageReference Include="Syrx.Oracle.Extensions" Version="2.4.5" />
```

## Quick Start

### Basic Setup

```csharp
using Syrx.Oracle.Extensions;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.UseSyrx(builder => builder
            .UseOracle(oracle => oracle
                .AddConnectionString("Default", Configuration.GetConnectionString("Oracle"))
                .AddCommand(types => types
                    .ForType<EmployeeRepository>(methods => methods
                        .ForMethod(nameof(EmployeeRepository.GetAllAsync), command => command
                            .UseConnectionAlias("Default")
                            .UseCommandText("SELECT employee_id, first_name, last_name, email FROM employees"))))));
    }
}
```

### With Repository Registration

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Register Syrx with Oracle
    services.UseSyrx(builder => builder
        .UseOracle(oracle => oracle
            .AddConnectionString("Default", connectionString)
            .AddCommand(/* command configuration */)));

    // Register your repositories
    services.AddScoped<IEmployeeRepository, EmployeeRepository>();
    services.AddScoped<IDepartmentRepository, DepartmentRepository>();
}
```

## Configuration Options

### Connection String Management

```csharp
services.UseSyrx(builder => builder
    .UseOracle(oracle => oracle
        // Multiple connection strings
        .AddConnectionString("Primary", "Data Source=prod-primary:1521/PROD;User Id=app;Password=secret;")
        .AddConnectionString("ReadReplica", "Data Source=prod-replica:1521/PROD;User Id=reader;Password=secret;")
        .AddConnectionString("Analytics", "Data Source=analytics:1521/DW;User Id=analyst;Password=secret;")
        
        .AddCommand(types => types
            .ForType<EmployeeRepository>(methods => methods
                .ForMethod("GetEmployees", command => command
                    .UseConnectionAlias("ReadReplica"))      // Read from replica
                .ForMethod("CreateEmployee", command => command
                    .UseConnectionAlias("Primary")))         // Write to primary
            .ForType<ReportRepository>(methods => methods
                .ForMethod("GenerateReport", command => command
                    .UseConnectionAlias("Analytics"))))));   // Use analytics DB
```

### Service Lifetime Configuration

```csharp
services.UseSyrx(builder => builder
    .UseOracle(
        oracle => oracle.AddConnectionString("Default", connectionString),
        ServiceLifetime.Scoped));  // Configure service lifetime
```

### Environment-Specific Configuration

```csharp
public void ConfigureServices(IServiceCollection services)
{
    var connectionString = Environment.IsDevelopment() 
        ? Configuration.GetConnectionString("Development")
        : Configuration.GetConnectionString("Production");

    services.UseSyrx(builder => builder
        .UseOracle(oracle => oracle
            .AddConnectionString("Default", connectionString)
            .AddCommand(LoadCommandConfiguration())));
}

private Action<ITypeSettingsBuilder> LoadCommandConfiguration()
{
    if (Environment.IsDevelopment())
    {
        // Development-specific commands with detailed logging
        return types => types
            .ForType<EmployeeRepository>(methods => methods
                .ForMethod("GetEmployees", command => command
                    .UseConnectionAlias("Default")
                    .UseCommandText("SELECT employee_id, first_name, last_name, email, hire_date FROM employees ORDER BY hire_date DESC")
                    .SetCommandTimeout(30)));
    }
    else
    {
        // Production-optimized commands
        return types => types
            .ForType<EmployeeRepository>(methods => methods
                .ForMethod("GetEmployees", command => command
                    .UseConnectionAlias("Default")
                    .UseCommandText("SELECT employee_id, first_name, last_name, email FROM employees")
                    .SetCommandTimeout(15)));
    }
}
```

## Advanced Configuration

### Oracle-Specific Features

```csharp
services.UseSyrx(builder => builder
    .UseOracle(oracle => oracle
        .AddConnectionString("Default", 
            "Data Source=localhost:1521/XE;User Id=hr;Password=password;" +
            "Min Pool Size=10;Max Pool Size=200;" +                     // Connection pooling
            "Connection Lifetime=300;" +                                // Pool management
            "Connection Timeout=30;Command Timeout=60;" +               // Timeouts
            "Pooling=true;Validate Connection=true")                    // Connection validation
        
        .AddCommand(types => types
            .ForType<ProductRepository>(methods => methods
                // Oracle hierarchical queries
                .ForMethod("GetProductHierarchy", command => command
                    .UseConnectionAlias("Default")
                    .UseCommandText(@"
                        SELECT product_id, product_name, parent_id, LEVEL as hierarchy_level
                        FROM products
                        START WITH parent_id IS NULL
                        CONNECT BY PRIOR product_id = parent_id
                        ORDER SIBLINGS BY product_name"))
                
                // Oracle analytical functions
                .ForMethod("GetProductSalesRanking", command => command
                    .UseConnectionAlias("Default")
                    .UseCommandText(@"
                        SELECT 
                            product_id, product_name, total_sales,
                            RANK() OVER (ORDER BY total_sales DESC) as sales_rank,
                            RATIO_TO_REPORT(total_sales) OVER () as percentage_of_total
                        FROM product_sales"))
                
                // PL/SQL procedure calls
                .ForMethod("UpdateProductPricing", command => command
                    .UseConnectionAlias("Default")
                    .UseCommandText("BEGIN update_product_pricing(:productId, :newPrice); END;"))))));
```

### Oracle Wallet Configuration

```csharp
services.UseSyrx(builder => builder
    .UseOracle(oracle => oracle
        .AddConnectionString("Wallet", 
            "Data Source=mydb_high;" +                                  // Oracle Cloud service name
            "TNS_ADMIN=/opt/oracle/wallet/;" +                         // Wallet location
            "Wallet_Location=/opt/oracle/wallet/;" +                   // Wallet files
            "Connection Timeout=60")                                   // Cloud timeout
        
        .AddCommand(/* secure command configuration */)));
```

### Performance Optimization

```csharp
services.UseSyrx(builder => builder
    .UseOracle(oracle => oracle
        // High-performance connection for OLTP
        .AddConnectionString("OLTP", 
            "Data Source=oltp.oracle.com:1521/PROD;User Id=app;Password=secret;" +
            "Min Pool Size=20;Max Pool Size=100;" +
            "Connection Lifetime=600;" +
            "Connection Timeout=5;Command Timeout=30;" +
            "Statement Cache Size=50;Self Tuning=true")
        
        // Analytics connection for long-running queries
        .AddConnectionString("Analytics", 
            "Data Source=analytics.oracle.com:1521/DW;User Id=analyst;Password=secret;" +
            "Min Pool Size=5;Max Pool Size=20;" +
            "Connection Lifetime=1800;" +
            "Connection Timeout=60;Command Timeout=600;" +
            "Statement Cache Size=100")
        
        .AddCommand(types => types
            .ForType<EmployeeRepository>(methods => methods
                .ForMethod("GetEmployee", command => command
                    .UseConnectionAlias("OLTP")
                    .SetCommandTimeout(10)))
            .ForType<ReportRepository>(methods => methods
                .ForMethod("GenerateComplexReport", command => command
                    .UseConnectionAlias("Analytics")
                    .SetCommandTimeout(1200))))));
```

## Configuration from Files

### JSON Configuration

```csharp
// appsettings.json
{
  "ConnectionStrings": {
    "Oracle": "Data Source=localhost:1521/XE;User Id=hr;Password=password;"
  },
  "Syrx": {
    "Commands": {
      "EmployeeRepository": {
        "GetAllEmployees": {
          "ConnectionAlias": "Default",
          "CommandText": "SELECT employee_id, first_name, last_name, email FROM employees"
        }
      }
    }
  }
}

// Startup.cs
services.UseSyrx(builder => builder
    .UseOracle(oracle => oracle
        .AddConnectionString("Default", Configuration.GetConnectionString("Oracle"))
        .FromConfiguration(Configuration.GetSection("Syrx"))));
```

### Configuration Builder Pattern

```csharp
public static class OracleConfiguration
{
    public static void ConfigureOracle(this OracleBuilder oracle, IConfiguration configuration)
    {
        oracle
            .AddConnectionString("Primary", configuration.GetConnectionString("Primary"))
            .AddConnectionString("ReadReplica", configuration.GetConnectionString("ReadReplica"))
            .AddCommand(types => types
                .ConfigureEmployeeRepository()
                .ConfigureDepartmentRepository()
                .ConfigureReportRepository());
    }
    
    private static ITypeSettingsBuilder ConfigureEmployeeRepository(this ITypeSettingsBuilder types)
    {
        return types.ForType<EmployeeRepository>(methods => methods
            .ForMethod("GetActiveEmployees", command => command
                .UseConnectionAlias("ReadReplica")
                .UseCommandText("SELECT * FROM employees WHERE is_active = 1"))
            .ForMethod("CreateEmployee", command => command
                .UseConnectionAlias("Primary")
                .UseCommandText("INSERT INTO employees (first_name, last_name, email) VALUES (:FirstName, :LastName, :Email)")));
    }
}

// Usage
services.UseSyrx(builder => builder
    .UseOracle(oracle => oracle.ConfigureOracle(Configuration)));
```

## Integration Examples

### ASP.NET Core Web API

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.UseSyrx(builder => builder
            .UseOracle(oracle => oracle
                .AddConnectionString("Default", Configuration.GetConnectionString("Oracle"))
                .AddCommand(/* configuration */)));

        services.AddControllers();
        services.AddScoped<IEmployeeService, EmployeeService>();
    }
}

[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeesController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Employee>>> GetEmployees()
    {
        var employees = await _employeeService.GetAllEmployeesAsync();
        return Ok(employees);
    }
}
```

### Background Services

```csharp
services.UseSyrx(builder => builder
    .UseOracle(oracle => oracle
        .AddConnectionString("Background", connectionString)
        .AddCommand(types => types
            .ForType<JobService>(methods => methods
                .ForMethod("GetPendingJobs", command => command
                    .UseConnectionAlias("Background")
                    .UseCommandText("SELECT * FROM job_queue WHERE status = 'PENDING'")))),
    ServiceLifetime.Singleton));

services.AddHostedService<JobProcessorService>();

public class JobProcessorService : BackgroundService
{
    private readonly ICommander<JobService> _commander;

    public JobProcessorService(ICommander<JobService> commander)
    {
        _commander = commander;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var pendingJobs = await _commander.QueryAsync<Job>();
            // Process jobs...
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
```

### Oracle Cloud Integration

```csharp
services.UseSyrx(builder => builder
    .UseOracle(oracle => oracle
        .AddConnectionString("AutonomousDB", 
            "Data Source=mydb_high;" +
            "User Id=admin;Password=complexPassword123!;" +
            "Connection Timeout=60;" +
            "TNS_ADMIN=/app/wallet/")
        .AddCommand(types => types
            .ForType<CustomerRepository>(methods => methods
                .ForMethod("GetCustomerInsights", command => command
                    .UseConnectionAlias("AutonomousDB")
                    .UseCommandText(@"
                        SELECT /*+ PARALLEL(4) */ 
                            customer_id, customer_name, total_orders, avg_order_value,
                            NTILE(5) OVER (ORDER BY total_orders DESC) as customer_tier
                        FROM customer_analytics")))));
```

### Testing Integration

```csharp
public class IntegrationTestBase
{
    protected ServiceProvider ServiceProvider { get; private set; }

    [SetUp]
    public void Setup()
    {
        var services = new ServiceCollection();
        
        services.UseSyrx(builder => builder
            .UseOracle(oracle => oracle
                .AddConnectionString("Test", GetTestConnectionString())
                .AddCommand(LoadTestConfiguration())));

        ServiceProvider = services.BuildServiceProvider();
    }

    private string GetTestConnectionString()
    {
        return "Data Source=localhost:1521/XE;User Id=test;Password=test;";
    }
}

[Test]
public async Task Should_Create_And_Retrieve_Employee()
{
    // Arrange
    var repository = ServiceProvider.GetService<EmployeeRepository>();
    var employee = new Employee { FirstName = "John", LastName = "Doe", Email = "john.doe@example.com" };

    // Act
    await repository.CreateEmployeeAsync(employee);
    var retrievedEmployee = await repository.GetEmployeeByEmailAsync(employee.Email);

    // Assert
    Assert.IsNotNull(retrievedEmployee);
    Assert.AreEqual(employee.FirstName, retrievedEmployee.FirstName);
}
```

## Service Lifetime Considerations

| Lifetime | Use Case | Oracle Considerations |
|----------|----------|----------------------|
| **Transient** | Default, stateless operations | New connection per operation, good for varied workloads |
| **Scoped** | Web requests, unit of work | Connection per request, ideal for web apps |
| **Singleton** | Background services, high throughput | Shared connection pool, careful with long-running operations |

## Oracle-Specific Optimizations

### Connection Pool Tuning

```csharp
// High-concurrency OLTP workload
.AddConnectionString("OLTP", 
    "Data Source=oltp:1521/PROD;User Id=app;Password=secret;" +
    "Min Pool Size=50;Max Pool Size=500;" +        // Large pool for high concurrency
    "Connection Lifetime=300;" +                   // Moderate lifetime
    "Connection Timeout=5;" +                      // Fast timeout
    "Incr Pool Size=10;Decr Pool Size=5;" +       // Pool growth/shrink
    "Validate Connection=true;Self Tuning=true")   // Oracle optimizations

// Data warehouse/analytics workload
.AddConnectionString("DW", 
    "Data Source=warehouse:1521/DW;User Id=analyst;Password=secret;" +
    "Min Pool Size=5;Max Pool Size=50;" +          // Smaller pool
    "Connection Lifetime=1800;" +                  // Longer lifetime
    "Connection Timeout=60;" +                     // Longer timeout
    "Statement Cache Size=200;" +                  // Large statement cache
    "Statement Cache Purge=false")                 // Keep statements cached
```

### Oracle Advanced Features

```csharp
// Parallel execution for large datasets
.ForMethod("ProcessLargeDataset", command => command
    .UseConnectionAlias("Analytics")
    .UseCommandText("SELECT /*+ PARALLEL(8) */ * FROM large_table WHERE process_date = :processDate"))

// Oracle partitioning awareness
.ForMethod("GetSalesByMonth", command => command
    .UseConnectionAlias("DW")
    .UseCommandText(@"
        SELECT /*+ PARTITION_WISE_JOIN */ 
            s.sale_id, s.customer_id, s.sale_date, s.amount
        FROM sales PARTITION(:partitionName) s
        WHERE s.sale_date BETWEEN :startDate AND :endDate"))

// Oracle Advanced Compression
.ForMethod("ArchiveOldData", command => command
    .UseConnectionAlias("Archive")
    .UseCommandText("ALTER TABLE sales_archive COMPRESS FOR QUERY HIGH"))
```

## Troubleshooting

### Common Configuration Issues

```csharp
// ❌ Missing connection alias
.ForMethod("GetEmployees", command => command
    .UseCommandText("SELECT * FROM employees"))  // Missing .UseConnectionAlias()

// ✅ Correct configuration
.ForMethod("GetEmployees", command => command
    .UseConnectionAlias("Default")
    .UseCommandText("SELECT * FROM employees"))

// ❌ Incorrect Oracle parameter syntax
.UseCommandText("SELECT * FROM employees WHERE id = @id")  // Wrong syntax for Oracle

// ✅ Correct Oracle parameter syntax
.UseCommandText("SELECT * FROM employees WHERE id = :id")  // Correct Oracle syntax
```

### Performance Troubleshooting

```csharp
// Enable Oracle trace and statistics
.AddConnectionString("Debug", 
    "Data Source=localhost:1521/XE;User Id=hr;Password=password;" +
    "Trace File Name=c:\\oracle\\trace\\app_trace.trc;" +
    "Trace Level=1;" +                            // Enable tracing
    "Statistics Enabled=true")                    // Enable statistics collection
```

### Oracle Cloud Troubleshooting

```csharp
// Autonomous Database connection troubleshooting
.AddConnectionString("ADB_Debug", 
    "Data Source=mydb_high;" +
    "User Id=admin;Password=password;" +
    "TNS_ADMIN=/app/wallet/;" +
    "Connection Timeout=120;" +                   // Longer timeout for cloud
    "Retry Count=3;Retry Delay=1000")            // Retry configuration
```

## Related Packages

- **[Syrx.Oracle](https://www.nuget.org/packages/Syrx.Oracle/)**: Core Oracle provider
- **[Syrx.Commanders.Databases.Connectors.Oracle](https://www.nuget.org/packages/Syrx.Commanders.Databases.Connectors.Oracle/)**: Oracle connector implementation
- **[Syrx](https://www.nuget.org/packages/Syrx/)**: Core Syrx framework
- **[Syrx.Extensions](https://www.nuget.org/packages/Syrx.Extensions/)**: Core dependency injection extensions

## License

This project is licensed under the [MIT License](https://github.com/Syrx/Syrx/blob/main/LICENSE).

## Credits

- Built on top of [Microsoft.Extensions.DependencyInjection](https://github.com/dotnet/extensions)
- Oracle connectivity via [Oracle.ManagedDataAccess.Core](https://www.nuget.org/packages/Oracle.ManagedDataAccess.Core/)
- High-performance data access with [Dapper](https://github.com/DapperLib/Dapper)