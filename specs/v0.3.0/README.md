# v0.3.0 - "Command Line"

## Overview

| Property | Value |
|----------|-------|
| Version | v0.3.0 |
| Theme | Command Line |
| Target Framework | net8.0 |
| Total Analyzers | 10 (5 new + 5 from v0.2.0) |

## Goals

1. Create `CodeCop.CLI` - a command-line interface for standalone analysis
2. Implement 5 new code style analyzers (CCS0010-CCS0014)
3. Enable CI/CD integration with exit codes and JSON output

---

## Projects to Create

| Project | Type | Framework | Description |
|---------|------|-----------|-------------|
| `CodeCop.CLI` | Console App | net8.0 | Command-line interface for standalone analysis |

---

## New Analyzers

| ID | Name | Category | Severity | Fix | Description |
|----|------|----------|----------|-----|-------------|
| CCS0010 | BracesRequired | Style | Warning | Yes | Enforce braces for if/else/for/foreach/while/using/lock |
| CCS0011 | PreferVarExplicit | Style | Info | Yes | Enforce explicit type vs var |
| CCS0012 | SingleLineStatements | Style | Warning | No | Avoid multiple statements on one line |
| CCS0013 | TrailingWhitespace | Style | Info | Yes | No trailing whitespace |
| CCS0014 | ConsistentNewlines | Style | Info | Yes | Consistent CRLF/LF style |

---

## Specification Documents

| Document | Description |
|----------|-------------|
| [CodeCop_CLI.md](CodeCop_CLI.md) | CLI application specification |
| [CCS0010_BracesRequired.md](CCS0010_BracesRequired.md) | Braces enforcement analyzer |
| [CCS0011_PreferVarExplicit.md](CCS0011_PreferVarExplicit.md) | Explicit type analyzer |
| [CCS0012_SingleLineStatements.md](CCS0012_SingleLineStatements.md) | Single-line statements analyzer |
| [CCS0013_TrailingWhitespace.md](CCS0013_TrailingWhitespace.md) | Trailing whitespace analyzer |
| [CCS0014_ConsistentNewlines.md](CCS0014_ConsistentNewlines.md) | Consistent newlines analyzer |

---

## Implementation Order

### Phase 1: CLI Infrastructure
1. Create `CodeCop.CLI` project
2. Implement `Program.cs` entry point
3. Implement commands (analyze, rules, version)
4. Implement formatters (Console, JSON)
5. Add exit code handling
6. Write CLI integration tests

### Phase 2: Style Analyzers
1. **CCS0010** - BracesRequired (most complex)
2. **CCS0011** - PreferVarExplicit
3. **CCS0012** - SingleLineStatements
4. **CCS0013** - TrailingWhitespace
5. **CCS0014** - ConsistentNewlines

### Phase 3: Documentation & Testing
1. Update README with CLI usage
2. Verify all tests pass
3. Test CLI tool installation
4. Test CI/CD integration

---

## Dependencies

### CodeCop.CLI
- `System.CommandLine` (2.0.0-beta4.22272.1) - Command-line parsing
- `CodeCop.Core` - Analysis runner

---

## Exit Codes

| Code | Name | Description |
|------|------|-------------|
| 0 | Success | Analysis completed, no issues found |
| 1 | ErrorsFound | Analysis completed, errors found |
| 2 | WarningsFound | Analysis completed, warnings found (no errors) |
| 3 | AnalysisFailed | Analysis failed (invalid target, exception) |

---

## CLI Commands

```bash
# Analyze a solution
codecop analyze --target ./MySolution.sln

# Output to JSON
codecop analyze --target ./MySolution.sln --format Json --output report.json

# CI/CD mode (fail on errors)
codecop analyze --target ./MySolution.sln --fail-on-error

# List all rules
codecop rules

# Show version
codecop version
```

---

## Directory Structure After v0.3.0

```
CodeCop.Sharp.sln
├── CodeCop.Sharp/
│   ├── Analyzers/
│   │   ├── Naming/           # v0.2.0 analyzers
│   │   └── Style/            # NEW - v0.3.0 analyzers
│   │       ├── BracesRequiredAnalyzer.cs
│   │       ├── PreferVarExplicitAnalyzer.cs
│   │       ├── SingleLineStatementsAnalyzer.cs
│   │       ├── TrailingWhitespaceAnalyzer.cs
│   │       └── ConsistentNewlinesAnalyzer.cs
│   └── CodeFixes/
│       ├── Naming/           # v0.2.0 code fixes
│       └── Style/            # NEW - v0.3.0 code fixes
├── CodeCop.Sharp.Tests/
│   ├── Analyzers/
│   │   ├── Naming/           # v0.2.0 tests
│   │   └── Style/            # NEW - v0.3.0 tests
│   └── CLI/                  # NEW - CLI tests
├── CodeCop.Core/             # v0.2.0 - Analysis runner
└── CodeCop.CLI/              # NEW - Command-line interface
    ├── Commands/
    ├── Formatters/
    ├── Models/
    └── Services/
```

---

## Verification Checklist

- [ ] All existing tests pass (176 tests)
- [ ] All new analyzer tests pass (~50 new tests)
- [ ] All CLI tests pass (~20 tests)
- [ ] `dotnet build` succeeds for all projects
- [ ] CLI tool installs correctly via `dotnet tool install`
- [ ] JSON output is valid and parseable
- [ ] Exit codes work correctly
- [ ] Console output displays with colors
