# Entity Framework to Syrx.Oracle Migration Guide

This guide provides step-by-step instructions for migrating from Entity Framework (Core) to Syrx.Oracle, including code examples, best practices, and common pitfalls to avoid.

## Overview

### Why Migrate to Syrx.Oracle?

- **Performance**: Direct SQL control with Dapper's performance
- **Oracle-Specific Features**: Full access to Oracle's advanced capabilities
- **Explicit Control**: No hidden SQL generation or N+1 query issues
- **Simplified Debugging**: See exactly what SQL is executed
- **Reduced Complexity**: No complex mapping configurations

### Migration Strategy

1. **Gradual Migration**: Migrate module by module
2. **Parallel Implementation**: Run both systems during transition
3. **Data Layer Isolation**: Migrate data access layer first
4. **Testing**: Comprehensive testing at each step

## Before You Start

### Assessment Checklist

- [ ] Identify all Entity Framework DbContexts
- [ ] List all entity models and relationships
- [ ] Document custom configurations (Fluent API, attributes)
- [ ] Identify complex queries and stored procedures
- [ ] List all migrations and schema changes
- [ ] Document performance-critical operations

### Prerequisites

```xml
<!-- Remove Entity Framework packages -->
<!-- <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
     <PackageReference Include="Oracle.EntityFrameworkCore" Version="8.21.121" /> -->

<!-- Add Syrx.Oracle packages -->
<PackageReference Include="Syrx.Oracle.Extensions" Version="2.4.5" />
<PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.0" />
```

## Step-by-Step Migration

### Step 1: Entity Models

Entity Framework models can often be used as-is with Syrx.Oracle.

#### Entity Framework
```csharp
[Table("employees")]
public class Employee
{
    [Key]
    [Column("employee_id")]
    public int EmployeeId { get; set; }
    
    [Required]
    [Column("first_name")]
    [MaxLength(50)]
    public string FirstName { get; set; }
    
    [Required]
    [Column("last_name")]
    [MaxLength(50)]
    public string LastName { get; set; }
    
    [Column("email")]
    [MaxLength(100)]
    public string Email { get; set; }
    
    [Column("salary")]
    [Precision(10, 2)]
    public decimal? Salary { get; set; }
    
    [Column("hire_date")]
    public DateTime HireDate { get; set; }
    
    [Column("department_id")]
    public int? DepartmentId { get; set; }
    
    // Navigation properties
    public Department Department { get; set; }
    public List<Order> Orders { get; set; } = new();
}

public class Department
{
    [Key]
    [Column("department_id")]
    public int DepartmentId { get; set; }
    
    [Required]
    [Column("department_name")]
    [MaxLength(100)]
    public string DepartmentName { get; set; }
    
    // Navigation property
    public List<Employee> Employees { get; set; } = new();
}
```

#### Syrx.Oracle (Simplified)
```csharp
// Remove EF attributes - Syrx uses property names directly
public class Employee
{
    public int EmployeeId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public decimal? Salary { get; set; }
    public DateTime HireDate { get; set; }
    public int? DepartmentId { get; set; }
    
    // Optional: Keep navigation properties for DTOs
    public Department? Department { get; set; }
    public List<Order> Orders { get; set; } = new();
}

public class Department
{
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public List<Employee> Employees { get; set; } = new();
}
```

### Step 2: DbContext to Repository

#### Entity Framework DbContext
```csharp
public class CompanyDbContext : DbContext
{
    public CompanyDbContext(DbContextOptions<CompanyDbContext> options) : base(options) { }
    
    public DbSet<Employee> Employees { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<Order> Orders { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.EmployeeId);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Salary).HasPrecision(10, 2);
            
            entity.HasOne(e => e.Department)
                  .WithMany(d => d.Employees)
                  .HasForeignKey(e => e.DepartmentId);
        });
        
        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(d => d.DepartmentId);
            entity.Property(d => d.DepartmentName).IsRequired().HasMaxLength(100);
        });
    }
}
```

