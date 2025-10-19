# Syrx.Commanders.Databases.Oracle

Oracle-specific database components and extensions for the Syrx data access framework.

## Table of Contents

- [Overview](#overview)
- [Key Features](#key-features)
- [Installation](#installation)
- [Core Components](#core-components)
  - [OracleDynamicParameters](#oracledynamicparameters)
- [Usage](#usage)
  - [Dynamic Parameters](#dynamic-parameters)
  - [Output Parameters](#output-parameters)
  - [Stored Procedures](#stored-procedures)
- [Oracle-Specific Features](#oracle-specific-features)
  - [Cursor Parameters](#cursor-parameters)
  - [CLOB/BLOB Handling](#clobblob-handling)
  - [Oracle Data Types](#oracle-data-types)
- [Parameter Mapping](#parameter-mapping)
- [Examples](#examples)
  - [Basic Usage](#basic-usage)
  - [Complex Stored Procedures](#complex-stored-procedures)
  - [Bulk Operations](#bulk-operations)
- [Related Packages](#related-packages)
- [License](#license)
- [Credits](#credits)

## Overview

`Syrx.Commanders.Databases.Oracle` provides Oracle-specific database components and utilities for the Syrx data access framework. This package extends Syrx's capabilities with Oracle-specific features like specialized parameter handling, Oracle data type support, and enhanced stored procedure integration.

>Note    
This package is automatically installed with **[Syrx.Oracle.Extensions](https://www.nuget.org/packages/Syrx.Oracle.Extensions/)**.

## Key Features

- **Oracle Dynamic Parameters**: Enhanced parameter handling for Oracle-specific data types
- **Output Parameter Support**: Full support for Oracle stored procedure output parameters
- **Cursor Handling**: Support for Oracle cursor parameters and result sets
- **CLOB/BLOB Support**: Specialized handling for large object data types
- **Oracle Data Types**: Native support for Oracle-specific data types
- **Stored Procedure Integration**: Enhanced integration with Oracle stored procedures and packages

## Installation

```bash
dotnet add package Syrx.Commanders.Databases.Oracle
```

**Package Manager**
```bash
Install-Package Syrx.Commanders.Databases.Oracle
```

**PackageReference**
```xml
<PackageReference Include="Syrx.Commanders.Databases.Oracle" Version="3.0.0" />
```

> **Note**: This package is typically used alongside `Syrx.Oracle` and `Syrx.Oracle.Extensions` for complete Oracle support.

## Core Components

### OracleDynamicParameters

Enhanced dynamic parameters class specifically designed for Oracle databases:

```csharp
public class OracleDynamicParameters : DynamicParameters
{
    // Oracle-specific parameter handling
    public void Add(string name, object value, OracleDbType? dbType = null, 
                   ParameterDirection? direction = null, int? size = null, 
                   byte? precision = null, byte? scale = null);
    
    // Cursor parameter support
    public void AddCursor(string name, ParameterDirection direction = ParameterDirection.Output);
    
    // CLOB/BLOB parameter support
    public void AddClob(string name, string value, ParameterDirection direction = ParameterDirection.Input);
    public void AddBlob(string name, byte[] value, ParameterDirection direction = ParameterDirection.Input);
    
    // Output parameter retrieval
    public T Get<T>(string name);
    public object Get(string name);
}
```

## Usage

### Dynamic Parameters

```csharp
public class UserRepository
{
    private readonly ICommander<UserRepository> _commander;
    
    public async Task<User> GetUserWithDetailsAsync(int userId)
    {
        var parameters = new OracleDynamicParameters();
        parameters.Add("p_user_id", userId, OracleDbType.Int32);
        parameters.Add("p_include_details", true, OracleDbType.Boolean);
        
        var users = await _commander.QueryAsync<User>(parameters);
        return users.FirstOrDefault();
    }
}
```

### Output Parameters

```csharp
public async Task<(User user, int totalCount)> GetUserWithCountAsync(int userId)
{
    var parameters = new OracleDynamicParameters();
    parameters.Add("p_user_id", userId, OracleDbType.Int32, ParameterDirection.Input);
    parameters.Add("p_total_count", dbType: OracleDbType.Int32, direction: ParameterDirection.Output);
    
    var users = await _commander.QueryAsync<User>(parameters);
    var totalCount = parameters.Get<int>("p_total_count");
    
    return (users.FirstOrDefault(), totalCount);
}
```

### Stored Procedures

```csharp
public async Task<bool> ProcessUserDataAsync(User user)
{
    var parameters = new OracleDynamicParameters();
    parameters.Add("p_user_id", user.Id, OracleDbType.Int32);
    parameters.Add("p_user_name", user.Name, OracleDbType.Varchar2, size: 100);
    parameters.Add("p_email", user.Email, OracleDbType.Varchar2, size: 255);
    parameters.Add("p_result", dbType: OracleDbType.Int32, direction: ParameterDirection.Output);
    
    await _commander.ExecuteAsync(parameters);
    
    var result = parameters.Get<int>("p_result");
    return result == 1; // Success indicator
}

// Configuration
.ForMethod("ProcessUserDataAsync", command => command
    .UseCommandText("PKG_USER.PROCESS_USER_DATA")
    .SetCommandType(CommandType.StoredProcedure))
```

## Oracle-Specific Features

### Cursor Parameters

Handle Oracle cursor parameters for complex result sets:

```csharp
public async Task<(IEnumerable<User>, IEnumerable<Order>)> GetUserDataAsync(int userId)
{
    using static Syrx.Commanders.Databases.Oracle.OracleDynamicParameters;
    
    string[] cursors = { "p_users_cursor", "p_orders_cursor" };
    var arguments = new { p_user_id = userId };
    var parameters = Cursors(cursors, arguments);
    
    // Define mapping function that processes multiple result sets
    Func<IEnumerable<User>, IEnumerable<Order>, (IEnumerable<User>, IEnumerable<Order>)> map = 
        (users, orders) => (users, orders);
    
    var result = await _commander.QueryAsync(map, parameters);
    return result.Single(); // Syrx returns IEnumerable of mapped results
}
```

### CLOB/BLOB Handling

Work with large object data types:

```csharp
public async Task<bool> SaveDocumentAsync(Document document)
{
    var parameters = new OracleDynamicParameters();
    parameters.Add("p_doc_id", document.Id, OracleDbType.Int32);
    parameters.Add("p_title", document.Title, OracleDbType.Varchar2, size: 500);
    parameters.AddClob("p_content", document.Content);
    parameters.AddBlob("p_attachment", document.AttachmentData);
    
    return await _commander.ExecuteAsync(parameters);
}

public async Task<Document> GetDocumentAsync(int documentId)
{
    var parameters = new OracleDynamicParameters();
    parameters.Add("p_doc_id", documentId, OracleDbType.Int32);
    
    var documents = await _commander.QueryAsync<Document>(parameters);
    return documents.FirstOrDefault();
}
```

### Oracle Data Types

Support for Oracle-specific data types:

```csharp
public async Task<bool> SaveComplexDataAsync(ComplexData data)
{
    var parameters = new OracleDynamicParameters();
    
    // Standard types
    parameters.Add("p_id", data.Id, OracleDbType.Int32);
    parameters.Add("p_name", data.Name, OracleDbType.Varchar2, size: 100);
    
    // Oracle-specific types
    parameters.Add("p_date", data.CreateDate, OracleDbType.Date);
    parameters.Add("p_timestamp", data.LastModified, OracleDbType.TimeStamp);
    parameters.Add("p_number", data.Amount, OracleDbType.Decimal, precision: 18, scale: 2);
    parameters.Add("p_raw_data", data.BinaryData, OracleDbType.Raw, size: 2000);
    
    return await _commander.ExecuteAsync(parameters);
}
```

## Parameter Mapping

The `OracleDynamicParameters` class provides automatic mapping between C# types and Oracle data types:

| C# Type | Oracle Type | Notes |
|---------|-------------|--------|
| `int` | `OracleDbType.Int32` | 32-bit integer |
| `long` | `OracleDbType.Int64` | 64-bit integer |
| `decimal` | `OracleDbType.Decimal` | Supports precision/scale |
| `string` | `OracleDbType.Varchar2` | Supports size specification |
| `DateTime` | `OracleDbType.Date` or `OracleDbType.TimeStamp` | Based on precision needs |
| `byte[]` | `OracleDbType.Raw` or `OracleDbType.Blob` | Based on size |
| `bool` | `OracleDbType.Boolean` | Oracle 12c+ |

## Examples

### Basic Usage

```csharp
public class ProductRepository
{
    private readonly ICommander<ProductRepository> _commander;
    
    public async Task<Product> GetProductAsync(int productId)
    {
        var parameters = new OracleDynamicParameters();
        parameters.Add("p_product_id", productId, OracleDbType.Int32);
        
        var products = await _commander.QueryAsync<Product>(parameters);
        return products.FirstOrDefault();
    }
    
    public async Task<bool> UpdateProductPriceAsync(int productId, decimal newPrice)
    {
        var parameters = new OracleDynamicParameters();
        parameters.Add("p_product_id", productId, OracleDbType.Int32);
        parameters.Add("p_new_price", newPrice, OracleDbType.Decimal, precision: 18, scale: 2);
        parameters.Add("p_rows_affected", dbType: OracleDbType.Int32, direction: ParameterDirection.Output);
        
        await _commander.ExecuteAsync(parameters);
        
        var rowsAffected = parameters.Get<int>("p_rows_affected");
        return rowsAffected > 0;
    }
}
```

### Complex Stored Procedures

```csharp
public class OrderService
{
    private readonly ICommander<OrderService> _commander;
    
    public async Task<OrderProcessingResult> ProcessOrderAsync(Order order)
    {
        var parameters = new OracleDynamicParameters();
        
        // Input parameters
        parameters.Add("p_customer_id", order.CustomerId, OracleDbType.Int32);
        parameters.Add("p_order_date", order.OrderDate, OracleDbType.Date);
        parameters.Add("p_total_amount", order.TotalAmount, OracleDbType.Decimal, precision: 18, scale: 2);
        
        // Output parameters
        parameters.Add("p_order_id", dbType: OracleDbType.Int32, direction: ParameterDirection.Output);
        parameters.Add("p_order_number", dbType: OracleDbType.Varchar2, direction: ParameterDirection.Output, size: 50);
        parameters.Add("p_status_code", dbType: OracleDbType.Int32, direction: ParameterDirection.Output);
        parameters.Add("p_status_message", dbType: OracleDbType.Varchar2, direction: ParameterDirection.Output, size: 500);
        
        // Cursor for order items
        parameters.AddCursor("p_order_items_cursor");
        
        await _commander.ExecuteAsync(parameters);
        
        return new OrderProcessingResult
        {
            OrderId = parameters.Get<int>("p_order_id"),
            OrderNumber = parameters.Get<string>("p_order_number"),
            StatusCode = parameters.Get<int>("p_status_code"),
            StatusMessage = parameters.Get<string>("p_status_message")
        };
    }
}
```

### Bulk Operations

```csharp
public async Task<bool> BulkInsertProductsAsync(IEnumerable<Product> products)
{
    var parameters = new OracleDynamicParameters();
    
    // Use Oracle array binding for bulk operations
    var ids = products.Select(p => p.Id).ToArray();
    var names = products.Select(p => p.Name).ToArray();
    var prices = products.Select(p => p.Price).ToArray();
    
    parameters.Add("p_ids", ids, OracleDbType.Int32);
    parameters.Add("p_names", names, OracleDbType.Varchar2, size: 255);
    parameters.Add("p_prices", prices, OracleDbType.Decimal, precision: 18, scale: 2);
    parameters.Add("p_rows_processed", dbType: OracleDbType.Int32, direction: ParameterDirection.Output);
    
    await _commander.ExecuteAsync(parameters);
    
    var rowsProcessed = parameters.Get<int>("p_rows_processed");
    return rowsProcessed == products.Count();
}
```

## Related Packages

### Oracle Provider Packages
- **[Syrx.Oracle](https://www.nuget.org/packages/Syrx.Oracle/)**: Complete Oracle database provider
- **[Syrx.Oracle.Extensions](https://www.nuget.org/packages/Syrx.Oracle.Extensions/)**: Oracle dependency injection extensions
- **[Syrx.Commanders.Databases.Connectors.Oracle](https://www.nuget.org/packages/Syrx.Commanders.Databases.Connectors.Oracle/)**: Oracle database connector
- **[Syrx.Commanders.Databases.Connectors.Oracle.Extensions](https://www.nuget.org/packages/Syrx.Commanders.Databases.Connectors.Oracle.Extensions/)**: Oracle connector extensions

### Core Framework
- **[Syrx.Commanders.Databases](https://www.nuget.org/packages/Syrx.Commanders.Databases/)**: Core database command abstractions
- **[Syrx](https://www.nuget.org/packages/Syrx/)**: Core Syrx interfaces

## License

This project is licensed under the [MIT License](https://github.com/Syrx/Syrx/blob/main/LICENSE).

## Credits

- Built on top of [Dapper](https://github.com/DapperLib/Dapper)
- Oracle connectivity provided by [Oracle.ManagedDataAccess.Core](https://www.nuget.org/packages/Oracle.ManagedDataAccess.Core/)
