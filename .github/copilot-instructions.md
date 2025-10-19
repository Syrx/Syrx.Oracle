# Copilot Instructions for Syrx.Oracle

This document provides comprehensive instructions for AI agents working on the Syrx.Oracle project. Follow these guidelines to maintain code quality, architectural consistency, and documentation standards.

## Project Overview

Syrx.Oracle is a high-performance Oracle database provider for the Syrx data access framework. It provides:

- Native Oracle database connectivity via Oracle.ManagedDataAccess.Core
- High-performance data access through Dapper integration
- Oracle-specific features (PL/SQL, hierarchical queries, analytical functions)
- Multiple result set support via cursor parameters
- Comprehensive dependency injection integration
- Enterprise-ready Oracle Cloud and on-premises support

## Architecture Principles

### 1. Core Architecture

```
Application Layer
    ↓ (uses)
Repository Pattern (ICommander<T>)
    ↓ (implemented by)
DatabaseCommander<T>
    ↓ (uses)
IDatabaseConnector (OracleDatabaseConnector)
    ↓ (uses)
Oracle.ManagedDataAccess.Core
    ↓ (connects to)
Oracle Database
```

**Key Components:**
- **OracleDatabaseConnector**: Core Oracle connectivity implementation
- **OracleDynamicParameters**: Oracle-specific parameter handling for multiple result sets
- **Extension Methods**: Fluent configuration APIs for dependency injection
- **Command Configuration**: Centralized SQL command management

### 2. Design Patterns

- **Repository Pattern**: Data access abstraction through `ICommander<T>`
- **Builder Pattern**: Fluent configuration APIs
- **Factory Pattern**: Database connector creation
- **Extension Methods**: Clean dependency injection integration
- **Static Factory Methods**: `OracleDynamicParameters.Cursors()`

### 3. Separation of Concerns

- **Data Access**: Repositories using `ICommander<T>`
- **Business Logic**: Service layer consuming repositories
- **Configuration**: Centralized command and connection configuration
- **Oracle-Specific Logic**: Isolated in dedicated classes and methods

## Code Standards

### 1. Naming Conventions

**Classes and Interfaces:**
```csharp
// ✅ Correct
public class EmployeeRepository { }
public interface IEmployeeRepository { }
public class OracleDatabaseConnector { }

// ❌ Incorrect
public class employeeRepository { }
public interface IEmployeeRepo { }
public class OracleDBConnector { }
```

**Methods:**
```csharp
// ✅ Correct - Async suffix for async methods
public async Task<Employee> GetEmployeeByIdAsync(int id) { }
public async Task<bool> CreateEmployeeAsync(Employee employee) { }

// ❌ Incorrect
public async Task<Employee> GetEmployeeById(int id) { }
public async Task<bool> CreateEmployee(Employee employee) { }
```

**Properties and Fields:**
```csharp
// ✅ Correct
public int EmployeeId { get; set; }
private readonly ICommander<EmployeeRepository> _commander;

// ❌ Incorrect  
public int employeeId { get; set; }
private readonly ICommander<EmployeeRepository> commander;
```

### 2. Oracle-Specific Conventions

**Parameter Names:**
```csharp
// ✅ Correct - Oracle uses colon prefix
.UseCommandText("SELECT * FROM employees WHERE employee_id = :employeeId")

// ❌ Incorrect - SQL Server syntax
.UseCommandText("SELECT * FROM employees WHERE employee_id = @employeeId")
```

**Multiple Result Sets:**
```csharp
// ✅ Correct - Use OracleDynamicParameters with Query method
using static Syrx.Commanders.Databases.Oracle.OracleDynamicParameters;
var parameters = Cursors(new { departmentId });
var result = await _commander.QueryAsync(mapFunction, parameters);

// ❌ Incorrect - Will cause ORA-01008 error
var result = await _commander.QueryAsync(mapFunction, new { departmentId });
```

### 3. Error Handling

**Oracle-Specific Exceptions:**
```csharp
// ✅ Correct - Handle Oracle-specific errors
try
{
    await _commander.ExecuteAsync(employee);
}
catch (OracleException ex) when (ex.Number == 1) // Unique constraint
{
    throw new DuplicateEmployeeException($"Employee with email {employee.Email} already exists", ex);
}
catch (OracleException ex) when (ex.Number == 2291) // Foreign key constraint
{
    throw new InvalidReferenceException("Referenced record does not exist", ex);
}
catch (OracleException ex)
{
    throw new DatabaseException($"Oracle error {ex.Number}: {ex.Message}", ex);
}
```

## Documentation Standards

### 1. XML Documentation

**All public classes, interfaces, and methods MUST have comprehensive XML documentation:**