#### Syrx.Oracle Repository
```csharp
public interface IEmployeeRepository
{
    Task<IEnumerable<Employee>> GetAllAsync();
    Task<Employee?> GetByIdAsync(int id);
    Task<IEnumerable<Employee>> GetByDepartmentAsync(int departmentId);
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

    public async Task<Employee?> GetByIdAsync(int id)
        => await _commander.QueryAsync<Employee>(new { id }).SingleOrDefaultAsync();

    public async Task<IEnumerable<Employee>> GetByDepartmentAsync(int departmentId)
        => await _commander.QueryAsync<Employee>(new { departmentId });

    public async Task<Employee> CreateAsync(Employee employee)
        => await _commander.ExecuteAsync(employee) ? employee : throw new InvalidOperationException("Failed to create employee");

    public async Task<bool> UpdateAsync(Employee employee)
        => await _commander.ExecuteAsync(employee);

    public async Task<bool> DeleteAsync(int id)
        => await _commander.ExecuteAsync(new { id });
}
```

### Step 3: Service Configuration

#### Entity Framework Configuration
```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddDbContext<CompanyDbContext>(options =>
        options.UseOracle(Configuration.GetConnectionString("Oracle")));
    
    services.AddScoped<IEmployeeService, EmployeeService>();
}
```

#### Syrx.Oracle Configuration
```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.UseSyrx(builder => builder
        .UseOracle(oracle => oracle
            .AddConnectionString("Default", Configuration.GetConnectionString("Oracle")!)
            .AddCommand(ConfigureCommands)));

    services.AddScoped<IEmployeeRepository, EmployeeRepository>();
    services.AddScoped<IEmployeeService, EmployeeService>();
}

private static void ConfigureCommands(ITypeSettingsBuilder types)
{
    types.ForType<EmployeeRepository>(methods => methods
        .ForMethod(nameof(EmployeeRepository.GetAllAsync), command => command
            .UseConnectionAlias("Default")
            .UseCommandText(@"
                SELECT employee_id, first_name, last_name, email, salary, hire_date, department_id
                FROM employees
                ORDER BY employee_id"))
        
        .ForMethod(nameof(EmployeeRepository.GetByIdAsync), command => command
            .UseConnectionAlias("Default")
            .UseCommandText(@"
                SELECT employee_id, first_name, last_name, email, salary, hire_date, department_id
                FROM employees
                WHERE employee_id = :id"))
        
        .ForMethod(nameof(EmployeeRepository.GetByDepartmentAsync), command => command
            .UseConnectionAlias("Default")
            .UseCommandText(@"
                SELECT employee_id, first_name, last_name, email, salary, hire_date, department_id
                FROM employees
                WHERE department_id = :departmentId
                ORDER BY last_name, first_name"))
        
        .ForMethod(nameof(EmployeeRepository.CreateAsync), command => command
            .UseConnectionAlias("Default")
            .UseCommandText(@"
                INSERT INTO employees (first_name, last_name, email, salary, hire_date, department_id)
                VALUES (:FirstName, :LastName, :Email, :Salary, :HireDate, :DepartmentId)"))
        
        .ForMethod(nameof(EmployeeRepository.UpdateAsync), command => command
            .UseConnectionAlias("Default")
            .UseCommandText(@"
                UPDATE employees
                SET first_name = :FirstName,
                    last_name = :LastName,
                    email = :Email,
                    salary = :Salary,
                    department_id = :DepartmentId
                WHERE employee_id = :EmployeeId"))
        
        .ForMethod(nameof(EmployeeRepository.DeleteAsync), command => command
            .UseConnectionAlias("Default")
            .UseCommandText("DELETE FROM employees WHERE employee_id = :id")));
}
```

### Step 4: Query Migration

#### Simple Queries

