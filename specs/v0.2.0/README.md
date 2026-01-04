# v0.2.0 "Foundation" - Specification Overview

## Release Summary

| Property | Value |
|----------|-------|
| Version | 0.2.0 |
| Codename | Foundation |
| Theme | Core infrastructure and naming convention analyzers |
| Total Analyzers | 5 (4 new + 1 existing) |

---

## Objectives

1. **Establish multi-project architecture** - Create CodeCop.Core as the shared analysis runner
2. **Extract shared utilities** - Move common code to reusable locations
3. **Implement naming analyzers** - Complete the naming conventions category
4. **Prepare for CLI** - Build the foundation that v0.3.0's CLI will consume

---

## Deliverables

### New Projects

| Project | Framework | Purpose |
|---------|-----------|---------|
| CodeCop.Core | netstandard2.0 | Analysis runner library |
| CodeCop.Core.Tests | net8.0 | Unit/integration tests for Core |

### New Analyzers

| Rule ID | Name | Description | Spec |
|---------|------|-------------|------|
| CCS0002 | ClassPascalCase | Class names must be PascalCase | [Spec](CCS0002_ClassPascalCase.md) |
| CCS0003 | InterfacePrefixI | Interfaces must start with 'I' | [Spec](CCS0003_InterfacePrefixI.md) |
| CCS0004 | PrivateFieldCamelCase | Private fields use camelCase | [Spec](CCS0004_PrivateFieldCamelCase.md) |
| CCS0005 | ConstantUpperCase | Constants use UPPER_CASE or PascalCase | [Spec](CCS0005_ConstantUpperCase.md) |

### Shared Infrastructure

| Component | Description | Spec |
|-----------|-------------|------|
| CodeCop.Core | Analysis runner library | [Spec](CodeCop_Core.md) |
| NamingUtilities | Shared naming conversion methods | [Spec](NamingUtilities.md) |

---

## Project Structure After v0.2.0

```
CodeCop.Sharp.sln
├── CodeCop.Sharp/                      # Analyzer library
│   ├── CodeCop.Sharp.csproj
│   ├── Analyzers/
│   │   └── Naming/
│   │       ├── MethodDeclarationAnalyzer.cs      (existing, refactored)
│   │       ├── ClassPascalCaseAnalyzer.cs        (NEW)
│   │       ├── InterfacePrefixIAnalyzer.cs       (NEW)
│   │       ├── PrivateFieldCamelCaseAnalyzer.cs  (NEW)
│   │       └── ConstantUpperCaseAnalyzer.cs      (NEW)
│   ├── CodeFixes/
│   │   └── Naming/
│   │       ├── MethodDeclarationCodeFixProvider.cs
│   │       ├── ClassPascalCaseCodeFixProvider.cs  (NEW)
│   │       ├── InterfacePrefixICodeFixProvider.cs (NEW)
│   │       ├── PrivateFieldCamelCaseCodeFixProvider.cs (NEW)
│   │       └── ConstantUpperCaseCodeFixProvider.cs (NEW)
│   └── Utilities/
│       └── NamingUtilities.cs                     (NEW)
│
├── CodeCop.Sharp.Tests/                # Analyzer tests
│   ├── CodeCop.Sharp.Tests.csproj
│   ├── Analyzers/
│   │   └── Naming/
│   │       ├── MethodDeclarationAnalyzerTests.cs  (existing)
│   │       ├── ClassPascalCaseAnalyzerTests.cs    (NEW)
│   │       ├── InterfacePrefixIAnalyzerTests.cs   (NEW)
│   │       ├── PrivateFieldCamelCaseAnalyzerTests.cs (NEW)
│   │       └── ConstantUpperCaseAnalyzerTests.cs  (NEW)
│   └── Utilities/
│       └── NamingUtilitiesTests.cs                (NEW)
│
├── CodeCop.Core/                       # Analysis runner (NEW)
│   ├── CodeCop.Core.csproj
│   ├── ICodeCopRunner.cs
│   ├── CodeCopRunner.cs
│   ├── Workspace/
│   │   └── WorkspaceLoader.cs
│   ├── Analysis/
│   │   ├── AnalyzerRunner.cs
│   │   └── AnalyzerRegistry.cs
│   ├── Models/
│   │   ├── AnalysisReport.cs
│   │   ├── AnalyzedProject.cs
│   │   ├── CopDiagnostic.cs
│   │   ├── SummaryStats.cs
│   │   └── AnalysisOptions.cs
│   └── Progress/
│       ├── IAnalysisProgress.cs
│       └── AnalysisPhase.cs
│
└── CodeCop.Core.Tests/                 # Core tests (NEW)
    ├── CodeCop.Core.Tests.csproj
    └── CodeCopRunnerTests.cs
```

