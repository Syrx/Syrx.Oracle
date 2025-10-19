# Basic CRUD Operations

This example demonstrates basic Create, Read, Update, Delete operations using Syrx.Oracle.

## Setup

### 1. Project Configuration

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Syrx.Oracle.Extensions" Version="2.4.5" />
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="8.0.0" />
  </ItemGroup>
</Project>
```

### 2. Database Schema

```sql
-- Create employees table
CREATE TABLE employees (
    employee_id NUMBER(10) PRIMARY KEY,
    first_name VARCHAR2(50) NOT NULL,
    last_name VARCHAR2(50) NOT NULL,
    email VARCHAR2(100) UNIQUE NOT NULL,
    salary NUMBER(10,2),
    hire_date DATE DEFAULT SYSDATE,
    department_id NUMBER(10),
    is_active NUMBER(1) DEFAULT 1
);

-- Create sequence for employee_id
CREATE SEQUENCE emp_seq START WITH 1 INCREMENT BY 1;

-- Create trigger for auto-increment
CREATE OR REPLACE TRIGGER emp_id_trigger
    BEFORE INSERT ON employees
    FOR EACH ROW
BEGIN
    SELECT emp_seq.NEXTVAL INTO :NEW.employee_id FROM dual;
END;
```

### 3. Configuration

**appsettings.json**
```json
{
  "ConnectionStrings": {
    "Oracle": "Data Source=localhost:1521/XE;User Id=hr;Password=password;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  }
}
```

## Implementation

### 1. Employee Model

```csharp
public class Employee
{
    public int EmployeeId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public decimal? Salary { get; set; }
    public DateTime HireDate { get; set; }
    public int? DepartmentId { get; set; }
    public bool IsActive { get; set; } = true;
    
    public string FullName => $"{FirstName} {LastName}";
}
```

### 2. Repository Interface

```csharp
public interface IEmployeeRepository
{
    Task<IEnumerable<Employee>> GetAllAsync();
    Task<IEnumerable<Employee>> GetActiveAsync();
    Task<Employee?> GetByIdAsync(int id);
    Task<Employee?> GetByEmailAsync(string email);
    Task<Employee> CreateAsync(Employee employee);
    Task<bool> UpdateAsync(Employee employee);
    Task<bool> DeleteAsync(int id);
    Task<bool> DeactivateAsync(int id);
    Task<int> GetCountAsync();
}
```

### 3. Repository Implementation

```csharp
public class EmployeeRepository : IEmployeeRepository
{
    private readonly ICommander<EmployeeRepository> _commander;

    public EmployeeRepository(ICommander<EmployeeRepository> commander)
    {
        _commander = commander;
    }

    public async Task<IEnumerable<Employee>> GetAllAsync()
        => await _commander.QueryAsync<Employee>();

    public async Task<IEnumerable<Employee>> GetActiveAsync()
        => await _commander.QueryAsync<Employee>();

    public async Task<Employee?> GetByIdAsync(int id)
        => await _commander.QueryAsync<Employee>(new { id }).SingleOrDefaultAsync();

    public async Task<Employee?> GetByEmailAsync(string email)
        => await _commander.QueryAsync<Employee>(new { email }).SingleOrDefaultAsync();

    public async Task<Employee> CreateAsync(Employee employee)
    {
        var result = await _commander.ExecuteAsync(employee);
        return result ? employee : throw new InvalidOperationException("Failed to create employee");
    }

    public async Task<bool> UpdateAsync(Employee employee)
        => await _commander.ExecuteAsync(employee);

    public async Task<bool> DeleteAsync(int id)
        => await _commander.ExecuteAsync(new { id });

    public async Task<bool> DeactivateAsync(int id)
        => await _commander.ExecuteAsync(new { id });

    public async Task<int> GetCountAsync()
        => await _commander.QueryAsync<int>().SingleOrDefaultAsync();
}
```

### 4. Configuration

```csharp
public class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Configure Syrx with Oracle
                services.UseSyrx(builder => builder
                    .UseOracle(oracle => oracle
                        .AddConnectionString("Default", context.Configuration.GetConnectionString("Oracle")!)
                        .AddCommand(ConfigureCommands)));

                // Register repositories
                services.AddScoped<IEmployeeRepository, EmployeeRepository>();
                services.AddScoped<EmployeeService>();
            })
            .Build();

        // Run example
        var service = host.Services.GetRequiredService<EmployeeService>();
        await service.RunExampleAsync();
    }

    private static void ConfigureCommands(ITypeSettingsBuilder types)
    {
        types.ForType<EmployeeRepository>(methods => methods
            // Read operations
            .ForMethod(nameof(EmployeeRepository.GetAllAsync), command => command
                .UseConnectionAlias("Default")
                .UseCommandText(@"
                    SELECT employee_id, first_name, last_name, email, salary, 
                           hire_date, department_id, is_active
                    FROM employees 
                    ORDER BY employee_id"))
            
            .ForMethod(nameof(EmployeeRepository.GetActiveAsync), command => command
                .UseConnectionAlias("Default")
                .UseCommandText(@"
                    SELECT employee_id, first_name, last_name, email, salary, 
                           hire_date, department_id, is_active
                    FROM employees 
                    WHERE is_active = 1 
                    ORDER BY hire_date DESC"))
            
            .ForMethod(nameof(EmployeeRepository.GetByIdAsync), command => command
                .UseConnectionAlias("Default")
                .UseCommandText(@"
                    SELECT employee_id, first_name, last_name, email, salary, 
                           hire_date, department_id, is_active
                    FROM employees 
                    WHERE employee_id = :id"))
            
            .ForMethod(nameof(EmployeeRepository.GetByEmailAsync), command => command
                .UseConnectionAlias("Default")
                .UseCommandText(@"
                    SELECT employee_id, first_name, last_name, email, salary, 
                           hire_date, department_id, is_active
                    FROM employees 
                    WHERE LOWER(email) = LOWER(:email)"))
            
            // Write operations
            .ForMethod(nameof(EmployeeRepository.CreateAsync), command => command
                .UseConnectionAlias("Default")
                .UseCommandText(@"
                    INSERT INTO employees (first_name, last_name, email, salary, department_id, is_active)
                    VALUES (:FirstName, :LastName, :Email, :Salary, :DepartmentId, :IsActive)"))
            
            .ForMethod(nameof(EmployeeRepository.UpdateAsync), command => command
                .UseConnectionAlias("Default")
                .UseCommandText(@"
                    UPDATE employees 
                    SET first_name = :FirstName,
                        last_name = :LastName,
                        email = :Email,
                        salary = :Salary,
                        department_id = :DepartmentId,
                        is_active = :IsActive
                    WHERE employee_id = :EmployeeId"))
            
            .ForMethod(nameof(EmployeeRepository.DeleteAsync), command => command
                .UseConnectionAlias("Default")
                .UseCommandText("DELETE FROM employees WHERE employee_id = :id"))
            
            .ForMethod(nameof(EmployeeRepository.DeactivateAsync), command => command
                .UseConnectionAlias("Default")
                .UseCommandText(@"
                    UPDATE employees 
                    SET is_active = 0 
                    WHERE employee_id = :id"))
            
            .ForMethod(nameof(EmployeeRepository.GetCountAsync), command => command
                .UseConnectionAlias("Default")
                .UseCommandText("SELECT COUNT(*) FROM employees WHERE is_active = 1")));
    }
}
```

### 5. Service Layer

```csharp
public class EmployeeService
{
    private readonly IEmployeeRepository _repository;
    private readonly ILogger<EmployeeService> _logger;

    public EmployeeService(IEmployeeRepository repository, ILogger<EmployeeService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task RunExampleAsync()
    {
        _logger.LogInformation("Starting Employee CRUD operations example...");

        try
        {
            // Create employees
            var employees = await CreateSampleEmployeesAsync();
            _logger.LogInformation("Created {Count} employees", employees.Count());

            // Read operations
            await ReadOperationsAsync();

            // Update operations
            await UpdateOperationsAsync(employees.First());

            // Delete operations
            await DeleteOperationsAsync(employees.Last());

            _logger.LogInformation("Example completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running example");
            throw;
        }
    }

    private async Task<IEnumerable<Employee>> CreateSampleEmployeesAsync()
    {
        var employees = new[]
        {
            new Employee
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@company.com",
                Salary = 75000,
                DepartmentId = 1
            },
            new Employee
            {
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane.smith@company.com",
                Salary = 82000,
                DepartmentId = 2
            },
            new Employee
            {
                FirstName = "Bob",
                LastName = "Johnson",
                Email = "bob.johnson@company.com",
                Salary = 68000,
                DepartmentId = 1
            }
        };

        var createdEmployees = new List<Employee>();
        foreach (var employee in employees)
        {
            var created = await _repository.CreateAsync(employee);
            createdEmployees.Add(created);
            _logger.LogInformation("Created employee: {Name} ({Email})", created.FullName, created.Email);
        }

        return createdEmployees;
    }

    private async Task ReadOperationsAsync()
    {
        _logger.LogInformation("Performing read operations...");

        // Get all employees
        var allEmployees = await _repository.GetAllAsync();
        _logger.LogInformation("Total employees: {Count}", allEmployees.Count());

        // Get active employees
        var activeEmployees = await _repository.GetActiveAsync();
        _logger.LogInformation("Active employees: {Count}", activeEmployees.Count());

        // Get employee by email
        var employee = await _repository.GetByEmailAsync("john.doe@company.com");
        if (employee != null)
        {
            _logger.LogInformation("Found employee by email: {Name}", employee.FullName);
        }

        // Get count
        var count = await _repository.GetCountAsync();
        _logger.LogInformation("Employee count: {Count}", count);
    }

    private async Task UpdateOperationsAsync(Employee employee)
    {
        _logger.LogInformation("Performing update operations...");

        // Update employee salary
        employee.Salary = 80000;
        var updated = await _repository.UpdateAsync(employee);
        
        if (updated)
        {
            _logger.LogInformation("Updated salary for {Name} to {Salary:C}", 
                employee.FullName, employee.Salary);
        }
    }

    private async Task DeleteOperationsAsync(Employee employee)
    {
        _logger.LogInformation("Performing delete operations...");

        // Soft delete (deactivate)
        var deactivated = await _repository.DeactivateAsync(employee.EmployeeId);
        if (deactivated)
        {
            _logger.LogInformation("Deactivated employee: {Name}", employee.FullName);
        }

        // Hard delete (optional - uncomment if needed)
        // var deleted = await _repository.DeleteAsync(employee.EmployeeId);  
        // if (deleted)
        // {
        //     _logger.LogInformation("Deleted employee: {Name}", employee.FullName);
        // }
    }
}
```

## Advanced Patterns

### 1. Validation

```csharp
public static class EmployeeValidator
{
    public static void Validate(Employee employee)
    {
        if (string.IsNullOrWhiteSpace(employee.FirstName))
            throw new ArgumentException("First name is required");
            
        if (string.IsNullOrWhiteSpace(employee.LastName))
            throw new ArgumentException("Last name is required");
            
        if (string.IsNullOrWhiteSpace(employee.Email))
            throw new ArgumentException("Email is required");
            
        if (!IsValidEmail(employee.Email))
            throw new ArgumentException("Invalid email format");
            
        if (employee.Salary < 0)
            throw new ArgumentException("Salary cannot be negative");
    }
    
    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
```

### 2. Error Handling

```csharp
public class EmployeeRepository : IEmployeeRepository
{
    // ... other methods ...

    public async Task<Employee> CreateAsync(Employee employee)
    {
        try
        {
            EmployeeValidator.Validate(employee);
            
            var result = await _commander.ExecuteAsync(employee);
            if (!result)
                throw new InvalidOperationException("Failed to create employee");
                
            return employee;
        }
        catch (OracleException ex) when (ex.Number == 1) // ORA-00001: unique constraint violated
        {
            throw new DuplicateEmployeeException($"Employee with email {employee.Email} already exists", ex);
        }
        catch (OracleException ex) when (ex.Number == 2291) // ORA-02291: integrity constraint violated
        {
            throw new InvalidReferenceException("Referenced department does not exist", ex);
        }
        catch (OracleException ex)
        {
            throw new DatabaseException($"Database error: {ex.Message}", ex);
        }
    }
}

public class DuplicateEmployeeException : Exception
{
    public DuplicateEmployeeException(string message, Exception innerException) 
        : base(message, innerException) { }
}

public class InvalidReferenceException : Exception
{
    public InvalidReferenceException(string message, Exception innerException) 
        : base(message, innerException) { }
}

public class DatabaseException : Exception
{
    public DatabaseException(string message, Exception innerException) 
        : base(message, innerException) { }
}
```

### 3. Caching

```csharp
public class CachedEmployeeRepository : IEmployeeRepository
{
    private readonly IEmployeeRepository _repository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachedEmployeeRepository> _logger;

    public CachedEmployeeRepository(
        IEmployeeRepository repository,
        IMemoryCache cache,
        ILogger<CachedEmployeeRepository> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Employee?> GetByIdAsync(int id)
    {
        var cacheKey = $"employee_{id}";
        
        if (_cache.TryGetValue(cacheKey, out Employee? cachedEmployee))
        {
            _logger.LogDebug("Employee {Id} found in cache", id);
            return cachedEmployee;
        }

        var employee = await _repository.GetByIdAsync(id);
        if (employee != null)
        {
            _cache.Set(cacheKey, employee, TimeSpan.FromMinutes(15));
            _logger.LogDebug("Employee {Id} cached for 15 minutes", id);
        }

        return employee;
    }

    public async Task<Employee> CreateAsync(Employee employee)
    {
        var result = await _repository.CreateAsync(employee);
        
        // Cache the new employee
        var cacheKey = $"employee_{result.EmployeeId}";
        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(15));
        
        return result;
    }

    // Implement other methods by delegating to _repository
    // and invalidating cache when appropriate
}
```

## Running the Example

1. **Setup Oracle Database**: Ensure Oracle is running and accessible
2. **Create Schema**: Run the SQL scripts to create tables and sequences
3. **Update Connection String**: Modify `appsettings.json` with your Oracle connection details
4. **Run Application**: Execute the console application

```bash
dotnet run
```

Expected output:
```
info: EmployeeService[0]
      Starting Employee CRUD operations example...
info: EmployeeService[0]
      Created employee: John Doe (john.doe@company.com)
info: EmployeeService[0]
      Created employee: Jane Smith (jane.smith@company.com)
info: EmployeeService[0]
      Created employee: Bob Johnson (bob.johnson@company.com)
info: EmployeeService[0]
      Created 3 employees
info: EmployeeService[0]
      Performing read operations...
info: EmployeeService[0]
      Total employees: 3
info: EmployeeService[0]
      Active employees: 3
info: EmployeeService[0]
      Found employee by email: John Doe
info: EmployeeService[0]
      Employee count: 3
info: EmployeeService[0]
      Performing update operations...
info: EmployeeService[0]
      Updated salary for John Doe to $80,000.00
info: EmployeeService[0]
      Performing delete operations...
info: EmployeeService[0]
      Deactivated employee: Bob Johnson
info: EmployeeService[0]
      Example completed successfully
```

This example demonstrates the core CRUD patterns using Syrx.Oracle with proper error handling, validation, and logging.