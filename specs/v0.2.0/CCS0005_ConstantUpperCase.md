# CCS0005: Constants Should Use UPPER_CASE or PascalCase

## Overview

| Property | Value |
|----------|-------|
| Rule ID | CCS0005 |
| Category | Naming |
| Severity | Info |
| Has Code Fix | Yes |
| Enabled by Default | Yes |

## Description

Constant fields (`const`) in C# should follow either UPPER_CASE (SCREAMING_SNAKE_CASE) or PascalCase naming convention. This rule flags constants that start with a lowercase letter.

### Why Two Conventions?

Both conventions are widely used in C#:
- **UPPER_CASE**: Traditional C/C++ style, highly visible, clearly identifies constants
- **PascalCase**: .NET Framework style, consistent with other public members

This rule accepts both conventions but rejects camelCase constants.

### Compliant Examples

```csharp
public class MyClass
{
    // UPPER_CASE style
    private const int MAX_SIZE = 100;
    public const string API_KEY = "key";
    private const double PI_VALUE = 3.14159;

    // PascalCase style
    private const int MaxSize = 100;
    public const string ApiKey = "key";
    private const double PiValue = 3.14159;
}
```

### Non-Compliant Examples

```csharp
public class MyClass
{
    private const int maxSize = 100;     // CCS0005: 'maxSize' → 'MaxSize'
    public const string apiKey = "key";  // CCS0005: 'apiKey' → 'ApiKey'
    private const double pi = 3.14159;   // CCS0005: 'pi' → 'Pi' or 'PI'
}
```

---

## Implementation Specification

### File Structure

```
CodeCop.Sharp/
├── Analyzers/
│   └── Naming/
│       └── ConstantUpperCaseAnalyzer.cs
└── CodeFixes/
    └── Naming/
        └── ConstantUpperCaseCodeFixProvider.cs
```

### Analyzer Implementation

```csharp
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ConstantUpperCaseAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "CCS0005";

    private static readonly LocalizableString Title =
        "Constant should use UPPER_CASE or PascalCase";
    private static readonly LocalizableString MessageFormat =
        "Constant '{0}' should use UPPER_CASE or PascalCase. Consider: '{1}'";
    private static readonly LocalizableString Description =
        "Constants should use UPPER_CASE (SCREAMING_SNAKE_CASE) or PascalCase naming.";
    private const string Category = "Naming";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Info,  // Info severity (less intrusive)
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

        // Only check const fields
        if (!fieldDeclaration.Modifiers.Any(SyntaxKind.ConstKeyword))
        {
            return;
        }

        foreach (var variable in fieldDeclaration.Declaration.Variables)
        {
            var constName = variable.Identifier.ValueText;

            if (string.IsNullOrEmpty(constName))
            {
                continue;
            }

            // Valid if starts with uppercase (PascalCase or UPPER_CASE)
            if (char.IsUpper(constName[0]))
            {
                continue;
            }

            // Starts with lowercase - violation
            var suggestedName = NamingUtilities.ToPascalCase(constName);
            var diagnostic = Diagnostic.Create(
                Rule,
                variable.Identifier.GetLocation(),
                constName,
                suggestedName);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
```

### Code Fix Provider Implementation

```csharp
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConstantUpperCaseCodeFixProvider)), Shared]
public class ConstantUpperCaseCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds
        => ImmutableArray.Create(ConstantUpperCaseAnalyzer.DiagnosticId);

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

        var constName = variableDeclarator.Identifier.ValueText;

        // Offer two fix options: PascalCase and UPPER_CASE
        var pascalName = NamingUtilities.ToPascalCase(constName);
        var upperName = NamingUtilities.ToUpperSnakeCase(constName);

        // Primary fix: PascalCase (more common in modern C#)
        context.RegisterCodeFix(
            CodeAction.Create(
                title: $"Rename to '{pascalName}' (PascalCase)",
                createChangedSolution: c => RenameConstAsync(context.Document, variableDeclarator, pascalName, c),
                equivalenceKey: "PascalCase"),
            diagnostic);

        // Alternative fix: UPPER_CASE (only if different from PascalCase)
        if (upperName != pascalName)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: $"Rename to '{upperName}' (UPPER_CASE)",
                    createChangedSolution: c => RenameConstAsync(context.Document, variableDeclarator, upperName, c),
                    equivalenceKey: "UPPER_CASE"),
                diagnostic);
        }
    }

    private async Task<Solution> RenameConstAsync(
        Document document,
        VariableDeclaratorSyntax variableDeclarator,
        string newName,
        CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken)
            .ConfigureAwait(false);
        var constSymbol = semanticModel.GetDeclaredSymbol(variableDeclarator, cancellationToken);

        var solution = document.Project.Solution;
        return await Renamer.RenameSymbolAsync(
            solution,
            constSymbol,
            newName,
            solution.Workspace.Options,
            cancellationToken)
            .ConfigureAwait(false);
    }
}
```