---

## Implementation Order

The following order minimizes dependencies and allows incremental progress:

### Phase 1: Shared Utilities (Day 1)

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. Create NamingUtilities.cs                                     │
│    - Extract ToPascalCase() from MethodDeclarationAnalyzer       │
│    - Add ToCamelCase()                                           │
│    - Add ToUpperSnakeCase()                                      │
│    - Write unit tests                                            │
└─────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│ 2. Refactor MethodDeclarationAnalyzer                           │
│    - Move to Analyzers/Naming/ directory                         │
│    - Use NamingUtilities.ToPascalCase()                          │
│    - Verify existing tests still pass                            │
└─────────────────────────────────────────────────────────────────┘
```

### Phase 2: New Analyzers (Days 2-3)

```
┌───────────────────────────────────────────────────────────────────────────┐
│ Implement in parallel (no dependencies between analyzers):                │
│                                                                           │
│ ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐ ┌───────────┐│
│ │ CCS0002         │ │ CCS0003         │ │ CCS0004         │ │ CCS0005   ││
│ │ ClassPascalCase │ │ InterfacePrefixI│ │ PrivateField    │ │ Constant  ││
│ └────────┬────────┘ └────────┬────────┘ └────────┬────────┘ └─────┬─────┘│
│          │                   │                   │                │      │
│          ▼                   ▼                   ▼                ▼      │
│ ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐ ┌───────────┐│
│ │ Analyzer        │ │ Analyzer        │ │ Analyzer        │ │ Analyzer  ││
│ │ CodeFix         │ │ CodeFix         │ │ CodeFix         │ │ CodeFix   ││
│ │ Tests           │ │ Tests           │ │ Tests           │ │ Tests     ││
│ └─────────────────┘ └─────────────────┘ └─────────────────┘ └───────────┘│
└───────────────────────────────────────────────────────────────────────────┘
```

### Phase 3: CodeCop.Core (Days 4-5)

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. Create CodeCop.Core project                                   │
│    - Add NuGet dependencies                                      │
│    - Add project reference to CodeCop.Sharp                      │
└─────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│ 2. Implement Models                                              │
│    - AnalysisReport, AnalyzedProject, CopDiagnostic             │
│    - SummaryStats, AnalysisOptions                               │
│    - DiagnosticSeverityLevel, AnalysisPhase                      │
└─────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│ 3. Implement Core Components                                     │
│    - ICodeCopRunner interface                                    │
│    - WorkspaceLoader (MSBuild integration)                       │
│    - AnalyzerRunner (CompilationWithAnalyzers)                   │
│    - AnalyzerRegistry (analyzer discovery)                       │
└─────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│ 4. Implement CodeCopRunner                                       │
│    - AnalyzeProjectAsync()                                       │
│    - AnalyzeSolutionAsync()                                      │
│    - Progress reporting                                          │
└─────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│ 5. Write Tests                                                   │
│    - Unit tests for models                                       │
│    - Unit tests for AnalyzerRegistry                             │
│    - Integration tests for CodeCopRunner                         │
└─────────────────────────────────────────────────────────────────┘
```

---

## Dependency Graph

```
                    ┌─────────────────────┐
                    │  External NuGet     │
                    │  Packages           │
                    └─────────┬───────────┘
                              │
        ┌─────────────────────┼─────────────────────┐
        │                     │                     │
        ▼                     ▼                     ▼
┌───────────────┐   ┌─────────────────┐   ┌───────────────────┐
│ MSBuild.      │   │ Microsoft.      │   │ Microsoft.        │
│ Locator       │   │ CodeAnalysis.   │   │ CodeAnalysis.     │
│               │   │ CSharp          │   │ Workspaces.MSBuild│
└───────┬───────┘   └────────┬────────┘   └─────────┬─────────┘
        │                    │                      │
        │                    ▼                      │
        │           ┌─────────────────┐             │
        │           │ CodeCop.Sharp   │             │
        │           │ (Analyzers)     │             │
        │           └────────┬────────┘             │
        │                    │                      │
        └────────────────────┼──────────────────────┘
                             │
                             ▼
                    ┌─────────────────┐
                    │  CodeCop.Core   │
                    │  (Runner)       │
                    └────────┬────────┘
                             │
                    ┌────────┴────────┐
                    │                 │
                    ▼                 ▼
           ┌─────────────┐   ┌─────────────────┐
           │ (v0.3.0)    │   │ (v0.7.0)        │
           │ CodeCop.CLI │   │ CodeCop.GUI     │
           └─────────────┘   └─────────────────┘
```

