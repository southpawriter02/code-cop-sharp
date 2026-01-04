# CCS0003: Interfaces Must Start With 'I' Prefix

## Overview

| Property | Value |
|----------|-------|
| Rule ID | CCS0003 |
| Category | Naming |
| Severity | Warning |
| Has Code Fix | Yes |
| Enabled by Default | Yes |

## Description

Interface names in C# should follow the convention of starting with an uppercase 'I' followed by PascalCase. This rule flags interface declarations that don't start with 'I'.

### Compliant Examples

```csharp
public interface IDisposable { }
public interface IMyService { }
public interface IUserRepository { }
public interface I { }  // Minimal valid interface name
```

### Non-Compliant Examples

```csharp
public interface Disposable { }      // CCS0003: 'Disposable' → 'IDisposable'
public interface MyService { }       // CCS0003: 'MyService' → 'IMyService'
public interface iDisposable { }     // CCS0003: lowercase 'i' is not valid
public interface _IService { }       // CCS0003: underscore before I
```

---

## Implementation Specification

### File Structure

```
CodeCop.Sharp/
├── Analyzers/
│   └── Naming/
│       └── InterfacePrefixIAnalyzer.cs
└── CodeFixes/
    └── Naming/
        └── InterfacePrefixICodeFixProvider.cs
```

### Analyzer Implementation

```csharp
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class InterfacePrefixIAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "CCS0003";

    private static readonly LocalizableString Title =
        "Interface name should start with 'I'";
    private static readonly LocalizableString MessageFormat =
        "Interface name '{0}' should start with 'I'. Consider: '{1}'";
    private static readonly LocalizableString Description =
        "Interface names should start with 'I' followed by PascalCase.";
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
        context.RegisterSyntaxNodeAction(AnalyzeInterfaceDeclaration, SyntaxKind.InterfaceDeclaration);
    }

    private void AnalyzeInterfaceDeclaration(SyntaxNodeAnalysisContext context)
    {
        var interfaceDeclaration = (InterfaceDeclarationSyntax)context.Node;
        var interfaceName = interfaceDeclaration.Identifier.ValueText;

        if (string.IsNullOrEmpty(interfaceName))
        {
            return;
        }

        // Check if starts with uppercase 'I' followed by uppercase letter (or just 'I')
        if (StartsWithValidIPrefix(interfaceName))
        {
            return;
        }

        var suggestedName = SuggestInterfaceName(interfaceName);
        var diagnostic = Diagnostic.Create(
            Rule,
            interfaceDeclaration.Identifier.GetLocation(),
            interfaceName,
            suggestedName);
        context.ReportDiagnostic(diagnostic);
    }

    private static bool StartsWithValidIPrefix(string name)
    {
        if (name.Length == 1)
        {
            return name == "I";
        }

        // Must start with 'I' followed by uppercase letter
        return name[0] == 'I' && char.IsUpper(name[1]);
    }

    public static string SuggestInterfaceName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        // If starts with lowercase 'i', replace with 'I' and ensure next char is upper
        if (name[0] == 'i')
        {
            if (name.Length == 1)
            {
                return "I";
            }
            return "I" + char.ToUpperInvariant(name[1]) + name.Substring(2);
        }

        // Otherwise, prepend 'I' and ensure PascalCase
        return "I" + NamingUtilities.ToPascalCase(name);
    }
}
```

### Code Fix Provider Implementation

```csharp
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(InterfacePrefixICodeFixProvider)), Shared]
public class InterfacePrefixICodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds
        => ImmutableArray.Create(InterfacePrefixIAnalyzer.DiagnosticId);

    public sealed override FixAllProvider GetFixAllProvider()
        => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
            .ConfigureAwait(false);

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var declaration = root.FindToken(diagnosticSpan.Start)
            .Parent
            .AncestorsAndSelf()
            .OfType<InterfaceDeclarationSyntax>()
            .First();

        var interfaceName = declaration.Identifier.ValueText;
        var newName = InterfacePrefixIAnalyzer.SuggestInterfaceName(interfaceName);

        context.RegisterCodeFix(
            CodeAction.Create(
                title: $"Rename to '{newName}'",
                createChangedSolution: c => RenameInterfaceAsync(context.Document, declaration, newName, c),
                equivalenceKey: nameof(InterfacePrefixICodeFixProvider)),
            diagnostic);
    }

    private async Task<Solution> RenameInterfaceAsync(
        Document document,
        InterfaceDeclarationSyntax interfaceDeclaration,
        string newName,
        CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken)
            .ConfigureAwait(false);
        var interfaceSymbol = semanticModel.GetDeclaredSymbol(interfaceDeclaration, cancellationToken);

        var solution = document.Project.Solution;
        return await Renamer.RenameSymbolAsync(
            solution,
            interfaceSymbol,
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
                    ┌──────────────────────────┐
                    │ InterfaceDeclarationSyntax │
                    └───────────┬──────────────┘
                                │
                    ┌───────────▼───────────┐
                    │ Get Identifier.ValueText │
                    └───────────┬───────────┘
                                │
              ┌─────────────────▼─────────────────┐
              │ Is name null or empty?            │
              └─────────────────┬─────────────────┘
                       │                │
                      YES              NO
                       │                │
                       ▼                ▼
                ┌──────────┐   ┌────────────────────┐
                │ SKIP     │   │ Is name exactly "I"? │
                └──────────┘   └─────────┬──────────┘
                                    │          │
                                   YES        NO
                                    │          │
                                    ▼          ▼
                             ┌──────────┐  ┌────────────────────┐
                             │ SKIP     │  │ Does name start    │
                             └──────────┘  │ with 'I' + UPPER?  │
                                           └─────────┬──────────┘
                                               │          │
                                              YES        NO
                                               │          │
                                               ▼          ▼
                                        ┌──────────┐  ┌───────────────┐
                                        │ SKIP     │  │ REPORT        │
                                        └──────────┘  │ DIAGNOSTIC    │
                                                      │ CCS0003       │
                                                      └───────────────┘
```

