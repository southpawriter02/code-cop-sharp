# CCS0045: SqlStringConcatenation

## Overview

| Property | Value |
|----------|-------|
| Rule ID | CCS0045 |
| Category | Security |
| Severity | Warning |
| Has Code Fix | No |
| Enabled by Default | Yes |
| CWE References | [CWE-89](https://cwe.mitre.org/data/definitions/89.html) |

## Description

Detects SQL string concatenation patterns that may indicate SQL injection vulnerabilities. This rule identifies when SQL queries are built using string concatenation with user-controlled or external values instead of parameterized queries.

### Why This Rule?

1. **SQL Injection**: Concatenated queries are vulnerable to injection attacks
2. **Data Breach**: SQL injection can expose entire databases
3. **OWASP Top 10**: SQL injection is consistently a top web vulnerability
4. **Compliance**: PCI-DSS requires protection against injection attacks
5. **Legal Risk**: Data breaches from injection have regulatory consequences

### What This Rule Detects

- String concatenation in SQL query patterns
- String interpolation in SQL query patterns
- `String.Format` with SQL patterns
- Direct variable concatenation in `SqlCommand`, `DbCommand` constructors

### Limitations

This is a pattern-based rule, not a data flow analysis rule. It cannot:
- Track tainted data through multiple assignments
- Verify if concatenated values are user-controlled
- Detect injection through stored procedures

---

## Configuration

```ini
# .editorconfig

# Enable/disable rule
dotnet_diagnostic.CCS0045.severity = warning

# SQL keywords to detect (regex pattern)
dotnet_code_quality.CCS0045.sql_patterns = SELECT|INSERT|UPDATE|DELETE|EXEC|EXECUTE|DROP|CREATE|ALTER|TRUNCATE

# Additional method names that execute SQL
dotnet_code_quality.CCS0045.sql_methods = ExecuteSql|ExecuteQuery|RawSql
```

---

## Detection Patterns

### SQL Query Indicators

| Keyword | Context |
|---------|---------|
| `SELECT` | Query construction |
| `INSERT` | Data insertion |
| `UPDATE` | Data modification |
| `DELETE` | Data deletion |
| `EXEC`/`EXECUTE` | Stored procedure calls |
| `DROP`/`CREATE`/`ALTER` | DDL operations |

### Concatenation Patterns

| Pattern | Example | Detected |
|---------|---------|----------|
| `+` operator | `"SELECT * FROM users WHERE id=" + id` | Yes |
| String interpolation | `$"SELECT * FROM users WHERE id={id}"` | Yes |
| `String.Format` | `String.Format("... WHERE id={0}", id)` | Yes |
| `StringBuilder.Append` | `sb.Append("WHERE id=").Append(id)` | Yes |

### Context Classes

| Class | Namespace | Description |
|-------|-----------|-------------|
| `SqlCommand` | System.Data.SqlClient | SQL Server command |
| `SqlConnection` | System.Data.SqlClient | SQL Server connection |
| `DbCommand` | System.Data.Common | Generic DB command |
| `OracleCommand` | Oracle.ManagedDataAccess | Oracle command |
| `MySqlCommand` | MySql.Data | MySQL command |
| `NpgsqlCommand` | Npgsql | PostgreSQL command |

---

## Compliant Examples

```csharp
using System.Data.SqlClient;

// Good - parameterized query
public User GetUser(int userId)
{
    using var connection = new SqlConnection(connectionString);
    using var command = new SqlCommand(
        "SELECT * FROM Users WHERE Id = @Id",
        connection);

    command.Parameters.AddWithValue("@Id", userId);

    // Execute...
}

// Good - parameterized query with SqlParameter
public void UpdateUser(int userId, string name)
{
    using var connection = new SqlConnection(connectionString);
    using var command = new SqlCommand(
        "UPDATE Users SET Name = @Name WHERE Id = @Id",
        connection);

    command.Parameters.Add(new SqlParameter("@Id", userId));
    command.Parameters.Add(new SqlParameter("@Name", name));

    command.ExecuteNonQuery();
}

// Good - Entity Framework parameterized
public User GetUserEF(int userId)
{
    return context.Users.FromSqlRaw(
        "SELECT * FROM Users WHERE Id = {0}",
        userId).FirstOrDefault();
}

// Good - Dapper parameterized
public User GetUserDapper(int userId)
{
    return connection.QueryFirstOrDefault<User>(
        "SELECT * FROM Users WHERE Id = @Id",
        new { Id = userId });
}

// Good - stored procedure
public User GetUserSP(int userId)
{
    using var command = new SqlCommand("sp_GetUser", connection);
    command.CommandType = CommandType.StoredProcedure;
    command.Parameters.AddWithValue("@Id", userId);

    // Execute...
}

// Good - constant SQL (no external input)
private const string SelectAllUsers = "SELECT * FROM Users";

public List<User> GetAllUsers()
{
    using var command = new SqlCommand(SelectAllUsers, connection);
    // Execute...
}

// Good - table name from allowlist (enum-based)
public List<User> GetFromTable(TableName table)
{
    var sql = table switch
    {
        TableName.Users => "SELECT * FROM Users",
        TableName.Admins => "SELECT * FROM Admins",
        _ => throw new ArgumentException("Invalid table")
    };

    using var command = new SqlCommand(sql, connection);
    // Execute...
}
```

## Non-Compliant Examples

```csharp
using System.Data.SqlClient;

// CCS0045 - string concatenation in SQL
public User GetUser(string userId)
{
    var sql = "SELECT * FROM Users WHERE Id = " + userId;  // SQL injection risk
    using var command = new SqlCommand(sql, connection);
    // Execute...
}

// CCS0045 - string interpolation in SQL
public User GetUserByName(string name)
{
    var sql = $"SELECT * FROM Users WHERE Name = '{name}'";  // SQL injection risk
    using var command = new SqlCommand(sql, connection);
    // Execute...
}

// CCS0045 - String.Format in SQL
public void DeleteUser(int userId)
{
    var sql = string.Format("DELETE FROM Users WHERE Id = {0}", userId);  // Risky
    using var command = new SqlCommand(sql, connection);
    command.ExecuteNonQuery();
}

// CCS0045 - concatenation in command constructor
public User SearchUsers(string searchTerm)
{
    using var command = new SqlCommand(
        "SELECT * FROM Users WHERE Name LIKE '%" + searchTerm + "%'",  // SQL injection
        connection);
    // Execute...
}

// CCS0045 - StringBuilder with SQL
public List<User> GetFilteredUsers(string filter)
{
    var sb = new StringBuilder();
    sb.Append("SELECT * FROM Users WHERE ");
    sb.Append(filter);  // SQL injection if filter is user input

    using var command = new SqlCommand(sb.ToString(), connection);
    // Execute...
}

// CCS0045 - dynamic ORDER BY
public List<User> GetSortedUsers(string sortColumn)
{
    var sql = $"SELECT * FROM Users ORDER BY {sortColumn}";  // SQL injection
    using var command = new SqlCommand(sql, connection);
    // Execute...
}

// CCS0045 - Entity Framework raw SQL with concatenation
public List<User> SearchUsersEF(string name)
{
    return context.Users.FromSqlRaw(
        "SELECT * FROM Users WHERE Name = '" + name + "'")  // SQL injection
        .ToList();
}

// CCS0045 - INSERT with concatenation
public void CreateUser(string name, string email)
{
    var sql = $"INSERT INTO Users (Name, Email) VALUES ('{name}', '{email}')";  // Injection
    using var command = new SqlCommand(sql, connection);
    command.ExecuteNonQuery();
}
```

---

## Implementation Specification

### File Structure

```
CodeCop.Sharp/
└── Analyzers/Security/SqlStringConcatenationAnalyzer.cs
```

### Analyzer Implementation

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace CodeCop.Sharp.Analyzers.Security
{
    /// <summary>
    /// Analyzer that detects SQL string concatenation patterns that may indicate SQL injection vulnerabilities.
    /// </summary>
    /// <remarks>
    /// Rule ID: CCS0045
    /// Category: Security
    /// Severity: Warning
    /// CWE: CWE-89
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SqlStringConcatenationAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CCS0045";

        private static readonly LocalizableString Title = "Potential SQL injection";
        private static readonly LocalizableString MessageFormat =
            "SQL query appears to use string concatenation. Use parameterized queries to prevent SQL injection.";
        private static readonly LocalizableString Description =
            "SQL queries should use parameterized queries or stored procedures instead of string concatenation.";
        private const string Category = "Security";
        private const string HelpLinkUri = "https://cwe.mitre.org/data/definitions/89.html";

        // SQL keyword pattern
        private static readonly Regex SqlPattern = new Regex(
            @"\b(SELECT|INSERT|UPDATE|DELETE|EXEC(UTE)?|DROP|CREATE|ALTER|TRUNCATE)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // SQL command types
        private static readonly string[] SqlCommandTypes = new[]
        {
            "System.Data.SqlClient.SqlCommand",
            "System.Data.Common.DbCommand",
            "Microsoft.Data.SqlClient.SqlCommand",
            "Oracle.ManagedDataAccess.Client.OracleCommand",
            "MySql.Data.MySqlClient.MySqlCommand",
            "Npgsql.NpgsqlCommand"
        };

        // EF Core raw SQL methods
        private static readonly string[] RawSqlMethods = new[]
        {
            "FromSqlRaw",
            "ExecuteSqlRaw",
            "FromSql",
            "ExecuteSql"
        };

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLinkUri,
            customTags: new[] { "Security", "CWE-89" });

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // Analyze binary expressions (string concatenation)
            context.RegisterSyntaxNodeAction(AnalyzeBinaryExpression, SyntaxKind.AddExpression);

            // Analyze interpolated strings
            context.RegisterSyntaxNodeAction(AnalyzeInterpolatedString, SyntaxKind.InterpolatedStringExpression);

            // Analyze String.Format calls
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private void AnalyzeBinaryExpression(SyntaxNodeAnalysisContext context)
        {
            var binary = (BinaryExpressionSyntax)context.Node;

            // Check if this is string concatenation
            var typeInfo = context.SemanticModel.GetTypeInfo(binary);
            if (typeInfo.Type?.SpecialType != SpecialType.System_String)
                return;

            // Get the full concatenated expression text
            var expressionText = GetConcatenatedStringContent(binary, context.SemanticModel);

            if (!ContainsSqlKeywords(expressionText))
                return;

            // Check if used in SQL context
            if (IsInSqlContext(binary, context.SemanticModel))
            {
                // Check if it involves non-literal values
                if (HasNonLiteralParts(binary))
                {
                    ReportDiagnostic(context, binary.GetLocation());
                }
            }
        }

        private void AnalyzeInterpolatedString(SyntaxNodeAnalysisContext context)
        {
            var interpolated = (InterpolatedStringExpressionSyntax)context.Node;

            // Get the literal parts
            var literalText = string.Join("", interpolated.Contents
                .OfType<InterpolatedStringTextSyntax>()
                .Select(t => t.TextToken.ValueText));

            if (!ContainsSqlKeywords(literalText))
                return;

            // Check if has interpolation (non-literal parts)
            var hasInterpolation = interpolated.Contents.OfType<InterpolationSyntax>().Any();

            if (hasInterpolation && IsInSqlContext(interpolated, context.SemanticModel))
            {
                ReportDiagnostic(context, interpolated.GetLocation());
            }
        }

        private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation);
            if (symbolInfo.Symbol is not IMethodSymbol method)
                return;

            // Check for String.Format with SQL
            if (method.Name == "Format" &&
                method.ContainingType?.SpecialType == SpecialType.System_String)
            {
                if (invocation.ArgumentList.Arguments.Count > 0)
                {
                    var firstArg = invocation.ArgumentList.Arguments[0].Expression;
                    if (firstArg is LiteralExpressionSyntax literal &&
                        literal.IsKind(SyntaxKind.StringLiteralExpression))
                    {
                        if (ContainsSqlKeywords(literal.Token.ValueText) &&
                            invocation.ArgumentList.Arguments.Count > 1)
                        {
                            if (IsInSqlContext(invocation, context.SemanticModel))
                            {
                                ReportDiagnostic(context, invocation.GetLocation());
                            }
                        }
                    }
                }
            }

            // Check for EF Core FromSqlRaw with concatenation
            if (RawSqlMethods.Contains(method.Name))
            {
                if (invocation.ArgumentList.Arguments.Count > 0)
                {
                    var sqlArg = invocation.ArgumentList.Arguments[0].Expression;

                    // Check if SQL argument is concatenation or interpolation
                    if (sqlArg is BinaryExpressionSyntax binary &&
                        binary.IsKind(SyntaxKind.AddExpression))
                    {
                        ReportDiagnostic(context, invocation.GetLocation());
                    }
                    else if (sqlArg is InterpolatedStringExpressionSyntax interpolated &&
                             interpolated.Contents.OfType<InterpolationSyntax>().Any())
                    {
                        // FromSqlRaw with interpolation is dangerous
                        // (FromSqlInterpolated handles it safely)
                        if (method.Name == "FromSqlRaw" || method.Name == "ExecuteSqlRaw")
                        {
                            ReportDiagnostic(context, invocation.GetLocation());
                        }
                    }
                }
            }
        }

        private static bool ContainsSqlKeywords(string text)
        {
            return SqlPattern.IsMatch(text);
        }

        private static string GetConcatenatedStringContent(BinaryExpressionSyntax binary, SemanticModel semanticModel)
        {
            var sb = new System.Text.StringBuilder();
            CollectStringContent(binary, sb);
            return sb.ToString();
        }

        private static void CollectStringContent(ExpressionSyntax expression, System.Text.StringBuilder sb)
        {
            if (expression is BinaryExpressionSyntax binary)
            {
                CollectStringContent(binary.Left, sb);
                CollectStringContent(binary.Right, sb);
            }
            else if (expression is LiteralExpressionSyntax literal &&
                     literal.IsKind(SyntaxKind.StringLiteralExpression))
            {
                sb.Append(literal.Token.ValueText);
            }
        }

        private static bool HasNonLiteralParts(BinaryExpressionSyntax binary)
        {
            // Check if any part is not a string literal
            return !AllPartsAreLiterals(binary);
        }

        private static bool AllPartsAreLiterals(ExpressionSyntax expression)
        {
            if (expression is BinaryExpressionSyntax binary)
            {
                return AllPartsAreLiterals(binary.Left) && AllPartsAreLiterals(binary.Right);
            }

            return expression is LiteralExpressionSyntax literal &&
                   literal.IsKind(SyntaxKind.StringLiteralExpression);
        }

        private bool IsInSqlContext(SyntaxNode node, SemanticModel semanticModel)
        {
            // Check if used as argument to SqlCommand constructor
            var argument = node.FirstAncestorOrSelf<ArgumentSyntax>();
            if (argument != null)
            {
                var argumentList = argument.Parent as ArgumentListSyntax;
                var parentNode = argumentList?.Parent;

                if (parentNode is ObjectCreationExpressionSyntax creation)
                {
                    var typeInfo = semanticModel.GetTypeInfo(creation);
                    var typeName = typeInfo.Type?.ToDisplayString();
                    if (SqlCommandTypes.Any(t => typeName?.StartsWith(t.Split('.').Last()) == true ||
                                                 typeName == t))
                    {
                        return true;
                    }
                }

                if (parentNode is InvocationExpressionSyntax invocation)
                {
                    var symbolInfo = semanticModel.GetSymbolInfo(invocation);
                    if (symbolInfo.Symbol is IMethodSymbol method)
                    {
                        if (RawSqlMethods.Contains(method.Name))
                            return true;

                        // Check for ExecuteNonQuery, ExecuteReader, etc.
                        if (method.Name.StartsWith("Execute"))
                            return true;
                    }
                }
            }

            // Check if assigned to SqlCommand.CommandText
            var assignment = node.FirstAncestorOrSelf<AssignmentExpressionSyntax>();
            if (assignment?.Left is MemberAccessExpressionSyntax memberAccess)
            {
                if (memberAccess.Name.Identifier.Text == "CommandText")
                    return true;
            }

            // Check if variable name suggests SQL context
            var variableDeclarator = node.FirstAncestorOrSelf<VariableDeclaratorSyntax>();
            if (variableDeclarator != null)
            {
                var name = variableDeclarator.Identifier.Text.ToLowerInvariant();
                if (name.Contains("sql") || name.Contains("query") || name.Contains("command"))
                    return true;
            }

            return false;
        }

        private void ReportDiagnostic(SyntaxNodeAnalysisContext context, Location location)
        {
            var diagnostic = Diagnostic.Create(Rule, location);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
```

---

## Decision Tree

```
                    ┌─────────────────────────────┐
                    │ Is it string manipulation?  │
                    │ (concat, interpolation,     │
                    │  String.Format)             │
                    └──────────────┬──────────────┘
                                   │
                           ┌───────▼───────┐
                           │      NO       │────────► SKIP
                           └───────┬───────┘
                                   │ YES
                                   ▼
                    ┌─────────────────────────────┐
                    │ Does string contain SQL     │
                    │ keywords? (SELECT, INSERT,  │
                    │ UPDATE, DELETE, etc.)       │
                    └──────────────┬──────────────┘
                                   │
                           ┌───────▼───────┐
                           │      NO       │────────► SKIP
                           └───────┬───────┘
                                   │ YES
                                   ▼
                    ┌─────────────────────────────┐
                    │ Does it include non-literal │
                    │ values? (variables,         │
                    │ interpolations, format args)│
                    └──────────────┬──────────────┘
                                   │
                           ┌───────▼───────┐
                           │      NO       │────────► SKIP (all literals)
                           └───────┬───────┘
                                   │ YES
                                   ▼
                    ┌─────────────────────────────┐
                    │ Is it in SQL context?       │
                    │ (SqlCommand, DbCommand,     │
                    │  FromSqlRaw, etc.)          │
                    └──────────────┬──────────────┘
                                   │
                           ┌───────▼───────┐
                           │      NO       │────────► SKIP
                           └───────┬───────┘
                                   │ YES
                                   ▼
                           REPORT CCS0045
```

---

## Test Cases

### Analyzer Tests - Should Trigger Diagnostic

| Test Name | Input | Expected |
|-----------|-------|----------|
| ConcatInSelect | `"SELECT * FROM Users WHERE Id=" + id` | CCS0045 |
| InterpolatedSelect | `$"SELECT * FROM Users WHERE Id={id}"` | CCS0045 |
| StringFormatSelect | `string.Format("SELECT ... WHERE Id={0}", id)` | CCS0045 |
| ConcatInSqlCommand | `new SqlCommand("SELECT..." + x, conn)` | CCS0045 |
| ConcatInInsert | `$"INSERT INTO Users VALUES ('{name}')"` | CCS0045 |
| ConcatInUpdate | `"UPDATE Users SET Name='" + name + "'"` | CCS0045 |
| ConcatInDelete | `$"DELETE FROM Users WHERE Id={id}"` | CCS0045 |
| FromSqlRawConcat | `FromSqlRaw("SELECT..." + filter)` | CCS0045 |
| FromSqlRawInterpolated | `FromSqlRaw($"SELECT...{filter}")` | CCS0045 |
| DynamicOrderBy | `$"SELECT * FROM Users ORDER BY {col}"` | CCS0045 |

### Analyzer Tests - Should NOT Trigger Diagnostic

| Test Name | Input | Expected |
|-----------|-------|----------|
| ParameterizedQuery | `"SELECT * FROM Users WHERE Id=@Id"` | No diagnostic |
| AllLiteralConcat | `"SELECT * " + "FROM Users"` | No diagnostic |
| NoSqlKeywords | `"Hello " + name` | No diagnostic |
| FromSqlInterpolated | `FromSqlInterpolated($"...{id}")` | No diagnostic |
| StoredProcedure | `new SqlCommand("sp_GetUser")` | No diagnostic |
| DapperParameterized | `Query("SELECT...@Id", new{Id=id})` | No diagnostic |
| ConstantSql | `const string sql = "SELECT..."` | No diagnostic |

---

## Edge Cases

| Case | Behavior | Rationale |
|------|----------|-----------|
| Const string concat | Not flagged | All parts are compile-time constants |
| Enum-based table names | Not flagged | Switch/enum values are controlled |
| Column name validation | Still flagged | Suggests dynamic SQL risk |
| LIKE with wildcards | Flagged | Wildcard injection possible |
| IN clause building | Flagged | Array expansion can be vulnerable |
| Dynamic stored proc | Flagged | EXEC with concat is risky |
| ORM-generated SQL | Not analyzed | Beyond pattern-based detection |
| StringBuilder | Flagged if SQL detected | Same risk as concatenation |

---

## Related Rules

| Rule | Relationship |
|------|--------------|
| CA2100 | Review SQL queries for security vulnerabilities (Roslyn built-in) |
| SCS0002 | SQL Injection (Security Code Scan) |

---

## References

- [CWE-89: SQL Injection](https://cwe.mitre.org/data/definitions/89.html)
- [OWASP SQL Injection](https://owasp.org/www-community/attacks/SQL_Injection)
- [OWASP SQL Injection Prevention Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/SQL_Injection_Prevention_Cheat_Sheet.html)
- [Microsoft: Parameterized Queries](https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/configuring-parameters-and-parameter-data-types)

---

## Deliverable Checklist

- [ ] Create `Analyzers/Security/SqlStringConcatenationAnalyzer.cs`
- [ ] Write analyzer tests (~20 tests)
- [ ] Add .editorconfig options support (sql_patterns, sql_methods)
- [ ] Verify all tests pass
- [ ] Test manually in Visual Studio
- [ ] Document limitations (pattern-based, not data flow)
