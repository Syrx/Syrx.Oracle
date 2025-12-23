# Oracle-Specific Features

This example demonstrates Oracle-specific features available in Syrx.Oracle, including multiple result sets, PL/SQL integration, hierarchical queries, and analytical functions.

## Prerequisites

- Oracle Database 12c or later
- Syrx.Oracle.Extensions package
- Sample HR schema (or create the schema below)

## Database Schema

```sql
-- Departments table
CREATE TABLE departments (
    department_id NUMBER(10) PRIMARY KEY,
    department_name VARCHAR2(100) NOT NULL,
    manager_id NUMBER(10),
    location_id NUMBER(10)
);

-- Employees table with hierarchy
CREATE TABLE employees (
    employee_id NUMBER(10) PRIMARY KEY,
    first_name VARCHAR2(50) NOT NULL,
    last_name VARCHAR2(50) NOT NULL,
    email VARCHAR2(100) UNIQUE NOT NULL,
    phone_number VARCHAR2(20),
    hire_date DATE DEFAULT SYSDATE,
    job_id VARCHAR2(10) NOT NULL,
    salary NUMBER(8,2),
    commission_pct NUMBER(2,2),
    manager_id NUMBER(10),
    department_id NUMBER(10),
    CONSTRAINT emp_dept_fk FOREIGN KEY (department_id) REFERENCES departments(department_id),
    CONSTRAINT emp_manager_fk FOREIGN KEY (manager_id) REFERENCES employees(employee_id)
);

-- Job history
CREATE TABLE job_history (
    employee_id NUMBER(10) NOT NULL,
    start_date DATE NOT NULL,
    end_date DATE NOT NULL,
    job_id VARCHAR2(10) NOT NULL,
    department_id NUMBER(10),
    PRIMARY KEY (employee_id, start_date),
    CONSTRAINT jhist_emp_fk FOREIGN KEY (employee_id) REFERENCES employees(employee_id),
    CONSTRAINT jhist_dept_fk FOREIGN KEY (department_id) REFERENCES departments(department_id)
);

-- Sales data for analytical examples
CREATE TABLE sales (
    sale_id NUMBER(10) PRIMARY KEY,
    salesperson_id NUMBER(10) NOT NULL,
    customer_id NUMBER(10) NOT NULL,
    sale_date DATE DEFAULT SYSDATE,
    amount NUMBER(10,2) NOT NULL,
    region VARCHAR2(50),
    product_category VARCHAR2(50),
    CONSTRAINT sales_emp_fk FOREIGN KEY (salesperson_id) REFERENCES employees(employee_id)
);

-- Sample data
INSERT INTO departments VALUES (10, 'Administration', 200, 1700);
INSERT INTO departments VALUES (20, 'Marketing', 201, 1800);
INSERT INTO departments VALUES (30, 'Purchasing', 114, 1700);
INSERT INTO departments VALUES (40, 'Human Resources', 203, 2400);
INSERT INTO departments VALUES (50, 'Shipping', 121, 1500);

INSERT INTO employees VALUES (100, 'Steven', 'King', 'sking@company.com', '515.123.4567', DATE '2003-06-17', 'AD_PRES', 24000, NULL, NULL, 90);
INSERT INTO employees VALUES (101, 'Neena', 'Kochhar', 'nkochhar@company.com', '515.123.4568', DATE '2005-09-21', 'AD_VP', 17000, NULL, 100, 90);
INSERT INTO employees VALUES (102, 'Lex', 'De Haan', 'ldehaan@company.com', '515.123.4569', DATE '2001-01-13', 'AD_VP', 17000, NULL, 100, 90);
INSERT INTO employees VALUES (103, 'Alexander', 'Hunold', 'ahunold@company.com', '590.423.4567', DATE '2006-01-03', 'IT_PROG', 9000, NULL, 102, 60);

-- Create PL/SQL package for advanced operations
CREATE OR REPLACE PACKAGE pkg_employee_ops AS
    TYPE emp_cursor_type IS REF CURSOR;
    
    PROCEDURE get_employee_summary(
        p_department_id IN NUMBER,
        p_employees OUT emp_cursor_type,
        p_job_history OUT emp_cursor_type,
        p_total_count OUT NUMBER,
        p_avg_salary OUT NUMBER
    );
    
    FUNCTION calculate_annual_bonus(p_employee_id NUMBER) RETURN NUMBER;
    
    PROCEDURE update_employee_salary(
        p_employee_id IN NUMBER,
        p_new_salary IN NUMBER,
        p_result OUT NUMBER
    );
END pkg_employee_ops;
/

CREATE OR REPLACE PACKAGE BODY pkg_employee_ops AS
    PROCEDURE get_employee_summary(
        p_department_id IN NUMBER,
        p_employees OUT emp_cursor_type,
        p_job_history OUT emp_cursor_type,
        p_total_count OUT NUMBER,
        p_avg_salary OUT NUMBER
    ) AS
    BEGIN
        -- Employee cursor
        OPEN p_employees FOR
            SELECT employee_id, first_name, last_name, email, salary, hire_date
            FROM employees
            WHERE department_id = p_department_id;
        
        -- Job history cursor
        OPEN p_job_history FOR
            SELECT jh.employee_id, jh.start_date, jh.end_date, jh.job_id
            FROM job_history jh
            JOIN employees e ON jh.employee_id = e.employee_id
            WHERE e.department_id = p_department_id;
        
        -- Summary statistics
        SELECT COUNT(*), NVL(AVG(salary), 0)
        INTO p_total_count, p_avg_salary
        FROM employees
        WHERE department_id = p_department_id;
    END get_employee_summary;
    
    FUNCTION calculate_annual_bonus(p_employee_id NUMBER) RETURN NUMBER AS
        v_salary NUMBER;
        v_commission NUMBER;
        v_bonus NUMBER;
    BEGIN
        SELECT salary, NVL(commission_pct, 0)
        INTO v_salary, v_commission
        FROM employees
        WHERE employee_id = p_employee_id;
        
        v_bonus := (v_salary * 12) * (0.1 + v_commission);
        RETURN v_bonus;
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            RETURN 0;
    END calculate_annual_bonus;
    
    PROCEDURE update_employee_salary(
        p_employee_id IN NUMBER,
        p_new_salary IN NUMBER,
        p_result OUT NUMBER
    ) AS
    BEGIN
        UPDATE employees
        SET salary = p_new_salary
        WHERE employee_id = p_employee_id;
        
        p_result := SQL%ROWCOUNT;
        COMMIT;
    EXCEPTION
        WHEN OTHERS THEN
            ROLLBACK;
            p_result := -1;
    END update_employee_salary;
END pkg_employee_ops;
/
```

