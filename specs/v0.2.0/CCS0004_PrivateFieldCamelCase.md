# CCS0004: Private Fields Should Use camelCase

## Overview

| Property | Value |
|----------|-------|
| Rule ID | CCS0004 |
| Category | Naming |
| Severity | Warning |
| Has Code Fix | Yes |
| Enabled by Default | Yes |

## Description

Private and internal fields in C# should follow camelCase naming convention (first letter lowercase). This rule supports the common convention of prefixing private fields with an underscore (`_fieldName`) as an alternative.

### Compliant Examples

```csharp
public class MyClass
{
    private int count;
    private string userName;
    private readonly ILogger _logger;      // Underscore prefix allowed
    private static int _instanceCount;     // Static with underscore
    internal string internalField;
}
```

### Non-Compliant Examples

```csharp
public class MyClass
{
    private int Count;           // CCS0004: 'Count' → 'count'
    private string UserName;     // CCS0004: 'UserName' → 'userName'
    private int NUMBER;          // CCS0004: 'NUMBER' → 'nUMBER' or 'number'
}
```

---

## Implementation Specification

### File Structure

```
CodeCop.Sharp/
├── Analyzers/
│   └── Naming/
│       └── PrivateFieldCamelCaseAnalyzer.cs
└── CodeFixes/
    └── Naming/
        └── PrivateFieldCamelCaseCodeFixProvider.cs
```

### Analyzer Implementation

```csharp
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class PrivateFieldCamelCaseAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "CCS0004";

    private static readonly LocalizableString Title =
        "Private field name should be in camelCase";
    private static readonly LocalizableString MessageFormat =
        "Private field '{0}' should be in camelCase. Consider: '{1}'";
    private static readonly LocalizableString Description =
        "Private and internal fields should use camelCase naming convention.";
    private const string Category = "Naming";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeFieldDeclaration, SyntaxKind.FieldDeclaration);
    }

    private void AnalyzeFieldDeclaration(SyntaxNodeAnalysisContext context)
    {
        var fieldDeclaration = (FieldDeclarationSyntax)context.Node;

        // Only check private or internal fields (no access modifier = private)
        if (!IsPrivateOrInternal(fieldDeclaration))
        {
            return;
        }

        // Check each variable in the declaration (e.g., private int a, b, c;)
        foreach (var variable in fieldDeclaration.Declaration.Variables)
        {
            var fieldName = variable.Identifier.ValueText;

            if (string.IsNullOrEmpty(fieldName))
            {
                continue;
            }

            // Skip if starts with underscore (common convention)
            if (fieldName[0] == '_')
            {
                continue;
            }

            // Skip if already camelCase (starts with lowercase)
            if (char.IsLower(fieldName[0]))
            {
                continue;
            }

            var suggestedName = NamingUtilities.ToCamelCase(fieldName);
            var diagnostic = Diagnostic.Create(
                Rule,
                variable.Identifier.GetLocation(),
                fieldName,
                suggestedName);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static bool IsPrivateOrInternal(FieldDeclarationSyntax field)
    {
        var modifiers = field.Modifiers;

        // If no access modifier, it's private by default
        if (!modifiers.Any(SyntaxKind.PublicKeyword) &&
            !modifiers.Any(SyntaxKind.ProtectedKeyword) &&
            !modifiers.Any(SyntaxKind.InternalKeyword) &&
            !modifiers.Any(SyntaxKind.PrivateKeyword))
        {
            return true; // Default is private
        }

        // Explicit private or internal (without protected)
        if (modifiers.Any(SyntaxKind.PrivateKeyword))
        {
            return true;
        }

        if (modifiers.Any(SyntaxKind.InternalKeyword) &&
            !modifiers.Any(SyntaxKind.ProtectedKeyword))
        {
            return true;
        }

        return false;
    }
}
```

### NamingUtilities Extension

```csharp
public static class NamingUtilities
{
    public static string ToPascalCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }
        return char.ToUpperInvariant(name[0]) + name.Substring(1);
    }

    public static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }
        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }
}
```

### Code Fix Provider Implementation

