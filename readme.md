# Syrx.Oracle

Syrx.Oracle provides Oracle database support for the Syrx data access framework.

## Overview

This repository contains the Oracle connector, Oracle-specific multiple-result-set support, extension methods for dependency injection, and unit and integration tests for the Oracle provider.

The 3.0.0 line targets .NET 10 and aligns package and workflow dependencies with the current Oracle and test toolchain.

## Packages

| Package | Purpose |
|--|--|
| Syrx.Oracle | Main Oracle provider package |
| Syrx.Oracle.Extensions | Convenience package that adds Oracle-specific registration helpers |
| Syrx.Commanders.Databases.Connectors.Oracle | Low-level Oracle connector implementation |
| Syrx.Commanders.Databases.Connectors.Oracle.Extensions | Dependency injection extensions for the Oracle connector |
| Syrx.Commanders.Databases.Oracle | Oracle-specific dynamic parameter support for cursor-based multiple result sets |

## Requirements

- .NET 10 SDK
- Oracle database access for runtime scenarios
- Docker or an equivalent container runtime for the integration test suite

## Installation

Install the higher-level extensions package when you want the simplest setup experience.

| Source | Command |
|--|--|
| .NET CLI | `dotnet add package Syrx.Oracle.Extensions --version 3.0.0` |
| Package Manager | `Install-Package Syrx.Oracle.Extensions -Version 3.0.0` |
| Package Reference | `<PackageReference Include="Syrx.Oracle.Extensions" Version="3.0.0" />` |

Install the core provider package when you only need the Oracle provider surface.

| Source | Command |
|--|--|
| .NET CLI | `dotnet add package Syrx.Oracle --version 3.0.0` |
| Package Manager | `Install-Package Syrx.Oracle -Version 3.0.0` |
| Package Reference | `<PackageReference Include="Syrx.Oracle" Version="3.0.0" />` |

## Quick start

```csharp
services.UseSyrx(builder => builder
    .UseOracle(oracle => oracle
        .AddConnectionString("Default", connectionString)
        .AddCommand(types => types
            .ForType<EmployeeRepository>(methods => methods
                .ForMethod(nameof(EmployeeRepository.GetAllAsync), command => command
                    .UseConnectionAlias("Default")
                    .UseCommandText("SELECT employee_id, first_name, last_name FROM employees"))))));
```

## Multiple result sets in Oracle

Oracle does not expose multiple result sets in the same way as SQL Server. For Syrx Oracle repositories, multiple result sets are returned through REF CURSOR parameters.

Use `OracleDynamicParameters.Cursors()` when a PL/SQL block opens one or more cursors.

### Numbered cursors

```sql
BEGIN
    OPEN :1 FOR SELECT employee_id, first_name FROM employees WHERE department_id = :deptId1;
    OPEN :2 FOR SELECT employee_id, first_name FROM employees WHERE department_id = :deptId2;
END;
```

```csharp
using static Syrx.Commanders.Databases.Oracle.OracleDynamicParameters;

var parameters = Cursors(new { deptId1 = 10, deptId2 = 20 });
var results = await commander.QueryAsync(map, parameters);
```

### Named cursors

```sql
BEGIN
    OPEN :employees FOR SELECT employee_id, first_name FROM employees WHERE department_id = :deptId;
    OPEN :departments FOR SELECT department_id, department_name FROM departments WHERE department_id = :deptId;
END;
```

```csharp
using static Syrx.Commanders.Databases.Oracle.OracleDynamicParameters;

string[] cursors = { "employees", "departments" };
var parameters = Cursors(cursors, new { deptId = 10 });
var results = await commander.QueryAsync(map, parameters);
```

Without the Oracle-specific cursor parameters, Oracle will typically raise `ORA-01008: not all variables bound`.

## Build and test

```powershell
dotnet restore
dotnet build Syrx.Oracle.sln --configuration Release
dotnet test Syrx.Oracle.sln --configuration Release
```

Run integration tests only when the required Oracle test container prerequisites are available.

## Repository layout

- `src/Syrx.Commanders.Databases.Connectors.Oracle` - Oracle connector implementation
- `src/Syrx.Commanders.Databases.Connectors.Oracle.Extensions` - DI and builder extensions
- `src/Syrx.Commanders.Databases.Oracle` - Oracle dynamic parameters for cursor handling
- `src/Syrx.Oracle` - Main package surface
- `src/Syrx.Oracle.Extensions` - Main extension package surface
- `tests/unit` - Unit tests
- `tests/integration` - Integration tests

## Related packages

- Syrx
- Syrx.Commanders.Databases
- Oracle.ManagedDataAccess.Core

## License

MIT