## Implementation

### 1. Models

```csharp
public class Employee
{
    public int EmployeeId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime HireDate { get; set; }
    public string JobId { get; set; } = string.Empty;
    public decimal Salary { get; set; }
    public decimal? CommissionPct { get; set; }
    public int? ManagerId { get; set; }
    public int DepartmentId { get; set; }
    public int HierarchyLevel { get; set; } // For hierarchical queries
}

public class JobHistory
{
    public int EmployeeId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string JobId { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
}

public class EmployeeSummary
{
    public int TotalCount { get; set; }
    public decimal AverageSalary { get; set; }
    public IEnumerable<Employee> Employees { get; set; } = Enumerable.Empty<Employee>();
    public IEnumerable<JobHistory> JobHistory { get; set; } = Enumerable.Empty<JobHistory>();
}

public class SalesRanking
{
    public int SalespersonId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal TotalSales { get; set; }
    public int SalesRank { get; set; }
    public decimal PercentageOfTotal { get; set; }
    public decimal? PreviousSales { get; set; }
}

public class ProductHierarchy
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public int HierarchyLevel { get; set; }
    public string Path { get; set; } = string.Empty;
}
```

### 2. Repository with Oracle Features

```csharp
public interface IOracleFeatureRepository
{
    // Multiple result sets
    Task<EmployeeSummary> GetEmployeeSummaryAsync(int departmentId);
    
    // Hierarchical queries
    Task<IEnumerable<Employee>> GetEmployeeHierarchyAsync(int? managerId = null);
    
    // Analytical functions
    Task<IEnumerable<SalesRanking>> GetSalesRankingAsync();
    
    // PL/SQL function calls
    Task<decimal> CalculateAnnualBonusAsync(int employeeId);
    
    // PL/SQL procedure calls
    Task<bool> UpdateEmployeeSalaryAsync(int employeeId, decimal newSalary);
    
    // Oracle-specific data types
    Task<Employee> CreateEmployeeWithClobAsync(Employee employee, string largeDescription);
    
    // Bulk operations
    Task<bool> BulkInsertSalesAsync(IEnumerable<Sale> sales);
    
    // Partitioned queries
    Task<IEnumerable<Sale>> GetSalesByPartitionAsync(DateTime partitionDate, DateTime startDate, DateTime endDate);
}

public class OracleFeatureRepository : IOracleFeatureRepository
{
    private readonly ICommander<OracleFeatureRepository> _commander;

    public OracleFeatureRepository(ICommander<OracleFeatureRepository> commander)
    {
        _commander = commander;
    }

    public async Task<EmployeeSummary> GetEmployeeSummaryAsync(int departmentId)
    {
        using static Syrx.Commanders.Databases.Oracle.OracleDynamicParameters;
        
        var parameters = Cursors(new { departmentId });
        
        // Define mapping function that processes multiple result sets
        Func<IEnumerable<Employee>, IEnumerable<JobHistory>, IEnumerable<dynamic>, EmployeeSummary> map = 
            (employees, jobHistory, summary) =>
            {
                var summaryData = summary.FirstOrDefault();
                return new EmployeeSummary
                {
                    Employees = employees,
                    JobHistory = jobHistory,
                    TotalEmployees = summaryData?.TOTAL_EMPLOYEES ?? 0,
                    AverageSalary = summaryData?.AVG_SALARY ?? 0
                };
            };
        
        var result = await _commander.QueryAsync(map, parameters);
        return result.Single(); // Syrx returns IEnumerable of mapped results
    }

    public async Task<IEnumerable<Employee>> GetEmployeeHierarchyAsync(int? managerId = null)
        => await _commander.QueryAsync<Employee>(new { managerId });

    public async Task<IEnumerable<SalesRanking>> GetSalesRankingAsync()
        => await _commander.QueryAsync<SalesRanking>();

    public async Task<decimal> CalculateAnnualBonusAsync(int employeeId)
        => await _commander.QueryAsync<decimal>(new { employeeId }).SingleOrDefaultAsync();

    public async Task<bool> UpdateEmployeeSalaryAsync(int employeeId, decimal newSalary)
        => await _commander.ExecuteAsync(new { employeeId, newSalary });

    public async Task<Employee> CreateEmployeeWithClobAsync(Employee employee, string largeDescription)
        => await _commander.ExecuteAsync(new { employee, largeDescription }) ? employee : throw new InvalidOperationException("Failed to create employee");

    public async Task<bool> BulkInsertSalesAsync(IEnumerable<Sale> sales)
        => await _commander.ExecuteAsync(sales);

    public async Task<IEnumerable<Sale>> GetSalesByPartitionAsync(DateTime partitionDate, DateTime startDate, DateTime endDate)
        => await _commander.QueryAsync<Sale>(new { partitionDate, startDate, endDate });
}
```

