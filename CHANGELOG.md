# Changelog

All notable changes to CodeCop.Sharp will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

#### Naming Convention Analyzers (CCS0001-CCS0005)

Five naming convention analyzers with code fixes, completing the naming rules for v0.2.0:

- **CCS0001 - MethodPascalCase** (Warning)
  - Enforces PascalCase naming for method names
  - Detects methods starting with lowercase letters
  - Code fix renames methods and updates all call sites
  - Skips methods starting with underscore (private convention)
  - Supports async, generic, and extension methods

- **CCS0002 - ClassPascalCase** (Warning)
  - Enforces PascalCase naming for class names
  - Detects classes starting with lowercase letters
  - Code fix renames classes and updates all references (usages, inheritance)
  - Supports nested, generic, abstract, static, partial, and sealed classes

- **CCS0003 - InterfacePrefixI** (Warning)
  - Enforces interface names start with uppercase 'I' followed by uppercase letter
  - Valid: `IService`, `I`, `IMyClass`, `I2Service`
  - Invalid: `Service`, `iService`, `Iservice` (I + lowercase)
  - Code fix adds/fixes 'I' prefix and updates all implementors

- **CCS0004 - PrivateFieldCamelCase** (Warning)
  - Enforces camelCase naming for private and internal fields
  - Applies to fields with no modifier (default private), `private`, `internal`, `private protected`
  - Skips fields starting with underscore (`_fieldName` is valid convention)
  - Skips const fields (handled by CCS0005)
  - Handles multiple variables per declaration (`private int a, B, c;`)

- **CCS0005 - ConstantUpperCase** (Info)
  - Enforces UPPER_CASE or PascalCase for constant fields
  - Both `MAX_SIZE` and `MaxSize` are valid conventions
  - Only applies to fields with `const` modifier
  - Offers two code fix options: PascalCase (primary) and UPPER_CASE (alternative)
  - Uses Info severity (less intrusive) since both conventions are widely used

#### Shared Utilities

- **NamingUtilities** (`CodeCop.Sharp/Utilities/NamingUtilities.cs`)
  - `ToPascalCase(string)` - Converts to PascalCase by capitalizing first character
  - `ToCamelCase(string)` - Converts to camelCase by lowercasing first character
  - `ToUpperSnakeCase(string)` - Converts to UPPER_SNAKE_CASE (e.g., `maxSize` → `MAX_SIZE`)
  - `StartsWithUpperCase(string)` - Checks if string starts with uppercase
  - `StartsWithLowerCase(string)` - Checks if string starts with lowercase

#### Project Structure

- Reorganized codebase with proper directory structure:
  ```
  CodeCop.Sharp/
  ├── Analyzers/Naming/          # All naming analyzers
  ├── CodeFixes/Naming/          # All naming code fix providers
  └── Utilities/                 # Shared utilities

  CodeCop.Sharp.Tests/
  ├── Analyzers/Naming/          # Analyzer tests
  └── Utilities/                 # Utility tests
  ```

#### Documentation

- Created comprehensive v1.0 roadmap (`roadmap/ROADMAP_V1.md`)
- Created detailed v0.2.0 specifications (`specs/v0.2.0/`)
  - Individual spec files for each analyzer (CCS0002-CCS0005)
  - NamingUtilities specification
  - CodeCop.Core specification

### Technical Details

#### Test Coverage

| Analyzer | Test Count | Coverage |
|----------|------------|----------|
| CCS0001 (MethodPascalCase) | 17 | Analyzer + Code Fix |
| CCS0002 (ClassPascalCase) | 22 | Analyzer + Code Fix |
| CCS0003 (InterfacePrefixI) | 34 | Analyzer + Code Fix + Unit Tests |
| CCS0004 (PrivateFieldCamelCase) | 23 | Analyzer + Code Fix |
| CCS0005 (ConstantUpperCase) | 17 | Analyzer + Code Fix |
| NamingUtilities | 63 | Unit Tests |
| **Total** | **176** | |

#### Roslyn APIs Used

- `DiagnosticAnalyzer` - Base class for all analyzers
- `CodeFixProvider` - Base class for all code fix providers
- `SyntaxKind.*Declaration` - Register for specific syntax nodes
- `Renamer.RenameSymbolAsync()` - Rename symbols with reference updates
- `DiagnosticDescriptor` - Define diagnostic metadata
- `AnalysisContext` - Configure analyzer behavior

#### Code Fix Features

All code fixes support:
- Symbol renaming with automatic reference updates
- Fix All in Document/Project/Solution via `WellKnownFixAllProviders.BatchFixer`
- Proper handling of cross-file references

## [0.1.0] - Initial Release

### Added

- Initial CCS0001 (MethodPascalCase) analyzer implementation
- Basic project structure with CodeCop.Sharp and CodeCop.Sharp.Tests

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)