---

## Naming Suggestion Logic

```
┌─────────────────────────────────────────────────────────────────┐
│                    SuggestInterfaceName(name)                   │
└─────────────────────────────────────────────────────────────────┘
                                │
              ┌─────────────────▼─────────────────┐
              │ Does name start with lowercase 'i'? │
              └─────────────────┬─────────────────┘
                       │                │
                      YES              NO
                       │                │
                       ▼                ▼
        ┌──────────────────────┐  ┌──────────────────────┐
        │ Replace 'i' with 'I' │  │ Prepend 'I' +        │
        │ Uppercase next char  │  │ ToPascalCase(name)   │
        └──────────────────────┘  └──────────────────────┘
                       │                │
                       ▼                ▼
        ┌──────────────────────┐  ┌──────────────────────┐
        │ "iService" → "IService" │  │ "Service" → "IService" │
        │ "iA" → "IA"          │  │ "service" → "IService" │
        └──────────────────────┘  └──────────────────────┘
```

---

## Test Cases

### Analyzer Tests

| Test Name | Input | Expected |
|-----------|-------|----------|
| NoIPrefix_ShouldTrigger | `interface MyService {}` | CCS0003 at 'MyService' |
| LowercaseIPrefix_ShouldTrigger | `interface iService {}` | CCS0003 at 'iService' |
| ValidIPrefix_NoTrigger | `interface IService {}` | No diagnostic |
| SingleI_NoTrigger | `interface I {}` | No diagnostic |
| IFollowedByLower_ShouldTrigger | `interface Iservice {}` | CCS0003 (I + lowercase) |
| UnderscorePrefix_ShouldTrigger | `interface _IService {}` | CCS0003 |
| LowercaseNoI_ShouldTrigger | `interface service {}` | CCS0003 |
| IFollowedByNumber_NoTrigger | `interface I2Service {}` | Skip (edge case) |
| NestedInterface_NoIPrefix | `class C { interface Inner {} }` | CCS0003 |
| GenericInterface_NoIPrefix | `interface MyList<T> {}` | CCS0003 |

### Code Fix Tests

| Test Name | Before | After |
|-----------|--------|-------|
| AddIPrefix | `interface Service {}` | `interface IService {}` |
| FixLowercaseI | `interface iService {}` | `interface IService {}` |
| RenameWithImplementors | `interface Service {} class Impl : Service {}` | `interface IService {} class Impl : IService {}` |
| HandleLowercaseName | `interface service {}` | `interface IService {}` |

### Test Code Template

```csharp
[Fact]
public async Task InterfaceName_NoIPrefix_ShouldTriggerDiagnostic()
{
    var testCode = @"
namespace TestNamespace
{
    public interface {|#0:MyService|}
    {
        void DoWork();
    }
}";

    var expected = VerifyCS.Diagnostic("CCS0003")
        .WithLocation(0)
        .WithArguments("MyService", "IMyService");

    await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
}

[Fact]
public async Task InterfaceName_LowercaseI_ShouldTriggerDiagnostic()
{
    var testCode = @"
namespace TestNamespace
{
    public interface {|#0:iService|}
    {
    }
}";

    var expected = VerifyCS.Diagnostic("CCS0003")
        .WithLocation(0)
        .WithArguments("iService", "IService");

    await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
}
```

---

## Integration Points

### Roslyn APIs Used

| API | Purpose |
|-----|---------|
| `SyntaxKind.InterfaceDeclaration` | Register for interface nodes |
| `InterfaceDeclarationSyntax` | Access interface syntax |
| `Identifier.ValueText` | Get interface name |
| `Identifier.GetLocation()` | Pinpoint diagnostic |
| `Renamer.RenameSymbolAsync()` | Rename with reference updates |

### Dependencies

- `NamingUtilities.ToPascalCase()` - Shared utility
- No external dependencies beyond existing Roslyn packages

---

## Deliverable Checklist

- [ ] Implement `InterfacePrefixIAnalyzer.cs`
- [ ] Implement `InterfacePrefixICodeFixProvider.cs`
- [ ] Add `SuggestInterfaceName()` static method
- [ ] Write analyzer tests (minimum 10 cases)
- [ ] Write code fix tests (minimum 4 cases)
- [ ] Write unit tests for `SuggestInterfaceName()`
- [ ] Verify all tests pass
- [ ] Test manually in Visual Studio

---

## Edge Cases

| Case | Behavior | Rationale |
|------|----------|-----------|
| `interface I {}` | Skip | Valid minimal interface name |
| `interface IService {}` | Skip | Already has valid prefix |
| `interface Iservice {}` | Report | 'I' followed by lowercase |
| `interface I2Service {}` | Skip | Numbers after 'I' acceptable |
| `interface iService {}` | Report | Lowercase 'i' not valid |
| `interface _IService {}` | Report | Underscore prefix not standard |
| `interface SERVICE {}` | Report → `ISERVICE` | All caps gets I prepended |

---

## Special Considerations

### Why "I + Uppercase" Rule?

The pattern `IService` follows .NET Framework Design Guidelines:
- The 'I' prefix distinguishes interfaces from classes
- The character after 'I' must be uppercase to avoid confusion with words starting with 'I' (e.g., `Image`, `Item`)
- `Iservice` looks like the word "Iservice", not "I-Service"

### Handling Existing Code

When applying to existing codebases:
- The code fix will rename all implementations and usages
- Consider running on a branch first to review changes
- Large codebases may have many violations - use "Fix All" feature
