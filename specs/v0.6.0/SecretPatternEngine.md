# Secret Pattern Engine

## Overview

The Secret Pattern Engine is a shared utility component that provides pattern matching capabilities for detecting hardcoded secrets across multiple security analyzers (CCS0040, CCS0041, CCS0042).

## Goals

1. **Centralized Pattern Matching**: Single source for secret detection patterns
2. **Extensibility**: Easy to add new secret providers and patterns
3. **Performance**: Compiled regex patterns for efficient matching
4. **Configurability**: Support for custom patterns via .editorconfig
5. **Reusability**: Shared across multiple analyzers

---

## Architecture

### File Structure

```
CodeCop.Sharp/
└── Utilities/
    ├── SecretPatternEngine.cs
    ├── SecretPattern.cs
    └── SecretCategory.cs
```

### Component Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                     Security Analyzers                      │
├───────────────┬───────────────┬───────────────┬────────────┤
│   CCS0040     │   CCS0041     │   CCS0042     │   Future   │
│  Password     │   ApiKey      │  ConnString   │  Analyzers │
└───────┬───────┴───────┬───────┴───────┬───────┴─────┬──────┘
        │               │               │             │
        └───────────────┴───────────────┴─────────────┘
                                │
                    ┌───────────▼───────────┐
                    │  SecretPatternEngine  │
                    ├───────────────────────┤
                    │ - Pattern Registry    │
                    │ - Name Matchers       │
                    │ - Value Patterns      │
                    │ - Provider Patterns   │
                    └───────────────────────┘
```

---

## Data Types

### SecretCategory Enum

```csharp
namespace CodeCop.Sharp.Utilities
{
    /// <summary>
    /// Categories of secrets that can be detected.
    /// </summary>
    public enum SecretCategory
    {
        /// <summary>Passwords and credentials</summary>
        Password,

        /// <summary>API keys and access tokens</summary>
        ApiKey,

        /// <summary>Database connection strings</summary>
        ConnectionString,

        /// <summary>Private keys (RSA, SSH, etc.)</summary>
        PrivateKey,

        /// <summary>OAuth/JWT tokens</summary>
        Token,

        /// <summary>Generic secret</summary>
        Generic
    }
}
```

### SecretPattern Class

```csharp
namespace CodeCop.Sharp.Utilities
{
    /// <summary>
    /// Represents a pattern for detecting a specific type of secret.
    /// </summary>
    public class SecretPattern
    {
        /// <summary>
        /// Unique identifier for this pattern (e.g., "aws_access_key").
        /// </summary>
        public string Id { get; init; }

        /// <summary>
        /// Human-readable name for the pattern.
        /// </summary>
        public string Name { get; init; }

        /// <summary>
        /// Category of secret this pattern detects.
        /// </summary>
        public SecretCategory Category { get; init; }

        /// <summary>
        /// Optional regex pattern for matching variable names.
        /// </summary>
        public Regex NamePattern { get; init; }

        /// <summary>
        /// Optional regex pattern for matching secret values.
        /// </summary>
        public Regex ValuePattern { get; init; }

        /// <summary>
        /// Minimum length for value matching.
        /// </summary>
        public int MinimumLength { get; init; } = 1;

        /// <summary>
        /// Provider/service this pattern is associated with (e.g., "AWS", "Stripe").
        /// </summary>
        public string Provider { get; init; }

        /// <summary>
        /// Whether this is a high-confidence pattern (exact match vs heuristic).
        /// </summary>
        public bool HighConfidence { get; init; }
    }
}
```

---

## SecretPatternEngine Implementation

```csharp
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;

namespace CodeCop.Sharp.Utilities
{
    /// <summary>
    /// Engine for detecting hardcoded secrets using pattern matching.
    /// </summary>
    public sealed class SecretPatternEngine
    {
        private static readonly Lazy<SecretPatternEngine> _instance =
            new Lazy<SecretPatternEngine>(() => new SecretPatternEngine());

        public static SecretPatternEngine Instance => _instance.Value;