### NamingUtilities Extension

```csharp
public static class NamingUtilities
{
    // ... existing methods ...

    /// <summary>
    /// Converts camelCase or PascalCase to UPPER_SNAKE_CASE.
    /// Example: "maxSize" → "MAX_SIZE", "apiKey" → "API_KEY"
    /// </summary>
    public static string ToUpperSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var result = new StringBuilder();

        for (int i = 0; i < name.Length; i++)
        {
            var c = name[i];

            // Insert underscore before uppercase letters (except first char)
            if (i > 0 && char.IsUpper(c) && !char.IsUpper(name[i - 1]))
            {
                result.Append('_');
            }

            result.Append(char.ToUpperInvariant(c));
        }

        return result.ToString();
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
              │ Has 'const' modifier?             │
              └─────────────────┬─────────────────┘
                       │                │
                      NO              YES
                       │                │
                       ▼                ▼
                ┌──────────┐   ┌───────────────────────┐
                │ SKIP     │   │ For each variable     │
                │(not const)│  │ in declaration        │
                └──────────┘   └───────────┬───────────┘
                                           │
                                           ▼
                              ┌────────────────────────┐
                              │ Get variable name      │
                              └───────────┬────────────┘
                                          │
                        ┌─────────────────▼─────────────────┐
                        │ Is first char uppercase?          │
                        └─────────────────┬─────────────────┘
                                 │                │
                                YES              NO
                                 │                │
                                 ▼                ▼
                          ┌──────────┐   ┌───────────────┐
                          │ SKIP     │   │ REPORT        │
                          │ (valid)  │   │ DIAGNOSTIC    │
                          └──────────┘   │ CCS0005       │
                                         └───────────────┘
```

---

## Naming Conversion Logic

```
┌─────────────────────────────────────────────────────────────────┐
│                   ToUpperSnakeCase(name)                        │
└─────────────────────────────────────────────────────────────────┘

Examples:
┌────────────────┬──────────────────┐
│ Input          │ Output           │
├────────────────┼──────────────────┤
│ "maxSize"      │ "MAX_SIZE"       │
│ "apiKey"       │ "API_KEY"        │
│ "pi"           │ "PI"             │
│ "httpClient"   │ "HTTP_CLIENT"    │
│ "url"          │ "URL"            │
│ "MAX_SIZE"     │ "MAX_SIZE"       │
│ "XMLParser"    │ "XMLPARSER"      │
└────────────────┴──────────────────┘

Algorithm:
1. Iterate through each character
2. If current char is uppercase AND previous is not uppercase
   → Insert underscore before current char
3. Convert all chars to uppercase
```

---

## Test Cases

### Analyzer Tests

| Test Name | Input | Expected |
|-----------|-------|----------|
| Const_CamelCase | `const int maxSize = 10;` | CCS0005 at 'maxSize' |
| Const_PascalCase | `const int MaxSize = 10;` | No diagnostic |
| Const_UpperCase | `const int MAX_SIZE = 10;` | No diagnostic |
| Const_SingleLowerChar | `const int x = 1;` | CCS0005 at 'x' |
| Const_SingleUpperChar | `const int X = 1;` | No diagnostic |
| PrivateConst_CamelCase | `private const int count = 5;` | CCS0005 |
| PublicConst_CamelCase | `public const string apiUrl = "";` | CCS0005 |
| StaticReadonly_CamelCase | `static readonly int max = 10;` | No diagnostic (not const) |
| MultipleConsts | `const int a = 1, B = 2;` | CCS0005 only at 'a' |
| Const_MixedCase | `const int HTTPCode = 200;` | No diagnostic |