### 3. Configuration

```csharp
private static void ConfigureOracleFeatures(ITypeSettingsBuilder types)
{
    types.ForType<OracleFeatureRepository>(methods => methods
        
        // Multiple result sets using PL/SQL procedure
        .ForMethod(nameof(OracleFeatureRepository.GetEmployeeSummaryAsync), command => command
            .UseConnectionAlias("Default")
            .UseCommandText(@"
                DECLARE
                    v_total_count NUMBER;
                    v_avg_salary NUMBER;
                BEGIN
                    pkg_employee_ops.get_employee_summary(
                        :departmentId,
                        :1,  -- employees cursor
                        :2,  -- job history cursor
                        v_total_count,
                        v_avg_salary
                    );
                    
                    -- Return summary data as third result set
                    OPEN :3 FOR
                        SELECT v_total_count as TotalCount, v_avg_salary as AverageSalary FROM dual;
                END;")
            .SetCommandType(CommandType.Text))
        
        // Hierarchical query using CONNECT BY
        .ForMethod(nameof(OracleFeatureRepository.GetEmployeeHierarchyAsync), command => command
            .UseConnectionAlias("Default")
            .UseCommandText(@"
                SELECT 
                    employee_id, 
                    first_name, 
                    last_name, 
                    email, 
                    salary, 
                    manager_id,
                    LEVEL as hierarchy_level,
                    SYS_CONNECT_BY_PATH(first_name || ' ' || last_name, '/') as path
                FROM employees
                START WITH manager_id = :managerId OR (:managerId IS NULL AND manager_id IS NULL)
                CONNECT BY PRIOR employee_id = manager_id
                ORDER SIBLINGS BY last_name, first_name"))
        
        // Analytical functions for sales ranking
        .ForMethod(nameof(OracleFeatureRepository.GetSalesRankingAsync), command => command
            .UseConnectionAlias("Default")
            .UseCommandText(@"
                SELECT 
                    s.salesperson_id,
                    e.first_name || ' ' || e.last_name as name,
                    SUM(s.amount) as total_sales,
                    RANK() OVER (ORDER BY SUM(s.amount) DESC) as sales_rank,
                    RATIO_TO_REPORT(SUM(s.amount)) OVER () as percentage_of_total,
                    LAG(SUM(s.amount)) OVER (ORDER BY SUM(s.amount) DESC) as previous_sales
                FROM sales s
                JOIN employees e ON s.salesperson_id = e.employee_id
                GROUP BY s.salesperson_id, e.first_name, e.last_name
                ORDER BY total_sales DESC"))
        
        // PL/SQL function call
        .ForMethod(nameof(OracleFeatureRepository.CalculateAnnualBonusAsync), command => command
            .UseConnectionAlias("Default")
            .UseCommandText("SELECT pkg_employee_ops.calculate_annual_bonus(:employeeId) FROM dual"))
        
        // PL/SQL procedure call with output parameter
        .ForMethod(nameof(OracleFeatureRepository.UpdateEmployeeSalaryAsync), command => command
            .UseConnectionAlias("Default")
            .UseCommandText(@"
                DECLARE
                    v_result NUMBER;
                BEGIN
                    pkg_employee_ops.update_employee_salary(:employeeId, :newSalary, v_result);
                    SELECT CASE WHEN v_result > 0 THEN 1 ELSE 0 END as result FROM dual;
                END;"))
        
        // CLOB handling
        .ForMethod(nameof(OracleFeatureRepository.CreateEmployeeWithClobAsync), command => command
            .UseConnectionAlias("Default")
            .UseCommandText(@"
                INSERT INTO employees (first_name, last_name, email, job_id, salary, department_id)
                VALUES (:FirstName, :LastName, :Email, :JobId, :Salary, :DepartmentId);
                
                INSERT INTO employee_details (employee_id, description)
                VALUES (emp_seq.CURRVAL, :largeDescription)"))
        
        // Bulk insert with Oracle array binding
        .ForMethod(nameof(OracleFeatureRepository.BulkInsertSalesAsync), command => command
            .UseConnectionAlias("Default")
            .UseCommandText(@"
                INSERT INTO sales (salesperson_id, customer_id, sale_date, amount, region, product_category)
                VALUES (:SalespersonId, :CustomerId, :SaleDate, :Amount, :Region, :ProductCategory)")
            .SetCommandTimeout(300))
        
        // Partition-aware query
        .ForMethod(nameof(OracleFeatureRepository.GetSalesByPartitionAsync), command => command
            .UseConnectionAlias("Default")
            .UseCommandText(@"
                SELECT /*+ PARTITION_WISE_JOIN */ 
                    sale_id, salesperson_id, customer_id, sale_date, amount, region
                FROM sales PARTITION (FOR (TO_DATE(:partitionDate, 'YYYY-MM-DD')))
                WHERE sale_date BETWEEN :startDate AND :endDate
                ORDER BY sale_date DESC")));
}
```