        private readonly ImmutableArray<SecretPattern> _patterns;
        private readonly ImmutableDictionary<SecretCategory, ImmutableArray<SecretPattern>> _patternsByCategory;

        private SecretPatternEngine()
        {
            _patterns = InitializePatterns();
            _patternsByCategory = _patterns
                .GroupBy(p => p.Category)
                .ToImmutableDictionary(g => g.Key, g => g.ToImmutableArray());
        }

        #region Pattern Initialization

        private static ImmutableArray<SecretPattern> InitializePatterns()
        {
            var patterns = new List<SecretPattern>();

            // Password patterns
            patterns.AddRange(GetPasswordPatterns());

            // API key patterns
            patterns.AddRange(GetApiKeyPatterns());

            // Connection string patterns
            patterns.AddRange(GetConnectionStringPatterns());

            // Provider-specific patterns
            patterns.AddRange(GetProviderPatterns());

            return patterns.ToImmutableArray();
        }

        private static IEnumerable<SecretPattern> GetPasswordPatterns()
        {
            yield return new SecretPattern
            {
                Id = "password_variable",
                Name = "Password Variable",
                Category = SecretCategory.Password,
                NamePattern = new Regex(
                    @"(password|passwd|pwd|secret|credential)",
                    RegexOptions.IgnoreCase | RegexOptions.Compiled),
                MinimumLength = 1,
                HighConfidence = false
            };
        }

        private static IEnumerable<SecretPattern> GetApiKeyPatterns()
        {
            yield return new SecretPattern
            {
                Id = "api_key_variable",
                Name = "API Key Variable",
                Category = SecretCategory.ApiKey,
                NamePattern = new Regex(
                    @"(api[_-]?key|access[_-]?key|secret[_-]?key|auth[_-]?token|bearer[_-]?token|client[_-]?secret|app[_-]?secret)",
                    RegexOptions.IgnoreCase | RegexOptions.Compiled),
                MinimumLength = 8,
                HighConfidence = false
            };
        }

        private static IEnumerable<SecretPattern> GetConnectionStringPatterns()
        {
            yield return new SecretPattern
            {
                Id = "connection_string_variable",
                Name = "Connection String Variable",
                Category = SecretCategory.ConnectionString,
                NamePattern = new Regex(
                    @"(connection[_-]?string|conn[_-]?str(ing)?|db[_-]?connection)",
                    RegexOptions.IgnoreCase | RegexOptions.Compiled),
                MinimumLength = 10,
                HighConfidence = false
            };

            yield return new SecretPattern
            {
                Id = "sql_server_connection",
                Name = "SQL Server Connection String",
                Category = SecretCategory.ConnectionString,
                ValuePattern = new Regex(
                    @"(Server|Data\s*Source)\s*=\s*[^;]+;.*(Initial\s*Catalog|Database)\s*=",
                    RegexOptions.IgnoreCase | RegexOptions.Compiled),
                MinimumLength = 20,
                HighConfidence = true
            };

            yield return new SecretPattern
            {
                Id = "mongodb_uri",
                Name = "MongoDB Connection URI",
                Category = SecretCategory.ConnectionString,
                ValuePattern = new Regex(
                    @"^mongodb(\+srv)?://",
                    RegexOptions.IgnoreCase | RegexOptions.Compiled),
                MinimumLength = 15,
                Provider = "MongoDB",
                HighConfidence = true
            };

            yield return new SecretPattern
            {
                Id = "redis_uri",
                Name = "Redis Connection URI",
                Category = SecretCategory.ConnectionString,
                ValuePattern = new Regex(
                    @"^redis(s)?://",
                    RegexOptions.IgnoreCase | RegexOptions.Compiled),
                MinimumLength = 10,
                Provider = "Redis",
                HighConfidence = true
            };
        }

