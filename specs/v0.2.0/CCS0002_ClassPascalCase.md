# CCS0002: Class Names Must Be PascalCase

## Overview

| Property | Value |
|----------|-------|
| Rule ID | CCS0002 |
| Category | Naming |
| Severity | Warning |
| Has Code Fix | Yes |
| Enabled by Default | Yes |

## Description

Class names in C# should follow PascalCase naming convention (also known as UpperCamelCase). This rule flags class declarations where the class name starts with a lowercase letter.

### Compliant Examples

```csharp
public class MyClass { }
public class DatabaseConnection { }
public class XMLParser { }  // Acronyms are acceptable
public class _InternalClass { }  // Underscore prefix allowed
```

### Non-Compliant Examples

```csharp
public class myClass { }        // CCS0002: 'myClass' → 'MyClass'
public class databaseConnection { }  // CCS0002
internal class helper { }       // CCS0002
```

---

## Implementation Specification

### File Structure

```
CodeCop.Sharp/
├── Analyzers/
│   └── Naming/
│       └── ClassPascalCaseAnalyzer.cs
├── CodeFixes/
│   └── Naming/
│       └── ClassPascalCaseCodeFixProvider.cs
└── Utilities/
    └── NamingUtilities.cs  (shared ToPascalCase)
```

### Analyzer Implementation

```csharp
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ClassPascalCaseAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "CCS0002";

    private static readonly LocalizableString Title =
        "Class name should be in PascalCase";
    private static readonly LocalizableString MessageFormat =
        "Class name '{0}' should be in PascalCase. Consider: '{1}'";
    private static readonly LocalizableString Description =
        "Class names should follow PascalCase naming convention.";
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
        context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
    }

    private void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        var className = classDeclaration.Identifier.ValueText;

        // Skip: null/empty, starts with uppercase, starts with underscore
        if (string.IsNullOrEmpty(className) ||
            char.IsUpper(className[0]) ||
            className[0] == '_')
        {
            return;
        }

        var suggestedName = NamingUtilities.ToPascalCase(className);
        var diagnostic = Diagnostic.Create(
            Rule,
            classDeclaration.Identifier.GetLocation(),
            className,
            suggestedName);
        context.ReportDiagnostic(diagnostic);
    }
}
```

### Code Fix Provider Implementation

```csharp
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ClassPascalCaseCodeFixProvider)), Shared]
public class ClassPascalCaseCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds
        => ImmutableArray.Create(ClassPascalCaseAnalyzer.DiagnosticId);

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
            .OfType<ClassDeclarationSyntax>()
            .First();

        var className = declaration.Identifier.ValueText;
        var newName = NamingUtilities.ToPascalCase(className);

        context.RegisterCodeFix(
            CodeAction.Create(
                title: $"Rename to '{newName}'",
                createChangedSolution: c => RenameClassAsync(context.Document, declaration, newName, c),
                equivalenceKey: nameof(ClassPascalCaseCodeFixProvider)),
            diagnostic);
    }

    private async Task<Solution> RenameClassAsync(
        Document document,
        ClassDeclarationSyntax classDeclaration,
        string newName,
        CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken)
            .ConfigureAwait(false);
        var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken);

        var solution = document.Project.Solution;
        return await Renamer.RenameSymbolAsync(
            solution,
            classSymbol,
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
                    ┌─────────────────────┐
                    │ ClassDeclarationSyntax │
                    └──────────┬──────────┘
                               │
                    ┌──────────▼──────────┐
                    │ Get Identifier.ValueText │
                    └──────────┬──────────┘
                               │
              ┌────────────────▼────────────────┐
              │ Is name null or empty?          │
              └────────────────┬────────────────┘
                      │                │
                     YES              NO
                      │                │
                      ▼                ▼
               ┌──────────┐   ┌───────────────────┐
               │ SKIP     │   │ Does first char   │
               └──────────┘   │ start with '_'?   │
                              └─────────┬─────────┘
                                  │          │
                                 YES        NO
                                  │          │
                                  ▼          ▼
                           ┌──────────┐  ┌───────────────────┐
                           │ SKIP     │  │ Is first char     │
                           └──────────┘  │ uppercase?        │
                                         └─────────┬─────────┘
                                             │          │
                                            YES        NO
                                             │          │
                                             ▼          ▼
                                      ┌──────────┐  ┌───────────────┐
                                      │ SKIP     │  │ REPORT        │
                                      └──────────┘  │ DIAGNOSTIC    │
                                                    │ CCS0002       │
                                                    └───────────────┘
```