### 4. Advanced Oracle Features Service

```csharp
public class OracleAdvancedService
{
    private readonly IOracleFeatureRepository _repository;
    private readonly ILogger<OracleAdvancedService> _logger;

    public OracleAdvancedService(IOracleFeatureRepository repository, ILogger<OracleAdvancedService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task DemonstrateMultipleResultSetsAsync()
    {
        _logger.LogInformation("Demonstrating Oracle multiple result sets...");
        
        var summary = await _repository.GetEmployeeSummaryAsync(departmentId: 90);
        
        _logger.LogInformation("Department Summary:");
        _logger.LogInformation("- Total Employees: {Count}", summary.TotalCount);
        _logger.LogInformation("- Average Salary: {Salary:C}", summary.AverageSalary);
        _logger.LogInformation("- Employees: {EmployeeCount}", summary.Employees.Count());
        _logger.LogInformation("- Job History Records: {HistoryCount}", summary.JobHistory.Count());
    }

    public async Task DemonstrateHierarchicalQueriesAsync()
    {
        _logger.LogInformation("Demonstrating Oracle hierarchical queries...");
        
        // Get hierarchy starting from CEO (no manager)
        var hierarchy = await _repository.GetEmployeeHierarchyAsync(managerId: null);
        
        foreach (var employee in hierarchy)
        {
            var indent = new string(' ', (employee.HierarchyLevel - 1) * 2);
            _logger.LogInformation("{Indent}Level {Level}: {Name} ({Title})", 
                indent, employee.HierarchyLevel, $"{employee.FirstName} {employee.LastName}", employee.JobId);
        }
    }

    public async Task DemonstrateAnalyticalFunctionsAsync()
    {
        _logger.LogInformation("Demonstrating Oracle analytical functions...");
        
        var rankings = await _repository.GetSalesRankingAsync();
        
        _logger.LogInformation("Sales Rankings:");
        foreach (var ranking in rankings.Take(10))
        {
            _logger.LogInformation("#{Rank}: {Name} - {Sales:C} ({Percentage:P2})", 
                ranking.SalesRank, 
                ranking.Name, 
                ranking.TotalSales, 
                ranking.PercentageOfTotal);
        }
    }

    public async Task DemonstratePlSqlIntegrationAsync()
    {
        _logger.LogInformation("Demonstrating PL/SQL integration...");
        
        const int employeeId = 100;
        
        // Call PL/SQL function
        var bonus = await _repository.CalculateAnnualBonusAsync(employeeId);
        _logger.LogInformation("Annual bonus for employee {Id}: {Bonus:C}", employeeId, bonus);
        
        // Call PL/SQL procedure
        var updated = await _repository.UpdateEmployeeSalaryAsync(employeeId, 25000);
        _logger.LogInformation("Salary update result: {Result}", updated ? "Success" : "Failed");
    }

    public async Task DemonstrateBulkOperationsAsync()
    {
        _logger.LogInformation("Demonstrating Oracle bulk operations...");
        
        var sales = GenerateSampleSales(1000);
        
        var stopwatch = Stopwatch.StartNew();
        var success = await _repository.BulkInsertSalesAsync(sales);
        stopwatch.Stop();
        
        _logger.LogInformation("Bulk inserted {Count} sales records in {Time}ms", 
            sales.Count(), stopwatch.ElapsedMilliseconds);
    }

    public async Task DemonstratePartitioningAsync()
    {
        _logger.LogInformation("Demonstrating Oracle partitioning...");
        
        var partitionDate = new DateTime(2024, 1, 1);
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 31);
        
        var sales = await _repository.GetSalesByPartitionAsync(partitionDate, startDate, endDate);
        
        _logger.LogInformation("Retrieved {Count} sales from partition for {Date:yyyy-MM}", 
            sales.Count(), partitionDate);
    }

    private static IEnumerable<Sale> GenerateSampleSales(int count)
    {
        var random = new Random();
        var salespeople = new[] { 100, 101, 102, 103 };
        var regions = new[] { "North", "South", "East", "West" };
        var categories = new[] { "Electronics", "Clothing", "Books", "Home & Garden" };
        
        for (int i = 0; i < count; i++)
        {
            yield return new Sale
            {
                SalespersonId = salespeople[random.Next(salespeople.Length)],
                CustomerId = random.Next(1000, 9999),
                SaleDate = DateTime.Now.AddDays(-random.Next(365)),
                Amount = (decimal)(random.NextDouble() * 10000 + 100),
                Region = regions[random.Next(regions.Length)],
                ProductCategory = categories[random.Next(categories.Length)]
            };
        }
    }
}

public class Sale
{
    public int SaleId { get; set; }
    public int SalespersonId { get; set; }
    public int CustomerId { get; set; }
    public DateTime SaleDate { get; set; }
    public decimal Amount { get; set; }
    public string Region { get; set; } = string.Empty;
    public string ProductCategory { get; set; } = string.Empty;
}
```