        private static IEnumerable<SecretPattern> GetProviderPatterns()
        {
            // AWS
            yield return new SecretPattern
            {
                Id = "aws_access_key",
                Name = "AWS Access Key ID",
                Category = SecretCategory.ApiKey,
                ValuePattern = new Regex(@"^AKIA[0-9A-Z]{16}$", RegexOptions.Compiled),
                Provider = "AWS",
                HighConfidence = true
            };

            yield return new SecretPattern
            {
                Id = "aws_secret_key",
                Name = "AWS Secret Access Key",
                Category = SecretCategory.ApiKey,
                ValuePattern = new Regex(@"^[A-Za-z0-9/+=]{40}$", RegexOptions.Compiled),
                NamePattern = new Regex(@"(secret|aws)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                Provider = "AWS",
                MinimumLength = 40,
                HighConfidence = false // Needs name context to avoid false positives
            };

            // Stripe
            yield return new SecretPattern
            {
                Id = "stripe_secret_key",
                Name = "Stripe Secret Key",
                Category = SecretCategory.ApiKey,
                ValuePattern = new Regex(@"^sk_(live|test)_[0-9a-zA-Z]{24,}$", RegexOptions.Compiled),
                Provider = "Stripe",
                HighConfidence = true
            };

            yield return new SecretPattern
            {
                Id = "stripe_publishable_key",
                Name = "Stripe Publishable Key",
                Category = SecretCategory.ApiKey,
                ValuePattern = new Regex(@"^pk_(live|test)_[0-9a-zA-Z]{24,}$", RegexOptions.Compiled),
                Provider = "Stripe",
                HighConfidence = true
            };

            // GitHub
            yield return new SecretPattern
            {
                Id = "github_pat",
                Name = "GitHub Personal Access Token",
                Category = SecretCategory.Token,
                ValuePattern = new Regex(@"^ghp_[0-9a-zA-Z]{36}$", RegexOptions.Compiled),
                Provider = "GitHub",
                HighConfidence = true
            };

            yield return new SecretPattern
            {
                Id = "github_pat_fine",
                Name = "GitHub Fine-Grained PAT",
                Category = SecretCategory.Token,
                ValuePattern = new Regex(@"^github_pat_[0-9a-zA-Z_]{22,}$", RegexOptions.Compiled),
                Provider = "GitHub",
                HighConfidence = true
            };

            // Slack
            yield return new SecretPattern
            {
                Id = "slack_token",
                Name = "Slack Token",
                Category = SecretCategory.Token,
                ValuePattern = new Regex(@"^xox[baprs]-[0-9a-zA-Z-]+$", RegexOptions.Compiled),
                Provider = "Slack",
                HighConfidence = true
            };

            // Google
            yield return new SecretPattern
            {
                Id = "google_api_key",
                Name = "Google API Key",
                Category = SecretCategory.ApiKey,
                ValuePattern = new Regex(@"^AIza[0-9A-Za-z_-]{35}$", RegexOptions.Compiled),
                Provider = "Google",
                HighConfidence = true
            };

            // Azure
            yield return new SecretPattern
            {
                Id = "azure_storage_key",
                Name = "Azure Storage Account Key",
                Category = SecretCategory.ApiKey,
                ValuePattern = new Regex(@"^[A-Za-z0-9+/]{86}==$", RegexOptions.Compiled),
                Provider = "Azure",
                HighConfidence = true
            };

            // JWT
            yield return new SecretPattern
            {
                Id = "jwt_token",
                Name = "JWT Token",
                Category = SecretCategory.Token,
                ValuePattern = new Regex(@"^eyJ[A-Za-z0-9_-]*\.eyJ[A-Za-z0-9_-]*\.[A-Za-z0-9_-]+$", RegexOptions.Compiled),
                HighConfidence = true
            };

            // Twilio
            yield return new SecretPattern
            {
                Id = "twilio_api_key",
                Name = "Twilio API Key",
                Category = SecretCategory.ApiKey,
                ValuePattern = new Regex(@"^SK[0-9a-fA-F]{32}$", RegexOptions.Compiled),
                Provider = "Twilio",
                HighConfidence = true
            };

            // SendGrid
            yield return new SecretPattern
            {
                Id = "sendgrid_api_key",
                Name = "SendGrid API Key",
                Category = SecretCategory.ApiKey,
                ValuePattern = new Regex(@"^SG\.[0-9A-Za-z_-]+\.[0-9A-Za-z_-]+$", RegexOptions.Compiled),
                Provider = "SendGrid",
                HighConfidence = true
            };
        }

        #endregion

        #region Public API

        /// <summary>
        /// Gets all registered patterns.
        /// </summary>
        public ImmutableArray<SecretPattern> GetAllPatterns() => _patterns;

        /// <summary>
        /// Gets patterns for a specific category.
        /// </summary>
        public ImmutableArray<SecretPattern> GetPatterns(SecretCategory category)
        {
            return _patternsByCategory.TryGetValue(category, out var patterns)
                ? patterns
                : ImmutableArray<SecretPattern>.Empty;
        }

        /// <summary>
        /// Checks if a variable name matches any secret pattern.
        /// </summary>
        /// <param name="name">The variable/field/property name to check.</param>
        /// <param name="category">Optional category filter.</param>
        /// <returns>Matching pattern or null.</returns>
        public SecretPattern MatchName(string name, SecretCategory? category = null)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            var patterns = category.HasValue
                ? GetPatterns(category.Value)
                : _patterns;

            return patterns.FirstOrDefault(p =>
                p.NamePattern != null && p.NamePattern.IsMatch(name));
        }