```csharp
/// <summary>
/// Provides Oracle database connectivity for the Syrx framework.
/// This connector implements Oracle-specific database connection and command execution capabilities
/// using Oracle.ManagedDataAccess.Core as the underlying provider.
/// </summary>
/// <remarks>
/// <para>
/// The OracleDatabaseConnector provides native Oracle database support through the official
/// Oracle.ManagedDataAccess.Core provider. It handles Oracle-specific connection pooling,
/// parameter binding, and transaction management.
/// </para>
/// <para>
/// Key features include:
/// <list type="bullet">
/// <item><description>Native Oracle connection pooling and management</description></item>
/// <item><description>Oracle-specific data type handling</description></item>
/// <item><description>PL/SQL stored procedure and function support</description></item>
/// </list>
/// </para>
/// </remarks>
/// <param name="settings">The commander settings containing connection strings and command configurations.</param>
/// <exception cref="ArgumentNullException">Thrown when <paramref name="settings"/> is null.</exception>
/// <example>
/// <para>Basic usage through dependency injection:</para>
/// <code>
/// services.AddScoped&lt;IDatabaseConnector, OracleDatabaseConnector&gt;();
/// </code>
/// </example>
public class OracleDatabaseConnector(ICommanderSettings settings) : DatabaseConnector(settings, () => OracleClientFactory.Instance)
{
}
```

### 2. README Files

**Every project MUST have a comprehensive README.md:**

Structure:
- Table of Contents
- Overview
- Key Features
- Installation instructions
- Usage examples
- Oracle-specific features
- Configuration options
- API reference
- Performance considerations
- Related packages
- Requirements
- License
- Credits

### 3. Code Comments

**Complex Oracle-specific logic should be well-commented:**

```csharp
// Oracle doesn't natively support multiple result sets like SQL Server.
// Instead, it uses REF CURSOR parameters to return multiple result sets
// from stored procedures or PL/SQL blocks.
private void AddCursorParameters(params string[] refCursorNames)
{
    foreach (string refCursorName in refCursorNames)
    {
        // Create cursor parameter with OracleDbType.RefCursor and Output direction
        var oracleParameter = new OracleParameter(refCursorName, OracleDbType.RefCursor, ParameterDirection.Output);
        oracleParameters.Add(oracleParameter);
    }
}
```

## Oracle-Specific Guidelines

### 1. Multiple Result Sets

**ALWAYS use `OracleDynamicParameters` for multiple result sets:**

```csharp
// ✅ Correct approach
public async Task<(IEnumerable<Employee>, IEnumerable<Department>)> GetEmployeesAndDepartmentsAsync()
{
    using static Syrx.Commanders.Databases.Oracle.OracleDynamicParameters;
    
    var parameters = Cursors();
    
    // Define mapping function that processes multiple result sets
    Func<IEnumerable<Employee>, IEnumerable<Department>, (IEnumerable<Employee>, IEnumerable<Department>)> map = 
        (employees, departments) => (employees, departments);
    
    var result = await _commander.QueryAsync(map, parameters);
    return result.Single(); // Syrx returns IEnumerable of mapped results
}

// Configuration
.UseCommandText(@"
    BEGIN
        OPEN :1 FOR SELECT employee_id, first_name, last_name FROM employees;
        OPEN :2 FOR SELECT department_id, department_name FROM departments;
    END;")
```

### 2. PL/SQL Integration

**Prefer PL/SQL for complex operations:**

```csharp
// ✅ Good - Use PL/SQL for complex business logic
.UseCommandText(@"
    BEGIN
        pkg_employee.process_employee_data(:employeeId, :salary, :result);
    END;")

// ✅ Good - Use Oracle functions for calculations
.UseCommandText("SELECT pkg_employee.calculate_bonus(:employeeId) FROM dual")

// ❌ Avoid - Complex logic in C#
public decimal CalculateBonus(Employee employee)
{
    // Complex calculation logic...
}
```

### 3. Oracle Data Types

**Use appropriate Oracle data types:**

```csharp
public class OracleEntity
{
    public decimal Id { get; set; }           // NUMBER
    public string Name { get; set; }          // VARCHAR2
    public string Description { get; set; }   // CLOB
    public byte[] Document { get; set; }      // BLOB
    public DateTime CreatedDate { get; set; } // DATE
    public DateTime Timestamp { get; set; }   // TIMESTAMP
}
```

### 4. Connection String Patterns

**Standard connection string patterns:**

```csharp
// Basic connection
"Data Source=localhost:1521/XE;User Id=hr;Password=password;"

// With connection pooling
"Data Source=localhost:1521/XE;User Id=hr;Password=password;" +
"Min Pool Size=10;Max Pool Size=200;Connection Lifetime=300;"

// Oracle Cloud Autonomous Database
"Data Source=mydb_high;User Id=admin;Password=password;" +
"TNS_ADMIN=/app/wallet/;Connection Timeout=60;"

// Oracle RAC
"Data Source=(DESCRIPTION=(ADDRESS_LIST=" +
"(ADDRESS=(PROTOCOL=TCP)(HOST=rac1)(PORT=1521))" +
"(ADDRESS=(PROTOCOL=TCP)(HOST=rac2)(PORT=1521))" +
"(LOAD_BALANCE=yes)(FAILOVER=yes))" +
"(CONNECT_DATA=(SERVICE_NAME=myservice)))" +
";User Id=app;Password=secret;"
```

