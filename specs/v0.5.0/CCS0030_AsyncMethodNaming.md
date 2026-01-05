# CCS0030: AsyncMethodNaming

## Overview

| Property | Value |
|----------|-------|
| Rule ID | CCS0030 |
| Category | BestPractices |
| Severity | Warning |
| Has Code Fix | Yes |
| Enabled by Default | Yes |

## Description

Async methods that return `Task`, `Task<T>`, `ValueTask`, or `ValueTask<T>` should have names ending with "Async". This naming convention makes it clear to callers that the method is asynchronous and should be awaited.

### Why This Rule?

1. **Clarity**: The "Async" suffix signals asynchronous behavior
2. **Consistency**: Standard .NET naming convention
3. **Discoverability**: Easy to identify async methods in IntelliSense
4. **Avoiding Confusion**: Prevents mixing sync/async methods with same name

---

## Configuration

This rule has no configurable options.

---

## Compliant Examples

```csharp
// Good - async method with Async suffix
public async Task LoadDataAsync()
{
    await Task.Delay(100);
}

public async Task<int> GetCountAsync()
{
    return await Task.FromResult(42);
}

public async ValueTask<string> FetchAsync()
{
    return await new ValueTask<string>("data");
}

// Good - Main entry point exception
public static async Task Main(string[] args)
{
    await RunAsync();
}

// Good - Event handler exception
private async void Button_Click(object sender, EventArgs e)
{
    await HandleClickAsync();
}
```

## Non-Compliant Examples

```csharp
// CCS0030 - async method without Async suffix
public async Task LoadData()        // Should be LoadDataAsync
{
    await Task.Delay(100);
}

public async Task<int> GetCount()   // Should be GetCountAsync
{
    return await Task.FromResult(42);
}

public async ValueTask<string> Fetch()  // Should be FetchAsync
{
    return await new ValueTask<string>("data");
}
```

---

## Implementation Specification

### File Structure

```
CodeCop.Sharp/
├── Analyzers/BestPractices/AsyncMethodNamingAnalyzer.cs
└── CodeFixes/BestPractices/AsyncMethodNamingCodeFixProvider.cs
```

