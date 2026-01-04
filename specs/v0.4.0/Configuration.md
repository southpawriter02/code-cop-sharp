# Configuration Guide

## Overview

CodeCop.Sharp v0.4.0 introduces configurable thresholds for quality analyzers. Configuration is done via `.editorconfig` files, following the standard Roslyn analyzer configuration pattern.

---

## Configuration Methods

### 1. .editorconfig (Recommended)

The primary and recommended way to configure CodeCop analyzers.

```ini
# .editorconfig
root = true

[*.cs]
# === Severity Configuration ===

# Disable a rule
dotnet_diagnostic.CCS0020.severity = none

# Change severity
dotnet_diagnostic.CCS0023.severity = error

# Available severities: error, warning, suggestion, silent, none

# === Threshold Configuration ===

# CCS0023: Method line threshold (default: 50)
dotnet_diagnostic.CCS0023.max_lines = 50

# CCS0024: Class line threshold (default: 500)
dotnet_diagnostic.CCS0024.max_lines = 500

# CCS0025: Cyclomatic complexity threshold (default: 10)
dotnet_diagnostic.CCS0025.max_complexity = 10

# CCS0026: Parameter count threshold (default: 5)
dotnet_diagnostic.CCS0026.max_parameters = 5
```

### 2. Project File (.csproj)

Severity can also be configured in the project file.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="CodeCop.Sharp" Version="0.4.0" />
  </ItemGroup>

  <!-- Rule severity configuration -->
  <PropertyGroup>
    <WarningsAsErrors>CCS0022</WarningsAsErrors>
    <NoWarn>CCS0011</NoWarn>
  </PropertyGroup>
</Project>
```

### 3. Global Suppressions

Use `[SuppressMessage]` for targeted suppressions.

```csharp
using System.Diagnostics.CodeAnalysis;

public class MyClass
{
    // Suppress on specific member
    [SuppressMessage("CodeCop", "CCS0020", Justification = "Used via reflection")]
    private int _reflectionField;

    // Suppress on class
    [SuppressMessage("CodeCop", "CCS0024", Justification = "Legacy class")]
    public class LegacyClass
    {
        // 500+ lines
    }
}

// GlobalSuppressions.cs
[assembly: SuppressMessage("CodeCop", "CCS0023",
    Scope = "member",
    Target = "~M:MyNamespace.MyClass.LongMethod",
    Justification = "Complex algorithm")]
```

---

## Configurable Rules

### CCS0023: MethodTooLong

| Property | Value |
|----------|-------|
| Configuration Key | `dotnet_diagnostic.CCS0023.max_lines` |
| Default Value | 50 |
| Valid Range | 1 - 10000 |
| Description | Maximum number of lines allowed in a method |

```ini
[*.cs]
# Default: 50 lines
dotnet_diagnostic.CCS0023.max_lines = 50

# More permissive for legacy code
dotnet_diagnostic.CCS0023.max_lines = 100

# Stricter for new code
dotnet_diagnostic.CCS0023.max_lines = 30
```

---

### CCS0024: ClassTooLong

| Property | Value |
|----------|-------|
| Configuration Key | `dotnet_diagnostic.CCS0024.max_lines` |
| Default Value | 500 |
| Valid Range | 1 - 50000 |
| Description | Maximum number of lines allowed in a class |

```ini
[*.cs]
# Default: 500 lines
dotnet_diagnostic.CCS0024.max_lines = 500

# More permissive
dotnet_diagnostic.CCS0024.max_lines = 1000

# Stricter
dotnet_diagnostic.CCS0024.max_lines = 300
```

---

### CCS0025: CyclomaticComplexity

| Property | Value |
|----------|-------|
| Configuration Key | `dotnet_diagnostic.CCS0025.max_complexity` |
| Default Value | 10 |
| Valid Range | 1 - 100 |
| Description | Maximum cyclomatic complexity allowed in a method |

```ini
[*.cs]
# Default: complexity of 10
dotnet_diagnostic.CCS0025.max_complexity = 10

# More permissive
dotnet_diagnostic.CCS0025.max_complexity = 15

# Stricter (recommended for new code)
dotnet_diagnostic.CCS0025.max_complexity = 7
```

**Complexity Reference:**
- 1-5: Simple, low risk
- 6-10: Moderate complexity
- 11-20: High complexity, consider refactoring
- 21+: Very high risk, refactor immediately

---

### CCS0026: TooManyParameters

| Property | Value |
|----------|-------|
| Configuration Key | `dotnet_diagnostic.CCS0026.max_parameters` |
| Default Value | 5 |
| Valid Range | 1 - 50 |
| Description | Maximum number of parameters allowed in a method |

```ini
[*.cs]
# Default: 5 parameters
dotnet_diagnostic.CCS0026.max_parameters = 5

