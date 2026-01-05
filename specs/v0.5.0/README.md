# v0.5.0 "Best Practices" - Specification Overview

## Overview

| Property | Value |
|----------|-------|
| Version | v0.5.0 |
| Theme | "Best Practices" |
| Target Framework | netstandard2.0 (analyzers) |
| Total Analyzers | 24 (7 new + 17 from previous versions) |
| Key Features | XML output, Rule documentation generator, Library mode detection |

## Goals

1. **Async Best Practices**: Enforce modern async/await patterns
2. **LINQ Optimization**: Encourage idiomatic LINQ usage over manual loops
3. **Null Safety**: Promote null-conditional and null-coalescing operators
4. **Documentation**: Generate rule documentation automatically
5. **XML Output**: Additional CI/CD integration format

---

## New Analyzers Summary

| ID | Name | Category | Severity | Fix | Description |
|----|------|----------|----------|-----|-------------|
| CCS0030 | AsyncMethodNaming | BestPractices | Warning | Yes | Async methods should end with 'Async' |
| CCS0031 | AvoidAsyncVoid | BestPractices | Error | No | Avoid async void except event handlers |
| CCS0032 | ConfigureAwaitFalse | BestPractices | Info | Yes | Use ConfigureAwait(false) in libraries |
| CCS0033 | PreferLinqMethod | BestPractices | Info | Yes | Prefer LINQ over manual loops |
| CCS0034 | SimplifyLinq | BestPractices | Info | Yes | Simplify LINQ (Count() > 0 to Any()) |
| CCS0035 | UseNullConditional | BestPractices | Info | Yes | Use ?. operator |
| CCS0036 | UseNullCoalescing | BestPractices | Info | Yes | Use ?? operator |

---

## Infrastructure Components

### 1. XML Output Formatter

XML output format for CI/CD tools that don't support SARIF or JSON.

### 2. Rule Documentation Generator

Generates Markdown documentation for all rules automatically from analyzer metadata.

### 3. Library vs Application Mode Detection

Detects whether the analyzed project is a library or application to adjust recommendations (e.g., ConfigureAwait).

---

## Project Structure

```
CodeCop.Sharp/
├── Analyzers/
│   ├── Naming/              # CCS0001-CCS0005
│   ├── Style/               # CCS0010-CCS0014
│   ├── Quality/             # CCS0020-CCS0026
│   └── BestPractices/       # NEW
│       ├── AsyncMethodNamingAnalyzer.cs
│       ├── AvoidAsyncVoidAnalyzer.cs
│       ├── ConfigureAwaitFalseAnalyzer.cs
│       ├── PreferLinqMethodAnalyzer.cs
│       ├── SimplifyLinqAnalyzer.cs
│       ├── UseNullConditionalAnalyzer.cs
│       └── UseNullCoalescingAnalyzer.cs
├── CodeFixes/
│   └── BestPractices/       # NEW
│       ├── AsyncMethodNamingCodeFixProvider.cs
│       ├── ConfigureAwaitFalseCodeFixProvider.cs
│       ├── PreferLinqMethodCodeFixProvider.cs
│       ├── SimplifyLinqCodeFixProvider.cs
│       ├── UseNullConditionalCodeFixProvider.cs
│       └── UseNullCoalescingCodeFixProvider.cs
└── Infrastructure/
    └── ProjectTypeDetector.cs  # NEW

CodeCop.CLI/
├── Formatters/
│   └── XmlFormatter.cs      # NEW
└── Documentation/
    └── RuleDocumentationGenerator.cs  # NEW
```

---

## Specification Documents

| Document | Description |
|----------|-------------|
| [CCS0030_AsyncMethodNaming.md](CCS0030_AsyncMethodNaming.md) | Async naming convention |
| [CCS0031_AvoidAsyncVoid.md](CCS0031_AvoidAsyncVoid.md) | Async void detection |
| [CCS0032_ConfigureAwaitFalse.md](CCS0032_ConfigureAwaitFalse.md) | ConfigureAwait for libraries |
| [CCS0033_PreferLinqMethod.md](CCS0033_PreferLinqMethod.md) | LINQ over loops |
| [CCS0034_SimplifyLinq.md](CCS0034_SimplifyLinq.md) | LINQ simplification |
| [CCS0035_UseNullConditional.md](CCS0035_UseNullConditional.md) | Null-conditional operator |
| [CCS0036_UseNullCoalescing.md](CCS0036_UseNullCoalescing.md) | Null-coalescing operator |
| [XML_Output.md](XML_Output.md) | XML formatter specification |
| [RuleDocumentationGenerator.md](RuleDocumentationGenerator.md) | Doc generator specification |
| [LibraryModeDetection.md](LibraryModeDetection.md) | Project type detection |

---

## Implementation Order

1. **Infrastructure First**:
   - `ProjectTypeDetector.cs` (needed by CCS0032)
   - `XmlFormatter.cs`