---

## Test Cases

### Analyzer Tests

| Test Name | Input | Expected |
|-----------|-------|----------|
| LowercaseClass_ShouldTrigger | `class myClass {}` | CCS0002 at 'myClass' |
| UppercaseClass_NoTrigger | `class MyClass {}` | No diagnostic |
| UnderscorePrefix_NoTrigger | `class _myClass {}` | No diagnostic |
| SingleLetter_Lowercase | `class a {}` | CCS0002 at 'a' |
| SingleLetter_Uppercase | `class A {}` | No diagnostic |
| NestedClass_Lowercase | `class Outer { class inner {} }` | CCS0002 at 'inner' |
| GenericClass_Lowercase | `class myList<T> {}` | CCS0002 at 'myList' |
| PartialClass_Lowercase | `partial class myClass {}` | CCS0002 |
| AbstractClass_Lowercase | `abstract class myClass {}` | CCS0002 |

### Code Fix Tests

| Test Name | Before | After |
|-----------|--------|-------|
| SimpleRename | `class myClass {}` | `class MyClass {}` |
| RenameWithUsages | `class myClass {} var x = new myClass();` | `class MyClass {} var x = new MyClass();` |
| RenameWithInheritance | `class myBase {} class Child : myBase {}` | `class MyBase {} class Child : MyBase {}` |

### Test Code Template

```csharp
[Fact]
public async Task ClassName_StartsWithLowercase_ShouldTriggerDiagnostic()
{
    var testCode = @"
namespace TestNamespace
{
    public class {|#0:myClass|}
    {
    }
}";

    var expected = VerifyCS.Diagnostic("CCS0002")
        .WithLocation(0)
        .WithArguments("myClass", "MyClass");

    await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
}
```

---

## Integration Points

### Roslyn APIs Used

| API | Purpose |
|-----|---------|
| `SyntaxKind.ClassDeclaration` | Register for class nodes |
| `ClassDeclarationSyntax` | Access class syntax |
| `Identifier.ValueText` | Get class name |
| `Identifier.GetLocation()` | Pinpoint diagnostic |
| `Renamer.RenameSymbolAsync()` | Rename with reference updates |

### Dependencies

- `NamingUtilities.ToPascalCase()` - Shared utility (extract from CCS0001)
- No external dependencies beyond existing Roslyn packages

---

## Deliverable Checklist

- [ ] Create `Analyzers/Naming/` directory structure
- [ ] Extract `NamingUtilities.ToPascalCase()` to shared utility
- [ ] Implement `ClassPascalCaseAnalyzer.cs`
- [ ] Implement `ClassPascalCaseCodeFixProvider.cs`
- [ ] Write analyzer tests (minimum 9 cases)
- [ ] Write code fix tests (minimum 3 cases)
- [ ] Update analyzer tests to use shared verifier aliases
- [ ] Verify all tests pass
- [ ] Test manually in Visual Studio

---

## Edge Cases

| Case | Behavior | Rationale |
|------|----------|-----------|
| `class _internal {}` | Skip | Convention for internal/private |
| `class XMLParser {}` | Skip | Starts with uppercase |
| `class xMLParser {}` | Report | Starts with lowercase |
| `class class1 {}` | Report | Starts with lowercase |
| Record classes | Skip for v0.2.0 | Different syntax kind |
