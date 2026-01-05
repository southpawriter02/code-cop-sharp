# CCS0041: HardcodedApiKey

## Overview

| Property | Value |
|----------|-------|
| Rule ID | CCS0041 |
| Category | Security |
| Severity | Error |
| Has Code Fix | No |
| Enabled by Default | Yes |
| CWE References | [CWE-798](https://cwe.mitre.org/data/definitions/798.html) |

## Description

Detects hardcoded API keys, tokens, and authentication secrets in source code. Hardcoded API keys pose significant security risks as they can be extracted from compiled binaries, exposed in version control, and are difficult to rotate.

### Why This Rule?

1. **Unauthorized Access**: Exposed API keys can be used by attackers
2. **Financial Risk**: Cloud service API keys can incur unexpected charges
3. **Data Breach**: API keys often grant access to sensitive data
4. **Version Control**: Keys in code persist in git history even after removal
5. **Rotation Difficulty**: Hardcoded keys require code changes to rotate

### What This Rule Detects

- String literals assigned to variables with API key-related names
- Common API key patterns (AWS, Azure, GCP, Stripe, etc.)
- Bearer tokens and JWT secrets
- OAuth client secrets
- Webhook secrets and signing keys

---

## Configuration

```ini
# .editorconfig

# Enable/disable rule
dotnet_diagnostic.CCS0041.severity = error

# Configure additional API key keywords (comma-separated)
dotnet_code_quality.CCS0041.additional_keywords = webhook_secret,signing_key

# Minimum key length to trigger (avoids false positives on short strings)
dotnet_code_quality.CCS0041.minimum_length = 8
```

---

## Detection Patterns

### Variable Name Patterns

The analyzer detects variables containing these keywords (case-insensitive):

| Keyword | Matches |
|---------|---------|
| `apikey` | ApiKey, API_KEY, apiKey, AWSApiKey |
| `api_key` | api_key, API_KEY, aws_api_key |
| `accesskey` | AccessKey, accessKey, access_key |
| `secretkey` | SecretKey, secret_key, aws_secret_key |
| `authtoken` | AuthToken, auth_token, authenticationToken |
| `bearertoken` | BearerToken, bearer_token |
| `clientsecret` | ClientSecret, client_secret, oauthClientSecret |
| `appsecret` | AppSecret, app_secret, applicationSecret |
| `privatekey` | PrivateKey, private_key (for API usage) |

### Known API Key Patterns (Regex-based)

| Provider | Pattern | Example |
|----------|---------|---------|
| AWS Access Key | `AKIA[0-9A-Z]{16}` | AKIAIOSFODNN7EXAMPLE |
| AWS Secret Key | `[A-Za-z0-9/+=]{40}` with context | wJalrXUtnFEMI/K7MDENG/... |
| Azure Storage | `[A-Za-z0-9+/]{86}==` | AccountKey pattern |
| Stripe | `sk_live_[0-9a-zA-Z]{24}` | sk_live_4eC39HqLyjWDarjtT1zdp7dc |
| GitHub PAT | `ghp_[0-9a-zA-Z]{36}` | ghp_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx |
| Slack Token | `xox[baprs]-[0-9a-zA-Z-]+` | xoxb-123456-abcdef |

---

## Compliant Examples

```csharp
// Good - API key from configuration
public class ApiService
{
    private readonly string _apiKey;

    public ApiService(IConfiguration config)
    {
        _apiKey = config["ExternalApi:ApiKey"];
    }
}

// Good - API key from environment variable
var apiKey = Environment.GetEnvironmentVariable("API_KEY");

// Good - API key from Azure Key Vault
var apiKey = await keyVaultClient.GetSecretAsync("api-key");

// Good - API key from AWS Secrets Manager
var apiKey = await secretsManager.GetSecretValueAsync(request);

// Good - API key passed as parameter (runtime value)
public void Configure(string apiKey)
{
    _client.SetApiKey(apiKey);
}

// Good - placeholder/template value
var apiKey = "<YOUR_API_KEY_HERE>";
var apiKey = "your-api-key";

// Good - empty value
var apiKey = "";
```

## Non-Compliant Examples

```csharp
// CCS0041 - hardcoded API key in field
private string _apiKey = "sk_live_4eC39HqLyjWDarjtT1zdp7dc";

// CCS0041 - hardcoded API key in constant
private const string ApiKey = "AIzaSyDaGmWKa4JsXZ-HjGw7ISLn_3namBGewQe";

// CCS0041 - hardcoded AWS access key
var awsAccessKey = "AKIAIOSFODNN7EXAMPLE";

// CCS0041 - hardcoded AWS secret key
var awsSecretKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY";

// CCS0041 - hardcoded bearer token
private string _bearerToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...";

// CCS0041 - hardcoded client secret
var clientSecret = "dGhpcyBpcyBhIHNlY3JldA==";

// CCS0041 - hardcoded GitHub token
const string GitHubToken = "ghp_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx";

// CCS0041 - hardcoded in method call
httpClient.DefaultRequestHeaders.Add("X-Api-Key", "real_api_key_12345");

// CCS0041 - hardcoded in object initializer
var config = new ApiConfig
{
    ApiKey = "production_key_abc123"
};

// CCS0041 - hardcoded Stripe key
var stripe = new StripeClient("sk_live_4eC39HqLyjWDarjtT1zdp7dc");
```

---

## Implementation Specification

### File Structure

```
CodeCop.Sharp/
├── Analyzers/Security/HardcodedApiKeyAnalyzer.cs
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
    /// Analyzer that detects hardcoded API keys and tokens in source code.
    /// </summary>
    /// <remarks>
    /// Rule ID: CCS0041
    /// Category: Security
    /// Severity: Error
    /// CWE: CWE-798
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class HardcodedApiKeyAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CCS0041";

        private static readonly LocalizableString Title = "Hardcoded API key detected";
        private static readonly LocalizableString MessageFormat =
            "Hardcoded API key in '{0}'. Store API keys in secure configuration or environment variables.";
        private static readonly LocalizableString Description =
            "API keys should not be hardcoded in source code. Use configuration, environment variables, or secret management services.";
        private const string Category = "Security";
        private const string HelpLinkUri = "https://cwe.mitre.org/data/definitions/798.html";

        private const int MinimumKeyLength = 8;

        // API key variable name patterns
        private static readonly Regex ApiKeyNamePattern = new Regex(
            @"(api[_-]?key|access[_-]?key|secret[_-]?key|auth[_-]?token|bearer[_-]?token|client[_-]?secret|app[_-]?secret|private[_-]?key)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Known API key format patterns
        private static readonly Regex[] ApiKeyValuePatterns = new[]
        {
            // AWS Access Key ID
            new Regex(@"^AKIA[0-9A-Z]{16}$", RegexOptions.Compiled),

            // AWS Secret Access Key (40 chars, base64-like)
            new Regex(@"^[A-Za-z0-9/+=]{40}$", RegexOptions.Compiled),

            // Stripe API Key
            new Regex(@"^sk_(live|test)_[0-9a-zA-Z]{24,}$", RegexOptions.Compiled),
            new Regex(@"^pk_(live|test)_[0-9a-zA-Z]{24,}$", RegexOptions.Compiled),

            // GitHub Personal Access Token
            new Regex(@"^ghp_[0-9a-zA-Z]{36}$", RegexOptions.Compiled),
            new Regex(@"^github_pat_[0-9a-zA-Z_]{22,}$", RegexOptions.Compiled),

            // Slack Token
            new Regex(@"^xox[baprs]-[0-9a-zA-Z-]+$", RegexOptions.Compiled),

            // Google API Key
            new Regex(@"^AIza[0-9A-Za-z_-]{35}$", RegexOptions.Compiled),

            // Azure Storage Account Key
            new Regex(@"^[A-Za-z0-9+/]{86}==$", RegexOptions.Compiled),

            // Generic JWT (base64.base64.base64)
            new Regex(@"^eyJ[A-Za-z0-9_-]*\.eyJ[A-Za-z0-9_-]*\.[A-Za-z0-9_-]+$", RegexOptions.Compiled),

            // Twilio API Key
            new Regex(@"^SK[0-9a-fA-F]{32}$", RegexOptions.Compiled),

            // SendGrid API Key
            new Regex(@"^SG\.[0-9A-Za-z_-]+\.[0-9A-Za-z_-]+$", RegexOptions.Compiled),
        };

        // Placeholder patterns to ignore
        private static readonly Regex PlaceholderPattern = new Regex(
            @"^(<.*>|your[_-]?.*|xxx+|placeholder|example|sample|test|dummy)",
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
            customTags: new[] { "Security", "CWE-798" });

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

            // Skip empty or short strings
            if (string.IsNullOrEmpty(value) || value.Length < MinimumKeyLength)
                return;

            // Skip placeholder values
            if (PlaceholderPattern.IsMatch(value))
                return;

            // Check 1: Is this assigned to an API key-named variable?
            var targetName = GetAssignmentTargetName(literal);
            if (targetName != null && ApiKeyNamePattern.IsMatch(targetName))
            {
                ReportDiagnostic(context, literal.GetLocation(), targetName);
                return;
            }

            // Check 2: Does the value match a known API key pattern?
            foreach (var pattern in ApiKeyValuePatterns)
            {
                if (pattern.IsMatch(value))
                {
                    var name = targetName ?? "string literal";
                    ReportDiagnostic(context, literal.GetLocation(), name);
                    return;
                }
            }
        }

        private static string GetAssignmentTargetName(LiteralExpressionSyntax literal)
        {
            var parent = literal.Parent;

            // Variable declaration: var apiKey = "...";
            if (parent is EqualsValueClauseSyntax equalsClause)
            {
                if (equalsClause.Parent is VariableDeclaratorSyntax declarator)
                    return declarator.Identifier.Text;

                if (equalsClause.Parent is PropertyDeclarationSyntax property)
                    return property.Identifier.Text;

                if (equalsClause.Parent is ParameterSyntax parameter)
                    return parameter.Identifier.Text;
            }

            // Assignment: apiKey = "...";
            if (parent is AssignmentExpressionSyntax assignment)
            {
                if (assignment.Left is IdentifierNameSyntax identifier)
                    return identifier.Identifier.Text;

                if (assignment.Left is MemberAccessExpressionSyntax memberAccess)
                    return memberAccess.Name.Identifier.Text;
            }

            // Named argument: ApiKey: "..."
            if (parent is ArgumentSyntax argument && argument.NameColon != null)
                return argument.NameColon.Name.Identifier.Text;

            // Object initializer: { ApiKey = "..." }
            if (parent is AssignmentExpressionSyntax initAssignment &&
                initAssignment.Parent is InitializerExpressionSyntax)
            {
                if (initAssignment.Left is IdentifierNameSyntax id)
                    return id.Identifier.Text;
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
                    │ Is string < 8 chars?        │
                    └──────────────┬──────────────┘
                                   │
                           ┌───────▼───────┐
                           │      YES      │────────► SKIP (too short)
                           └───────┬───────┘
                                   │ NO
                                   ▼
                    ┌─────────────────────────────┐
                    │ Is it a placeholder value?  │
                    │ (<...>, your_*, xxx, etc.)  │
                    └──────────────┬──────────────┘
                                   │
                           ┌───────▼───────┐
                           │      YES      │────────► SKIP
                           └───────┬───────┘
                                   │ NO
                                   ▼
                    ┌─────────────────────────────┐
                    │ Is variable name API key-   │
                    │ related?                    │
                    └──────────────┬──────────────┘
                                   │
                           ┌───────▼───────┐
                           │      YES      │────────► REPORT CCS0041
                           └───────┬───────┘
                                   │ NO
                                   ▼
                    ┌─────────────────────────────┐
                    │ Does value match known API  │
                    │ key pattern? (AWS, Stripe,  │
                    │ GitHub, etc.)               │
                    └──────────────┬──────────────┘
                                   │
                           ┌───────▼───────┐
                           │      YES      │────────► REPORT CCS0041
                           └───────┬───────┘
                                   │ NO
                                   ▼
                                 SKIP
```

---

## Test Cases

### Analyzer Tests - Should Trigger Diagnostic

| Test Name | Input | Expected |
|-----------|-------|----------|
| ApiKeyField | `private string _apiKey = "abc123xyz789";` | CCS0041 |
| AccessKeyField | `private string accessKey = "AKIAIOSFODNN7EXAMPLE";` | CCS0041 |
| AwsSecretKey | `var secretKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYKEY";` | CCS0041 |
| StripeKey | `const string key = "sk_live_4eC39HqLyjWDarj";` | CCS0041 |
| GitHubToken | `var token = "ghp_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx";` | CCS0041 |
| BearerToken | `var bearerToken = "eyJhbGciOiJIUzI1...";` | CCS0041 |
| ClientSecret | `var clientSecret = "dGhpcyBpcyBhIHNlY3JldA==";` | CCS0041 |
| GoogleApiKey | `var key = "AIzaSyDaGmWKa4JsXZ-HjGw7ISLn_3namBGe";` | CCS0041 |
| SlackToken | `var slack = "xoxb-123456789012-abcdefghij";` | CCS0041 |
| ObjectInitializer | `new Config { ApiKey = "real_key" }` | CCS0041 |

### Analyzer Tests - Should NOT Trigger Diagnostic

| Test Name | Input | Expected |
|-----------|-------|----------|
| EmptyApiKey | `var apiKey = "";` | No diagnostic |
| ShortValue | `var apiKey = "abc";` | No diagnostic |
| PlaceholderKey | `var apiKey = "<YOUR_API_KEY>";` | No diagnostic |
| YourApiKey | `var apiKey = "your-api-key";` | No diagnostic |
| ConfigApiKey | `var apiKey = config["ApiKey"];` | No diagnostic |
| EnvApiKey | `var key = Environment.GetVar("KEY");` | No diagnostic |
| NonKeyVariable | `var username = "admin123456";` | No diagnostic |
| TestKey | `var apiKey = "test_key_12345678";` | No diagnostic |

---

## Edge Cases

| Case | Behavior | Rationale |
|------|----------|-----------|
| Placeholder values | Not reported | `<YOUR_KEY>`, `your-api-key` are templates |
| Short strings (<8 chars) | Not reported | Unlikely to be real API keys |
| Test/sample values | Not reported | Values starting with `test_`, `sample_` |
| Base64-like strings | Context-dependent | Only reported if in key-named variable or matches provider pattern |
| Environment variable names | Not reported | `"API_KEY"` as env var name is fine |
| Documentation strings | Not reported | XML doc comments are skipped |
| Interpolated strings | Analyze if contains literal | `$"Bearer {hardcodedKey}"` - analyze the variable |

---

## Related Rules

| Rule | Relationship |
|------|--------------|
| CCS0040 | HardcodedPassword - Similar pattern for passwords |
| CCS0042 | HardcodedConnectionString - Connection string detection |

---

## References

- [CWE-798: Use of Hard-coded Credentials](https://cwe.mitre.org/data/definitions/798.html)
- [OWASP: Hardcoded Credentials](https://owasp.org/www-community/vulnerabilities/Use_of_hard-coded_password)
- [GitHub Secret Scanning Patterns](https://docs.github.com/en/code-security/secret-scanning/secret-scanning-patterns)

---

## Deliverable Checklist

- [ ] Create `Analyzers/Security/HardcodedApiKeyAnalyzer.cs`
- [ ] Share SecretPatternMatcher utility with CCS0040
- [ ] Write analyzer tests (~25 tests)
- [ ] Add known provider pattern tests (AWS, Stripe, GitHub, etc.)
- [ ] Add .editorconfig options support
- [ ] Verify all tests pass
- [ ] Test manually in Visual Studio
