# CCS0031: AvoidAsyncVoid

## Overview

| Property | Value |
|----------|-------|
| Rule ID | CCS0031 |
| Category | BestPractices |
| Severity | Error |
| Has Code Fix | No |
| Enabled by Default | Yes |

## Description

Async methods should return `Task` or `Task<T>` instead of `void`. Async void methods have several issues:
- Exceptions cannot be caught by the caller
- Cannot be awaited
- Difficult to test
- Can cause unobserved task exceptions

The only exception is event handlers, which must return void to match delegate signatures.

## Why This Rule?

1. **Exception Handling**: Exceptions in async void crash the application
2. **Awaitability**: Cannot await completion of async void methods
3. **Testing**: Async void methods are hard to unit test
4. **Composition**: Cannot chain or compose async void methods

## Compliant Examples

```csharp
// Good - returns Task
public async Task ProcessAsync()
{
    await Task.Delay(100);
}

// Good - returns Task<T>
public async Task<int> CalculateAsync()
{
    return await Task.FromResult(42);
}

// Good - event handler exception
private async void Button_Click(object sender, EventArgs e)
{
    await ProcessAsync();
}

// Good - ICommand.Execute implementation
public async void Execute(object parameter)
{
    // MVVM command pattern - acceptable exception
    await ProcessAsync();
}
```

## Non-Compliant Examples

```csharp
// CCS0031 - async void not allowed
public async void ProcessData()    // Should return Task
{
    await Task.Delay(100);
}

// CCS0031 - private async void not event handler
private async void DoWork()        // Should return Task
{
    await LoadDataAsync();
}

// CCS0031 - static async void
public static async void Initialize()  // Should return Task
{
    await SetupAsync();
}
```

## Implementation Specification

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCop.Sharp.Analyzers.BestPractices
{
    /// <summary>
    /// Analyzer that detects async void methods (except event handlers).
    /// </summary>
    /// <remarks>
    /// Rule ID: CCS0031
    /// Category: BestPractices
    /// Severity: Error
    ///
    /// Async void is dangerous because:
    /// - Exceptions cannot be caught
    /// - Cannot be awaited
    /// - Hard to test
    ///
    /// Exception: Event handlers must be void.
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AvoidAsyncVoidAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CCS0031";

        private static readonly LocalizableString Title = "Avoid async void";
        private static readonly LocalizableString MessageFormat =
            "Async method '{0}' returns void. Use 'Task' instead for proper exception handling and awaitability.";
        private static readonly LocalizableString Description =
            "Async void methods cannot be awaited and exceptions cannot be caught. Use async Task instead.";
        private const string Category = "BestPractices";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Error,
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

            // Check if method has async modifier
            if (!method.Modifiers.Any(SyntaxKind.AsyncKeyword))
                return;

            // Check if return type is void
            var returnType = method.ReturnType;
            if (returnType is not PredefinedTypeSyntax predefined ||
                !predefined.Keyword.IsKind(SyntaxKind.VoidKeyword))
                return;

            // Get semantic model for more checks
            var methodSymbol = context.SemanticModel.GetDeclaredSymbol(method);
            if (methodSymbol == null)
                return;

            // Skip if it's an event handler
            if (IsEventHandler(methodSymbol))
                return;

            // Skip if it implements an interface method that returns void
            // (like ICommand.Execute)
            if (IsInterfaceImplementationReturningVoid(methodSymbol))
                return;

            var diagnostic = Diagnostic.Create(
                Rule,
                method.Identifier.GetLocation(),
                method.Identifier.Text);
            context.ReportDiagnostic(diagnostic);
        }

        private static bool IsEventHandler(IMethodSymbol method)
        {
            // Standard event handler signature: (object sender, EventArgs e)
            if (method.Parameters.Length != 2)
                return false;

            var firstParam = method.Parameters[0];
            var secondParam = method.Parameters[1];

            // First parameter should be object
            if (firstParam.Type.SpecialType != SpecialType.System_Object)
                return false;

            // Second parameter should be EventArgs or derived
            var secondType = secondParam.Type;
            if (secondType.Name == "EventArgs")
                return true;

            return InheritsFrom(secondType, "System.EventArgs");
        }

        private static bool IsInterfaceImplementationReturningVoid(IMethodSymbol method)
        {
            // Check explicit implementations
            if (method.ExplicitInterfaceImplementations.Any())
                return true;

            // Check implicit implementations
            var containingType = method.ContainingType;
            foreach (var iface in containingType.AllInterfaces)
            {
                foreach (var member in iface.GetMembers().OfType<IMethodSymbol>())
                {
                    if (member.ReturnsVoid)
                    {
                        var impl = containingType.FindImplementationForInterfaceMember(member);
                        if (SymbolEqualityComparer.Default.Equals(impl, method))
                            return true;
                    }
                }
            }
            return false;
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
    }
}
```

## Decision Tree

```
┌────────────────────────────────────┐
│ Is method marked 'async'?          │
└─────────────────┬──────────────────┘
                  │
          ┌───────▼───────┐
          │      NO       │──────────► SKIP
          └───────┬───────┘
                  │ YES
                  ▼
┌────────────────────────────────────┐
│ Does method return 'void'?         │
└─────────────────┬──────────────────┘
                  │
          ┌───────▼───────┐
          │      NO       │──────────► SKIP (returns Task/Task<T>)
          └───────┬───────┘
                  │ YES
                  ▼
┌────────────────────────────────────┐
│ Is it an event handler?            │
│ (object, EventArgs parameters)     │
└─────────────────┬──────────────────┘
                  │
          ┌───────▼───────┐
          │      YES      │──────────► SKIP (event handler exception)
          └───────┬───────┘
                  │ NO
                  ▼
┌────────────────────────────────────┐
│ Is it implementing an interface    │
│ method that returns void?          │
└─────────────────┬──────────────────┘
                  │
          ┌───────▼───────┐
          │      YES      │──────────► SKIP (interface constraint)
          └───────┬───────┘
                  │ NO
                  ▼
            REPORT CCS0031
```

## Test Cases

### Should Trigger Diagnostic

| Test Name | Input | Expected |
|-----------|-------|----------|
| SimpleAsyncVoid | `async void Process() { }` | CCS0031 |
| PublicAsyncVoid | `public async void DoWork() { }` | CCS0031 |
| PrivateAsyncVoid | `private async void Helper() { }` | CCS0031 |
| StaticAsyncVoid | `static async void Init() { }` | CCS0031 |
| ProtectedAsyncVoid | `protected async void Setup() { }` | CCS0031 |

### Should NOT Trigger Diagnostic

| Test Name | Input | Expected |
|-----------|-------|----------|
| AsyncTask | `async Task ProcessAsync() { }` | No diagnostic |
| AsyncTaskT | `async Task<int> GetAsync() { }` | No diagnostic |
| EventHandler | `async void Click(object s, EventArgs e)` | No diagnostic |
| CustomEventHandler | `async void OnCustom(object s, CustomEventArgs e)` | No diagnostic |
| ICommandExecute | `async void Execute(object param)` | No diagnostic (if ICommand impl) |
| NonAsyncVoid | `void Process() { }` | No diagnostic |

## Why No Code Fix?

Changing `async void` to `async Task` is a **breaking change**:
1. All callers must be updated to await the method
2. Event handler registrations may break
3. Interface implementations may become incompatible

This requires manual analysis and cannot be safely automated.