        /// <summary>
        /// Checks if a value matches any known secret pattern.
        /// </summary>
        /// <param name="value">The string value to check.</param>
        /// <param name="category">Optional category filter.</param>
        /// <returns>Matching pattern or null.</returns>
        public SecretPattern MatchValue(string value, SecretCategory? category = null)
        {
            if (string.IsNullOrEmpty(value))
                return null;

            var patterns = category.HasValue
                ? GetPatterns(category.Value)
                : _patterns;

            return patterns.FirstOrDefault(p =>
                p.ValuePattern != null &&
                value.Length >= p.MinimumLength &&
                p.ValuePattern.IsMatch(value));
        }

        /// <summary>
        /// Checks if a name/value combination indicates a secret.
        /// </summary>
        /// <param name="name">The variable/field/property name.</param>
        /// <param name="value">The string value.</param>
        /// <param name="category">Optional category filter.</param>
        /// <returns>Matching pattern or null.</returns>
        public SecretPattern Match(string name, string value, SecretCategory? category = null)
        {
            // First check for high-confidence value patterns
            var valueMatch = MatchValue(value, category);
            if (valueMatch?.HighConfidence == true)
                return valueMatch;

            // Then check name patterns
            var nameMatch = MatchName(name, category);
            if (nameMatch != null)
                return nameMatch;

            // Return low-confidence value match if found
            return valueMatch;
        }

        /// <summary>
        /// Checks if a value looks like a placeholder (not a real secret).
        /// </summary>
        public bool IsPlaceholder(string value)
        {
            if (string.IsNullOrEmpty(value))
                return true;

            return PlaceholderPattern.IsMatch(value);
        }