**Entity Framework**
```csharp
public async Task<IEnumerable<Employee>> GetActiveEmployeesAsync()
{
    return await _context.Employees
        .Where(e => e.IsActive)
        .OrderBy(e => e.LastName)
        .ToListAsync();
}
```

**Syrx.Oracle**
```csharp
public async Task<IEnumerable<Employee>> GetActiveEmployeesAsync()
    => await _commander.QueryAsync<Employee>();

// Configuration
.ForMethod(nameof(EmployeeRepository.GetActiveEmployeesAsync), command => command
    .UseConnectionAlias("Default")
    .UseCommandText(@"
        SELECT employee_id, first_name, last_name, email, salary, hire_date, department_id
        FROM employees
        WHERE is_active = 1
        ORDER BY last_name"))
```

#### Complex Queries with Joins

**Entity Framework**
```csharp
public async Task<IEnumerable<EmployeeDto>> GetEmployeesWithDepartmentAsync()
{
    return await _context.Employees
        .Include(e => e.Department)
        .Select(e => new EmployeeDto
        {
            EmployeeId = e.EmployeeId,
            FullName = e.FirstName + " " + e.LastName,
            Email = e.Email,
            DepartmentName = e.Department.DepartmentName,
            Salary = e.Salary
        })
        .ToListAsync();
}
```

**Syrx.Oracle**
```csharp
public async Task<IEnumerable<EmployeeDto>> GetEmployeesWithDepartmentAsync()
    => await _commander.QueryAsync<EmployeeDto>();

// Configuration
.ForMethod(nameof(EmployeeRepository.GetEmployeesWithDepartmentAsync), command => command
    .UseConnectionAlias("Default")
    .UseCommandText(@"
        SELECT 
            e.employee_id,
            e.first_name || ' ' || e.last_name as full_name,
            e.email,
            d.department_name,
            e.salary
        FROM employees e
        JOIN departments d ON e.department_id = d.department_id
        ORDER BY e.last_name, e.first_name"))
```

#### Aggregate Queries

**Entity Framework**
```csharp
public async Task<DepartmentStatsDto> GetDepartmentStatsAsync(int departmentId)
{
    var stats = await _context.Employees
        .Where(e => e.DepartmentId == departmentId)
        .GroupBy(e => e.DepartmentId)
        .Select(g => new DepartmentStatsDto
        {
            DepartmentId = g.Key.Value,
            EmployeeCount = g.Count(),
            AverageSalary = g.Average(e => e.Salary ?? 0),
            TotalSalary = g.Sum(e => e.Salary ?? 0),
            MinSalary = g.Min(e => e.Salary ?? 0),
            MaxSalary = g.Max(e => e.Salary ?? 0)
        })
        .FirstOrDefaultAsync();
    
    return stats ?? new DepartmentStatsDto();
}
```

**Syrx.Oracle**
```csharp
public async Task<DepartmentStatsDto> GetDepartmentStatsAsync(int departmentId)
    => await _commander.QueryAsync<DepartmentStatsDto>(new { departmentId }).SingleOrDefaultAsync() ?? new DepartmentStatsDto();

// Configuration
.ForMethod(nameof(EmployeeRepository.GetDepartmentStatsAsync), command => command
    .UseConnectionAlias("Default")
    .UseCommandText(@"
        SELECT 
            department_id,
            COUNT(*) as employee_count,
            AVG(NVL(salary, 0)) as average_salary,
            SUM(NVL(salary, 0)) as total_salary,
            MIN(NVL(salary, 0)) as min_salary,
            MAX(NVL(salary, 0)) as max_salary
        FROM employees
        WHERE department_id = :departmentId
        GROUP BY department_id"))
```

### Step 5: Transactions

