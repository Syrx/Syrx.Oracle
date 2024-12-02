# Syrx.Oracle

Provides Syrx support for Oracle databases. 

# Installation
We recommend installing the Extensions package which includes extension methods for easier configuration. 

```
install-package Syrx.Oracle.Extensions
```
However, if you don't need the configuration options, you can install the standalone Oracle package. 

```
install-package Syrx.Oracle
```
---
# Known Issues
## Multiple Result Sets
Oracle doesn't natively support multiple result sets in the same way that SQL Server does. However, it _is_ possible to approximate this behaviour using cursors. For example, consider the PL/SQL below. 


```sql
BEGIN
    OPEN :1 FOR select cast(id as number(5)) as ""Id"", name as ""Name"", value as ""Value"", modified as ""Modified"" from poco where id < 2;
    OPEN :2 FOR select cast(id as number(5)) as ""Id"", name as ""Name"", value as ""Value"", modified as ""Modified"" from poco where id < 3;
    OPEN :3 FOR select cast(id as number(5)) as ""Id"", name as ""Name"", value as ""Value"", modified as ""Modified"" from poco where id < 4;
END;
```

Executing this SQL via `Query<>` or `Query<>` execute may return an `OracleException` with a similar stack trace to this: 

```
Oracle.ManagedDataAccess.Client.OracleException : ORA-01008: not all variables bound
https://docs.oracle.com/error-help/db/ora-01008/
Stack Trace:
     at OracleInternal.ServiceObjects.OracleFailoverMgrImpl.OnError(OracleConnection connection, CallHistoryRecord chr, Object mi, Exception ex, Boolean bTopLevelCall, Boolean& bCanRecordNewCall)
     at Oracle.ManagedDataAccess.Client.OracleCommand.ExecuteDbDataReader(CommandBehavior behavior)
  /_/Dapper/SqlMapper.cs(1156,0): at Dapper.SqlMapper.ExecuteReaderWithFlagsFallback(IDbCommand cmd, Boolean wasClosed, CommandBehavior behavior)
  /_/Dapper/SqlMapper.cs(1123,0): at Dapper.SqlMapper.QueryMultipleImpl(IDbConnection cnn, CommandDefinition& command)
  /_/Dapper/SqlMapper.cs(1108,0): at Dapper.SqlMapper.QueryMultiple(IDbConnection cnn, CommandDefinition command)
   ....<your stack>....
     at System.RuntimeMethodHandle.InvokeMethod(Object target, Void** arguments, Signature sig, Boolean isConstructor)
     at System.Reflection.MethodBaseInvoker.InvokeDirectByRefWithFewArgs(Object obj, Span`1 copyOfArgs, BindingFlags invokeAttr)
``` 

## Solution
The solution to this is simple, albeit less elegant than the authors would like. 

1. In your repository code, add a reference to `Syrx.Commanders.Databases.Oracle`
2. Pass a static instance of `OracleDynamicParameters.Cursors()` method as part of your parameters. 

We recommend leverage the `using static` language feature. 


### Examples
You can see many exmaples of this in the `Syrx.Oracle.Tests.Integration` project. 

This solution was taken from https://stackoverflow.com/a/41110515 with many thanks to _nw._ and _greyseal96_.

#### Without parameters
```csharp
using static Syrx.Commanders.Databases.Oracle.OracleDynamicParameters;

// assumping a Func<> delegate called 'map' on a method that would not normally need parameters 
var result = _commander.Query(map, Cursors());
```

#### With parameters
There are two flavours to cursors with parameters:
* Named cursors: if you have some reaon to know the names of the cursors.
* Numbered cursors: these are still named cursors, but Syrx provides a convenience method to leverage default values. 

#### Numbered Cursors
This is the most common and simplest approach. Using the SQL below as an example, we can see that `OPEN :1` is the first of the numbered cursors and `:id1` as the corresponding parameter for that statement.  

```sql
BEGIN
    OPEN :1 FOR select cast(id as number(5)) as ""Id"", name as ""Name"", value as ""Value"", modified as ""Modified"" from poco where id < :id1;
    OPEN :2 FOR select cast(id as number(5)) as ""Id"", name as ""Name"", value as ""Value"", modified as ""Modified"" from poco where id < :id2;
    OPEN :3 FOR select cast(id as number(5)) as ""Id"", name as ""Name"", value as ""Value"", modified as ""Modified"" from poco where id < :id3;
END;
```

To use this in C# we need to make use of the `Cursors` static method of the `OracleDynamicParameters` type. 
We recommend that that you make use of the `using static` directive to help keep your code neat and concise. 

In this example all we're doing is passing the arguments we'd normally pass directly to the `Query<>` method to the `NumberedCursors` static method. 
We then take the instance of the `OracleDynamicParameters` and pass that to the `Query<>` method instead.

```csharp
// at the top of your file, add this using static directive. 
using static Syrx.Commanders.Databases.Oracle.OracleDynamicParameters;

var arguments = new { id1 = 2, id2 = 3, id3 = 4 };  // the arguments we'd normally pass to the Query<> method. 
var parameters = Cursors(arguments);                // return instance of OracleDynamicParameters. 
var result = _commander.Query(map, parameters);     // pass OracleDynamicParameters to the Query<> method instead.

```

#### Named Cursors
Although it's unlikely to be that common, there may be cases where you need to pass the names of the cursors. 

In this SQL we can see that the names of the cursors are `by_id`,`by_name` and `by_value`.S

```sql
BEGIN
    OPEN :by_id    FOR select cast(id as number(5)) as ""Id"", name as ""Name"", value as ""Value"", modified as ""Modified"" from poco where id < :id;
    OPEN :by_name  FOR select cast(id as number(5)) as ""Id"", name as ""Name"", value as ""Value"", modified as ""Modified"" from poco where name like :name and id < 3;
    OPEN :by_value FOR select cast(id as number(5)) as ""Id"", name as ""Name"", value as ""Value"", modified as ""Modified"" from poco where value < :value;
END;
```

To use this in C# we need to make use of the overload of the `Cursors` static method which accepts a mandatory `string[]` argument. 

In this example initialize a `string[]` variable with the cursors names corresponding to our SQL above. 
We then pass this string array to the overloaded `Cursors` method. The `parameters` argument of this method is optional as it's entirely possible that you may find yourself with a query that leverages cursors but that has no parameters to limit the result sets within the query. 

```csharp
// at the top of your file, add this using static directive. 
using static Syrx.Commanders.Databases.Oracle.OracleDynamicParameters;


string[] cursors = { "by_id", "by_name", "by_value" };          // the string array holding our cursors 
var arguments = new { id = 2, name = "entry%", value = 40 };    // the arguments we'd normally pass to the Query<> method
var parameters = Cursors(cursors, arguments);                   // return instance of OracleDynamicParameters from overload. 
var result = _commander.Query(map, parameters);                 // pass OracleDynamicParameters to the Query<> method instead.
```

---