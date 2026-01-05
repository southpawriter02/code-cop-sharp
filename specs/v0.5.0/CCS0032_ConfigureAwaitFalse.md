# CCS0032: ConfigureAwaitFalse

## Overview

| Property | Value |
|----------|-------|
| Rule ID | CCS0032 |
| Category | BestPractices |
| Severity | Info |
| Has Code Fix | Yes |
| Enabled by Default | Yes (library mode only) |
| Configurable | Yes (library vs application mode) |

## Description

In library code, `await` expressions should use `.ConfigureAwait(false)` to avoid deadlocks and improve performance. Application code (UI apps, ASP.NET Core) should NOT use ConfigureAwait(false) as it can cause issues with context-dependent code.

## Configuration

```ini
# .editorconfig
[*.cs]
# Detect library vs application automatically (default)
dotnet_diagnostic.CCS0032.mode = auto

# Force library mode (always suggest ConfigureAwait)
dotnet_diagnostic.CCS0032.mode = library

# Force application mode (never suggest ConfigureAwait)
dotnet_diagnostic.CCS0032.mode = application
```

## Library Detection Logic

A project is considered a **library** if:
1. Output type is "Library" (DLL)
2. Does NOT reference ASP.NET Core packages
3. Does NOT reference WPF/WinForms/MAUI packages
4. Does NOT reference Xamarin packages

## Compliant Examples

```csharp
// Good - library code with ConfigureAwait(false)
public async Task<string> FetchDataAsync()
{
    var response = await _client.GetAsync(url).ConfigureAwait(false);
    var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
    return content;
}

// Good - application code without ConfigureAwait
// (in ASP.NET Core or UI application)
public async Task UpdateUIAsync()
{
    var data = await LoadDataAsync();  // No ConfigureAwait needed
    label.Text = data;                 // Needs UI context
}
```

## Non-Compliant Examples

```csharp
// In library code:

// CCS0032 - missing ConfigureAwait(false)
public async Task<string> FetchAsync()
{
    var result = await _client.GetAsync(url);  // Missing ConfigureAwait(false)
    return await result.Content.ReadAsStringAsync();  // Missing ConfigureAwait(false)
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
    /// Analyzer that suggests ConfigureAwait(false) for library code.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ConfigureAwaitFalseAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CCS0032";

        private static readonly LocalizableString Title = "Use ConfigureAwait(false)";
        private static readonly LocalizableString MessageFormat =
            "Add ConfigureAwait(false) to prevent potential deadlocks in library code";
        private static readonly LocalizableString Description =
            "Library code should use ConfigureAwait(false) on awaited tasks to avoid deadlocks.";
        private const string Category = "BestPractices";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(compilationContext =>
            {
                var mode = GetMode(compilationContext);
                if (mode == ProjectMode.Application)
                    return;  // Don't analyze application code

                compilationContext.RegisterSyntaxNodeAction(
                    ctx => AnalyzeAwait(ctx),
                    SyntaxKind.AwaitExpression);
            });
        }

        private void AnalyzeAwait(SyntaxNodeAnalysisContext context)
        {
            var awaitExpression = (AwaitExpressionSyntax)context.Node;
            var expression = awaitExpression.Expression;

            // Skip if already has ConfigureAwait
            if (HasConfigureAwait(expression))
                return;

            // Skip if not a Task/ValueTask
            var typeInfo = context.SemanticModel.GetTypeInfo(expression);
            if (!IsAwaitableType(typeInfo.Type))
                return;

            var diagnostic = Diagnostic.Create(
                Rule,
                awaitExpression.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }

        private static bool HasConfigureAwait(ExpressionSyntax expression)
        {
            if (expression is InvocationExpressionSyntax invocation)
            {
                if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                {
                    return memberAccess.Name.Identifier.Text == "ConfigureAwait";
                }
            }
            return false;
        }

        private static bool IsAwaitableType(ITypeSymbol? type)
        {
            if (type == null) return false;

            var typeName = type.ToDisplayString();
            return typeName.StartsWith("System.Threading.Tasks.Task") ||
                   typeName.StartsWith("System.Threading.Tasks.ValueTask");
        }

        private static ProjectMode GetMode(CompilationStartAnalysisContext context)
        {
            // Check for configuration override
            var options = context.Options.AnalyzerConfigOptionsProvider;

            // Check referenced assemblies for UI/Web frameworks
            var compilation = context.Compilation;
            foreach (var reference in compilation.ReferencedAssemblyNames)
            {
                var name = reference.Name;
                if (name.StartsWith("Microsoft.AspNetCore") ||
                    name.StartsWith("System.Windows.Forms") ||
                    name == "PresentationFramework" ||
                    name.StartsWith("Microsoft.Maui") ||
                    name.StartsWith("Xamarin"))
                {
                    return ProjectMode.Application;
                }
            }

            return ProjectMode.Library;
        }

        private enum ProjectMode
        {
            Auto,
            Library,
            Application
        }
    }
}
```

## Code Fix Provider

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

namespace CodeCop.Sharp.CodeFixes.BestPractices
{
    /// <summary>
    /// Code fix provider for CCS0032 (ConfigureAwaitFalse).
    /// Adds .ConfigureAwait(false) to await expressions.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConfigureAwaitFalseCodeFixProvider)), Shared]
    public class ConfigureAwaitFalseCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(ConfigureAwaitFalseAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
            => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var awaitExpression = root.FindNode(diagnosticSpan)
                .AncestorsAndSelf()
                .OfType<AwaitExpressionSyntax>()
                .First();

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Add ConfigureAwait(false)",
                    createChangedDocument: c => AddConfigureAwaitAsync(context.Document, awaitExpression, c),
                    equivalenceKey: "AddConfigureAwait"),
                diagnostic);
        }

        private async Task<Document> AddConfigureAwaitAsync(
            Document document,
            AwaitExpressionSyntax awaitExpression,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // Create .ConfigureAwait(false)
            var configureAwaitInvocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    awaitExpression.Expression,
                    SyntaxFactory.IdentifierName("ConfigureAwait")),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(
                            SyntaxFactory.LiteralExpression(
                                SyntaxKind.FalseLiteralExpression)))));

            var newAwait = awaitExpression.WithExpression(configureAwaitInvocation);
            var newRoot = root.ReplaceNode(awaitExpression, newAwait);

            return document.WithSyntaxRoot(newRoot);
        }
    }
}
```

## Test Cases

### Should Trigger Diagnostic (Library Mode)

| Test Name | Input | Expected |
|-----------|-------|----------|
| SimpleAwait | `await Task.Delay(100);` | CCS0032 |
| AwaitMethodCall | `await _service.GetAsync();` | CCS0032 |
| AwaitInLoop | `await item.ProcessAsync();` | CCS0032 |
| ChainedAwait | `await (await GetAsync()).ReadAsync();` | CCS0032 x2 |

### Should NOT Trigger Diagnostic

| Test Name | Input | Expected |
|-----------|-------|----------|
| HasConfigureAwait | `await task.ConfigureAwait(false);` | No diagnostic |
| ConfigureAwaitTrue | `await task.ConfigureAwait(true);` | No diagnostic |
| ApplicationMode | Any await in app project | No diagnostic |
| NonTaskAwait | Custom awaitable without ConfigureAwait | No diagnostic |