```csharp
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PrivateFieldCamelCaseCodeFixProvider)), Shared]
public class PrivateFieldCamelCaseCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds
        => ImmutableArray.Create(PrivateFieldCamelCaseAnalyzer.DiagnosticId);

    public sealed override FixAllProvider GetFixAllProvider()
        => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
            .ConfigureAwait(false);

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var variableDeclarator = root.FindToken(diagnosticSpan.Start)
            .Parent
            .AncestorsAndSelf()
            .OfType<VariableDeclaratorSyntax>()
            .First();

        var fieldName = variableDeclarator.Identifier.ValueText;
        var newName = NamingUtilities.ToCamelCase(fieldName);

        context.RegisterCodeFix(
            CodeAction.Create(
                title: $"Rename to '{newName}'",
                createChangedSolution: c => RenameFieldAsync(context.Document, variableDeclarator, newName, c),
                equivalenceKey: nameof(PrivateFieldCamelCaseCodeFixProvider)),
            diagnostic);
    }

    private async Task<Solution> RenameFieldAsync(
        Document document,
        VariableDeclaratorSyntax variableDeclarator,
        string newName,
        CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken)
            .ConfigureAwait(false);
        var fieldSymbol = semanticModel.GetDeclaredSymbol(variableDeclarator, cancellationToken);

        var solution = document.Project.Solution;
        return await Renamer.RenameSymbolAsync(
            solution,
            fieldSymbol,
            newName,
            solution.Workspace.Options,
            cancellationToken)
            .ConfigureAwait(false);
    }
}
```

---

## Decision Tree

```
                    ┌─────────────────────────┐
                    │ FieldDeclarationSyntax  │
                    └───────────┬─────────────┘
                                │
              ┌─────────────────▼─────────────────┐
              │ Is field private or internal?     │
              │ (check modifiers)                 │
              └─────────────────┬─────────────────┘
                       │                │
                      NO              YES
                       │                │
                       ▼                ▼
                ┌──────────┐   ┌───────────────────────┐
                │ SKIP     │   │ For each variable     │
                │ (public/ │   │ in declaration        │
                │ protected)│   └───────────┬───────────┘
                └──────────┘               │
                                           ▼
                              ┌────────────────────────┐
                              │ Get variable name      │
                              └───────────┬────────────┘
                                          │
                        ┌─────────────────▼─────────────────┐
                        │ Does name start with '_'?         │
                        └─────────────────┬─────────────────┘
                                 │                │
                                YES              NO
                                 │                │
                                 ▼                ▼
                          ┌──────────┐   ┌────────────────────┐
                          │ SKIP     │   │ Is first char      │
                          └──────────┘   │ lowercase?         │
                                         └─────────┬──────────┘
                                             │          │
                                            YES        NO
                                             │          │
                                             ▼          ▼
                                      ┌──────────┐  ┌───────────────┐
                                      │ SKIP     │  │ REPORT        │
                                      └──────────┘  │ DIAGNOSTIC    │
                                                    │ CCS0004       │
                                                    └───────────────┘
```

---

## Access Modifier Logic

```
┌─────────────────────────────────────────────────────────────────┐
│                   IsPrivateOrInternal(field)                    │
└─────────────────────────────────────────────────────────────────┘
                                │
              ┌─────────────────▼─────────────────┐
              │ Has any access modifier?          │
              └─────────────────┬─────────────────┘
                       │                │
                      NO              YES
                       │                │
                       ▼                ▼
                ┌──────────────┐  ┌────────────────────────┐
                │ Return TRUE  │  │ Check specific modifier│
                │ (default is  │  └───────────┬────────────┘
                │  private)    │              │
                └──────────────┘              ▼
                              ┌───────────────────────────────┐
                              │ private                  → TRUE │
                              │ internal (not protected) → TRUE │
                              │ public                   → FALSE│
                              │ protected                → FALSE│
                              │ protected internal       → FALSE│
                              │ private protected        → TRUE │
                              └───────────────────────────────┘
```

---

## Test Cases

### Analyzer Tests

| Test Name | Input | Expected |
|-----------|-------|----------|
| PrivateField_PascalCase | `private int Count;` | CCS0004 at 'Count' |
| PrivateField_CamelCase | `private int count;` | No diagnostic |
| PrivateField_Underscore | `private int _count;` | No diagnostic |
| NoModifier_PascalCase | `int Count;` | CCS0004 (default private) |
| InternalField_PascalCase | `internal int Count;` | CCS0004 |
| PublicField_PascalCase | `public int Count;` | No diagnostic |
| ProtectedField_PascalCase | `protected int Count;` | No diagnostic |
| ProtectedInternal_PascalCase | `protected internal int Count;` | No diagnostic |
| PrivateProtected_PascalCase | `private protected int Count;` | CCS0004 |
| StaticPrivate_PascalCase | `private static int Count;` | CCS0004 |
| ReadonlyPrivate_PascalCase | `private readonly int Count;` | CCS0004 |
| MultipleVariables | `private int A, b, C;` | CCS0004 at 'A' and 'C' |
| ConstPrivate | `private const int MAX = 10;` | No diagnostic (covered by CCS0005) |

