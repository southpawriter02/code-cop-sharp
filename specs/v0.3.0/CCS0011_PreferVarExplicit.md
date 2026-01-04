# CCS0011: PreferVarExplicit

## Overview

| Property | Value |
|----------|-------|
| Rule ID | CCS0011 |
| Category | Style |
| Severity | Info |
| Has Code Fix | Yes |
| Enabled by Default | Yes |

## Description

This analyzer enforces explicit type declarations over `var` when the type is not apparent from the right-hand side of the assignment. When the type IS apparent (from `new`, cast, or literal), `var` is acceptable.

### Why This Rule?

1. **Readability**: Explicit types make code easier to read without IDE hover
2. **Code Review**: Types are visible in diffs without needing an IDE
3. **Self-Documentation**: Code documents its own types

### Note on Controversy

This rule is intentionally less strict than similar rules. It only flags `var` when the type cannot be inferred at a glance. Some teams prefer `var` everywhere - this rule should be configurable via `.editorconfig`.

---

## Compliant Examples

```csharp
// Good - type is apparent from 'new'
var list = new List<int>();
var customer = new Customer();

// Good - type is apparent from cast
var number = (int)value;
var person = (Person)obj;

// Good - type is apparent from literal
var name = "John";
var count = 42;
var price = 19.99m;
var flag = true;

// Good - explicit type used
List<int> items = GetItems();
string result = ProcessData();
Customer customer = repository.GetById(id);
```

## Non-Compliant Examples

```csharp
// Bad - type not apparent from method call
var items = GetItems();           // CCS0011 - what type is 'items'?
var result = ProcessData();       // CCS0011 - what type is 'result'?
var customer = repository.Get(1); // CCS0011 - what type is 'customer'?

// Bad - type not apparent from property
var length = text.Length;         // CCS0011 - int, but not obvious
var first = collection.First();   // CCS0011 - element type not apparent

// Bad - type not apparent from complex expression
var data = items.Where(x => x.Active).Select(x => x.Data).ToList();
// CCS0011 - what is the type of 'data'?
```

---

## Implementation Specification

### File Structure

```
CodeCop.Sharp/
├── Analyzers/
│   └── Style/
│       └── PreferVarExplicitAnalyzer.cs
└── CodeFixes/
    └── Style/
        └── PreferVarExplicitCodeFixProvider.cs
```

