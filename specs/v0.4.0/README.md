# CodeCop.Sharp v0.4.0 - "Quality Gate"

## Overview

| Property | Value |
|----------|-------|
| Version | v0.4.0 |
| Theme | Quality Gate |
| Target Framework | netstandard2.0 (analyzers), net8.0 (CLI) |
| Total Analyzers | 17 (7 new + 10 from v0.2.0/v0.3.0) |
| Key Features | SARIF output, configurable thresholds, SuppressMessage support |

## Goals

1. **Code Quality Detection**: Identify maintainability issues (unused code, complexity, length)
2. **CI/CD Integration**: SARIF output for GitHub Actions and Azure DevOps
3. **Configurable Rules**: Allow threshold customization via .editorconfig
4. **Suppression Support**: Honor `[SuppressMessage]` attributes

---

## New Analyzers

| ID | Name | Category | Severity | Fix | Description |
|----|------|----------|----------|-----|-------------|
| CCS0020 | UnusedPrivateField | Quality | Warning | Yes | Detect unused private fields |
| CCS0021 | UnusedParameter | Quality | Warning | No | Detect unused method parameters |
| CCS0022 | EmptyCatchBlock | Quality | Warning | Yes | Empty catch blocks must have comment |
| CCS0023 | MethodTooLong | Quality | Warning | No | Methods exceeding line threshold |
| CCS0024 | ClassTooLong | Quality | Warning | No | Classes exceeding line threshold |
| CCS0025 | CyclomaticComplexity | Quality | Warning | No | High complexity methods |
| CCS0026 | TooManyParameters | Quality | Warning | No | Methods with too many parameters |

---

## Analyzer Categories

### Unused Code Detection (CCS0020-CCS0021)
These analyzers use **semantic analysis** to detect unused symbols across the compilation:
- **CCS0020**: Detects private fields that are never read
- **CCS0021**: Detects method parameters that are never used

### Pattern Detection (CCS0022)
- **CCS0022**: Detects empty catch blocks that silently swallow exceptions

### Metrics-Based (CCS0023-CCS0026)
These analyzers use **configurable thresholds** to enforce code quality metrics:
- **CCS0023**: Method line count (default: 50)
- **CCS0024**: Class line count (default: 500)
- **CCS0025**: Cyclomatic complexity (default: 10)
- **CCS0026**: Parameter count (default: 5)

---

## Infrastructure Components

### 1. SARIF Output Formatter

SARIF (Static Analysis Results Interchange Format) is a JSON-based standard for CI/CD integration:

```bash
codecop analyze --target ./MySolution.sln --format Sarif --output results.sarif
```

Supported by:
- GitHub Advanced Security
- GitHub Actions
- Azure DevOps
- Visual Studio
- VS Code SARIF Viewer

### 2. Configurable Thresholds

Rules CCS0023-CCS0026 support threshold customization via `.editorconfig`:

```ini
[*.cs]
# CCS0023: Method line threshold (default: 50)
dotnet_diagnostic.CCS0023.max_lines = 50

# CCS0024: Class line threshold (default: 500)
dotnet_diagnostic.CCS0024.max_lines = 500

# CCS0025: Cyclomatic complexity threshold (default: 10)
dotnet_diagnostic.CCS0025.max_complexity = 10

# CCS0026: Parameter count threshold (default: 5)
dotnet_diagnostic.CCS0026.max_parameters = 5
```

### 3. SuppressMessage Support

All analyzers respect the `[SuppressMessage]` attribute:

```csharp
[System.Diagnostics.CodeAnalysis.SuppressMessage("CodeCop", "CCS0023",
    Justification = "Legacy code, refactoring planned")]
public void LegacyMethod() { /* ... */ }
```

---

## Project Structure

```
CodeCop.Sharp/
├── Analyzers/
│   ├── Naming/              # Existing (CCS0001-CCS0005)
│   ├── Style/               # Existing (CCS0010-CCS0014)
│   └── Quality/             # NEW
│       ├── UnusedPrivateFieldAnalyzer.cs
│       ├── UnusedParameterAnalyzer.cs
│       ├── EmptyCatchBlockAnalyzer.cs
│       ├── MethodTooLongAnalyzer.cs
│       ├── ClassTooLongAnalyzer.cs
│       ├── CyclomaticComplexityAnalyzer.cs
│       └── TooManyParametersAnalyzer.cs
├── CodeFixes/
│   └── Quality/             # NEW
│       ├── UnusedPrivateFieldCodeFixProvider.cs
│       └── EmptyCatchBlockCodeFixProvider.cs
└── Infrastructure/          # NEW
    ├── ConfigurationReader.cs
    └── ThresholdConfiguration.cs

CodeCop.CLI/
└── Formatters/
    └── SarifFormatter.cs    # NEW
```

---

## Specification Documents

| Document | Description |
|----------|-------------|
| [CCS0020_UnusedPrivateField.md](CCS0020_UnusedPrivateField.md) | Unused private field detection |
| [CCS0021_UnusedParameter.md](CCS0021_UnusedParameter.md) | Unused parameter detection |
| [CCS0022_EmptyCatchBlock.md](CCS0022_EmptyCatchBlock.md) | Empty catch block detection |
| [CCS0023_MethodTooLong.md](CCS0023_MethodTooLong.md) | Method length analyzer |
| [CCS0024_ClassTooLong.md](CCS0024_ClassTooLong.md) | Class length analyzer |
| [CCS0025_CyclomaticComplexity.md](CCS0025_CyclomaticComplexity.md) | Complexity analyzer |
| [CCS0026_TooManyParameters.md](CCS0026_TooManyParameters.md) | Parameter count analyzer |
| [SARIF_Output.md](SARIF_Output.md) | SARIF output formatter |
| [Configuration.md](Configuration.md) | Threshold configuration |