#### Entity Framework Transactions
```csharp
public async Task<bool> TransferEmployeeAsync(int employeeId, int newDepartmentId)
{
    using var transaction = await _context.Database.BeginTransactionAsync();
    
    try
    {
        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null) return false;
        
        // Update employee department
        employee.DepartmentId = newDepartmentId;
        
        // Log the transfer
        _context.TransferLogs.Add(new TransferLog
        {
            EmployeeId = employeeId,
            FromDepartmentId = employee.DepartmentId,
            ToDepartmentId = newDepartmentId,
            TransferDate = DateTime.Now
        });
        
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return true;
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

#### Syrx.Oracle Transactions
```csharp
public async Task<bool> TransferEmployeeAsync(int employeeId, int newDepartmentId)
    => await _commander.ExecuteAsync(new { employeeId, newDepartmentId });

// Configuration with PL/SQL procedure for transaction
.ForMethod(nameof(EmployeeRepository.TransferEmployeeAsync), command => command
    .UseConnectionAlias("Default")
    .UseCommandText(@"
        DECLARE
            v_old_dept_id NUMBER;
            v_result NUMBER := 0;
        BEGIN
            -- Get current department
            SELECT department_id INTO v_old_dept_id 
            FROM employees 
            WHERE employee_id = :employeeId;
            
            -- Update employee department
            UPDATE employees 
            SET department_id = :newDepartmentId 
            WHERE employee_id = :employeeId;
            
            IF SQL%ROWCOUNT > 0 THEN
                -- Log the transfer
                INSERT INTO transfer_logs (employee_id, from_department_id, to_department_id, transfer_date)
                VALUES (:employeeId, v_old_dept_id, :newDepartmentId, SYSDATE);
                
                v_result := 1;
            END IF;
            
            SELECT v_result FROM dual;
            
        EXCEPTION
            WHEN OTHERS THEN
                ROLLBACK;
                RAISE;
        END;"))
```

### Step 6: Advanced Scenarios

#### Raw SQL Migration

**Entity Framework**
```csharp
public async Task<IEnumerable<Employee>> GetTopPerformersAsync(int count)
{
    return await _context.Employees
        .FromSql($@"
            SELECT e.* FROM employees e
            JOIN (
                SELECT employee_id, SUM(sales_amount) as total_sales
                FROM sales
                WHERE sale_date >= ADD_MONTHS(SYSDATE, -12)
                GROUP BY employee_id
                ORDER BY total_sales DESC
                FETCH FIRST {count} ROWS ONLY
            ) s ON e.employee_id = s.employee_id")
        .ToListAsync();
}
```

**Syrx.Oracle**
```csharp
public async Task<IEnumerable<Employee>> GetTopPerformersAsync(int count)
    => await _commander.QueryAsync<Employee>(new { count });

// Configuration
.ForMethod(nameof(EmployeeRepository.GetTopPerformersAsync), command => command
    .UseConnectionAlias("Default")
    .UseCommandText(@"
        SELECT e.employee_id, e.first_name, e.last_name, e.email, e.salary, e.hire_date, e.department_id
        FROM employees e
        JOIN (
            SELECT employee_id, SUM(sales_amount) as total_sales
            FROM sales
            WHERE sale_date >= ADD_MONTHS(SYSDATE, -12)
            GROUP BY employee_id
            ORDER BY total_sales DESC
            FETCH FIRST :count ROWS ONLY
        ) s ON e.employee_id = s.employee_id"))
```

## Common Migration Patterns

### 1. Change Tracking

**Entity Framework** (automatic change tracking)
```csharp
var employee = await _context.Employees.FindAsync(id);
employee.Salary = newSalary;
await _context.SaveChangesAsync();
```

**Syrx.Oracle** (explicit updates)
```csharp
var employee = await _repository.GetByIdAsync(id);
employee.Salary = newSalary;
await _repository.UpdateAsync(employee);
```

### 2. Lazy Loading

**Entity Framework**
```csharp
var employee = await _context.Employees.FindAsync(id);
// Department loaded automatically when accessed
var departmentName = employee.Department.DepartmentName;
```

**Syrx.Oracle** (explicit loading)
```csharp
// Load with join
var employee = await _repository.GetEmployeeWithDepartmentAsync(id);
var departmentName = employee.Department?.DepartmentName;

// Or load separately
var employee = await _repository.GetByIdAsync(id);
var department = await _departmentRepository.GetByIdAsync(employee.DepartmentId);
```

### 3. Bulk Operations

**Entity Framework**
```csharp
_context.Employees.AddRange(employees);
await _context.SaveChangesAsync();
```

**Syrx.Oracle**
```csharp
await _repository.BulkInsertAsync(employees);

// Configuration
.ForMethod(nameof(EmployeeRepository.BulkInsertAsync), command => command
    .UseConnectionAlias("Default")
    .UseCommandText(@"
        INSERT INTO employees (first_name, last_name, email, salary, department_id)
        VALUES (:FirstName, :LastName, :Email, :Salary, :DepartmentId)")
    .SetCommandTimeout(300))
```

## Testing Migration

### Unit Tests

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

### Integration Tests

```csharp
public class EmployeeRepositoryIntegrationTests
{
    private readonly IEmployeeRepository _repository;
    
    [SetUp]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.UseSyrx(builder => builder
            .UseOracle(oracle => oracle
                .AddConnectionString("Integration", GetIntegrationConnectionString())
                .AddCommand(ConfigureCommands)));
        
        var provider = services.BuildServiceProvider();
        _repository = provider.GetService<IEmployeeRepository>();
    }
    
    [Test]
    public async Task Should_Create_And_Retrieve_Employee()
    {
        // Arrange
        var employee = new Employee
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test.user@company.com",
            Salary = 50000,
            DepartmentId = 10
        };
        
        // Act
        var created = await _repository.CreateAsync(employee);
        var retrieved = await _repository.GetByEmailAsync(employee.Email);
        
        // Assert
        Assert.IsNotNull(retrieved);
        Assert.AreEqual(employee.FirstName, retrieved.FirstName);
        Assert.AreEqual(employee.LastName, retrieved.LastName);
        Assert.AreEqual(employee.Email, retrieved.Email);
    }
}
```

## Performance Considerations

### Query Optimization

1. **Explicit SQL Control**: Write optimized Oracle SQL with hints
2. **Batch Operations**: Use bulk operations for multiple records
3. **Connection Pooling**: Configure Oracle connection pooling
4. **Statement Caching**: Enable Oracle statement caching

### Best Practices

1. **Index Usage**: Ensure queries use appropriate indexes
2. **Parameter Binding**: Always use parameterized queries
3. **Connection Management**: Let Syrx manage connections
4. **Error Handling**: Handle Oracle-specific exceptions

## Common Pitfalls

### 1. Navigation Properties
- **EF**: Automatic loading of related entities
- **Syrx**: Must explicitly join or load separately

### 2. Change Tracking
- **EF**: Automatic change detection
- **Syrx**: Must explicitly call update methods

### 3. Lazy Loading
- **EF**: Properties loaded on demand
- **Syrx**: All data must be explicitly loaded

### 4. Bulk Operations
- **EF**: Less efficient bulk operations
- **Syrx**: Highly efficient Oracle bulk operations

## Migration Checklist

- [ ] Models converted (removed EF attributes)
- [ ] DbContext replaced with repositories
- [ ] All queries explicitly defined
- [ ] Service configuration updated
- [ ] Transactions updated
- [ ] Tests updated and passing
- [ ] Performance benchmarks meet requirements
- [ ] Error handling covers Oracle-specific cases
- [ ] Documentation updated

## Rollback Plan

1. **Parallel Deployment**: Run both systems during transition
2. **Feature Flags**: Toggle between EF and Syrx implementations
3. **Database Compatibility**: Ensure schema works with both systems
4. **Monitoring**: Track performance and errors in both systems
5. **Quick Rollback**: Have procedure to quickly revert if issues occur

This migration guide provides a comprehensive approach to moving from Entity Framework to Syrx.Oracle while maintaining functionality and improving performance.