# More permissive
dotnet_diagnostic.CCS0026.max_parameters = 7

# Stricter
dotnet_diagnostic.CCS0026.max_parameters = 4
```

---

## Severity Levels

| Level | Build Effect | Typical Use |
|-------|--------------|-------------|
| `error` | Fails build | Critical issues |
| `warning` | Shows warning | Important issues |
| `suggestion` | IDE hint | Nice-to-have improvements |
| `silent` | Hidden in IDE | Active but not visible |
| `none` | Disabled | Turn off the rule |

---

## Configuration Inheritance

`.editorconfig` files are hierarchical:

```
/project
├── .editorconfig              # Root settings
├── src/
│   ├── .editorconfig          # Overrides for src
│   └── Legacy/
│       └── .editorconfig      # Overrides for legacy code
└── tests/
    └── .editorconfig          # Overrides for tests
```

### Example: Root Configuration

```ini
# /project/.editorconfig
root = true

[*.cs]
dotnet_diagnostic.CCS0023.max_lines = 50
dotnet_diagnostic.CCS0024.max_lines = 500
dotnet_diagnostic.CCS0025.max_complexity = 10
dotnet_diagnostic.CCS0026.max_parameters = 5
```

### Example: Legacy Code Override

```ini
# /project/src/Legacy/.editorconfig
[*.cs]
# More permissive thresholds for legacy code
dotnet_diagnostic.CCS0023.max_lines = 200
dotnet_diagnostic.CCS0024.max_lines = 2000
dotnet_diagnostic.CCS0025.max_complexity = 25
dotnet_diagnostic.CCS0026.max_parameters = 10

# Disable some rules entirely
dotnet_diagnostic.CCS0020.severity = none
dotnet_diagnostic.CCS0021.severity = none
```

### Example: Test Projects

```ini
# /project/tests/.editorconfig
[*.cs]
# Tests can have longer methods
dotnet_diagnostic.CCS0023.max_lines = 100

# Test classes can be larger
dotnet_diagnostic.CCS0024.max_lines = 1000

# Disable unused parameter check for test fixtures
dotnet_diagnostic.CCS0021.severity = none
```

---

## Implementation Details

### Reading Configuration in Analyzers

Analyzers read configuration using the `AnalyzerConfigOptionsProvider`:

```csharp
private static int GetMaxLines(SyntaxNodeAnalysisContext context)
{
    var options = context.Options.AnalyzerConfigOptionsProvider
        .GetOptions(context.Node.SyntaxTree);

    if (options.TryGetValue("dotnet_diagnostic.CCS0023.max_lines", out var value) &&
        int.TryParse(value, out var maxLines) &&
        maxLines > 0)
    {
        return maxLines;
    }

    return DefaultMaxLines;
}
```

### Shared Configuration Reader

Create a utility class for consistent configuration reading:

```csharp
namespace CodeCop.Sharp.Infrastructure
{
    /// <summary>
    /// Utility for reading analyzer configuration from .editorconfig.
    /// </summary>
    public static class ConfigurationReader
    {
        /// <summary>
        /// Gets an integer threshold from configuration.
        /// </summary>
        public static int GetThreshold(
            SyntaxNodeAnalysisContext context,
            string diagnosticId,
            string key,
            int defaultValue)
        {
            var options = context.Options.AnalyzerConfigOptionsProvider
                .GetOptions(context.Node.SyntaxTree);

            var fullKey = $"dotnet_diagnostic.{diagnosticId}.{key}";

            if (options.TryGetValue(fullKey, out var value) &&
                int.TryParse(value, out var threshold) &&
                threshold > 0)
            {
                return threshold;
            }

            return defaultValue;
        }

        /// <summary>
        /// Gets a boolean setting from configuration.
        /// </summary>
        public static bool GetBoolean(
            SyntaxNodeAnalysisContext context,
            string diagnosticId,
            string key,
            bool defaultValue)
        {
            var options = context.Options.AnalyzerConfigOptionsProvider
                .GetOptions(context.Node.SyntaxTree);

            var fullKey = $"dotnet_diagnostic.{diagnosticId}.{key}";

            if (options.TryGetValue(fullKey, out var value) &&
                bool.TryParse(value, out var result))
            {
                return result;
            }

            return defaultValue;
        }
    }
}
```

---

## Complete .editorconfig Example

```ini
# CodeCop.Sharp v0.4.0 Configuration
# https://github.com/southpawriter02/code-cop-sharp