### Code Fix Tests

| Test Name | Before | After |
|-----------|--------|-------|
| SimpleRename | `private int Count;` | `private int count;` |
| RenameWithUsages | `private int Count; void M() { Count = 1; }` | `private int count; void M() { count = 1; }` |
| MultipleFields | `private int A, B;` | Fix each individually |

### Test Code Template

```csharp
[Fact]
public async Task PrivateField_StartsWithUppercase_ShouldTriggerDiagnostic()
{
    var testCode = @"
public class MyClass
{
    private int {|#0:Count|};
}";

    var expected = VerifyCS.Diagnostic("CCS0004")
        .WithLocation(0)
        .WithArguments("Count", "count");

    await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
}

[Fact]
public async Task PrivateField_WithUnderscore_NoDiagnostic()
{
    var testCode = @"
public class MyClass
{
    private int _count;
}";

    await VerifyCS.VerifyAnalyzerAsync(testCode);
}
```

---

## Integration Points

### Roslyn APIs Used

| API | Purpose |
|-----|---------|
| `SyntaxKind.FieldDeclaration` | Register for field nodes |
| `FieldDeclarationSyntax` | Access field declaration |
| `FieldDeclaration.Modifiers` | Check access modifiers |
| `FieldDeclaration.Declaration.Variables` | Iterate multiple variables |
| `VariableDeclaratorSyntax` | Individual variable in declaration |
| `Renamer.RenameSymbolAsync()` | Rename with reference updates |

### Dependencies

- `NamingUtilities.ToCamelCase()` - New utility method
- `NamingUtilities.ToPascalCase()` - Existing utility (extract from CCS0001)

---

## Deliverable Checklist

- [ ] Add `ToCamelCase()` to `NamingUtilities`
- [ ] Implement `PrivateFieldCamelCaseAnalyzer.cs`
- [ ] Implement `PrivateFieldCamelCaseCodeFixProvider.cs`
- [ ] Handle multiple variables in single declaration
- [ ] Handle all access modifier combinations
- [ ] Write analyzer tests (minimum 13 cases)
- [ ] Write code fix tests (minimum 3 cases)
- [ ] Write unit tests for `ToCamelCase()`
- [ ] Verify all tests pass
- [ ] Test manually in Visual Studio

---

## Edge Cases

| Case | Behavior | Rationale |
|------|----------|-----------|
| `private int _Count;` | Skip | Underscore prefix is valid convention |
| `private int count;` | Skip | Already camelCase |
| `private int NUMBER;` | Report → `nUMBER` | Only changes first char |
| `int Field;` | Report | No modifier = private |
| `private int a, B, c;` | Report only 'B' | Check each variable |
| `private const int MAX;` | Skip | Constants handled by CCS0005 |
| `public int Count;` | Skip | Public fields excluded |

---

## Configuration Considerations (Future)

In future versions, consider making configurable:
- Whether underscore prefix is required or just allowed
- Whether to enforce `_camelCase` style vs plain `camelCase`
- Whether to apply to `internal` fields or only `private`

For v0.2.0, use the permissive approach:
- Allow both `_fieldName` and `fieldName`
- Apply to both `private` and `internal`

---

## Special Notes

### Multiple Variables Per Declaration

A single `FieldDeclarationSyntax` can contain multiple variables:
```csharp
private int a, b, c;
```

The analyzer iterates through `Declaration.Variables` and reports each violation separately. The code fix renames each variable independently using the `VariableDeclaratorSyntax`.

### Const Fields

Constant fields (`const`) are excluded from this rule because they are covered by CCS0005 (ConstantUpperCase). The analyzer should check for the `const` modifier and skip those fields.

```csharp
private void AnalyzeFieldDeclaration(SyntaxNodeAnalysisContext context)
{
    var fieldDeclaration = (FieldDeclarationSyntax)context.Node;

    // Skip const fields (handled by CCS0005)
    if (fieldDeclaration.Modifiers.Any(SyntaxKind.ConstKeyword))
    {
        return;
    }
    // ... rest of analysis
}
```
