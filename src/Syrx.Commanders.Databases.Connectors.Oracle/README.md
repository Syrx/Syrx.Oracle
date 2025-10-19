# Syrx.Commanders.Databases.Connectors.Oracle

Core Oracle database connector for the Syrx framework.

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Installation](#installation)
- [Architecture](#architecture)
- [Key Components](#key-components)
- [Connection Management](#connection-management)
- [Configuration](#configuration)
- [Oracle-Specific Features](#oracle-specific-features)
- [Multiple Result Sets](#multiple-result-sets)
- [Error Handling](#error-handling)
- [Performance Considerations](#performance-considerations)
- [Testing](#testing)
- [Related Packages](#related-packages)
- [Requirements](#requirements)
- [License](#license)
- [Credits](#credits)

## Overview

`Syrx.Commanders.Databases.Connectors.Oracle` provides the foundational Oracle database connectivity layer for the Syrx framework. This package implements the `IDatabaseConnector` interface specifically for Oracle databases using Oracle.ManagedDataAccess.Core as the underlying provider.

## Features

- **Native Oracle Support**: Direct integration with Oracle.ManagedDataAccess.Core provider
- **Connection Pool Management**: Efficient Oracle connection pooling
- **Transaction Support**: Full transaction lifecycle management
- **Async Operations**: Complete async/await pattern support
- **Error Handling**: Oracle-specific error handling and recovery
- **Parameter Binding**: Safe parameter binding with Oracle types
- **Performance Optimized**: Leverages Oracle's performance characteristics

## Installation

> **Note**: This package is typically installed automatically as a dependency of `Syrx.Oracle` or `Syrx.Oracle.Extensions`.

```bash
dotnet add package Syrx.Commanders.Databases.Connectors.Oracle
```

**Package Manager**
```bash
Install-Package Syrx.Commanders.Databases.Connectors.Oracle
```

**PackageReference**
```xml
<PackageReference Include="Syrx.Commanders.Databases.Connectors.Oracle" Version="3.0.0" />
```

## Core Components

### OracleDatabaseConnector

The main connector implementation that inherits from `DatabaseConnector`:

```csharp
public class OracleDatabaseConnector : DatabaseConnector
{
    public OracleDatabaseConnector() 
        : base(() => OracleClientFactory.Instance)
    {
    }

    public override DbProviderFactory Factory => OracleClientFactory.Instance;
}
```

### Key Characteristics

- **Provider Factory**: Uses `OracleClientFactory.Instance` for connection creation
- **Thread-Safe**: Designed for concurrent access across multiple threads
- **Connection Lifecycle**: Automatic connection opening/closing and disposal
- **Transaction Management**: Supports both explicit and implicit transactions

## Oracle-Specific Features

### Data Type Support

The connector provides native support for Oracle data types:

```csharp
// Oracle native types
public class OracleEntity
{
    public decimal Id { get; set; }           // NUMBER
    public string Name { get; set; }          // VARCHAR2
    public string Description { get; set; }   // CLOB
    public byte[] Document { get; set; }      // BLOB
    public DateTime CreatedDate { get; set; } // DATE
    public DateTime Timestamp { get; set; }   // TIMESTAMP
    public decimal Amount { get; set; }       // NUMBER(10,2)
    public string LargeText { get; set; }     // LONG
}
```

### Parameter Handling

Oracle-specific parameter binding:

```csharp
// Oracle parameter syntax (colon prefix)
var employeeId = 12345;
var result = await commander.QueryAsync<Employee>(new { employeeId });

// CLOB parameters
var description = "Large text content...";
var result = await commander.ExecuteAsync(new { description });

// DATE parameters
var hireDate = DateTime.Now;
var result = await commander.ExecuteAsync(new { hireDate });

// NUMBER parameters with precision
var salary = 75000.50m;
var result = await commander.ExecuteAsync(new { salary });
```

### Connection String Support

Supports all Oracle.ManagedDataAccess.Core connection string parameters:

```csharp
// Basic connection
"Data Source=localhost:1521/XE;User Id=hr;Password=password;"

// With pooling configuration
"Data Source=localhost:1521/XE;User Id=hr;Password=password;Min Pool Size=10;Max Pool Size=200;"

// With Oracle Wallet
"Data Source=mydb_high;TNS_ADMIN=/opt/oracle/wallet/;Wallet_Location=/opt/oracle/wallet/;"

// With timeouts and tuning
"Data Source=localhost:1521/XE;User Id=hr;Password=password;Connection Timeout=30;Command Timeout=60;Statement Cache Size=50;"
```

## Usage Examples

### Through Dependency Injection

```csharp
// Automatic registration via extensions
services.UseSyrx(builder => builder
    .UseOracle(oracle => oracle
        .AddConnectionString("Default", connectionString)
        .AddCommand(/* configuration */)));

// Manual registration (advanced scenarios)
services.AddTransient<IDatabaseConnector, OracleDatabaseConnector>();
```

## Connection Management

### Connection Pooling

The connector leverages Oracle's connection pooling:

```csharp
// Pool configuration via connection string
var connectionString = 
    "Data Source=localhost:1521/XE;User Id=hr;Password=password;" +
    "Min Pool Size=10;" +        // Minimum connections to keep open
    "Max Pool Size=200;" +       // Maximum total connections
    "Connection Lifetime=300;" + // Connection lifetime in seconds
    "Pooling=true;" +            // Enable pooling (default: true)
    "Incr Pool Size=10;" +       // Pool growth increment
    "Decr Pool Size=5;";         // Pool shrink decrement
```

### Connection Lifecycle

```csharp
// Connection lifecycle is automatically managed
public async Task<T> QueryAsync<T>(
    string connectionString,
    CommandSetting commandSetting,
    object parameters = null,
    CancellationToken cancellationToken = default)
{
    // 1. Get connection from pool
    using var connection = Factory.CreateConnection();
    connection.ConnectionString = connectionString;
    
    // 2. Open connection
    await connection.OpenAsync(cancellationToken);
    
    // 3. Execute command
    var result = await connection.QueryAsync<T>(
        commandSetting.CommandText, 
        parameters);
    
    // 4. Connection automatically returned to pool on disposal
    return result;
}
```

## Transaction Support

### Automatic Transaction Management

```csharp
public async Task<bool> ExecuteAsync(
    string connectionString,
    CommandSetting commandSetting,
    object parameters = null,
    CancellationToken cancellationToken = default)
{
    using var connection = Factory.CreateConnection();
    connection.ConnectionString = connectionString;
    await connection.OpenAsync(cancellationToken);
    
    using var transaction = await connection.BeginTransactionAsync(cancellationToken);
    try
    {
        var result = await connection.ExecuteAsync(
            commandSetting.CommandText,
            parameters,
            transaction);
            
        await transaction.CommitAsync(cancellationToken);
        return result > 0;
    }
    catch
    {
        await transaction.RollbackAsync(cancellationToken);
        throw;
    }
}
```

### Transaction Isolation Levels

```csharp
// Oracle supports standard isolation levels
using var transaction = await connection.BeginTransactionAsync(
    IsolationLevel.ReadCommitted,  // Default
    cancellationToken);

// Oracle-specific: Serializable isolation
using var transaction = await connection.BeginTransactionAsync(
    IsolationLevel.Serializable,
    cancellationToken);
```

## Error Handling

### Oracle-Specific Exceptions

```csharp
public async Task<T> QueryWithErrorHandlingAsync<T>(/* parameters */)
{
    try
    {
        return await connector.QueryAsync<T>(/* parameters */);
    }
    catch (OracleException ex)
    {
        // Oracle-specific error handling
        switch (ex.Number)
        {
            case 1: // ORA-00001: unique constraint violated
                throw new DuplicateKeyException("Record already exists", ex);
            case 2291: // ORA-02291: integrity constraint violated - parent key not found
                throw new ReferenceConstraintException("Referenced record not found", ex);
            case 2290: // ORA-02290: check constraint violated
                throw new CheckConstraintException("Check constraint violated", ex);
            case 942: // ORA-00942: table or view does not exist
                throw new TableNotFoundException("Table does not exist", ex);
            case 904: // ORA-00904: invalid identifier
                throw new ColumnNotFoundException("Column does not exist", ex);
            case 1017: // ORA-01017: invalid username/password; logon denied
                throw new AuthenticationException("Invalid database credentials", ex);
            case 12541: // ORA-12541: TNS:no listener
                throw new DatabaseConnectionException("Database server not available", ex);
            default:
                throw; // Re-throw unknown Oracle errors
        }
    }
    catch (Exception ex) when (ex.Message.Contains("timeout"))
    {
        throw new TimeoutException("Database operation timed out", ex);
    }
}
```

## Performance Considerations

### Connection Pool Optimization

```csharp
// Optimize for high-throughput scenarios
var highThroughputConnectionString = 
    "Data Source=localhost:1521/PROD;User Id=app;Password=secret;" +
    "Min Pool Size=50;Max Pool Size=500;" +      // Large pool for high concurrency
    "Connection Lifetime=600;" +                 // Longer connection lifetime
    "Incr Pool Size=20;Decr Pool Size=10;" +    // Aggressive pool scaling
    "Connection Timeout=5;" +                    // Fast connection timeout
    "Statement Cache Size=100;" +                // Large statement cache
    "Self Tuning=true;";                        // Enable Oracle self-tuning

// Optimize for low-latency scenarios  
var lowLatencyConnectionString =
    "Data Source=localhost:1521/PROD;User Id=app;Password=secret;" +
    "Min Pool Size=20;Max Pool Size=100;" +      // Moderate pool size
    "Connection Lifetime=300;" +                 // Shorter lifetime
    "Connection Timeout=3;" +                    // Very fast timeout
    "Statement Cache Size=50;" +                 // Moderate cache
    "Validate Connection=true;";                 // Ensure connection validity
```

### Statement Caching

The connector automatically benefits from Oracle's statement caching:

```csharp
// Repeated executions of the same SQL will be automatically cached
var sql = "SELECT * FROM employees WHERE department_id = :departmentId";

// First execution: statement prepared and cached
var employees1 = await connector.QueryAsync<Employee>(connectionString, new { departmentId = 10 });

// Subsequent executions: use cached statement
var employees2 = await connector.QueryAsync<Employee>(connectionString, new { departmentId = 20 });
```

### Oracle-Specific Optimizations

```csharp
// Use Oracle hints for performance
var sqlWithHints = @"
    SELECT /*+ FIRST_ROWS(100) INDEX(e emp_dept_idx) */ 
        employee_id, first_name, last_name, salary
    FROM employees e
    WHERE department_id = :departmentId
    ORDER BY salary DESC";

// Leverage Oracle parallel execution
var parallelSql = @"
    SELECT /*+ PARALLEL(e, 4) */ 
        department_id, COUNT(*) as employee_count, AVG(salary) as avg_salary
    FROM employees e
    GROUP BY department_id";
```

## Integration Testing

### Test Setup

```csharp
[TestFixture]
public class OracleConnectorTests
{
    private OracleDatabaseConnector _connector;
    private string _testConnectionString;

    [SetUp]
    public void Setup()
    {
        _connector = new OracleDatabaseConnector();
        _testConnectionString = "Data Source=localhost:1521/XE;User Id=test;Password=test;";
    }

    [Test]
    public async Task Should_Connect_To_Oracle()
    {
        // Arrange
        var commandSetting = new CommandSetting
        {
            ConnectionAlias = "test",
            CommandText = "SELECT * FROM v$version WHERE ROWNUM = 1"
        };

        // Act
        var result = await _connector.QueryAsync<string>(_testConnectionString, commandSetting);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.First().Contains("Oracle"));
    }
}
```

### Docker Test Environment

```yaml
# docker-compose.test.yml
version: '3.8'
services:
  oracle-test:
    image: gvenzl/oracle-xe:21-slim
    environment:
      ORACLE_PASSWORD: test
      ORACLE_DATABASE: TESTDB
    ports:
      - "1521:1521"
    volumes:
      - oracle_data:/opt/oracle/oradata
```

## Oracle Cloud Integration

### Autonomous Database Support

```csharp
// Autonomous Database connection
var autonomousConnectionString = 
    "Data Source=mydb_high;" +                   // Service name
    "User Id=admin;Password=complexPassword123!;" +
    "TNS_ADMIN=/app/wallet/;" +                  // Wallet location
    "Wallet_Location=/app/wallet/;" +            // Wallet files
    "Connection Timeout=60;" +                   // Cloud timeout
    "Statement Cache Size=100;";                 // Performance tuning
```

### Oracle Cloud Infrastructure

```csharp
// OCI Database Service connection
var ociConnectionString = 
    "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=oracle.oci.com)(PORT=1521))" +
    "(CONNECT_DATA=(SERVICE_NAME=myservice)));" +
    "User Id=app;Password=secret;" +
    "Connection Timeout=30;" +
    "Command Timeout=120;" +
    "HA Events=true;";                          // High availability events
```

## Monitoring and Diagnostics

### Connection Pool Monitoring

```csharp
// Enable Oracle tracing
var connectionStringWithTracing = 
    "Data Source=localhost:1521/XE;User Id=hr;Password=password;" +
    "Trace File Name=c:\\oracle\\trace\\app_trace.trc;" +
    "Trace Level=1;" +                          // Enable SQL tracing
    "Statistics Enabled=true;" +                // Enable statistics
    "Performance Counters=true;";               // Enable performance counters
```

### Performance Metrics

```csharp
public class OracleMetrics
{
    private readonly IMetrics _metrics;

    public async Task<T> QueryWithMetricsAsync<T>(/* parameters */)
    {
        using var activity = _metrics.StartActivity("oracle.query");
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var result = await _connector.QueryAsync<T>(/* parameters */);
            _metrics.Counter("oracle.queries.success").Increment();
            return result;
        }
        catch (OracleException ex)
        {
            _metrics.Counter("oracle.queries.error").Increment();
            _metrics.Histogram("oracle.errors").Record(1, new[] { 
                new KeyValuePair<string, object>("error_code", ex.Number),
                new KeyValuePair<string, object>("error_type", ex.GetType().Name) 
            });
            throw;
        }
        finally
        {
            _metrics.Histogram("oracle.query.duration")
                .Record(stopwatch.ElapsedMilliseconds);
        }
    }
}
```

## Security Considerations

### Connection Security

```csharp
// SSL/TLS configuration (Oracle 12c+)
var secureConnectionString = 
    "Data Source=secure.oracle.com:2484/PROD;" +
    "User Id=app;Password=secret;" +
    "Connection Timeout=30;" +
    "Wallet_Location=/app/wallet/;" +           // Oracle Wallet for SSL
    "TNS_ADMIN=/app/tns/;";                     // TNS configuration

// Advanced security features
var advancedSecurityConnectionString = 
    "Data Source=localhost:1521/PROD;" +
    "User Id=app;Password=secret;" +
    "DBA Privilege=Normal;" +                   // Privilege level
    "Validate Connection=true;" +               // Connection validation
    "Connection Lifetime=300;";                 // Limit connection lifetime
```

### Parameter Security

```csharp
// The connector automatically handles parameter sanitization
public async Task<Employee> GetEmployeeByEmailAsync(string email)
{
    // Safe from SQL injection - parameters are properly bound
    var sql = "SELECT * FROM employees WHERE email = :email";
    var employee = await _connector.QueryAsync<Employee>(connectionString, new { email });
    return employee.FirstOrDefault();
}

// ❌ Never do this - SQL injection vulnerability
// var sql = $"SELECT * FROM employees WHERE email = '{email}'";
```

## Oracle-Specific Features

### PL/SQL Block Execution

```csharp
// Execute PL/SQL anonymous blocks
var plsqlBlock = @"
    BEGIN
        update_employee_salary(:employeeId, :newSalary);
        calculate_bonus(:employeeId);
        COMMIT;
    END;";

var result = await _connector.ExecuteAsync(connectionString, new { 
    employeeId = 12345, 
    newSalary = 75000 
});
```

### Oracle Object Types

```csharp
// Work with Oracle object types and collections
var sql = @"
    SELECT employee_id, 
           address_type(street, city, state, zip) as address
    FROM employees 
    WHERE employee_id = :employeeId";

var result = await _connector.QueryAsync<Employee>(connectionString, new { employeeId });
```

### Hierarchical Queries

```csharp
// Oracle CONNECT BY hierarchical queries
var hierarchicalSql = @"
    SELECT employee_id, first_name, last_name, manager_id, LEVEL
    FROM employees
    START WITH manager_id IS NULL
    CONNECT BY PRIOR employee_id = manager_id
    ORDER SIBLINGS BY last_name";

var hierarchy = await _connector.QueryAsync<EmployeeHierarchy>(connectionString, null);
```

## Related Packages

- **[Syrx.Oracle](https://www.nuget.org/packages/Syrx.Oracle/)**: High-level Oracle provider
- **[Syrx.Oracle.Extensions](https://www.nuget.org/packages/Syrx.Oracle.Extensions/)**: Dependency injection extensions
- **[Syrx.Commanders.Databases.Connectors](https://www.nuget.org/packages/Syrx.Commanders.Databases.Connectors/)**: Base connector interfaces
- **[Oracle.ManagedDataAccess.Core](https://www.nuget.org/packages/Oracle.ManagedDataAccess.Core/)**: The underlying Oracle provider

## Requirements

- **.NET 8.0** or later
- **Oracle Database 12c** or later (recommended)
- **Oracle.ManagedDataAccess.Core 3.21** or later

## License

This project is licensed under the [MIT License](https://github.com/Syrx/Syrx/blob/main/LICENSE).

## Credits

- Built on top of [Oracle.ManagedDataAccess.Core](https://www.nuget.org/packages/Oracle.ManagedDataAccess.Core/) - Oracle's official .NET provider
- Integrates with [Dapper](https://github.com/DapperLib/Dapper) for high-performance data access
- Follows Oracle database best practices and conventions