---

## Implementation Order

1. **Phase 1: Semantic Analyzers**
   - CCS0020 (UnusedPrivateField) - most complex, requires compilation-wide analysis
   - CCS0021 (UnusedParameter) - similar pattern

2. **Phase 2: Pattern Analyzer**
   - CCS0022 (EmptyCatchBlock) - simpler syntax-based detection

3. **Phase 3: Metrics Analyzers**
   - CCS0023-CCS0026 - similar patterns, configurable thresholds

4. **Phase 4: Infrastructure**
   - Configuration reader utility
   - SARIF output formatter
   - CLI integration

---

## Key Implementation Patterns

### Semantic Analysis Pattern (CCS0020/CCS0021)

```csharp
context.RegisterCompilationStartAction(compilationContext =>
{
    // Collection phase
    var symbols = new Dictionary<ISymbol, Location>();
    var usedSymbols = new HashSet<ISymbol>();

    compilationContext.RegisterSymbolAction(symbolContext =>
    {
        // Collect declarations
    }, SymbolKind.Field);

    compilationContext.RegisterSyntaxNodeAction(nodeContext =>
    {
        // Track usage
    }, SyntaxKind.IdentifierName);

    compilationContext.RegisterCompilationEndAction(endContext =>
    {
        // Report unused symbols
    });
});
```

### Configuration Reading Pattern

```csharp
private static int GetThreshold(SyntaxNodeAnalysisContext context, int defaultValue)
{
    var options = context.Options.AnalyzerConfigOptionsProvider
        .GetOptions(context.Node.SyntaxTree);

    if (options.TryGetValue("dotnet_diagnostic.CCS0023.max_lines", out var value) &&
        int.TryParse(value, out var threshold))
    {
        return threshold;
    }

    return defaultValue;
}
```

---

## Test Strategy

### Unit Test Structure

```
CodeCop.Sharp.Tests/
├── Analyzers/
│   └── Quality/
│       ├── UnusedPrivateFieldAnalyzerTests.cs
│       ├── UnusedParameterAnalyzerTests.cs
│       ├── EmptyCatchBlockAnalyzerTests.cs
│       ├── MethodTooLongAnalyzerTests.cs
│       ├── ClassTooLongAnalyzerTests.cs
│       ├── CyclomaticComplexityAnalyzerTests.cs
│       └── TooManyParametersAnalyzerTests.cs
└── CLI/
    └── Formatters/
        └── SarifFormatterTests.cs
```

### Test Categories

1. **Should Trigger**: Cases where diagnostic is expected
2. **Should NOT Trigger**: Valid code that passes analysis
3. **Edge Cases**: Unusual scenarios (partial classes, lambdas, LINQ, etc.)
4. **Configuration**: Custom threshold tests
5. **Code Fixes**: Fix application tests

---

## Deliverable Checklist

### Analyzers
- [ ] Create `Analyzers/Quality/` directory
- [ ] Implement CCS0020 (UnusedPrivateField)
- [ ] Implement CCS0021 (UnusedParameter)
- [ ] Implement CCS0022 (EmptyCatchBlock)
- [ ] Implement CCS0023 (MethodTooLong)
- [ ] Implement CCS0024 (ClassTooLong)
- [ ] Implement CCS0025 (CyclomaticComplexity)
- [ ] Implement CCS0026 (TooManyParameters)

### Code Fixes
- [ ] Create `CodeFixes/Quality/` directory
- [ ] Implement UnusedPrivateFieldCodeFixProvider
- [ ] Implement EmptyCatchBlockCodeFixProvider

### Infrastructure
- [ ] Create ConfigurationReader utility
- [ ] Implement SarifFormatter
- [ ] Add SARIF format option to CLI
- [ ] Create SARIF model classes

### Tests
- [ ] Write tests for CCS0020 (~15 tests)
- [ ] Write tests for CCS0021 (~12 tests)
- [ ] Write tests for CCS0022 (~10 tests)
- [ ] Write tests for CCS0023 (~8 tests)
- [ ] Write tests for CCS0024 (~6 tests)
- [ ] Write tests for CCS0025 (~10 tests)
- [ ] Write tests for CCS0026 (~8 tests)
- [ ] Write tests for SarifFormatter (~5 tests)

### Documentation
- [ ] Create individual spec files
- [ ] Update CHANGELOG.md
- [ ] Add configuration examples

### Verification
- [ ] All existing tests pass (~226 tests)
- [ ] All new tests pass (~70 tests)
- [ ] SARIF output validates correctly
- [ ] Threshold configuration works
- [ ] `dotnet build` succeeds

---

## Dependencies

No new NuGet packages required for analyzers. For SARIF output:
- Use `System.Text.Json` (included in .NET)
- Alternatively: `Microsoft.CodeAnalysis.Sarif` package

---

## Exit Criteria

v0.4.0 is complete when:
1. All 7 new analyzers are implemented and tested
2. 2 code fix providers work correctly
3. SARIF output format is supported
4. Threshold configuration via .editorconfig works
5. All ~296 tests pass (226 existing + 70 new)
6. Documentation is complete
