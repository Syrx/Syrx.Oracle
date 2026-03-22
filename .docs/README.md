# Syrx.Oracle Documentation

Comprehensive technical documentation for the Syrx Oracle database provider.

## Table of Contents

- [Overview](#overview)
- [Quick Start](#quick-start)
- [Architecture](#architecture)
- [Installation](#installation)
- [Configuration](#configuration)
- [Usage Patterns](#usage-patterns)
- [Oracle-Specific Features](#oracle-specific-features)
- [Performance](#performance)
- [Troubleshooting](#troubleshooting)
- [API Reference](#api-reference)
- [Examples](#examples)
- [Migration Guide](#migration-guide)

## Overview

Syrx.Oracle provides high-performance Oracle database connectivity for .NET applications using the Syrx data access framework. Built on Oracle.ManagedDataAccess.Core and Dapper, it offers:

- **Native Oracle Support**: Full Oracle database feature support
- **High Performance**: Optimized for Oracle's capabilities
- **Type Safety**: Compile-time safety with runtime performance
- **Flexible Configuration**: Multiple configuration patterns
- **Enterprise Ready**: Support for Oracle Cloud, RAC, and enterprise features

## Quick Start

### 1. Installation

```bash
dotnet add package Syrx.Oracle.Extensions
```

### 2. Basic Setup

```csharp
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
```

### 3. Repository Usage

```csharp
public class EmployeeRepository
{
    private readonly ICommander<EmployeeRepository> _commander;

    public EmployeeRepository(ICommander<EmployeeRepository> commander)
    {
        _commander = commander;
    }

    public async Task<IEnumerable<Employee>> GetAllAsync()
        => await _commander.QueryAsync<Employee>();

    public async Task<Employee> GetByIdAsync(int id)
        => await _commander.QueryAsync<Employee>(new { id }).SingleOrDefaultAsync();
}
```

## Architecture

Syrx.Oracle consists of several key components:

### Core Components

```
┌─────────────────────────────────────────┐
│              Application                │
├─────────────────────────────────────────┤
│           Repository Layer              │
│  ┌─────────────────────────────────┐    │
│  │      ICommander<T>              │    │
│  └─────────────────────────────────┘    │
├─────────────────────────────────────────┤
│         Syrx.Oracle Layer               │
│  ┌─────────────────────────────────┐    │
│  │    DatabaseCommander<T>         │    │
│  │  ┌─────────────────────────┐    │    │
│  │  │  IDatabaseConnector     │    │    │
│  │  │ (OracleDatabaseConnector)    │    │
│  │  └─────────────────────────┘    │    │
│  └─────────────────────────────────┘    │
├─────────────────────────────────────────┤
│         Oracle Provider Layer           │
│  ┌─────────────────────────────────┐    │
│  │  Oracle.ManagedDataAccess.Core  │    │
│  └─────────────────────────────────┘    │
├─────────────────────────────────────────┤
│            Oracle Database              │
└─────────────────────────────────────────┘
```

### Package Structure

- **Syrx.Oracle**: Core Oracle provider
- **Syrx.Oracle.Extensions**: Dependency injection extensions
- **Syrx.Commanders.Databases.Oracle**: Oracle-specific components
- **Syrx.Commanders.Databases.Connectors.Oracle**: Oracle connector
- **Syrx.Commanders.Databases.Connectors.Oracle.Extensions**: Connector DI extensions

## Installation

### Package Dependencies

```xml
<!-- Primary package (includes all dependencies) -->
<PackageReference Include="Syrx.Oracle.Extensions" Version="3.0.0" />

<!-- Or individual packages -->
<PackageReference Include="Syrx.Oracle" Version="3.0.0" />
<PackageReference Include="Syrx.Commanders.Databases.Oracle" Version="3.0.0" />
<PackageReference Include="Syrx.Commanders.Databases.Connectors.Oracle" Version="3.0.0" />
```

### Requirements

- **.NET 8.0** or later
- **Oracle Database 12c** or later (recommended)
- **Oracle.ManagedDataAccess.Core 3.21** or later

## Configuration

### Connection String Examples

#### Basic Connection
```csharp
"Data Source=localhost:1521/XE;User Id=hr;Password=password;"
```

#### With Connection Pooling
```csharp
"Data Source=localhost:1521/XE;User Id=hr;Password=password;" +
"Min Pool Size=10;Max Pool Size=200;Connection Lifetime=300;"
```

#### Oracle Cloud Autonomous Database
```csharp
"Data Source=mydb_high;User Id=admin;Password=password;" +
"TNS_ADMIN=/app/wallet/;Connection Timeout=60;"
```

#### Oracle RAC
```csharp
"Data Source=(DESCRIPTION=" +
"(ADDRESS_LIST=" +
"(ADDRESS=(PROTOCOL=TCP)(HOST=rac1)(PORT=1521))" +
"(ADDRESS=(PROTOCOL=TCP)(HOST=rac2)(PORT=1521))" +
"(LOAD_BALANCE=yes)(FAILOVER=yes))" +
"(CONNECT_DATA=(SERVICE_NAME=myservice)))" +
";User Id=app;Password=secret;"
```

### Service Configuration

#### Basic Configuration
```csharp
services.UseSyrx(builder => builder
    .UseOracle(oracle => oracle
        .AddConnectionString("Default", connectionString)
        .AddCommand(/* command configuration */)));
```

#### Multiple Connections
```csharp
services.UseSyrx(builder => builder
    .UseOracle(oracle => oracle
        .AddConnectionString("Primary", primaryConnectionString)
        .AddConnectionString("ReadOnly", readOnlyConnectionString)
        .AddCommand(types => types
            .ForType<UserRepository>(methods => methods
                .ForMethod("GetUsers", command => command
                    .UseConnectionAlias("ReadOnly"))
                .ForMethod("CreateUser", command => command
                    .UseConnectionAlias("Primary"))))));
```

#### Service Lifetimes
```csharp
services.UseSyrx(builder => builder
    .UseOracle(
        oracle => oracle.AddConnectionString("Default", connectionString),
        ServiceLifetime.Scoped));
```

## Usage Patterns

### Repository Pattern

```csharp
public interface IEmployeeRepository
{
    Task<IEnumerable<Employee>> GetAllAsync();
    Task<Employee> GetByIdAsync(int id);
    Task<Employee> CreateAsync(Employee employee);
    Task<bool> UpdateAsync(Employee employee);
    Task<bool> DeleteAsync(int id);
}

public class EmployeeRepository : IEmployeeRepository
{
    private readonly ICommander<EmployeeRepository> _commander;

    public EmployeeRepository(ICommander<EmployeeRepository> commander)
    {
        _commander = commander;
    }

    public async Task<IEnumerable<Employee>> GetAllAsync()
        => await _commander.QueryAsync<Employee>();

    public async Task<Employee> GetByIdAsync(int id)
        => await _commander.QueryAsync<Employee>(new { id }).SingleOrDefaultAsync();

    public async Task<Employee> CreateAsync(Employee employee)
        => await _commander.ExecuteAsync(employee) ? employee : default;

    public async Task<bool> UpdateAsync(Employee employee)
        => await _commander.ExecuteAsync(employee);

    public async Task<bool> DeleteAsync(int id)
        => await _commander.ExecuteAsync(new { id });
}
```

### Service Layer Pattern

```csharp
public class EmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IDepartmentRepository _departmentRepository;

    public EmployeeService(
        IEmployeeRepository employeeRepository,
        IDepartmentRepository departmentRepository)
    {
        _employeeRepository = employeeRepository;
        _departmentRepository = departmentRepository;
    }

    public async Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeRequest request)
    {
        // Validate department exists
        var department = await _departmentRepository.GetByIdAsync(request.DepartmentId);
        if (department == null)
            throw new InvalidOperationException("Department not found");

        // Create employee
        var employee = new Employee
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            DepartmentId = request.DepartmentId
        };

        var createdEmployee = await _employeeRepository.CreateAsync(employee);
        return MapToDto(createdEmployee);
    }
}
```

## Oracle-Specific Features

### Multiple Result Sets

Oracle handles multiple result sets differently than other databases. Use `OracleDynamicParameters`:

```csharp
using static Syrx.Commanders.Databases.Oracle.OracleDynamicParameters;

public async Task<(IEnumerable<Employee>, IEnumerable<Department>)> 
    GetEmployeesAndDepartmentsAsync(int departmentId)
{
    var arguments = new { departmentId };
    var parameters = Cursors(arguments);
    
    // Define mapping function that processes multiple result sets
    Func<IEnumerable<Employee>, IEnumerable<Department>, (IEnumerable<Employee>, IEnumerable<Department>)> map = 
        (employees, departments) => (employees, departments);
    
    var result = await _commander.QueryAsync(map, parameters);
    return result.Single(); // Syrx returns IEnumerable of mapped results
}
```

### PL/SQL Support

```csharp
// Stored procedure execution
public async Task<bool> ProcessEmployeeDataAsync(Employee employee)
{
    return await _commander.ExecuteAsync(employee);
}

// Configuration
.ForMethod("ProcessEmployeeDataAsync", command => command
    .UseConnectionAlias("Default")
    .UseCommandText("PKG_EMPLOYEE.PROCESS_EMPLOYEE_DATA")
    .SetCommandType(CommandType.StoredProcedure))
```

### Oracle Data Types

```csharp
public class ComplexEntity
{
    public decimal Id { get; set; }           // NUMBER
    public string Name { get; set; }          // VARCHAR2
    public string Description { get; set; }   // CLOB
    public byte[] Document { get; set; }      // BLOB
    public DateTime CreatedDate { get; set; } // DATE
    public DateTime Timestamp { get; set; }   // TIMESTAMP
}
```

### Hierarchical Queries

```csharp
public async Task<IEnumerable<EmployeeHierarchy>> GetEmployeeHierarchyAsync(int managerId)
    => await _commander.QueryAsync<EmployeeHierarchy>(new { managerId });

// Configuration
.UseCommandText(@"
    SELECT employee_id, first_name, last_name, manager_id, LEVEL as hierarchy_level
    FROM employees
    START WITH manager_id = :managerId
    CONNECT BY PRIOR employee_id = manager_id
    ORDER SIBLINGS BY last_name")
```

### Analytical Functions

```csharp
public async Task<IEnumerable<SalesReport>> GetSalesRankingAsync()
    => await _commander.QueryAsync<SalesReport>();

// Configuration
.UseCommandText(@"
    SELECT 
        salesperson_id, name, total_sales,
        RANK() OVER (ORDER BY total_sales DESC) as sales_rank,
        RATIO_TO_REPORT(total_sales) OVER () as percentage_of_total
    FROM sales_summary")
```

## Performance

### Connection Pool Optimization

```csharp
// High-throughput OLTP
var oltpConnectionString = 
    "Data Source=localhost:1521/PROD;User Id=app;Password=secret;" +
    "Min Pool Size=50;Max Pool Size=500;" +
    "Connection Lifetime=600;" +
    "Connection Timeout=5;Command Timeout=30;" +
    "Statement Cache Size=100;Self Tuning=true;";

// Data warehouse workload
var dwConnectionString = 
    "Data Source=warehouse:1521/DW;User Id=analyst;Password=secret;" +
    "Min Pool Size=5;Max Pool Size=50;" +
    "Connection Lifetime=1800;" +
    "Connection Timeout=60;Command Timeout=600;" +
    "Statement Cache Size=200;";
```

### Query Optimization

```csharp
// Use Oracle hints
.UseCommandText(@"
    SELECT /*+ FIRST_ROWS(100) INDEX(e emp_dept_idx) */ 
        employee_id, first_name, last_name, salary
    FROM employees e
    WHERE department_id = :departmentId
    ORDER BY salary DESC")

// Parallel execution
.UseCommandText(@"
    SELECT /*+ PARALLEL(e, 4) */ 
        department_id, COUNT(*) as employee_count, AVG(salary) as avg_salary
    FROM employees e
    GROUP BY department_id")
```

### Bulk Operations

```csharp
public async Task<bool> BulkInsertEmployeesAsync(IEnumerable<Employee> employees)
{
    return await _commander.ExecuteAsync(employees);
}

// Configuration with longer timeout
.ForMethod("BulkInsertEmployeesAsync", command => command
    .UseConnectionAlias("Default")
    .UseCommandText("INSERT INTO employees (first_name, last_name, email) VALUES (:FirstName, :LastName, :Email)")
    .SetCommandTimeout(300))
```

## Troubleshooting

### Common Issues

#### ORA-01008: not all variables bound

This occurs when using multiple result sets without `OracleDynamicParameters`:

```csharp
// ❌ Wrong - will cause ORA-01008
var result = await _commander.QueryAsync(mapFunction, new { id = 1 });

// ✅ Correct - use OracleDynamicParameters
using static Syrx.Commanders.Databases.Oracle.OracleDynamicParameters;
var parameters = Cursors(new { id = 1 });
var result = await _commander.QueryAsync(mapFunction, parameters);
```

#### Connection Pool Exhaustion

```csharp
// Monitor pool usage
"Data Source=localhost:1521/XE;User Id=hr;Password=password;" +
"Min Pool Size=10;Max Pool Size=100;" +
"Connection Lifetime=300;" +
"Performance Counters=true;"  // Enable monitoring
```

#### Timeout Issues

```csharp
// Increase timeouts for long-running operations
.ForMethod("LongRunningReport", command => command
    .UseConnectionAlias("Analytics")
    .UseCommandText("/* complex query */")
    .SetCommandTimeout(1200))  // 20 minutes
```

### Debugging

#### Enable Oracle Tracing

```csharp
"Data Source=localhost:1521/XE;User Id=hr;Password=password;" +
"Trace File Name=c:\\oracle\\trace\\app_trace.trc;" +
"Trace Level=1;"  // Enable SQL tracing
```

#### Connection String Validation

```csharp
public static bool ValidateOracleConnection(string connectionString)
{
    try
    {
        using var connection = new OracleConnection(connectionString);
        connection.Open();
        using var command = new OracleCommand("SELECT 1 FROM dual", connection);
        var result = command.ExecuteScalar();
        return result != null;
    }
    catch (Exception ex)
    {
        // Log exception details
        return false;
    }
}
```

## API Reference

See individual package documentation:

- [Syrx.Oracle](../src/Syrx.Oracle/README.md)
- [Syrx.Oracle.Extensions](../src/Syrx.Oracle.Extensions/README.md)
- [Syrx.Commanders.Databases.Oracle](../src/Syrx.Commanders.Databases.Oracle/README.md)
- [Syrx.Commanders.Databases.Connectors.Oracle](../src/Syrx.Commanders.Databases.Connectors.Oracle/README.md)

## Examples

See the [examples](examples/) directory for complete working examples:

- [Basic CRUD Operations](examples/basic-crud/)
- [Advanced Oracle Features](examples/oracle-features/)
- [Performance Optimization](examples/performance/)
- [Cloud Integration](examples/cloud/)
- [Enterprise Scenarios](examples/enterprise/)

## Migration Guide

### From Entity Framework

- [Entity Framework to Syrx.Oracle](migration/entity-framework.md)

### From Raw ADO.NET

- [ADO.NET to Syrx.Oracle](migration/adonet.md)

### From Other ORMs

- [Dapper to Syrx.Oracle](migration/dapper.md)
- [NHibernate to Syrx.Oracle](migration/nhibernate.md)

---

For more information, visit the [Syrx.Oracle GitHub repository](https://github.com/Syrx/Syrx.Oracle).