        private static readonly Regex PlaceholderPattern = new Regex(
            @"^(<.*>|your[_-]?.*|xxx+|placeholder|example|sample|test[_-]?|dummy|fake|mock)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        #endregion
    }
}
```

---

## Usage Examples

### In HardcodedPasswordAnalyzer

```csharp
private void AnalyzeStringLiteral(SyntaxNodeAnalysisContext context)
{
    var literal = (LiteralExpressionSyntax)context.Node;
    var value = literal.Token.ValueText;

    if (string.IsNullOrEmpty(value))
        return;

    var engine = SecretPatternEngine.Instance;

    // Skip placeholder values
    if (engine.IsPlaceholder(value))
        return;

    var targetName = GetAssignmentTargetName(literal);
    var match = engine.Match(targetName, value, SecretCategory.Password);

    if (match != null)
    {
        ReportDiagnostic(context, literal.GetLocation(), targetName ?? "string literal");
    }
}
```

### In HardcodedApiKeyAnalyzer

```csharp
private void AnalyzeStringLiteral(SyntaxNodeAnalysisContext context)
{
    var literal = (LiteralExpressionSyntax)context.Node;
    var value = literal.Token.ValueText;

    if (string.IsNullOrEmpty(value) || value.Length < 8)
        return;

    var engine = SecretPatternEngine.Instance;

    if (engine.IsPlaceholder(value))
        return;

    var targetName = GetAssignmentTargetName(literal);

    // Check for high-confidence provider patterns first
    var match = engine.MatchValue(value, SecretCategory.ApiKey);
    if (match?.HighConfidence == true)
    {
        ReportDiagnostic(context, literal.GetLocation(),
            $"{match.Provider ?? "API"} key detected");
        return;
    }

    // Then check name patterns
    match = engine.Match(targetName, value, SecretCategory.ApiKey);
    if (match != null)
    {
        ReportDiagnostic(context, literal.GetLocation(), targetName);
    }
}
```

---

## Configuration

### .editorconfig Support

```ini
# Add custom patterns (comma-separated)
dotnet_code_quality.CCS0040.additional_name_patterns = mysecret,mycred

# Add custom value patterns (comma-separated regex)
dotnet_code_quality.CCS0041.additional_value_patterns = ^MYCO_[A-Z0-9]{32}$

# Exclude specific patterns
dotnet_code_quality.CCS0040.excluded_patterns = test_password
```

### Runtime Configuration

```csharp
public class ConfigurableSecretPatternEngine
{
    private readonly SecretPatternEngine _baseEngine;
    private readonly ImmutableArray<SecretPattern> _additionalPatterns;
    private readonly ImmutableHashSet<string> _excludedPatternIds;

    public ConfigurableSecretPatternEngine(
        AnalyzerConfigOptions options)
    {
        _baseEngine = SecretPatternEngine.Instance;

        // Parse additional patterns from config
        _additionalPatterns = ParseAdditionalPatterns(options);

        // Parse exclusions
        _excludedPatternIds = ParseExclusions(options);
    }
}
```

---

## Performance Considerations

1. **Compiled Regex**: All patterns use `RegexOptions.Compiled` for performance
2. **Lazy Initialization**: Singleton pattern engine is lazily initialized
3. **Immutable Collections**: Thread-safe pattern access
4. **Early Exit**: Length checks before regex matching
5. **Category Filtering**: Can scope search to specific categories

---

## Testing

### Unit Tests

```csharp
public class SecretPatternEngineTests
{
    [Theory]
    [InlineData("password", true)]
    [InlineData("userPassword", true)]
    [InlineData("PASSWORD", true)]
    [InlineData("username", false)]
    public void MatchName_PasswordPatterns(string name, bool shouldMatch)
    {
        var engine = SecretPatternEngine.Instance;
        var result = engine.MatchName(name, SecretCategory.Password);

        Assert.Equal(shouldMatch, result != null);
    }

    [Theory]
    [InlineData("AKIAXXXXXXXXXXXXXXXX", "AWS")]
    [InlineData("sk_live_xxxxxxxxxxxxxxxxxxxxxxxxxxxx", "Stripe")]
    [InlineData("ghp_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx", "GitHub")]
    public void MatchValue_ProviderPatterns(string value, string expectedProvider)
    {
        var engine = SecretPatternEngine.Instance;
        var result = engine.MatchValue(value);

        Assert.NotNull(result);
        Assert.Equal(expectedProvider, result.Provider);
        Assert.True(result.HighConfidence);
    }

    [Theory]
    [InlineData("<YOUR_API_KEY>")]
    [InlineData("your-api-key")]
    [InlineData("placeholder")]
    [InlineData("test_value")]
    public void IsPlaceholder_ReturnsTrue(string value)
    {
        var engine = SecretPatternEngine.Instance;
        Assert.True(engine.IsPlaceholder(value));
    }
}
```

---

## Deliverable Checklist

- [ ] Create `Utilities/SecretCategory.cs`
- [ ] Create `Utilities/SecretPattern.cs`
- [ ] Create `Utilities/SecretPatternEngine.cs`
- [ ] Write unit tests for pattern matching
- [ ] Add provider-specific pattern tests
- [ ] Add configuration support
- [ ] Document pattern extension process
- [ ] Performance test with large codebases