### Analyzer Implementation

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace CodeCop.Sharp.Analyzers.BestPractices
{
    /// <summary>
    /// Analyzer that ensures async methods end with 'Async' suffix.
    /// </summary>
    /// <remarks>
    /// Rule ID: CCS0030
    /// Category: BestPractices
    /// Severity: Warning
    ///
    /// Exceptions:
    /// - Main entry point methods
    /// - Event handlers (async void with standard event handler signature)
    /// - Interface implementations (must match interface)
    /// - Override methods (must match base class)
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AsyncMethodNamingAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CCS0030";
        private const string AsyncSuffix = "Async";

        private static readonly LocalizableString Title = "Async method naming";
        private static readonly LocalizableString MessageFormat =
            "Async method '{0}' should end with 'Async' suffix";
        private static readonly LocalizableString Description =
            "Methods that return Task, Task<T>, ValueTask, or ValueTask<T> should have names ending with 'Async'.";
        private const string Category = "BestPractices";

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

            context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
        }

        private void AnalyzeMethod(SyntaxNodeAnalysisContext context)
        {
            var method = (MethodDeclarationSyntax)context.Node;
            var methodName = method.Identifier.Text;

            // Skip if already ends with Async
            if (methodName.EndsWith(AsyncSuffix))
                return;

            // Skip Main entry points
            if (methodName == "Main")
                return;

            // Get semantic model to check return type
            var methodSymbol = context.SemanticModel.GetDeclaredSymbol(method);
            if (methodSymbol == null)
                return;

            // Skip override methods - they must match base
            if (methodSymbol.IsOverride)
                return;

            // Skip interface implementations
            if (IsInterfaceImplementation(methodSymbol))
                return;

            // Check if method returns async type
            if (!IsAsyncReturnType(methodSymbol.ReturnType))
                return;

            // Skip event handlers (async void with EventArgs parameter)
            if (IsEventHandler(methodSymbol))
                return;

            var diagnostic = Diagnostic.Create(
                Rule,
                method.Identifier.GetLocation(),
                methodName);
            context.ReportDiagnostic(diagnostic);
        }

        private static bool IsAsyncReturnType(ITypeSymbol returnType)
        {
            if (returnType == null)
                return false;

            var typeName = returnType.ToDisplayString();

            // Check for Task types
            if (typeName == "System.Threading.Tasks.Task" ||
                typeName.StartsWith("System.Threading.Tasks.Task<"))
                return true;

            // Check for ValueTask types
            if (typeName == "System.Threading.Tasks.ValueTask" ||
                typeName.StartsWith("System.Threading.Tasks.ValueTask<"))
                return true;

            // Check for IAsyncEnumerable
            if (typeName.StartsWith("System.Collections.Generic.IAsyncEnumerable<"))
                return true;

            return false;
        }

        private static bool IsEventHandler(IMethodSymbol method)
        {
            // async void + (object, EventArgs) signature = event handler
            if (!method.ReturnsVoid)
                return false;

            if (!method.IsAsync)
                return false;

            if (method.Parameters.Length != 2)
                return false;

            var secondParam = method.Parameters[1].Type;
            return secondParam.Name == "EventArgs" ||
                   InheritsFrom(secondParam, "System.EventArgs");
        }

        private static bool InheritsFrom(ITypeSymbol type, string baseTypeName)
        {
            var current = type.BaseType;
            while (current != null)
            {
                if (current.ToDisplayString() == baseTypeName)
                    return true;
                current = current.BaseType;
            }
            return false;
        }

        private static bool IsInterfaceImplementation(IMethodSymbol method)
        {
            if (method.ExplicitInterfaceImplementations.Length > 0)
                return true;

            var containingType = method.ContainingType;
            foreach (var iface in containingType.AllInterfaces)
            {
                foreach (var member in iface.GetMembers().OfType<IMethodSymbol>())
                {
                    var impl = containingType.FindImplementationForInterfaceMember(member);
                    if (SymbolEqualityComparer.Default.Equals(impl, method))
                        return true;
                }
            }
            return false;
        }
    }
}
```

### Code Fix Provider

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCop.Sharp.CodeFixes.BestPractices
{
    /// <summary>
    /// Code fix provider for CCS0030 (AsyncMethodNaming).
    /// Appends 'Async' suffix to method name.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AsyncMethodNamingCodeFixProvider)), Shared]
    public class AsyncMethodNamingCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(AsyncMethodNamingAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
            => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var methodDeclaration = root.FindToken(diagnosticSpan.Start)
                .Parent?.AncestorsAndSelf()
                .OfType<MethodDeclarationSyntax>()
                .First();

            if (methodDeclaration == null)
                return;

            var newName = methodDeclaration.Identifier.Text + "Async";

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: $"Rename to '{newName}'",
                    createChangedSolution: c => RenameMethodAsync(context.Document, methodDeclaration, newName, c),
                    equivalenceKey: "AddAsyncSuffix"),
                diagnostic);
        }

        private async Task<Solution> RenameMethodAsync(
            Document document,
            MethodDeclarationSyntax methodDeclaration,
            string newName,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration, cancellationToken);

            if (methodSymbol == null)
                return document.Project.Solution;

            var solution = document.Project.Solution;

            return await Renamer.RenameSymbolAsync(
                solution,
                methodSymbol,
                new SymbolRenameOptions(),
                newName,
                cancellationToken).ConfigureAwait(false);
        }
    }
}
```

---

## Decision Tree

```
┌────────────────────────────────────┐
│ Is it a method declaration?        │
└─────────────────┬──────────────────┘
                  │
          ┌───────▼───────┐
          │      NO       │──────────► SKIP
          └───────┬───────┘
                  │ YES
                  ▼
┌────────────────────────────────────┐
│ Does name already end with 'Async'?│
└─────────────────┬──────────────────┘
                  │
          ┌───────▼───────┐
          │      YES      │──────────► SKIP (already compliant)
          └───────┬───────┘
                  │ NO
                  ▼
┌────────────────────────────────────┐
│ Is method named 'Main'?            │
└─────────────────┬──────────────────┘
                  │
          ┌───────▼───────┐
          │      YES      │──────────► SKIP (entry point exception)
          └───────┬───────┘
                  │ NO
                  ▼
┌────────────────────────────────────┐
│ Is method an override?             │
└─────────────────┬──────────────────┘
                  │
          ┌───────▼───────┐
          │      YES      │──────────► SKIP (must match base)
          └───────┬───────┘
                  │ NO
                  ▼
┌────────────────────────────────────┐
│ Is method an interface impl?       │
└─────────────────┬──────────────────┘
                  │
          ┌───────▼───────┐
          │      YES      │──────────► SKIP (must match interface)
          └───────┬───────┘
                  │ NO
                  ▼
┌────────────────────────────────────┐
│ Does method return Task/ValueTask? │
└─────────────────┬──────────────────┘
                  │
          ┌───────▼───────┐
          │      NO       │──────────► SKIP (not async return type)
          └───────┬───────┘
                  │ YES
                  ▼
┌────────────────────────────────────┐
│ Is method an event handler?        │
│ (async void + EventArgs param)     │
└─────────────────┬──────────────────┘
                  │
          ┌───────▼───────┐
          │      YES      │──────────► SKIP (event handler exception)
          └───────┬───────┘
                  │ NO
                  ▼
            REPORT CCS0030
```