### 5. Main Program

```csharp
public class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.UseSyrx(builder => builder
                    .UseOracle(oracle => oracle
                        .AddConnectionString("Default", context.Configuration.GetConnectionString("Oracle")!)
                        .AddCommand(ConfigureOracleFeatures)));

                services.AddScoped<IOracleFeatureRepository, OracleFeatureRepository>();
                services.AddScoped<OracleAdvancedService>();
            })
            .Build();

        var service = host.Services.GetRequiredService<OracleAdvancedService>();
        
        try
        {
            await service.DemonstrateMultipleResultSetsAsync();
            await service.DemonstrateHierarchicalQueriesAsync();
            await service.DemonstrateAnalyticalFunctionsAsync();
            await service.DemonstratePlSqlIntegrationAsync();
            await service.DemonstrateBulkOperationsAsync();
            await service.DemonstratePartitioningAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
```

## Key Oracle Features Demonstrated

### 1. Multiple Result Sets
- Uses `OracleDynamicParameters.Cursors()` for REF CURSOR handling
- PL/SQL procedures returning multiple cursors
- Proper handling of Oracle's unique multiple result set approach

### 2. Hierarchical Queries
- `START WITH` and `CONNECT BY` clauses
- `LEVEL` pseudo-column for hierarchy depth
- `SYS_CONNECT_BY_PATH` for building paths

### 3. Analytical Functions
- Window functions (`OVER` clause)
- `RANK()`, `RATIO_TO_REPORT()`, `LAG()` functions
- Partitioning and ordering within windows

### 4. PL/SQL Integration
- Stored procedure calls with multiple output cursors
- Function calls returning values
- Procedures with output parameters

### 5. Oracle Data Types
- CLOB handling for large text
- BLOB support for binary data
- Oracle-specific NUMBER precision

### 6. Performance Features
- Bulk operations with array binding
- Partition-aware queries
- Oracle hints for query optimization

This example showcases the powerful Oracle-specific features available through Syrx.Oracle while maintaining clean, maintainable code.
