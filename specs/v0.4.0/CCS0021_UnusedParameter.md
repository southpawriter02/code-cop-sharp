# CCS0021: UnusedParameter

## Overview

| Property | Value |
|----------|-------|
| Rule ID | CCS0021 |
| Category | Quality |
| Severity | Warning |
| Has Code Fix | No |
| Enabled by Default | Yes |

## Description

Detects method parameters that are declared but never used within the method body. Unused parameters indicate incomplete implementation or unnecessary API surface.

### Why This Rule?

1. **API Clarity**: Parameters that aren't used confuse callers
2. **Dead Code**: May indicate incomplete implementation
3. **Maintenance**: Removing unused parameters simplifies methods
4. **Documentation**: Unused parameters are misleading in API docs

### Exclusions

This analyzer **does not** flag unused parameters in:
- **Override methods**: Must match base signature
- **Interface implementations**: Must match interface signature
- **Abstract methods**: No body to analyze
- **Extern methods**: No body to analyze
- **Partial methods**: Signature defined separately
- **Discard parameters**: Explicitly named `_`
- **Parameters with attributes**: May be used by frameworks (e.g., `[CallerMemberName]`)

---

## Compliant Examples

```csharp
public class Calculator
{
    // All parameters used
    public int Add(int a, int b)
    {
        return a + b;
    }

    // Override methods are exempt - must match base
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
    }

    // Interface implementation is exempt
    public void IDisposable.Dispose()
    {
        // Empty implementation is OK for interface
    }

    // Discard parameter is intentional
    public void HandleEvent(object sender, EventArgs _)
    {
        Console.WriteLine("Event handled");
    }

    // Parameter with attribute may be framework-used
    public void Log(string message, [CallerMemberName] string caller = "")
    {
        Console.WriteLine($"{caller}: {message}");
    }
}
```

## Non-Compliant Examples

```csharp
public class MyClass
{
    // CCS0021 on 'unusedParam'
    public void Process(string data, int unusedParam)
    {
        Console.WriteLine(data);
        // unusedParam is never used
    }

    // CCS0021 on 'c'
    public int Calculate(int a, int b, int c)
    {
        return a + b;  // c is never used
    }

    // CCS0021 on 'options'
    public MyClass(string name, Options options)
    {
        _name = name;
        // options is ignored
    }

    // CCS0021 on 'filter' (local function)
    public void DoWork()
    {
        void LocalHelper(int value, Func<int, bool> filter)
        {
            Console.WriteLine(value);
            // filter is never called
        }
    }
}
```

---

## Implementation Specification

### File Structure

```
CodeCop.Sharp/
└── Analyzers/
    └── Quality/
        └── UnusedParameterAnalyzer.cs
```

**Note**: No code fix provider for this rule. Removing a parameter is a breaking change that requires updating all call sites and cannot be safely automated.