2. **Async Analyzers** (CCS0030-CCS0032):
   - AsyncMethodNamingAnalyzer + CodeFix
   - AvoidAsyncVoidAnalyzer (no code fix)
   - ConfigureAwaitFalseAnalyzer + CodeFix

3. **LINQ Analyzers** (CCS0033-CCS0034):
   - PreferLinqMethodAnalyzer + CodeFix
   - SimplifyLinqAnalyzer + CodeFix

4. **Null Analyzers** (CCS0035-CCS0036):
   - UseNullConditionalAnalyzer + CodeFix
   - UseNullCoalescingAnalyzer + CodeFix

5. **Documentation**:
   - RuleDocumentationGenerator
   - CLI `docs` command

---

## Deliverable Checklist

### Analyzers

- [ ] Create `Analyzers/BestPractices/` directory
- [ ] Implement `AsyncMethodNamingAnalyzer.cs` (CCS0030)
- [ ] Implement `AvoidAsyncVoidAnalyzer.cs` (CCS0031)
- [ ] Implement `ConfigureAwaitFalseAnalyzer.cs` (CCS0032)
- [ ] Implement `PreferLinqMethodAnalyzer.cs` (CCS0033)
- [ ] Implement `SimplifyLinqAnalyzer.cs` (CCS0034)
- [ ] Implement `UseNullConditionalAnalyzer.cs` (CCS0035)
- [ ] Implement `UseNullCoalescingAnalyzer.cs` (CCS0036)

### Code Fixes

- [ ] Create `CodeFixes/BestPractices/` directory
- [ ] Implement `AsyncMethodNamingCodeFixProvider.cs`
- [ ] Implement `ConfigureAwaitFalseCodeFixProvider.cs`
- [ ] Implement `PreferLinqMethodCodeFixProvider.cs`
- [ ] Implement `SimplifyLinqCodeFixProvider.cs`
- [ ] Implement `UseNullConditionalCodeFixProvider.cs`
- [ ] Implement `UseNullCoalescingCodeFixProvider.cs`

### Infrastructure

- [ ] Implement `XmlFormatter.cs`
- [ ] Implement `RuleDocumentationGenerator.cs`
- [ ] Implement `ProjectTypeDetector.cs`
- [ ] Add XML format option to CLI
- [ ] Add `docs` command to CLI

### Tests

- [ ] Write tests for CCS0030 (~15 tests)
- [ ] Write tests for CCS0031 (~12 tests)
- [ ] Write tests for CCS0032 (~10 tests)
- [ ] Write tests for CCS0033 (~15 tests)
- [ ] Write tests for CCS0034 (~15 tests)
- [ ] Write tests for CCS0035 (~12 tests)
- [ ] Write tests for CCS0036 (~12 tests)
- [ ] Write tests for XmlFormatter (~5 tests)
- [ ] Write tests for RuleDocumentationGenerator (~3 tests)
- [ ] Write tests for ProjectTypeDetector (~5 tests)

### Verification

- [ ] All existing tests pass (~296 tests from v0.4.0)
- [ ] All new analyzer tests pass (~95 new tests)
- [ ] All infrastructure tests pass (~15 tests)
- [ ] XML output is valid
- [ ] Generated documentation is correct
- [ ] Library mode detection works
- [ ] `dotnet build` succeeds

---

## Critical Implementation Notes

### 1. Async Pattern Detection

CCS0030 and CCS0031 require careful handling of:
- Task vs Task<T> vs ValueTask vs ValueTask<T>
- IAsyncEnumerable<T>
- Custom awaitables
- Event handler signatures (object, EventArgs)
- Override and interface implementation constraints

### 2. LINQ Pattern Matching

CCS0033 and CCS0034 involve complex syntax tree analysis:
- Matching loop patterns (foreach with specific body structures)
- Identifying LINQ method chains
- Preserving semantics when suggesting transformations
- Handling query syntax vs method syntax

### 3. Null Operator Suggestions

CCS0035 and CCS0036 require:
- Tracking identifier usage across conditionals
- Ensuring the simplified form is semantically equivalent
- Handling nullable reference types correctly
- Not suggesting changes that would change null behavior

### 4. Project Type Detection

For CCS0032 (ConfigureAwait), accurate detection of library vs application is crucial:
- Check assembly references for framework indicators
- Allow configuration override
- Default to library mode when uncertain (safer)

### 5. Code Fix Complexity

Several code fixes involve complex transformations:
- AsyncMethodNamingCodeFixProvider: Uses Renamer.RenameSymbolAsync
- ConfigureAwaitFalseCodeFixProvider: Wraps await expression
- PreferLinqMethodCodeFixProvider: Replaces entire loop with LINQ
- SimplifyLinqCodeFixProvider: Restructures method chains

### 6. Testing Strategy

Use the established testing patterns:
- `AnalyzerVerifier<T>` for analyzer tests
- `CodeFixVerifier<TAnalyzer, TCodeFix>` for code fix tests
- Test both positive and negative cases
- Cover all edge cases (generics, async, lambdas, etc.)