### Code Fix Tests

| Test Name | Before | After (PascalCase) | After (UPPER_CASE) |
|-----------|--------|--------------------|--------------------|
| SimpleRename | `const int maxSize = 10;` | `const int MaxSize = 10;` | `const int MAX_SIZE = 10;` |
| WithUsages | `const int max = 10; int x = max;` | `const int Max = 10; int x = Max;` | `const int MAX = 10; int x = MAX;` |
| CamelCaseToUpperCase | `const string apiKey = "";` | `const string ApiKey = "";` | `const string API_KEY = "";` |

### Test Code Template

```csharp
[Fact]
public async Task Const_StartsWithLowercase_ShouldTriggerDiagnostic()
{
    var testCode = @"
public class MyClass
{
    private const int {|#0:maxSize|} = 100;
}";

    var expected = VerifyCS.Diagnostic("CCS0005")
        .WithLocation(0)
        .WithArguments("maxSize", "MaxSize");

    await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
}

[Fact]
public async Task Const_PascalCase_NoDiagnostic()
{
    var testCode = @"
public class MyClass
{
    private const int MaxSize = 100;
}";

    await VerifyCS.VerifyAnalyzerAsync(testCode);
}

[Fact]
public async Task Const_UpperSnakeCase_NoDiagnostic()
{
    var testCode = @"
public class MyClass
{
    private const int MAX_SIZE = 100;
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
| `SyntaxKind.ConstKeyword` | Check for const modifier |
| `FieldDeclarationSyntax` | Access field declaration |
| `VariableDeclaratorSyntax` | Individual constant variable |
| `Renamer.RenameSymbolAsync()` | Rename with reference updates |

### Dependencies

- `NamingUtilities.ToPascalCase()` - Existing utility
- `NamingUtilities.ToUpperSnakeCase()` - New utility method

---

## Deliverable Checklist

- [ ] Add `ToUpperSnakeCase()` to `NamingUtilities`
- [ ] Implement `ConstantUpperCaseAnalyzer.cs`
- [ ] Implement `ConstantUpperCaseCodeFixProvider.cs`
- [ ] Offer both PascalCase and UPPER_CASE fix options
- [ ] Write analyzer tests (minimum 10 cases)
- [ ] Write code fix tests (minimum 3 cases for each fix type)
- [ ] Write unit tests for `ToUpperSnakeCase()`
- [ ] Verify all tests pass
- [ ] Test manually in Visual Studio

---

## Edge Cases

| Case | Behavior | Rationale |
|------|----------|-----------|
| `const int MaxSize = 10;` | Skip | Valid PascalCase |
| `const int MAX_SIZE = 10;` | Skip | Valid UPPER_CASE |
| `const int maxSize = 10;` | Report | Starts with lowercase |
| `const int x = 1;` | Report → `X` or `X` | Single char |
| `const int _max = 10;` | Report | Underscore prefix not valid for const |
| `static readonly int max;` | Skip | Not a const |
| `const int HTTPCode = 200;` | Skip | Starts with uppercase |
| `const int API_KEY = "";` | Skip | Already UPPER_CASE |

---

## Special Notes

### Why Info Severity?

This rule uses `DiagnosticSeverity.Info` instead of `Warning` because:
1. Both PascalCase and UPPER_CASE are valid conventions
2. The violation is stylistic rather than potentially problematic
3. Existing codebases may have mixed conventions
4. Less disruptive for legacy code integration

### Two Fix Options

Unlike other naming rules, this one offers two code fix options:
1. **PascalCase** (default): More consistent with modern .NET style
2. **UPPER_CASE**: Traditional C-style, highly visible

The code fix provider registers both options, and the user can choose which convention to apply.

### Distinguishing from CCS0004

The analyzer must check for the `const` modifier to avoid conflicting with CCS0004 (Private Field camelCase):
- `private int maxSize;` → CCS0004 (expects camelCase or underscore)
- `private const int maxSize;` → CCS0005 (expects PascalCase or UPPER_CASE)

Both rules register for `SyntaxKind.FieldDeclaration`, so each must filter appropriately.
