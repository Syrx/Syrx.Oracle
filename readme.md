# Syrx.Oracle

Provides Syrx support for Oracle databases. 

## Installation
We recommend installing the Extensions package which includes extension methods for easier configuration. 

```
install-package Syrx.Oracle.Extensions
```
However, if you don't need the configuration options, you can install the standalone Oracle package. 

```
install-package Syrx.Oracle
```
---
## Known Issues
### Multiple Result Sets
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

### Solution
The solution to this is simple, albeit less elegant than the authors would like. 

1. In your repository code, add a reference to `Syrx.Commanders.Databases.Oracle`
2. Pass a static instance of `OracleDynamicParameters.Cursors()` method as part of your parameters. 

We recommend leverage the `using static` language feature. 


#### Examples
You can see many exmaples of this in the `Syrx.Oracle.Tests.Integration` project. 

This solution was taken from https://stackoverflow.com/a/41110515 with many thanks to _nw._ and _greyseal96_.

**Without parameters**
```csharp
using static Syrx.Commanders.Databases.Oracle.OracleDynamicParameters;

// assumping a Func<> delegate called 'map' on a method that would not normally need parameters 
var result = _commander.Query(map, Cursors());
```

**With parameters**
```csharp
// assumping a Func<> delegate called 'map' on a method that DOES need parameters 
string[] cursors = { "1" }; 
var parameters = new OracleDynamicParameters(template:new { id = 2 }, cursorNames: cursors);

var result = _commander.Query(map, parameters, method);
```
---