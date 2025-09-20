# Syrx.Oracle

Oracle database data access provider for the Syrx framework.

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Oracle-Specific Features](#oracle-specific-features)
- [Configuration Patterns](#configuration-patterns)
- [Advanced Usage](#advanced-usage)
- [Multiple Result Sets](#multiple-result-sets)
- [Error Handling](#error-handling)
- [Performance Tips](#performance-tips)
- [Testing](#testing)
- [Migration Scenarios](#migration-scenarios)
- [Related Packages](#related-packages)
- [Requirements](#requirements)
- [License](#license)
- [Credits](#credits)

## Overview

`Syrx.Oracle` provides Oracle database connectivity for the Syrx data access framework. Built on top of Oracle.ManagedDataAccess.Core (Oracle's official .NET provider), this package offers seamless integration with Oracle databases while maintaining Syrx's core principles of control, performance, and flexibility.

## Features

- **Oracle Integration**: Native support for Oracle databases via Oracle.ManagedDataAccess.Core
- **High Performance**: Leverages Oracle's optimized connection handling and Dapper's speed
- **Oracle Types**: Support for Oracle-specific data types (NUMBER, VARCHAR2, CLOB, BLOB, etc.)
- **PL/SQL Support**: Execute stored procedures, functions, and PL/SQL blocks
- **Advanced Features**: Support for Oracle advanced features (partitioning, parallel execution, etc.)
- **Connection Pooling**: Efficient Oracle connection pool management
- **Transaction Support**: Full transaction control with rollback capabilities
- **Async/Await**: Complete async operation support
- **Multi-mapping**: Complex object composition from query results

## Installation

```bash
dotnet add package Syrx.Oracle
```

**Package Manager**
```bash
Install-Package Syrx.Oracle
```

**PackageReference**
```xml
<PackageReference Include="Syrx.Oracle" Version="2.4.5" />
```

## Quick Start

### 1. Configure Services

```csharp
using Syrx.Oracle.Extensions;

public void ConfigureServices(IServiceCollection services)
{
    services.UseSyrx(builder => builder
        .UseOracle(oracle => oracle
            .AddConnectionString("Default", "Data Source=localhost:1521/XE;User Id=hr;Password=password;")
            .AddCommand(types => types
                .ForType<EmployeeRepository>(methods => methods
                    .ForMethod(nameof(EmployeeRepository.GetAllEmployeesAsync), command => command
                        .UseConnectionAlias("Default")
                        .UseCommandText("SELECT employee_id, first_name, last_name, email, hire_date FROM employees"))))));
}
```

### 2. Create Repository

```csharp
public class EmployeeRepository
{
    private readonly ICommander<EmployeeRepository> _commander;

    public EmployeeRepository(ICommander<EmployeeRepository> commander)
    {
        _commander = commander;
    }

    public async Task<IEnumerable<Employee>> GetAllEmployeesAsync()
        => await _commander.QueryAsync<Employee>();

    public async Task<Employee> GetEmployeeByIdAsync(int employeeId)
        => await _commander.QueryAsync<Employee>(new { employeeId }).SingleOrDefaultAsync();

    public async Task<bool> CreateEmployeeAsync(Employee employee)
        => await _commander.ExecuteAsync(employee) > 0;
}
```

### 3. Configure Commands

```csharp
services.UseSyrx(builder => builder
    .UseOracle(oracle => oracle
        .AddConnectionString("Default", connectionString)
        .AddCommand(types => types
            .ForType<EmployeeRepository>(methods => methods
                .ForMethod(nameof(EmployeeRepository.GetEmployeeByIdAsync), command => command
                    .UseConnectionAlias("Default")
                    .UseCommandText("SELECT employee_id, first_name, last_name, email, hire_date FROM employees WHERE employee_id = :employeeId"))
                .ForMethod(nameof(EmployeeRepository.CreateEmployeeAsync), command => command
                    .UseConnectionAlias("Default")
                    .UseCommandText("INSERT INTO employees (first_name, last_name, email, hire_date) VALUES (:FirstName, :LastName, :Email, :HireDate)"))))));
```

## Oracle-Specific Features

### Oracle Data Types

Oracle's specific data types are fully supported:

```csharp
public class OracleEntity
{
    public decimal Id { get; set; }           // NUMBER
    public string Name { get; set; }          // VARCHAR2
    public string Description { get; set; }   // CLOB
    public byte[] Document { get; set; }      // BLOB
    public DateTime CreatedDate { get; set; } // DATE
    public DateTime Timestamp { get; set; }   // TIMESTAMP
    public decimal Salary { get; set; }       // NUMBER(10,2)
}
```

### PL/SQL Support

Execute stored procedures and functions:

```csharp
public class EmployeeRepository
{
    private readonly ICommander<EmployeeRepository> _commander;

    public EmployeeRepository(ICommander<EmployeeRepository> commander)
    {
        _commander = commander;
    }

    // Call stored procedure
    public async Task<bool> UpdateEmployeeSalaryAsync(int employeeId, decimal newSalary)
        => await _commander.ExecuteAsync(new { employeeId, newSalary }) > 0;

    // Call function
    public async Task<decimal> CalculateAnnualSalaryAsync(int employeeId)
        => await _commander.QueryAsync<decimal>(new { employeeId }).SingleOrDefaultAsync();
}

// Configuration for PL/SQL
.ForMethod(nameof(EmployeeRepository.UpdateEmployeeSalaryAsync), command => command
    .UseConnectionAlias("Default")
    .UseCommandText("BEGIN update_employee_salary(:employeeId, :newSalary); END;"))
.ForMethod(nameof(EmployeeRepository.CalculateAnnualSalaryAsync), command => command
    .UseConnectionAlias("Default")
    .UseCommandText("SELECT calculate_annual_salary(:employeeId) FROM dual"))
```

### Oracle-Specific SQL Features

```csharp
public class ReportRepository
{
    private readonly ICommander<ReportRepository> _commander;

    public ReportRepository(ICommander<ReportRepository> commander)
    {
        _commander = commander;
    }

    // Oracle hierarchical queries
    public async Task<IEnumerable<Employee>> GetEmployeeHierarchyAsync(int managerId)
        => await _commander.QueryAsync<Employee>(new { managerId });

    // Oracle analytical functions
    public async Task<IEnumerable<SalesReport>> GetSalesRankingAsync()
        => await _commander.QueryAsync<SalesReport>();
}

// Oracle hierarchical query
.UseCommandText(@"
    SELECT employee_id, first_name, last_name, manager_id, LEVEL as hierarchy_level
    FROM employees
    START WITH manager_id = :managerId
    CONNECT BY PRIOR employee_id = manager_id
    ORDER SIBLINGS BY last_name")

// Oracle analytical functions
.UseCommandText(@"
    SELECT 
        salesperson_id,
        name,
        total_sales,
        RANK() OVER (ORDER BY total_sales DESC) as sales_rank,
        LAG(total_sales) OVER (ORDER BY total_sales DESC) as previous_sales,
        RATIO_TO_REPORT(total_sales) OVER () as percentage_of_total
    FROM sales_summary")
```

## Configuration Patterns

### Basic Configuration

```csharp
services.UseSyrx(builder => builder
    .UseOracle(oracle => oracle
        .AddConnectionString("Default", "Data Source=localhost:1521/XE;User Id=hr;Password=password;")));
```

### Multiple Databases

```csharp
services.UseSyrx(builder => builder
    .UseOracle(oracle => oracle
        .AddConnectionString("Primary", "Data Source=prod-primary:1521/PROD;User Id=app;Password=secret;")
        .AddConnectionString("ReadOnly", "Data Source=prod-replica:1521/PROD;User Id=reader;Password=secret;")
        .AddCommand(types => types
            .ForType<ReportRepository>(methods => methods
                .ForMethod("GetReportData", command => command
                    .UseConnectionAlias("ReadOnly")))  // Use read-only for reports
            .ForType<EmployeeRepository>(methods => methods
                .ForMethod("CreateEmployee", command => command
                    .UseConnectionAlias("Primary"))))));  // Use primary for writes
```

### Connection Pool Optimization

```csharp
services.UseSyrx(builder => builder
    .UseOracle(oracle => oracle
        .AddConnectionString("Optimized", 
            "Data Source=localhost:1521/XE;User Id=app;Password=password;" +
            "Min Pool Size=10;Max Pool Size=200;Connection Lifetime=300;" +
            "Connection Timeout=30;Command Timeout=60;")));
```

### Oracle Wallet Configuration

```csharp
services.UseSyrx(builder => builder
    .UseOracle(oracle => oracle
        .AddConnectionString("Wallet", 
            "Data Source=mydb_high;TNS_ADMIN=/opt/oracle/wallet/;Wallet_Location=/opt/oracle/wallet/;")));
```

## Advanced Usage

### Bulk Operations

```csharp
public async Task<bool> BulkInsertEmployeesAsync(IEnumerable<Employee> employees)
{
    // Use Oracle bulk insert
    return await _commander.ExecuteAsync(employees) > 0;
}

// Configuration for bulk operations
.ForMethod(nameof(EmployeeRepository.BulkInsertEmployeesAsync), command => command
    .UseConnectionAlias("Default")
    .UseCommandText("INSERT INTO employees (first_name, last_name, email) VALUES (:FirstName, :LastName, :Email)")
    .SetCommandTimeout(300))
```

### Complex Queries with CTEs

```csharp
public async Task<IEnumerable<EmployeeStatistics>> GetEmployeeStatisticsAsync()
    => await _commander.QueryAsync<EmployeeStatistics>();

// Oracle CTE support (12c+)
.UseCommandText(@"
    WITH emp_stats AS (
        SELECT 
            employee_id,
            COUNT(*) as project_count,
            SUM(hours) as total_hours,
            AVG(hours) as avg_hours
        FROM project_assignments 
        GROUP BY employee_id
    )
    SELECT 
        e.employee_id,
        e.first_name,
        e.last_name,
        NVL(es.project_count, 0) as project_count,
        NVL(es.total_hours, 0) as total_hours,
        NVL(es.avg_hours, 0) as avg_hours
    FROM employees e
    LEFT JOIN emp_stats es ON e.employee_id = es.employee_id")
```

### Oracle Partitioning

```csharp
public async Task<IEnumerable<Sale>> GetSalesByDateRangeAsync(DateTime startDate, DateTime endDate)
    => await _commander.QueryAsync<Sale>(new { startDate, endDate });

// Leverage Oracle partitioning
.UseCommandText(@"
    SELECT /*+ PARALLEL(4) */ 
        sale_id, customer_id, sale_date, amount, region
    FROM sales PARTITION FOR (TO_DATE(:startDate, 'YYYY-MM-DD'))
    WHERE sale_date BETWEEN :startDate AND :endDate")
```

## Error Handling

Oracle-specific error handling:

```csharp
public async Task<Employee> CreateEmployeeAsync(Employee employee)
{
    try
    {
        var success = await _commander.ExecuteAsync(employee) > 0;
        return success ? employee : null;
    }
    catch (OracleException ex) when (ex.Number == 1) // ORA-00001: unique constraint violated
    {
        throw new DuplicateEmployeeException($"Employee with email {employee.Email} already exists", ex);
    }
    catch (OracleException ex) when (ex.Number == 2291) // ORA-02291: integrity constraint violated
    {
        throw new InvalidReferenceException("Referenced record does not exist", ex);
    }
    catch (OracleException ex) when (ex.Number == 1017) // ORA-01017: invalid username/password
    {
        throw new AuthenticationException("Invalid database credentials", ex);
    }
}
```

## Performance Tips

### Connection Management
- Use connection pooling for better performance
- Configure appropriate pool sizes based on load
- Set reasonable connection lifetimes

### Query Optimization
- Use Oracle hints when appropriate
- Leverage Oracle's cost-based optimizer
- Use bind variables for repeated queries

### Oracle-Specific Optimizations
- Use Oracle's parallel execution capabilities
- Leverage partitioning for large tables
- Use Oracle's advanced compression features

## Testing

Example test configuration:

```csharp
[Test]
public async Task Should_Retrieve_Employees_From_Oracle()
{
    // Arrange
    var services = new ServiceCollection();
    services.UseSyrx(builder => builder
        .UseOracle(oracle => oracle
            .AddConnectionString("Test", "Data Source=localhost:1521/XE;User Id=test;Password=test;")
            .AddCommand(types => types
                .ForType<EmployeeRepository>(methods => methods
                    .ForMethod(nameof(EmployeeRepository.GetAllEmployeesAsync), command => command
                        .UseConnectionAlias("Test")
                        .UseCommandText("SELECT employee_id, first_name, last_name FROM employees"))))));

    var provider = services.BuildServiceProvider();
    var repository = provider.GetService<EmployeeRepository>();

    // Act
    var employees = await repository.GetAllEmployeesAsync();

    // Assert
    Assert.IsNotNull(employees);
}
```

## Migration from Other Providers

### From Entity Framework

```csharp
// Entity Framework
var employees = await context.Employees
    .Where(e => e.IsActive)
    .OrderBy(e => e.LastName)
    .ToListAsync();

// Syrx equivalent
public async Task<IEnumerable<Employee>> GetActiveEmployeesAsync()
    => await _commander.QueryAsync<Employee>();

.UseCommandText("SELECT * FROM employees WHERE is_active = 1 ORDER BY last_name")
```

### From Raw ADO.NET

```csharp
// Raw ADO.NET
using var connection = new OracleConnection(connectionString);
await connection.OpenAsync();
using var command = new OracleCommand("SELECT * FROM employees WHERE employee_id = :id", connection);
command.Parameters.Add(":id", OracleDbType.Int32).Value = employeeId;
using var reader = await command.ExecuteReaderAsync();
// Manual mapping...

// Syrx equivalent
public async Task<Employee> GetEmployeeByIdAsync(int employeeId)
    => await _commander.QueryAsync<Employee>(new { employeeId }).SingleOrDefaultAsync();
```

## Oracle Cloud Integration

### Autonomous Database

```csharp
services.UseSyrx(builder => builder
    .UseOracle(oracle => oracle
        .AddConnectionString("AutonomousDB", 
            "Data Source=mydb_high;User Id=admin;Password=password;" +
            "Connection Timeout=60;TNS_ADMIN=/opt/oracle/wallet/;")));
```

### Oracle Cloud Infrastructure

```csharp
services.UseSyrx(builder => builder
    .UseOracle(oracle => oracle
        .AddConnectionString("OCI", 
            "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=oracle.oci.com)(PORT=1521))" +
            "(CONNECT_DATA=(SERVICE_NAME=myservice)));User Id=app;Password=secret;" +
            "Connection Timeout=30;Command Timeout=120;")));
```

## Related Packages

- **[Syrx.Oracle.Extensions](https://www.nuget.org/packages/Syrx.Oracle.Extensions/)**: Dependency injection extensions
- **[Syrx.Commanders.Databases.Connectors.Oracle](https://www.nuget.org/packages/Syrx.Commanders.Databases.Connectors.Oracle/)**: Core Oracle connector
- **[Syrx](https://www.nuget.org/packages/Syrx/)**: Core Syrx framework
- **[Syrx.Commanders.Databases](https://www.nuget.org/packages/Syrx.Commanders.Databases/)**: Database command framework

## Requirements

- **.NET 8.0** or later
- **Oracle Database 12c** or later (recommended)
- **Oracle.ManagedDataAccess.Core 3.21** or later

## License

This project is licensed under the [MIT License](https://github.com/Syrx/Syrx/blob/main/LICENSE).

## Credits

- Built on top of [Oracle.ManagedDataAccess.Core](https://www.nuget.org/packages/Oracle.ManagedDataAccess.Core/) - Oracle's official .NET provider
- Uses [Dapper](https://github.com/DapperLib/Dapper) for object mapping
- Inspired by Oracle's powerful enterprise database features