---

## Test Cases

### Analyzer Tests - Should Trigger Diagnostic

| Test Name | Input | Expected |
|-----------|-------|----------|
| TaskMethod | `public async Task LoadData() { }` | CCS0030 |
| TaskTMethod | `public async Task<int> GetCount() { }` | CCS0030 |
| ValueTaskMethod | `public async ValueTask Process() { }` | CCS0030 |
| ValueTaskTMethod | `public ValueTask<string> Fetch() { }` | CCS0030 |
| NonAsyncTask | `public Task Run() { return Task.CompletedTask; }` | CCS0030 |
| IAsyncEnumerable | `public IAsyncEnumerable<int> Stream() { }` | CCS0030 |

### Analyzer Tests - Should NOT Trigger Diagnostic

| Test Name | Input | Expected |
|-----------|-------|----------|
| AlreadyAsync | `public async Task LoadDataAsync() { }` | No diagnostic |
| MainMethod | `public static async Task Main() { }` | No diagnostic |
| Override | `public override async Task Process() { }` | No diagnostic |
| InterfaceImpl | Interface implementation | No diagnostic |
| EventHandler | `async void Button_Click(object s, EventArgs e)` | No diagnostic |
| VoidMethod | `public void Process() { }` | No diagnostic |
| IntMethod | `public int Calculate() { }` | No diagnostic |

---

## Test Code Template

```csharp
using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<
    CodeCop.Sharp.Analyzers.BestPractices.AsyncMethodNamingAnalyzer>;

namespace CodeCop.Sharp.Tests.Analyzers.BestPractices
{
    public class AsyncMethodNamingAnalyzerTests
    {
        [Fact]
        public async Task AsyncMethod_WithoutAsyncSuffix_ShouldTriggerDiagnostic()
        {
            var testCode = @"
using System.Threading.Tasks;

public class MyClass
{
    public async Task {|#0:LoadData|}()
    {
        await Task.Delay(100);
    }
}";

            var expected = VerifyCS.Diagnostic("CCS0030")
                .WithLocation(0)
                .WithArguments("LoadData");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task AsyncMethod_WithAsyncSuffix_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
using System.Threading.Tasks;

public class MyClass
{
    public async Task LoadDataAsync()
    {
        await Task.Delay(100);
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task MainMethod_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
using System.Threading.Tasks;

public class Program
{
    public static async Task Main(string[] args)
    {
        await Task.Delay(100);
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task EventHandler_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
using System;
using System.Threading.Tasks;

public class MyClass
{
    private async void Button_Click(object sender, EventArgs e)
    {
        await Task.Delay(100);
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }
    }
}
```

---

## Edge Cases

| Case | Behavior | Rationale |
|------|----------|-----------|
| Method already ends with "async" (lowercase) | Report | Suffix must be "Async" (PascalCase) |
| Lambda expressions | Not analyzed | Lambdas don't have names |
| Local functions | Analyze | Local functions should follow convention |
| Partial methods | Analyze | Partial methods have names |
| Generic async methods | Analyze | `Task<T>` is still async |

---

## Deliverable Checklist

- [ ] Create `Analyzers/BestPractices/AsyncMethodNamingAnalyzer.cs`
- [ ] Create `CodeFixes/BestPractices/AsyncMethodNamingCodeFixProvider.cs`
- [ ] Write analyzer tests (~15 tests)
- [ ] Write code fix tests (~5 tests)
- [ ] Verify all tests pass
- [ ] Test manually in Visual Studio