---

## Testing Strategy

### Test Coverage Targets

| Component | Unit Tests | Integration Tests | Min Coverage |
|-----------|------------|-------------------|--------------|
| NamingUtilities | Yes | No | 100% |
| CCS0002 Analyzer | Yes | No | 90% |
| CCS0003 Analyzer | Yes | No | 90% |
| CCS0004 Analyzer | Yes | No | 90% |
| CCS0005 Analyzer | Yes | No | 90% |
| CodeCop.Core | Yes | Yes | 80% |

### Test Counts by Component

| Component | Analyzer Tests | CodeFix Tests | Unit Tests | Total |
|-----------|----------------|---------------|------------|-------|
| CCS0002 | 9 | 3 | 0 | 12 |
| CCS0003 | 10 | 4 | 3 | 17 |
| CCS0004 | 13 | 3 | 0 | 16 |
| CCS0005 | 10 | 6 | 3 | 19 |
| NamingUtilities | 0 | 0 | 12 | 12 |
| CodeCop.Core | 0 | 0 | 15 | 15 |
| **Total** | **42** | **16** | **33** | **91** |

---

## Risk Assessment

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| MSBuild loading issues | Medium | High | Test on multiple .NET SDK versions |
| Analyzer conflicts | Low | Medium | Each analyzer checks distinct syntax |
| Performance regression | Low | Medium | Profile large solutions |
| Breaking existing tests | Low | Low | Run tests after each refactor |

---

## Acceptance Criteria

### Analyzers
- [ ] All 5 analyzers compile without warnings
- [ ] All analyzer tests pass
- [ ] All code fix tests pass
- [ ] Manual testing in Visual Studio shows squiggles and fixes

### CodeCop.Core
- [ ] Can analyze a single .csproj file
- [ ] Can analyze a .sln file with multiple projects
- [ ] Reports correct diagnostic counts in SummaryStats
- [ ] Progress callbacks are invoked correctly
- [ ] Handles missing SDK gracefully

### Code Quality
- [ ] No compiler warnings
- [ ] All public APIs have XML documentation
- [ ] Code follows existing project conventions
- [ ] Solution builds and tests pass in CI

---

## Specification Documents

| Document | Description |
|----------|-------------|
| [CCS0002_ClassPascalCase.md](CCS0002_ClassPascalCase.md) | Class naming analyzer |
| [CCS0003_InterfacePrefixI.md](CCS0003_InterfacePrefixI.md) | Interface naming analyzer |
| [CCS0004_PrivateFieldCamelCase.md](CCS0004_PrivateFieldCamelCase.md) | Private field analyzer |
| [CCS0005_ConstantUpperCase.md](CCS0005_ConstantUpperCase.md) | Constant naming analyzer |
| [CodeCop_Core.md](CodeCop_Core.md) | Analysis runner library |
| [NamingUtilities.md](NamingUtilities.md) | Shared naming utilities |

---

## Migration Notes

### Refactoring Existing Code

The existing `MethodDeclarationAnalyzer` will be refactored:

1. Move from root to `Analyzers/Naming/` directory
2. Extract `ToPascalCase()` to `NamingUtilities`
3. Update to use shared utility
4. Update test file location to match

### Solution File Updates

Add new projects to `CodeCop.Sharp.sln`:

```
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "CodeCop.Core", "CodeCop.Core\CodeCop.Core.csproj", "{NEW-GUID}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "CodeCop.Core.Tests", "CodeCop.Core.Tests\CodeCop.Core.Tests.csproj", "{NEW-GUID}"
EndProject
```

---

## Next Version Preview

v0.3.0 "Command Line" will:
- Create `CodeCop.CLI` project
- Consume `CodeCop.Core` for analysis
- Implement `analyze` and `rules` commands
- Add Console and JSON output formatters
- Add 5 code style analyzers (CCS0010-CCS0014)