### Analyzer Implementation

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCop.Sharp.Analyzers.Quality
{
    /// <summary>
    /// Analyzer that detects unused method parameters.
    /// </summary>
    /// <remarks>
    /// Rule ID: CCS0021
    /// Category: Quality
    /// Severity: Warning
    ///
    /// This analyzer reports parameters that are never referenced
    /// in the method body. Override methods and interface implementations
    /// are excluded.
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UnusedParameterAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        /// The diagnostic ID for this analyzer.
        /// </summary>
        public const string DiagnosticId = "CCS0021";

        private static readonly LocalizableString Title = "Unused parameter";
        private static readonly LocalizableString MessageFormat =
            "Parameter '{0}' is never used";
        private static readonly LocalizableString Description =
            "Method parameters that are never used should be removed.";
        private const string Category = "Quality";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
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

            context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeConstructor, SyntaxKind.ConstructorDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeLocalFunction, SyntaxKind.LocalFunctionStatement);
            context.RegisterSyntaxNodeAction(AnalyzeLambda, SyntaxKind.ParenthesizedLambdaExpression);
            context.RegisterSyntaxNodeAction(AnalyzeSimpleLambda, SyntaxKind.SimpleLambdaExpression);
        }

        private void AnalyzeMethod(SyntaxNodeAnalysisContext context)
        {
            var methodDeclaration = (MethodDeclarationSyntax)context.Node;

            // Skip methods without bodies
            if (methodDeclaration.Body == null && methodDeclaration.ExpressionBody == null)
                return;

            // Skip override methods - they must match base signature
            if (methodDeclaration.Modifiers.Any(SyntaxKind.OverrideKeyword))
                return;

            // Skip partial methods
            if (methodDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
                return;

            // Get the method symbol to check for interface implementation
            var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDeclaration);
            if (methodSymbol != null && IsInterfaceImplementation(methodSymbol))
                return;

            AnalyzeParameters(context, methodDeclaration.ParameterList?.Parameters,
                methodDeclaration.Body, methodDeclaration.ExpressionBody);
        }

        private void AnalyzeConstructor(SyntaxNodeAnalysisContext context)
        {
            var constructor = (ConstructorDeclarationSyntax)context.Node;

            if (constructor.Body == null && constructor.ExpressionBody == null)
                return;

            // Include parameters used in the initializer (: this() or : base())
            var initializerIdentifiers = constructor.Initializer?
                .DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .Select(id => id.Identifier.Text)
                .ToHashSet() ?? new HashSet<string>();

            AnalyzeParameters(context, constructor.ParameterList?.Parameters,
                constructor.Body, constructor.ExpressionBody, initializerIdentifiers);
        }

        private void AnalyzeLocalFunction(SyntaxNodeAnalysisContext context)
        {
            var localFunction = (LocalFunctionStatementSyntax)context.Node;

            AnalyzeParameters(context, localFunction.ParameterList?.Parameters,
                localFunction.Body, localFunction.ExpressionBody);
        }

        private void AnalyzeLambda(SyntaxNodeAnalysisContext context)
        {
            var lambda = (ParenthesizedLambdaExpressionSyntax)context.Node;

            // For lambdas, we analyze the body
            var body = lambda.Body;
            if (body == null)
                return;

            var usedIdentifiers = body.DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .Select(id => id.Identifier.Text)
                .ToHashSet();

            foreach (var parameter in lambda.ParameterList.Parameters)
            {
                if (ShouldSkipParameter(parameter))
                    continue;

                if (!usedIdentifiers.Contains(parameter.Identifier.Text))
                {
                    var diagnostic = Diagnostic.Create(
                        Rule,
                        parameter.Identifier.GetLocation(),
                        parameter.Identifier.Text);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private void AnalyzeSimpleLambda(SyntaxNodeAnalysisContext context)
        {
            var lambda = (SimpleLambdaExpressionSyntax)context.Node;

            var body = lambda.Body;
            if (body == null)
                return;

            var usedIdentifiers = body.DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .Select(id => id.Identifier.Text)
                .ToHashSet();

            // Simple lambda has single parameter
            var parameter = lambda.Parameter;
            if (parameter.Identifier.Text == "_")
                return;

            if (!usedIdentifiers.Contains(parameter.Identifier.Text))
            {
                var diagnostic = Diagnostic.Create(
                    Rule,
                    parameter.Identifier.GetLocation(),
                    parameter.Identifier.Text);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private void AnalyzeParameters(
            SyntaxNodeAnalysisContext context,
            SeparatedSyntaxList<ParameterSyntax>? parameters,
            BlockSyntax? body,
            ArrowExpressionClauseSyntax? expressionBody,
            HashSet<string>? additionalUsedIdentifiers = null)
        {
            if (parameters == null || !parameters.Value.Any())
                return;

            var bodyNode = (SyntaxNode?)body ?? expressionBody;
            if (bodyNode == null)
                return;

            // Get all identifiers used in the body
            var usedIdentifiers = bodyNode.DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .Select(id => id.Identifier.Text)
                .ToHashSet();

            // Add any additional identifiers (e.g., from constructor initializer)
            if (additionalUsedIdentifiers != null)
            {
                usedIdentifiers.UnionWith(additionalUsedIdentifiers);
            }

            foreach (var parameter in parameters.Value)
            {
                if (ShouldSkipParameter(parameter))
                    continue;

                if (!usedIdentifiers.Contains(parameter.Identifier.Text))
                {
                    var diagnostic = Diagnostic.Create(
                        Rule,
                        parameter.Identifier.GetLocation(),
                        parameter.Identifier.Text);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        /// <summary>
        /// Determines if a parameter should be skipped from analysis.
        /// </summary>
        private bool ShouldSkipParameter(ParameterSyntax parameter)
        {
            // Skip discard parameters (_)
            if (parameter.Identifier.Text == "_")
                return true;

            // Skip parameters with attributes (may be framework-used)
            if (parameter.AttributeLists.Any())
                return true;

            return false;
        }

        /// <summary>
        /// Checks if a method is an interface implementation.
        /// </summary>
        private static bool IsInterfaceImplementation(IMethodSymbol methodSymbol)
        {
            // Check explicit interface implementations
            if (methodSymbol.ExplicitInterfaceImplementations.Any())
                return true;

            // Check implicit interface implementations
            var containingType = methodSymbol.ContainingType;
            if (containingType == null)
                return false;

            foreach (var iface in containingType.AllInterfaces)
            {
                foreach (var member in iface.GetMembers().OfType<IMethodSymbol>())
                {
                    var impl = containingType.FindImplementationForInterfaceMember(member);
                    if (SymbolEqualityComparer.Default.Equals(impl, methodSymbol))
                        return true;
                }
            }

            return false;
        }
    }
}
```

---

## Decision Tree

```
┌────────────────────────────────────┐
│ Is it a method/constructor/        │
│ local function/lambda?             │
└─────────────────┬──────────────────┘
                  │
          ┌───────▼───────┐
          │      NO       │──────────► SKIP
          └───────┬───────┘
                  │ YES
                  ▼
┌────────────────────────────────────┐
│ Does it have a body?               │
│ (not abstract, extern, partial)    │
└─────────────────┬──────────────────┘
                  │
          ┌───────▼───────┐
          │      NO       │──────────► SKIP
          └───────┬───────┘
                  │ YES
                  ▼
┌────────────────────────────────────┐
│ Is it an override method?          │
└─────────────────┬──────────────────┘
                  │
          ┌───────▼───────┐
          │      YES      │──────────► SKIP (must match base)
          └───────┬───────┘
                  │ NO
                  ▼
┌────────────────────────────────────┐
│ Is it an interface implementation? │
└─────────────────┬──────────────────┘
                  │
          ┌───────▼───────┐
          │      YES      │──────────► SKIP (must match interface)
          └───────┬───────┘
                  │ NO
                  ▼
┌────────────────────────────────────┐
│ For each parameter:                │
└─────────────────┬──────────────────┘
                  │
                  ▼
┌────────────────────────────────────┐
│ Is it a discard (_)?               │
└─────────────────┬──────────────────┘
                  │
          ┌───────▼───────┐
          │      YES      │──────────► SKIP (intentional)
          └───────┬───────┘
                  │ NO
                  ▼
┌────────────────────────────────────┐
│ Does it have attributes?           │
└─────────────────┬──────────────────┘
                  │
          ┌───────▼───────┐
          │      YES      │──────────► SKIP (framework may use)
          └───────┬───────┘
                  │ NO
                  ▼
┌────────────────────────────────────┐
│ Is the parameter used in body?     │
└─────────────────┬──────────────────┘
                  │
          ┌───────▼───────┐
          │      YES      │──────────► SKIP (parameter is used)
          └───────┬───────┘
                  │ NO
                  ▼
            REPORT CCS0021
```

---

## Test Cases

### Analyzer Tests - Should Trigger Diagnostic

| Test Name | Input | Expected |
|-----------|-------|----------|
| SimpleUnused | `void M(int unused) { }` | CCS0021 |
| LastUnused | `void M(int a, int b) { Console.WriteLine(a); }` | CCS0021 on b |
| FirstUnused | `void M(int a, int b) { Console.WriteLine(b); }` | CCS0021 on a |
| AllUnused | `void M(int a, int b) { }` | CCS0021 on both |
| ConstructorUnused | `MyClass(int x) { }` | CCS0021 |
| LocalFunctionUnused | `void M() { void L(int x) { } }` | CCS0021 |
| ExpressionBodyUnused | `void M(int x) => Console.WriteLine("test");` | CCS0021 |
| LambdaUnused | `Action<int> a = (x) => { };` | CCS0021 |

### Analyzer Tests - Should NOT Trigger Diagnostic

| Test Name | Input | Expected |
|-----------|-------|----------|
| ParameterUsed | `void M(int x) { Console.WriteLine(x); }` | No diagnostic |
| OverrideMethod | `override void M(int x) { }` | No diagnostic |
| ExplicitInterface | `void IFoo.M(int x) { }` | No diagnostic |
| ImplicitInterface | Method implementing interface | No diagnostic |
| AbstractMethod | `abstract void M(int x);` | No diagnostic |
| ExternMethod | `extern void M(int x);` | No diagnostic |
| PartialMethod | `partial void M(int x);` | No diagnostic |
| DiscardParam | `void M(int _) { }` | No diagnostic |
| AttributedParam | `void M([CallerMemberName] string name = "")` | No diagnostic |
| ConstructorInitializer | `MyClass(int x) : base(x) { }` | No diagnostic |
| UsedInExpression | `void M(int x) { var y = x + 1; }` | No diagnostic |
| UsedInLinq | `void M(int x) { arr.Where(i => i > x); }` | No diagnostic |

---

## Test Code Template

```csharp
using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<
    CodeCop.Sharp.Analyzers.Quality.UnusedParameterAnalyzer>;

namespace CodeCop.Sharp.Tests.Analyzers.Quality
{
    public class UnusedParameterAnalyzerTests
    {
        [Fact]
        public async Task UnusedParameter_ShouldTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    public void Process(string data, int {|#0:unusedParam|})
    {
        System.Console.WriteLine(data);
    }
}";

            var expected = VerifyCS.Diagnostic("CCS0021")
                .WithLocation(0)
                .WithArguments("unusedParam");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task AllParametersUsed_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    public int Add(int a, int b)
    {
        return a + b;
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task OverrideMethod_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
public class BaseClass
{
    public virtual void Process(int value) { }
}

public class DerivedClass : BaseClass
{
    public override void Process(int value)
    {
        // value not used, but this is an override
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task InterfaceImplementation_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
public interface IProcessor
{
    void Process(int value);
}

public class Processor : IProcessor
{
    public void Process(int value)
    {
        // value not used, but this implements interface
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task DiscardParameter_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    public void HandleEvent(object sender, System.EventArgs _)
    {
        System.Console.WriteLine(""Event handled"");
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task ConstructorUnused_ShouldTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    private string _name;

    public MyClass(string name, int {|#0:unused|})
    {
        _name = name;
    }
}";

            var expected = VerifyCS.Diagnostic("CCS0021")
                .WithLocation(0)
                .WithArguments("unused");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task ConstructorWithInitializer_ParameterUsedInBase_ShouldNotTrigger()
        {
            var testCode = @"
public class BaseClass
{
    public BaseClass(int value) { }
}

public class DerivedClass : BaseClass
{
    public DerivedClass(int value) : base(value)
    {
        // value is used in base() call
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task LocalFunctionUnused_ShouldTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    public void Method()
    {
        void LocalHelper(int {|#0:unused|})
        {
            System.Console.WriteLine(""Hello"");
        }
    }
}";

            var expected = VerifyCS.Diagnostic("CCS0021")
                .WithLocation(0)
                .WithArguments("unused");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task ExpressionBodyUnused_ShouldTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    public void Process(int {|#0:unused|}) => System.Console.WriteLine(""test"");
}";

            var expected = VerifyCS.Diagnostic("CCS0021")
                .WithLocation(0)
                .WithArguments("unused");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }
    }
}
```

---

## Edge Cases

| Case | Behavior | Rationale |
|------|----------|-----------|
| Override methods | SKIP | Must match base signature |
| Interface implementations | SKIP | Must match interface signature |
| Abstract methods | SKIP | No body to analyze |
| Extern methods | SKIP | No body to analyze |
| Partial methods | SKIP | Signature may be in different file |
| Discard parameter `_` | SKIP | Intentionally unused |
| Parameters with attributes | SKIP | Framework may use (e.g., `[CallerMemberName]`) |
| Used in constructor initializer | NOT flagged | `: base(param)` counts as usage |
| Used in lambda inside body | NOT flagged | Lambda captures are tracked |
| Event handler patterns | Consider | `(sender, e)` is common pattern |
| Primary constructors | Handle | C# 12 feature |

---

## Why No Code Fix?

Removing a parameter is a **breaking change** for all callers. The fix cannot be automated because:

1. **Call Site Updates**: All callers must be updated
2. **Public API**: May break external consumers
3. **Reflection**: Runtime code may use the parameter
4. **Design Decision**: Developer must decide if parameter should be used or removed
5. **Documentation**: Removing changes API documentation

Developers should manually evaluate:
- Is the parameter part of a public API?
- Should the parameter be used in the implementation?
- Can all call sites be safely updated?

---

## Deliverable Checklist

- [ ] Create `Analyzers/Quality/UnusedParameterAnalyzer.cs`
- [ ] Implement method parameter analysis
- [ ] Implement constructor parameter analysis
- [ ] Implement local function parameter analysis
- [ ] Implement lambda parameter analysis
- [ ] Handle override methods (skip)
- [ ] Handle interface implementations (skip)
- [ ] Handle discard parameters (skip)
- [ ] Handle attributed parameters (skip)
- [ ] Handle constructor initializers (track usage)
- [ ] Write analyzer tests (~12 tests)
- [ ] Verify all tests pass
- [ ] Test manually in Visual Studio