### Analyzer Implementation

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace CodeCop.Sharp.Analyzers.Style
{
    /// <summary>
    /// Analyzer that enforces explicit type declarations when type is not apparent.
    /// </summary>
    /// <remarks>
    /// Rule ID: CCS0011
    /// Category: Style
    /// Severity: Info
    ///
    /// This analyzer reports a diagnostic when 'var' is used and the type
    /// cannot be easily inferred from the right-hand side.
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class PreferVarExplicitAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        /// The diagnostic ID for this analyzer.
        /// </summary>
        public const string DiagnosticId = "CCS0011";

        private static readonly LocalizableString Title = "Prefer explicit type over var";
        private static readonly LocalizableString MessageFormat =
            "Consider using explicit type '{0}' instead of 'var'";
        private static readonly LocalizableString Description =
            "Use explicit type declarations when the type is not apparent from the right-hand side.";
        private const string Category = "Style";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: Description);

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Rule);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeLocalDeclaration, SyntaxKind.LocalDeclarationStatement);
        }

        private void AnalyzeLocalDeclaration(SyntaxNodeAnalysisContext context)
        {
            var localDeclaration = (LocalDeclarationStatementSyntax)context.Node;
            var variableDeclaration = localDeclaration.Declaration;

            // Check if using 'var'
            if (!variableDeclaration.Type.IsVar)
            {
                return;
            }

            // Get the initializer
            var variable = variableDeclaration.Variables.FirstOrDefault();
            if (variable?.Initializer?.Value == null)
            {
                return;
            }

            var initializer = variable.Initializer.Value;

            // Check if type is apparent from the initializer
            if (IsTypeApparent(initializer))
            {
                return;
            }

            // Get the actual type for the message
            var semanticModel = context.SemanticModel;
            var typeInfo = semanticModel.GetTypeInfo(initializer, context.CancellationToken);
            var typeName = typeInfo.Type?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat) ?? "var";

            var diagnostic = Diagnostic.Create(
                Rule,
                variableDeclaration.Type.GetLocation(),
                typeName);
            context.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        /// Determines if the type is apparent from the expression.
        /// </summary>
        private bool IsTypeApparent(ExpressionSyntax expression)
        {
            return expression switch
            {
                // new Foo() - type is apparent
                ObjectCreationExpressionSyntax => true,

                // new[] { ... } - type might not be apparent
                ArrayCreationExpressionSyntax => true,

                // (Foo)bar - type is apparent from cast
                CastExpressionSyntax => true,

                // bar as Foo - type is apparent
                BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.AsExpression) => true,

                // Literals - type is apparent
                LiteralExpressionSyntax => true,

                // default(Foo) or default - context dependent
                DefaultExpressionSyntax => true,

                // typeof(Foo) - always System.Type
                TypeOfExpressionSyntax => true,

                // nameof(x) - always string
                InvocationExpressionSyntax inv when IsNameofExpression(inv) => true,

                // await - check the awaited expression
                AwaitExpressionSyntax awaitExpr => IsTypeApparent(awaitExpr.Expression),

                // Parenthesized - check inner
                ParenthesizedExpressionSyntax paren => IsTypeApparent(paren.Expression),

                // Target-typed new (new()) - type is NOT apparent without context
                ImplicitObjectCreationExpressionSyntax => false,

                // Everything else - type is NOT apparent
                _ => false
            };
        }

        private bool IsNameofExpression(InvocationExpressionSyntax invocation)
        {
            return invocation.Expression is IdentifierNameSyntax identifier &&
                   identifier.Identifier.ValueText == "nameof";
        }
    }
}
```

### Code Fix Provider Implementation

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCop.Sharp.CodeFixes.Style
{
    /// <summary>
    /// Code fix provider for CCS0011 (PreferVarExplicit).
    /// Replaces 'var' with the explicit type.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PreferVarExplicitCodeFixProvider)), Shared]
    public class PreferVarExplicitCodeFixProvider : CodeFixProvider
    {
        /// <inheritdoc/>
        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(PreferVarExplicitAnalyzer.DiagnosticId);

        /// <inheritdoc/>
        public sealed override FixAllProvider GetFixAllProvider()
            => WellKnownFixAllProviders.BatchFixer;

        /// <inheritdoc/>
        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var varToken = root.FindToken(diagnosticSpan.Start);
            var variableDeclaration = varToken.Parent?.AncestorsAndSelf()
                .OfType<VariableDeclarationSyntax>()
                .FirstOrDefault();

            if (variableDeclaration == null) return;

            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            var variable = variableDeclaration.Variables.FirstOrDefault();
            if (variable?.Initializer?.Value == null) return;

            var typeInfo = semanticModel.GetTypeInfo(variable.Initializer.Value, context.CancellationToken);
            if (typeInfo.Type == null) return;

            var typeName = typeInfo.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: $"Use explicit type '{typeName}'",
                    createChangedDocument: c => UseExplicitTypeAsync(context.Document, variableDeclaration, typeInfo.Type, c),
                    equivalenceKey: "UseExplicitType"),
                diagnostic);
        }

        private async Task<Document> UseExplicitTypeAsync(
            Document document,
            VariableDeclarationSyntax variableDeclaration,
            ITypeSymbol type,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var typeSyntax = SyntaxFactory.ParseTypeName(type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat))
                .WithLeadingTrivia(variableDeclaration.Type.GetLeadingTrivia())
                .WithTrailingTrivia(variableDeclaration.Type.GetTrailingTrivia());

            var newDeclaration = variableDeclaration.WithType(typeSyntax);
            var newRoot = root.ReplaceNode(variableDeclaration, newDeclaration);

            return document.WithSyntaxRoot(newRoot);
        }
    }
}
```

---

## Decision Tree

```
┌────────────────────────────────────┐
│ Is it a local variable declaration │
│ using 'var' keyword?               │
└─────────────────┬──────────────────┘
                  │
          ┌───────▼───────┐
          │      NO       │──────────► SKIP (already explicit)
          └───────┬───────┘
                  │ YES
                  ▼
┌────────────────────────────────────┐
│ Does it have an initializer?       │
└─────────────────┬──────────────────┘
                  │
          ┌───────▼───────┐
          │      NO       │──────────► SKIP (compiler error)
          └───────┬───────┘
                  │ YES
                  ▼
┌────────────────────────────────────┐
│ Is type apparent from initializer? │
│ (new X(), cast, literal, etc.)     │
└─────────────────┬──────────────────┘
                  │
          ┌───────▼───────┐
          │      YES      │──────────► SKIP (var is acceptable)
          └───────┬───────┘
                  │ NO
                  ▼
            REPORT CCS0011
```

### Type Apparent Decision

```
┌───────────────────────────────────────────────────┐
│              Is Type Apparent?                    │
└───────────────────────────────────────────────────┘
                        │
    ┌───────────────────┼───────────────────┐
    │                   │                   │
    ▼                   ▼                   ▼
┌─────────┐      ┌─────────────┐      ┌──────────────┐
│ YES     │      │ MAYBE       │      │ NO           │
├─────────┤      ├─────────────┤      ├──────────────┤
│ new X() │      │ default     │      │ Method()     │
│ (X)expr │      │ default(X)  │      │ Property     │
│ expr as │      │ new[]       │      │ LINQ         │
│ "str"   │      │             │      │ Ternary      │
│ 123     │      │             │      │ new()        │
│ true    │      │             │      │              │
│ nameof  │      │             │      │              │
│ typeof  │      │             │      │              │
└─────────┘      └─────────────┘      └──────────────┘
```

---

## Test Cases

