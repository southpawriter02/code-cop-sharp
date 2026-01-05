# CCS0042: HardcodedConnectionString

## Overview

| Property | Value |
|----------|-------|
| Rule ID | CCS0042 |
| Category | Security |
| Severity | Warning |
| Has Code Fix | No |
| Enabled by Default | Yes |
| CWE References | [CWE-259](https://cwe.mitre.org/data/definitions/259.html), [CWE-798](https://cwe.mitre.org/data/definitions/798.html) |

## Description

Detects hardcoded database connection strings in source code. Connection strings often contain server addresses, database names, and credentials that should be stored securely in configuration files or environment variables.

### Why This Rule?

1. **Credential Exposure**: Connection strings may contain usernames and passwords
2. **Server Information**: Reveals database server locations and ports
3. **Environment Coupling**: Hardcoded strings prevent environment-specific configuration
4. **Version Control**: Connection strings in code persist in git history
5. **Deployment Risk**: Same credentials used across all environments

### What This Rule Detects

- SQL Server connection strings (`Server=`, `Data Source=`)
- MySQL connection strings (`Server=`, `Database=`)
- PostgreSQL connection strings (`Host=`, `Database=`)
- Oracle connection strings (`Data Source=`)
- MongoDB connection strings (`mongodb://`)
- Redis connection strings (`redis://`)
- Connection strings with embedded credentials

---

## Configuration

```ini
# .editorconfig

# Enable/disable rule
dotnet_diagnostic.CCS0042.severity = warning

# Allow connection strings without credentials (server/database only)
dotnet_code_quality.CCS0042.allow_server_only = false

# Allow localhost/development connections
dotnet_code_quality.CCS0042.allow_localhost = true
```

---

## Detection Patterns

### Variable Name Patterns

The analyzer detects variables containing these keywords (case-insensitive):

| Keyword | Matches |
|---------|---------|
| `connectionstring` | ConnectionString, connectionString, connection_string |
| `connstring` | ConnString, connString, conn_string |
| `connstr` | ConnStr, connStr |
| `dbconnection` | DbConnection, db_connection |

### Connection String Format Patterns

| Database | Pattern Indicators |
|----------|-------------------|
| SQL Server | `Server=`, `Data Source=`, `Initial Catalog=`, `Integrated Security=` |
| MySQL | `Server=`, `Database=`, `Uid=`, `Pwd=` |
| PostgreSQL | `Host=`, `Database=`, `Username=`, `Password=` |
| Oracle | `Data Source=`, `User Id=`, `Password=` |
| MongoDB | `mongodb://`, `mongodb+srv://` |
| Redis | `redis://`, `,password=` |
| SQLite | `Data Source=*.db`, `Data Source=*.sqlite` |

### Credential Indicators

Connection strings are flagged with higher severity when they contain:
- `Password=` or `Pwd=`
- `User Id=` or `Uid=` with literal values
- Embedded credentials in URI format (`user:pass@host`)

---

## Compliant Examples

```csharp
// Good - connection string from configuration
public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DefaultConnection");
    }
}

// Good - connection string from environment variable
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL");

// Good - connection string from appsettings
var connectionString = configuration["ConnectionStrings:MainDb"];

// Good - Azure Key Vault
var connectionString = await keyVault.GetSecretAsync("db-connection-string");

// Good - localhost for development (when allow_localhost = true)
var connectionString = "Server=localhost;Database=TestDb;Integrated Security=true";

// Good - connection string builder (credentials from config)
var builder = new SqlConnectionStringBuilder
{
    DataSource = config["DbServer"],
    InitialCatalog = config["DbName"],
    UserID = config["DbUser"],
    Password = config["DbPassword"]
};

// Good - parameterized connection
public void Connect(string connectionString)
{
    using var connection = new SqlConnection(connectionString);
}
```

## Non-Compliant Examples

```csharp
// CCS0042 - hardcoded SQL Server connection string with credentials
private string _connString = "Server=prod-db.company.com;Database=MainDb;User Id=admin;Password=P@ssw0rd123;";

// CCS0042 - hardcoded connection string in field
private const string ConnectionString =
    "Data Source=192.168.1.100;Initial Catalog=AppDb;User ID=sa;Password=secret123";

// CCS0042 - hardcoded MySQL connection string
var mysqlConn = "Server=mysql.example.com;Database=myapp;Uid=root;Pwd=mysql123;";

// CCS0042 - hardcoded PostgreSQL connection string
var pgConn = "Host=postgres.prod.internal;Database=orders;Username=app_user;Password=pg_secret";

// CCS0042 - hardcoded MongoDB connection string
var mongoUri = "mongodb://admin:mongopass@mongo.cluster.internal:27017/mydb";

// CCS0042 - hardcoded Redis connection string
var redis = "redis://default:redispass@redis.internal:6379";

// CCS0042 - hardcoded Oracle connection string
var oracleConn = "Data Source=oracle.prod:1521/ORCL;User Id=system;Password=oracle123;";

// CCS0042 - hardcoded in object initializer
var config = new DbConfig
{
    ConnectionString = "Server=db.internal;Database=App;User=app;Password=secret"
};

// CCS0042 - hardcoded in method call
services.AddDbContext<AppContext>(options =>
    options.UseSqlServer("Server=prod;Database=App;User Id=sa;Password=pass123"));
```

---

## Implementation Specification

### File Structure

```
CodeCop.Sharp/
├── Analyzers/Security/HardcodedConnectionStringAnalyzer.cs
└── Utilities/ConnectionStringDetector.cs
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
    /// Analyzer that detects hardcoded connection strings in source code.
    /// </summary>
    /// <remarks>
    /// Rule ID: CCS0042
    /// Category: Security
    /// Severity: Warning
    /// CWE: CWE-259, CWE-798
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class HardcodedConnectionStringAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CCS0042";

        private static readonly LocalizableString Title = "Hardcoded connection string detected";
        private static readonly LocalizableString MessageFormat =
            "Hardcoded connection string in '{0}'. Store connection strings in configuration or environment variables.";
        private static readonly LocalizableString Description =
            "Connection strings should not be hardcoded. Use IConfiguration, environment variables, or secret management.";
        private const string Category = "Security";
        private const string HelpLinkUri = "https://cwe.mitre.org/data/definitions/798.html";

        // Connection string variable name patterns
        private static readonly Regex ConnectionStringNamePattern = new Regex(
            @"(connection[_-]?string|conn[_-]?str(ing)?|db[_-]?connection)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // SQL Server connection string patterns
        private static readonly Regex SqlServerPattern = new Regex(
            @"(Server|Data\s*Source)\s*=\s*[^;]+;.*(Initial\s*Catalog|Database)\s*=",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // MySQL connection string pattern
        private static readonly Regex MySqlPattern = new Regex(
            @"Server\s*=\s*[^;]+;.*Database\s*=\s*[^;]+;.*(Uid|User\s*Id)\s*=",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // PostgreSQL connection string pattern
        private static readonly Regex PostgresPattern = new Regex(
            @"Host\s*=\s*[^;]+;.*Database\s*=\s*[^;]+;",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // MongoDB URI pattern
        private static readonly Regex MongoDbPattern = new Regex(
            @"^mongodb(\+srv)?://",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Redis URI pattern
        private static readonly Regex RedisPattern = new Regex(
            @"^redis(s)?://",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Credential indicators
        private static readonly Regex CredentialPattern = new Regex(
            @"(Password|Pwd|User\s*Id|Uid|Username)\s*=\s*[^;]+",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // URI with embedded credentials
        private static readonly Regex UriCredentialPattern = new Regex(
            @"://[^:]+:[^@]+@",
            RegexOptions.Compiled);

        // Localhost pattern (may be allowed)
        private static readonly Regex LocalhostPattern = new Regex(
            @"(localhost|127\.0\.0\.1|::1|\(local\)|\(localdb\))",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLinkUri,
            customTags: new[] { "Security", "CWE-259", "CWE-798" });

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeStringLiteral, SyntaxKind.StringLiteralExpression);
        }

        private void AnalyzeStringLiteral(SyntaxNodeAnalysisContext context)
        {
            var literal = (LiteralExpressionSyntax)context.Node;
            var value = literal.Token.ValueText;

            if (string.IsNullOrWhiteSpace(value) || value.Length < 10)
                return;

            // Check if assigned to connection string variable
            var targetName = GetAssignmentTargetName(literal);
            bool isConnectionStringVariable = targetName != null &&
                ConnectionStringNamePattern.IsMatch(targetName);

            // Check if value looks like a connection string
            bool isConnectionStringFormat = IsConnectionStringFormat(value);

            if (!isConnectionStringVariable && !isConnectionStringFormat)
                return;

            // Check for localhost (may be allowed for development)
            bool isLocalhost = LocalhostPattern.IsMatch(value);
            // TODO: Check editorconfig for allow_localhost setting

            // Check for credentials in the connection string
            bool hasCredentials = HasEmbeddedCredentials(value);

            // Report if it's a connection string format or assigned to conn string variable
            if (isConnectionStringFormat || isConnectionStringVariable)
            {
                // Skip localhost without credentials if allowed
                if (isLocalhost && !hasCredentials)
                {
                    // Check configuration - for now, allow localhost dev connections
                    return;
                }

                var name = targetName ?? "connection string";
                ReportDiagnostic(context, literal.GetLocation(), name);
            }
        }

        private static bool IsConnectionStringFormat(string value)
        {
            // Check for database-specific patterns
            if (SqlServerPattern.IsMatch(value))
                return true;

            if (MySqlPattern.IsMatch(value))
                return true;

            if (PostgresPattern.IsMatch(value))
                return true;

            if (MongoDbPattern.IsMatch(value))
                return true;

            if (RedisPattern.IsMatch(value))
                return true;

            return false;
        }

        private static bool HasEmbeddedCredentials(string value)
        {
            // Check for key=value credential patterns
            if (CredentialPattern.IsMatch(value))
                return true;

            // Check for URI embedded credentials (user:pass@host)
            if (UriCredentialPattern.IsMatch(value))
                return true;

            return false;
        }

        private static string GetAssignmentTargetName(LiteralExpressionSyntax literal)
        {
            var parent = literal.Parent;

            if (parent is EqualsValueClauseSyntax equalsClause)
            {
                if (equalsClause.Parent is VariableDeclaratorSyntax declarator)
                    return declarator.Identifier.Text;

                if (equalsClause.Parent is PropertyDeclarationSyntax property)
                    return property.Identifier.Text;
            }

            if (parent is AssignmentExpressionSyntax assignment)
            {
                if (assignment.Left is IdentifierNameSyntax identifier)
                    return identifier.Identifier.Text;

                if (assignment.Left is MemberAccessExpressionSyntax memberAccess)
                    return memberAccess.Name.Identifier.Text;
            }

            if (parent is ArgumentSyntax argument)
            {
                // Check if this is UseSqlServer, UseNpgsql, etc.
                var invocation = argument.FirstAncestorOrSelf<InvocationExpressionSyntax>();
                if (invocation?.Expression is MemberAccessExpressionSyntax method)
                {
                    var methodName = method.Name.Identifier.Text;
                    if (methodName.StartsWith("Use") &&
                        (methodName.Contains("Sql") || methodName.Contains("Npgsql") ||
                         methodName.Contains("MySql") || methodName.Contains("Postgres")))
                    {
                        return methodName + " argument";
                    }
                }
            }

            return null;
        }

        private void ReportDiagnostic(SyntaxNodeAnalysisContext context, Location location, string name)
        {
            var diagnostic = Diagnostic.Create(Rule, location, name);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
```

---

## Decision Tree

```
                    ┌─────────────────────────────┐
                    │ Is it a string literal?     │
                    └──────────────┬──────────────┘
                                   │
                           ┌───────▼───────┐
                           │      NO       │────────► SKIP
                           └───────┬───────┘
                                   │ YES
                                   ▼
                    ┌─────────────────────────────┐
                    │ Is string < 10 chars?       │
                    └──────────────┬──────────────┘
                                   │
                           ┌───────▼───────┐
                           │      YES      │────────► SKIP
                           └───────┬───────┘
                                   │ NO
                                   ▼
        ┌───────────────────────────────────────────────────┐
        │ Is variable name connection-string-related?       │
        │ OR does value match connection string pattern?    │
        └─────────────────────────┬─────────────────────────┘
                                  │
                          ┌───────▼───────┐
                          │      NO       │────────► SKIP
                          └───────┬───────┘
                                  │ YES
                                  ▼
                    ┌─────────────────────────────┐
                    │ Is it localhost/127.0.0.1?  │
                    └──────────────┬──────────────┘
                                   │
                           ┌───────▼───────┐
                           │      YES      │
                           └───────┬───────┘
                                   │
                    ┌──────────────▼──────────────┐
                    │ Has embedded credentials?   │
                    └──────────────┬──────────────┘
                                   │
                ┌──────────────────┼──────────────────┐
                │                  │                  │
        ┌───────▼───────┐  ┌───────▼───────┐  ┌───────▼───────┐
        │ Localhost +   │  │ Localhost +   │  │ Non-localhost │
        │ No Credentials│  │ Credentials   │  │               │
        └───────┬───────┘  └───────┬───────┘  └───────┬───────┘
                │                  │                  │
                ▼                  ▼                  ▼
              SKIP           REPORT CCS0042    REPORT CCS0042
```

---

## Test Cases

### Analyzer Tests - Should Trigger Diagnostic

| Test Name | Input | Expected |
|-----------|-------|----------|
| SqlServerWithCreds | `"Server=prod;Database=App;User Id=sa;Password=x"` | CCS0042 |
| MySqlWithCreds | `"Server=mysql.com;Database=db;Uid=root;Pwd=x"` | CCS0042 |
| PostgresWithCreds | `"Host=pg.com;Database=db;Username=u;Password=x"` | CCS0042 |
| MongoDbWithCreds | `"mongodb://user:pass@mongo.com/db"` | CCS0042 |
| RedisWithCreds | `"redis://default:pass@redis.com:6379"` | CCS0042 |
| RemoteServer | `"Server=192.168.1.100;Database=App"` | CCS0042 |
| ProductionServer | `"Server=prod-db.company.com;Database=App"` | CCS0042 |
| ConnectionStringVar | `connectionString = "Server=x;Database=y"` | CCS0042 |
| UseSqlServerCall | `options.UseSqlServer("Server=x;...")` | CCS0042 |

### Analyzer Tests - Should NOT Trigger Diagnostic

| Test Name | Input | Expected |
|-----------|-------|----------|
| EmptyString | `var connStr = "";` | No diagnostic |
| ConfigurationCall | `config.GetConnectionString("Default")` | No diagnostic |
| EnvironmentVariable | `Environment.GetEnvironmentVariable("DB")` | No diagnostic |
| LocalhostNoCreds | `"Server=localhost;Database=Test;Integrated Security=true"` | No diagnostic |
| LocalDbNoCreds | `"Server=(localdb)\\mssqllocaldb;Database=Test"` | No diagnostic |
| ConnectionBuilder | `new SqlConnectionStringBuilder()` | No diagnostic |
| NonConnectionVar | `var server = "prod-db.company.com";` | No diagnostic |

---

## Edge Cases

| Case | Behavior | Rationale |
|------|----------|-----------|
| Localhost connections | Configurable | Development environments may use localhost |
| Integrated Security | Lower priority | No credentials in string, but still location info |
| SQLite local file | Context-dependent | `Data Source=app.db` is often acceptable |
| Named parameters | Detected | Connection string keywords are recognized |
| Multi-line strings | Analyzed | Verbatim strings are still literals |
| Connection string builders | Not flagged | Credentials may come from config separately |
| In-memory databases | Not flagged | `Data Source=:memory:` is acceptable |

---

## Related Rules

| Rule | Relationship |
|------|--------------|
| CCS0040 | HardcodedPassword - Passwords detected separately |
| CCS0041 | HardcodedApiKey - API keys detected separately |

---

## References

- [CWE-259: Use of Hard-coded Password](https://cwe.mitre.org/data/definitions/259.html)
- [CWE-798: Use of Hard-coded Credentials](https://cwe.mitre.org/data/definitions/798.html)
- [OWASP: Configuration and Deployment Management](https://owasp.org/www-project-web-security-testing-guide/latest/4-Web_Application_Security_Testing/02-Configuration_and_Deployment_Management_Testing/)

---

## Deliverable Checklist

- [ ] Create `Analyzers/Security/HardcodedConnectionStringAnalyzer.cs`
- [ ] Create `Utilities/ConnectionStringDetector.cs` for pattern matching
- [ ] Write analyzer tests (~20 tests)
- [ ] Add .editorconfig options support (allow_localhost, allow_server_only)
- [ ] Verify all tests pass
- [ ] Test manually in Visual Studio
