# CCS0040: HardcodedPassword

## Overview

| Property | Value |
|----------|-------|
| Rule ID | CCS0040 |
| Category | Security |
| Severity | Error |
| Has Code Fix | No |
| Enabled by Default | Yes |
| CWE References | [CWE-259](https://cwe.mitre.org/data/definitions/259.html), [CWE-798](https://cwe.mitre.org/data/definitions/798.html) |

## Description

Detects hardcoded passwords in source code. Hardcoded credentials are a significant security vulnerability that can lead to unauthorized access if the source code is exposed or decompiled.

### Why This Rule?

1. **Security Risk**: Hardcoded passwords can be extracted from compiled code
2. **Version Control Exposure**: Passwords in code end up in version control history
3. **Difficult Rotation**: Changing hardcoded passwords requires code changes and redeployment
4. **Compliance**: Violates security standards (OWASP, PCI-DSS, SOC2)
5. **Shared Risk**: All developers with code access have credential access

### What This Rule Detects

- String literals assigned to variables with password-related names
- Password parameters with literal string arguments
- Password properties initialized with literal values
- Dictionary/collection entries with password keys and literal values

---

## Configuration

```ini
# .editorconfig

# Enable/disable rule
dotnet_diagnostic.CCS0040.severity = error

# Configure additional password keywords (comma-separated)
dotnet_code_quality.CCS0040.additional_keywords = secret,credential,passphrase

# Minimum password length to trigger (avoids empty string false positives)
dotnet_code_quality.CCS0040.minimum_length = 1
```

---

## Detection Patterns

### Variable Name Patterns

The analyzer detects variables, fields, properties, and parameters containing these keywords (case-insensitive):

| Keyword | Matches |
|---------|---------|
| `password` | Password, PASSWORD, userPassword, PasswordHash |
| `passwd` | passwd, PASSWD, rootPasswd |
| `pwd` | pwd, PWD, userPwd, adminPwd |
| `secret` | secret, SECRET, clientSecret |
| `credential` | credential, Credentials, userCredential |

### Detection Contexts

| Context | Example | Detected |
|---------|---------|----------|
| Field initialization | `private string _password = "abc123";` | Yes |
| Local variable | `var password = "abc123";` | Yes |
| Property initializer | `public string Password { get; } = "abc123";` | Yes |
| Parameter default | `void Login(string password = "admin")` | Yes |
| Method argument | `SetPassword("abc123")` | Yes |
| Dictionary entry | `config["password"] = "abc123";` | Yes |
| Object initializer | `new Config { Password = "abc123" }` | Yes |

---

## Compliant Examples

```csharp
// Good - password from configuration
public class AuthService
{
    private readonly string _password;

    public AuthService(IConfiguration config)
    {
        _password = config["Database:Password"];
    }
}

// Good - password from environment variable
var password = Environment.GetEnvironmentVariable("DB_PASSWORD");

// Good - password from secure storage
var password = await secretManager.GetSecretAsync("database-password");

// Good - password from user input
Console.Write("Enter password: ");
var password = Console.ReadLine();

// Good - password parameter without default value
public void SetPassword(string password)
{
    // password is provided at runtime
}

// Good - empty/null password (testing scenarios)
var password = "";
string password = null;

// Good - password placeholder for documentation
/// <summary>
/// Password should be retrieved from environment variable DB_PASSWORD
/// </summary>
private string _password;
```

## Non-Compliant Examples

```csharp
// CCS0040 - hardcoded password in field
private string _password = "SuperSecret123!";

// CCS0040 - hardcoded password in local variable
var dbPassword = "admin123";

// CCS0040 - hardcoded password in property initializer
public string Password { get; set; } = "P@ssw0rd";

// CCS0040 - hardcoded password as method argument
connection.SetPassword("MySecretPwd");

// CCS0040 - hardcoded password in parameter default
public void Connect(string pwd = "default123")
{
}

// CCS0040 - hardcoded password in dictionary
var config = new Dictionary<string, string>
{
    ["password"] = "secret123"
};

// CCS0040 - hardcoded password in object initializer
var settings = new DatabaseSettings
{
    Password = "db_password_123"
};

// CCS0040 - hardcoded credential
private const string ClientSecret = "abc123xyz789";
```

---

## Implementation Specification

### File Structure

```
CodeCop.Sharp/
├── Analyzers/Security/HardcodedPasswordAnalyzer.cs
└── Utilities/SecretPatternMatcher.cs (shared)
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
    /// Analyzer that detects hardcoded passwords in source code.
    /// </summary>
    /// <remarks>
    /// Rule ID: CCS0040
    /// Category: Security
    /// Severity: Error
    /// CWE: CWE-259, CWE-798
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class HardcodedPasswordAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CCS0040";

        private static readonly LocalizableString Title = "Hardcoded password detected";
        private static readonly LocalizableString MessageFormat =
            "Hardcoded password in '{0}'. Store passwords in secure configuration or environment variables.";
        private static readonly LocalizableString Description =
            "Passwords should not be hardcoded in source code. Use configuration, environment variables, or secret management services.";
        private const string Category = "Security";
        private const string HelpLinkUri = "https://cwe.mitre.org/data/definitions/798.html";

        // Password-related keywords (case-insensitive)
        private static readonly string[] PasswordKeywords = new[]
        {
            "password", "passwd", "pwd", "secret", "credential"
        };

        private static readonly Regex PasswordPattern = new Regex(
            @"(password|passwd|pwd|secret|credential)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Error,
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

            // Analyze variable declarations
            context.RegisterSyntaxNodeAction(AnalyzeVariableDeclaration, SyntaxKind.VariableDeclaration);

            // Analyze property declarations
            context.RegisterSyntaxNodeAction(AnalyzePropertyDeclaration, SyntaxKind.PropertyDeclaration);

            // Analyze assignment expressions
            context.RegisterSyntaxNodeAction(AnalyzeAssignment, SyntaxKind.SimpleAssignmentExpression);

            // Analyze method arguments
            context.RegisterSyntaxNodeAction(AnalyzeArgument, SyntaxKind.Argument);

            // Analyze parameter defaults
            context.RegisterSyntaxNodeAction(AnalyzeParameter, SyntaxKind.Parameter);
        }

        private void AnalyzeVariableDeclaration(SyntaxNodeAnalysisContext context)
        {
            var declaration = (VariableDeclarationSyntax)context.Node;

            foreach (var variable in declaration.Variables)
            {
                if (!IsPasswordRelatedName(variable.Identifier.Text))
                    continue;

                if (variable.Initializer?.Value is LiteralExpressionSyntax literal &&
                    literal.IsKind(SyntaxKind.StringLiteralExpression))
                {
                    var value = literal.Token.ValueText;
                    if (!string.IsNullOrEmpty(value))
                    {
                        ReportDiagnostic(context, variable.Identifier.GetLocation(), variable.Identifier.Text);
                    }
                }
            }
        }

        private void AnalyzePropertyDeclaration(SyntaxNodeAnalysisContext context)
        {
            var property = (PropertyDeclarationSyntax)context.Node;

            if (!IsPasswordRelatedName(property.Identifier.Text))
                return;

            // Check initializer (e.g., public string Password { get; } = "secret";)
            if (property.Initializer?.Value is LiteralExpressionSyntax literal &&
                literal.IsKind(SyntaxKind.StringLiteralExpression))
            {
                var value = literal.Token.ValueText;
                if (!string.IsNullOrEmpty(value))
                {
                    ReportDiagnostic(context, property.Identifier.GetLocation(), property.Identifier.Text);
                }
            }

            // Check expression body (e.g., public string Password => "secret";)
            if (property.ExpressionBody?.Expression is LiteralExpressionSyntax exprLiteral &&
                exprLiteral.IsKind(SyntaxKind.StringLiteralExpression))
            {
                var value = exprLiteral.Token.ValueText;
                if (!string.IsNullOrEmpty(value))
                {
                    ReportDiagnostic(context, property.Identifier.GetLocation(), property.Identifier.Text);
                }
            }
        }

        private void AnalyzeAssignment(SyntaxNodeAnalysisContext context)
        {
            var assignment = (AssignmentExpressionSyntax)context.Node;

            // Get the name being assigned to
            string targetName = null;
            Location targetLocation = null;

            if (assignment.Left is IdentifierNameSyntax identifier)
            {
                targetName = identifier.Identifier.Text;
                targetLocation = identifier.GetLocation();
            }
            else if (assignment.Left is MemberAccessExpressionSyntax memberAccess)
            {
                targetName = memberAccess.Name.Identifier.Text;
                targetLocation = memberAccess.Name.GetLocation();
            }
            else if (assignment.Left is ElementAccessExpressionSyntax elementAccess)
            {
                // Handle dictionary["password"] = "value"
                if (elementAccess.ArgumentList.Arguments.Count == 1 &&
                    elementAccess.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax keyLiteral &&
                    keyLiteral.IsKind(SyntaxKind.StringLiteralExpression))
                {
                    targetName = keyLiteral.Token.ValueText;
                    targetLocation = keyLiteral.GetLocation();
                }
            }

            if (targetName == null || !IsPasswordRelatedName(targetName))
                return;

            if (assignment.Right is LiteralExpressionSyntax literal &&
                literal.IsKind(SyntaxKind.StringLiteralExpression))
            {
                var value = literal.Token.ValueText;
                if (!string.IsNullOrEmpty(value))
                {
                    ReportDiagnostic(context, targetLocation, targetName);
                }
            }
        }

        private void AnalyzeArgument(SyntaxNodeAnalysisContext context)
        {
            var argument = (ArgumentSyntax)context.Node;

            // Check if argument is a string literal
            if (!(argument.Expression is LiteralExpressionSyntax literal) ||
                !literal.IsKind(SyntaxKind.StringLiteralExpression))
                return;

            var value = literal.Token.ValueText;
            if (string.IsNullOrEmpty(value))
                return;

            // Get the parameter name this argument corresponds to
            var semanticModel = context.SemanticModel;
            var argumentList = argument.Parent as ArgumentListSyntax;
            var invocation = argumentList?.Parent;

            IMethodSymbol method = null;
            if (invocation is InvocationExpressionSyntax inv)
            {
                method = semanticModel.GetSymbolInfo(inv).Symbol as IMethodSymbol;
            }
            else if (invocation is ObjectCreationExpressionSyntax creation)
            {
                method = semanticModel.GetSymbolInfo(creation).Symbol as IMethodSymbol;
            }

            if (method == null)
                return;

            // Find the parameter this argument maps to
            var argumentIndex = argumentList.Arguments.IndexOf(argument);
            if (argument.NameColon != null)
            {
                // Named argument
                var paramName = argument.NameColon.Name.Identifier.Text;
                if (IsPasswordRelatedName(paramName))
                {
                    ReportDiagnostic(context, literal.GetLocation(), paramName);
                }
            }
            else if (argumentIndex >= 0 && argumentIndex < method.Parameters.Length)
            {
                // Positional argument
                var param = method.Parameters[argumentIndex];
                if (IsPasswordRelatedName(param.Name))
                {
                    ReportDiagnostic(context, literal.GetLocation(), param.Name);
                }
            }
        }

        private void AnalyzeParameter(SyntaxNodeAnalysisContext context)
        {
            var parameter = (ParameterSyntax)context.Node;

            if (!IsPasswordRelatedName(parameter.Identifier.Text))
                return;

            // Check default value
            if (parameter.Default?.Value is LiteralExpressionSyntax literal &&
                literal.IsKind(SyntaxKind.StringLiteralExpression))
            {
                var value = literal.Token.ValueText;
                if (!string.IsNullOrEmpty(value))
                {
                    ReportDiagnostic(context, parameter.Identifier.GetLocation(), parameter.Identifier.Text);
                }
            }
        }

        private static bool IsPasswordRelatedName(string name)
        {
            return PasswordPattern.IsMatch(name);
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
                    │ Is string empty or null?    │
                    └──────────────┬──────────────┘
                                   │
                           ┌───────▼───────┐
                           │      YES      │────────► SKIP
                           └───────┬───────┘
                                   │ NO
                                   ▼
                    ┌─────────────────────────────┐
                    │ Is target name password-    │
                    │ related? (variable, field,  │
                    │ property, parameter, key)   │
                    └──────────────┬──────────────┘
                                   │
                           ┌───────▼───────┐
                           │      NO       │────────► SKIP
                           └───────┬───────┘
                                   │ YES
                                   ▼
                           REPORT CCS0040
```

---

## Test Cases

### Analyzer Tests - Should Trigger Diagnostic

| Test Name | Input | Expected |
|-----------|-------|----------|
| FieldWithPassword | `private string _password = "secret";` | CCS0040 |
| LocalVariablePassword | `var password = "abc123";` | CCS0040 |
| PropertyPassword | `public string Password { get; } = "test";` | CCS0040 |
| ConstPassword | `const string Pwd = "admin";` | CCS0040 |
| MethodArgPassword | `Login(password: "secret")` | CCS0040 |
| ParameterDefaultPassword | `void Set(string password = "x")` | CCS0040 |
| DictionaryPassword | `dict["password"] = "value";` | CCS0040 |
| ObjectInitializerPassword | `new Cfg { Password = "x" }` | CCS0040 |
| SecretField | `private string _secret = "xyz";` | CCS0040 |
| CredentialField | `private string credential = "abc";` | CCS0040 |
| MixedCasePassword | `var PASSWORD = "test";` | CCS0040 |
| CamelCasePassword | `var userPassword = "test";` | CCS0040 |

### Analyzer Tests - Should NOT Trigger Diagnostic

| Test Name | Input | Expected |
|-----------|-------|----------|
| EmptyPassword | `var password = "";` | No diagnostic |
| NullPassword | `string password = null;` | No diagnostic |
| ConfigPassword | `var password = config["pwd"];` | No diagnostic |
| EnvPassword | `var pwd = Environment.GetVariable("X")` | No diagnostic |
| ParameterNoDefault | `void Set(string password)` | No diagnostic |
| NonPasswordField | `var username = "admin";` | No diagnostic |
| PasswordInComment | `// password = "secret"` | No diagnostic |
| PasswordAsKey | `dict[password] = value;` | No diagnostic |

---

## Edge Cases

| Case | Behavior | Rationale |
|------|----------|-----------|
| Empty string literal | Not reported | Empty passwords are likely placeholders |
| Null assignment | Not reported | Null is not a hardcoded value |
| Interpolated string | Analyze contained literals | `$"prefix{password}"` parts analyzed |
| Verbatim string | Reported | `@"password"` is still literal |
| Const fields | Reported | Constants are compiled into assemblies |
| Readonly fields | Reported | Still hardcoded at compile time |
| Test projects | Reported | Tests may leak to production |
| Generated code | Skipped | Configured via GeneratedCodeAnalysisFlags |
| Comments | Skipped | Not executable code |

---

## Related Rules

| Rule | Relationship |
|------|--------------|
| CCS0041 | HardcodedApiKey - Similar pattern for API keys |
| CCS0042 | HardcodedConnectionString - Connection string detection |

---

## References

- [CWE-259: Use of Hard-coded Password](https://cwe.mitre.org/data/definitions/259.html)
- [CWE-798: Use of Hard-coded Credentials](https://cwe.mitre.org/data/definitions/798.html)
- [OWASP: Hard-coded Password](https://owasp.org/www-community/vulnerabilities/Use_of_hard-coded_password)

---

## Deliverable Checklist

- [ ] Create `Analyzers/Security/HardcodedPasswordAnalyzer.cs`
- [ ] Add SecretPatternMatcher utility (shared with CCS0041, CCS0042)
- [ ] Write analyzer tests (~20 tests)
- [ ] Add .editorconfig options support
- [ ] Verify all tests pass
- [ ] Test manually in Visual Studio