### Analyzer Tests - Should Trigger Diagnostic

| Test Name | Input | Expected |
|-----------|-------|----------|
| Var_MethodCall | `var x = GetValue();` | CCS0011 |
| Var_PropertyAccess | `var x = obj.Property;` | CCS0011 |
| Var_LinqQuery | `var x = items.Where(...).ToList();` | CCS0011 |
| Var_Ternary | `var x = cond ? a : b;` | CCS0011 |
| Var_NullCoalescing | `var x = a ?? b;` | CCS0011 |
| Var_ImplicitNew | `var x = new();` | CCS0011 |
| Var_FieldAccess | `var x = obj.field;` | CCS0011 |

### Analyzer Tests - Should NOT Trigger Diagnostic

| Test Name | Input | Expected |
|-----------|-------|----------|
| Var_NewExpression | `var x = new List<int>();` | No diagnostic |
| Var_Cast | `var x = (int)y;` | No diagnostic |
| Var_AsExpression | `var x = obj as string;` | No diagnostic |
| Var_StringLiteral | `var x = "hello";` | No diagnostic |
| Var_IntLiteral | `var x = 42;` | No diagnostic |
| Var_BoolLiteral | `var x = true;` | No diagnostic |
| Var_DecimalLiteral | `var x = 19.99m;` | No diagnostic |
| Var_Nameof | `var x = nameof(MyClass);` | No diagnostic |
| Var_Typeof | `var x = typeof(string);` | No diagnostic |
| ExplicitType_Used | `List<int> x = GetList();` | No diagnostic |

### Code Fix Tests

| Test Name | Before | After |
|-----------|--------|-------|
| MethodCall_ToExplicit | `var x = GetItems();` | `List<int> x = GetItems();` |
| Property_ToExplicit | `var x = obj.Name;` | `string x = obj.Name;` |
| Linq_ToExplicit | `var x = items.First();` | `Item x = items.First();` |

---

## Test Code Template

```csharp
using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<
    CodeCop.Sharp.Analyzers.Style.PreferVarExplicitAnalyzer>;
using VerifyCodeFix = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.CodeFixVerifier<
    CodeCop.Sharp.Analyzers.Style.PreferVarExplicitAnalyzer,
    CodeCop.Sharp.CodeFixes.Style.PreferVarExplicitCodeFixProvider>;

namespace CodeCop.Sharp.Tests.Analyzers.Style
{
    public class PreferVarExplicitAnalyzerTests
    {
        [Fact]
        public async Task Var_MethodCall_ShouldTriggerDiagnostic()
        {
            var testCode = @"
using System.Collections.Generic;

public class MyClass
{
    public void Test()
    {
        {|#0:var|} items = GetItems();
    }

    private List<int> GetItems() => new List<int>();
}";

            var expected = VerifyCS.Diagnostic("CCS0011").WithLocation(0).WithArguments("List<int>");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task Var_NewExpression_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
using System.Collections.Generic;

public class MyClass
{
    public void Test()
    {
        var items = new List<int>();
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task CodeFix_ReplacesVarWithExplicitType()
        {
            var testCode = @"
using System.Collections.Generic;

public class MyClass
{
    public void Test()
    {
        {|#0:var|} items = GetItems();
    }

    private List<int> GetItems() => new List<int>();
}";

            var fixedCode = @"
using System.Collections.Generic;

public class MyClass
{
    public void Test()
    {
        List<int> items = GetItems();
    }

    private List<int> GetItems() => new List<int>();
}";

            var expected = VerifyCodeFix.Diagnostic("CCS0011").WithLocation(0).WithArguments("List<int>");
            await VerifyCodeFix.VerifyCodeFixAsync(testCode, expected, fixedCode);
        }
    }
}
```

---

## Edge Cases

| Case | Behavior | Rationale |
|------|----------|-----------|
| `var x = new List<int>();` | Skip | Type apparent from `new` |
| `var x = new();` | Report | Target-typed new, type not obvious |
| `var x = default;` | Skip | Type often apparent from context |
| `var x = items.Select(i => i.Name).ToList();` | Report | LINQ chain type not obvious |
| `var x = a?.Property;` | Report | Null conditional, type not obvious |
| `var (a, b) = GetTuple();` | Skip | Tuple deconstruction (different syntax) |

---

## Configuration (Future)

```ini
# .editorconfig
[*.cs]
# CCS0011: Prefer explicit type
dotnet_diagnostic.CCS0011.severity = none  # Disable if preferring var everywhere
```

---

## Deliverable Checklist

- [ ] Create `Analyzers/Style/PreferVarExplicitAnalyzer.cs`
- [ ] Create `CodeFixes/Style/PreferVarExplicitCodeFixProvider.cs`
- [ ] Implement `IsTypeApparent()` logic
- [ ] Handle all "type apparent" scenarios
- [ ] Write analyzer tests (~12 tests)
- [ ] Write code fix tests (~4 tests)
- [ ] Verify all tests pass
- [ ] Test manually in Visual Studio