## Configuration Patterns

### 1. Service Registration

**Standard Syrx.Oracle service registration:**

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.UseSyrx(builder => builder
        .UseOracle(oracle => oracle
            .AddConnectionString("Default", Configuration.GetConnectionString("Oracle")!)
            .AddCommand(ConfigureCommands)));

    services.AddScoped<IEmployeeRepository, EmployeeRepository>();
}

private static void ConfigureCommands(ITypeSettingsBuilder types)
{
    types.ForType<EmployeeRepository>(methods => methods
        .ForMethod(nameof(EmployeeRepository.GetAllAsync), command => command
            .UseConnectionAlias("Default")
            .UseCommandText("SELECT employee_id, first_name, last_name FROM employees"))
        
        .ForMethod(nameof(EmployeeRepository.CreateAsync), command => command
            .UseConnectionAlias("Default")
            .UseCommandText("INSERT INTO employees (first_name, last_name) VALUES (:FirstName, :LastName)")));
}
```

### 2. Multiple Connections

**Pattern for multiple database connections:**

```csharp
services.UseSyrx(builder => builder
    .UseOracle(oracle => oracle
        .AddConnectionString("Primary", primaryConnectionString)
        .AddConnectionString("ReadOnly", readOnlyConnectionString)
        .AddConnectionString("Analytics", analyticsConnectionString)
        .AddCommand(types => types
            .ForType<EmployeeRepository>(methods => methods
                .ForMethod("GetEmployees", command => command
                    .UseConnectionAlias("ReadOnly"))    // Read operations
                .ForMethod("CreateEmployee", command => command
                    .UseConnectionAlias("Primary")))    // Write operations
            .ForType<ReportRepository>(methods => methods
                .ForMethod("GenerateReport", command => command
                    .UseConnectionAlias("Analytics"))))); // Analytics
```

## Testing Guidelines

### 1. Unit Tests

**Repository unit tests:**

```csharp
[Test]
public async Task Should_Get_Employee_By_Id()
{
    // Arrange
    var services = new ServiceCollection();
    services.UseSyrx(builder => builder
        .UseOracle(oracle => oracle
            .AddConnectionString("Test", GetTestConnectionString())
            .AddCommand(ConfigureTestCommands)));
    
    var provider = services.BuildServiceProvider();
    var repository = provider.GetService<IEmployeeRepository>();
    
    // Act
    var employee = await repository.GetByIdAsync(100);
    
    // Assert
    Assert.IsNotNull(employee);
    Assert.AreEqual(100, employee.EmployeeId);
}
```

### 2. Integration Tests

**Oracle-specific integration tests:**

```csharp
public class OracleIntegrationTests
{
    [Test]
    public async Task Should_Handle_Multiple_Result_Sets()
    {
        // Test Oracle cursor parameter functionality
        var result = await _repository.GetEmployeesAndDepartmentsAsync();
        
        Assert.IsNotNull(result.employees);
        Assert.IsNotNull(result.departments);
    }
    
    [Test]
    public async Task Should_Execute_PL_SQL_Procedure()
    {
        // Test PL/SQL procedure execution
        var success = await _repository.ProcessEmployeeDataAsync(employee);
        
        Assert.IsTrue(success);
    }
}
```

## Performance Guidelines

### 1. Connection Pooling

**Configure Oracle connection pooling appropriately:**

```csharp
// High-throughput OLTP
"Min Pool Size=50;Max Pool Size=500;Connection Lifetime=600;" +
"Connection Timeout=5;Command Timeout=30;Statement Cache Size=100;"

// Analytics/Reporting
"Min Pool Size=5;Max Pool Size=50;Connection Lifetime=1800;" +
"Connection Timeout=60;Command Timeout=600;Statement Cache Size=200;"
```

### 2. Query Optimization

**Use Oracle-specific optimizations:**

```csharp
// Use Oracle hints
.UseCommandText(@"
    SELECT /*+ FIRST_ROWS(100) INDEX(e emp_dept_idx) */ 
        employee_id, first_name, last_name
    FROM employees e
    WHERE department_id = :departmentId")

// Parallel execution
.UseCommandText(@"
    SELECT /*+ PARALLEL(e, 4) */ 
        department_id, COUNT(*) as employee_count
    FROM employees e
    GROUP BY department_id")
```

### 3. Bulk Operations

**Use Oracle bulk operations for large datasets:**

```csharp
public async Task<bool> BulkInsertEmployeesAsync(IEnumerable<Employee> employees)
{
    return await _commander.ExecuteAsync(employees);
}

// Configuration with appropriate timeout
.ForMethod("BulkInsertEmployeesAsync", command => command
    .UseConnectionAlias("Default")
    .UseCommandText("INSERT INTO employees (first_name, last_name) VALUES (:FirstName, :LastName)")
    .SetCommandTimeout(300))
```

## Troubleshooting Common Issues

### 1. ORA-01008: not all variables bound

**Cause**: Using multiple result sets without `OracleDynamicParameters`

**Solution**: Always use `OracleDynamicParameters.Cursors()` for multiple result sets with appropriate mapping function

### 2. Connection Pool Exhaustion

**Cause**: Not properly disposing connections or too small pool size

**Solution**: Configure appropriate pool sizes and ensure proper connection lifecycle

### 3. Parameter Binding Issues

**Cause**: Using wrong parameter syntax (@ instead of :)

**Solution**: Always use Oracle colon-prefix syntax (`:parameterName`)

## Security Guidelines

### 1. Parameter Binding

**ALWAYS use parameterized queries:**

```csharp
// ✅ Secure - parameterized
.UseCommandText("SELECT * FROM employees WHERE email = :email")

// ❌ Insecure - SQL injection risk
.UseCommandText($"SELECT * FROM employees WHERE email = '{email}'")
```

### 2. Connection Security

**Use secure connection strings:**

```csharp
// Oracle Wallet for SSL
"Data Source=secure.oracle.com:2484/PROD;User Id=app;Password=secret;" +
"Wallet_Location=/app/wallet/;TNS_ADMIN=/app/tns/;"

// Connection validation
"Validate Connection=true;Connection Lifetime=300;"
```

### 3. Credential Management

**Never hardcode credentials:**

```csharp
// ✅ Good - use configuration
var connectionString = Configuration.GetConnectionString("Oracle");

// ✅ Good - use environment variables
var connectionString = $"Data Source={Environment.GetEnvironmentVariable("ORACLE_HOST")};...";

// ❌ Bad - hardcoded credentials
var connectionString = "Data Source=localhost;User Id=hr;Password=password;";
```

## Maintenance Guidelines

### 1. Package Updates

- Monitor Oracle.ManagedDataAccess.Core updates
- Test thoroughly after Oracle driver updates
- Update Syrx dependencies consistently
- Review Oracle compatibility matrices

### 2. Performance Monitoring

- Monitor connection pool usage
- Track query execution times
- Watch for Oracle-specific errors
- Monitor memory usage with large result sets

### 3. Documentation Maintenance

- Keep README files current with API changes
- Update XML documentation for all changes
- Maintain example code accuracy
- Update migration guides for new features

## When Adding New Features

### 1. Oracle Feature Integration

**Research Oracle capabilities first:**
1. Check Oracle documentation for feature support
2. Test with different Oracle versions (12c, 19c, 21c)
3. Consider Oracle Cloud compatibility
4. Document version requirements

### 2. API Design

**Follow established patterns:**
1. Use fluent configuration APIs
2. Provide both simple and advanced options
3. Include comprehensive examples
4. Add appropriate error handling

### 3. Testing Requirements

**Comprehensive testing for new features:**
1. Unit tests for all public methods
2. Integration tests with real Oracle database
3. Performance benchmarks
4. Edge case testing
5. Oracle version compatibility testing

## Code Review Checklist

When reviewing code, ensure:

- [ ] Comprehensive XML documentation on all public members
- [ ] Oracle-specific error handling
- [ ] Proper use of `OracleDynamicParameters` for multiple result sets
- [ ] Parameterized queries (no SQL injection risks)
- [ ] Appropriate connection string patterns
- [ ] Performance considerations (indexing, hints, bulk operations)
- [ ] Unit and integration tests
- [ ] README updates if public API changed
- [ ] Consistent naming conventions
- [ ] Proper async/await patterns
- [ ] Resource disposal (using statements where appropriate)

## Getting Help

### Resources

1. **Oracle Documentation**: Official Oracle database and driver documentation
2. **Syrx Core Documentation**: Understand base framework patterns
3. **Dapper Documentation**: Understand underlying ORM capabilities
4. **Oracle Community**: Oracle community forums and support

### Debugging Oracle Issues

1. **Enable Oracle Tracing**: Add trace parameters to connection string
2. **Check Oracle Error Codes**: Use Oracle error documentation
3. **Monitor SQL Execution**: Use Oracle SQL monitoring tools
4. **Connection Pool Monitoring**: Enable performance counters

This document should be your primary reference for maintaining consistency, quality, and Oracle-specific best practices in the Syrx.Oracle project.