root = true

[*.cs]
# ===== Naming Rules (CCS0001-CCS0005) =====

# CCS0001: Method names must be PascalCase
dotnet_diagnostic.CCS0001.severity = warning

# CCS0002: Class names must be PascalCase
dotnet_diagnostic.CCS0002.severity = warning

# CCS0003: Interfaces must start with 'I'
dotnet_diagnostic.CCS0003.severity = warning

# CCS0004: Private fields should use camelCase
dotnet_diagnostic.CCS0004.severity = warning

# CCS0005: Constants should use UPPER_CASE or PascalCase
dotnet_diagnostic.CCS0005.severity = suggestion


# ===== Style Rules (CCS0010-CCS0014) =====

# CCS0010: Control statements should use braces
dotnet_diagnostic.CCS0010.severity = warning

# CCS0011: Prefer explicit type over var
dotnet_diagnostic.CCS0011.severity = suggestion

# CCS0012: Avoid multiple statements on one line
dotnet_diagnostic.CCS0012.severity = warning

# CCS0013: Remove trailing whitespace
dotnet_diagnostic.CCS0013.severity = suggestion

# CCS0014: Use consistent line endings
dotnet_diagnostic.CCS0014.severity = suggestion


# ===== Quality Rules (CCS0020-CCS0026) =====

# CCS0020: Remove unused private fields
dotnet_diagnostic.CCS0020.severity = warning

# CCS0021: Remove unused parameters
dotnet_diagnostic.CCS0021.severity = warning

# CCS0022: Empty catch blocks should have handling or comment
dotnet_diagnostic.CCS0022.severity = warning

# CCS0023: Method line threshold
dotnet_diagnostic.CCS0023.severity = warning
dotnet_diagnostic.CCS0023.max_lines = 50

# CCS0024: Class line threshold
dotnet_diagnostic.CCS0024.severity = warning
dotnet_diagnostic.CCS0024.max_lines = 500

# CCS0025: Cyclomatic complexity threshold
dotnet_diagnostic.CCS0025.severity = warning
dotnet_diagnostic.CCS0025.max_complexity = 10

# CCS0026: Parameter count threshold
dotnet_diagnostic.CCS0026.severity = warning
dotnet_diagnostic.CCS0026.max_parameters = 5


# ===== Generated Code =====

[*.g.cs]
# Disable all CodeCop rules for generated code
dotnet_analyzer_diagnostic.category-CodeCop.severity = none

[*.Designer.cs]
dotnet_analyzer_diagnostic.category-CodeCop.severity = none


# ===== Test Projects =====

[*Tests/**/*.cs]
# More permissive settings for test code
dotnet_diagnostic.CCS0023.max_lines = 100
dotnet_diagnostic.CCS0024.max_lines = 1000
dotnet_diagnostic.CCS0021.severity = none
```

---

## Troubleshooting

### Configuration Not Applied

1. **Check file location**: `.editorconfig` must be in the project directory or parent
2. **Check syntax**: Use lowercase keys, proper indentation
3. **Restart IDE**: Visual Studio may cache configuration
4. **Run `dotnet build`**: Force reanalysis

### Verify Configuration

Add a test to verify configuration is read:

```csharp
[Fact]
public async Task CustomThreshold_IsRespected()
{
    var testCode = @"
public class MyClass
{
    public void Method() { /* 60 lines */ }
}";

    var editorconfig = @"
[*.cs]
dotnet_diagnostic.CCS0023.max_lines = 100
";

    // With threshold of 100, 60 lines should NOT trigger
    await new AnalyzerTest<MethodTooLongAnalyzer>
    {
        TestCode = testCode,
        AnalyzerConfigDocument = editorconfig
    }.RunAsync();
}
```

---

## Deliverable Checklist

- [ ] Create `Infrastructure/ConfigurationReader.cs`
- [ ] Implement threshold reading in CCS0023-CCS0026
- [ ] Verify configuration inheritance works
- [ ] Test with various .editorconfig setups
- [ ] Document all configuration options
- [ ] Add configuration tests (~5 tests)
- [ ] Create sample .editorconfig templates
