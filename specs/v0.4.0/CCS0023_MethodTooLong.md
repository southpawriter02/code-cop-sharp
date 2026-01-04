# CCS0023: MethodTooLong

## Overview

| Property | Value |
|----------|-------|
| Rule ID | CCS0023 |
| Category | Quality |
| Severity | Warning |
| Has Code Fix | No |
| Enabled by Default | Yes |
| Default Threshold | 50 lines |
| Configurable | Yes |

## Description

Detects methods that exceed a configurable line threshold. Long methods are harder to understand, test, and maintain. This rule encourages breaking down complex methods into smaller, focused functions.

### Why This Rule?

1. **Readability**: Shorter methods are easier to understand at a glance
2. **Testability**: Small methods are easier to unit test
3. **Maintainability**: Changes are localized to smaller code units
4. **Single Responsibility**: Long methods often do too many things
5. **Code Review**: Shorter methods are easier to review

### Default Threshold

The default threshold is **50 lines**, which is a commonly accepted maximum for method length. This can be customized via `.editorconfig`.

---

## Configuration

Configure the threshold via `.editorconfig`:

```ini
[*.cs]
# CCS0023: Method line threshold (default: 50)
dotnet_diagnostic.CCS0023.max_lines = 50

# Example: More permissive threshold
dotnet_diagnostic.CCS0023.max_lines = 100

# Example: Stricter threshold
dotnet_diagnostic.CCS0023.max_lines = 30
```

---

## Compliant Examples

```csharp
public class OrderProcessor
{
    // Good - focused method under threshold
    public void ProcessOrder(Order order)
    {
        ValidateOrder(order);
        CalculateTotals(order);
        ApplyDiscounts(order);
        SaveOrder(order);
        SendConfirmation(order);
    }

    // Good - each helper method is small and focused
    private void ValidateOrder(Order order)
    {
        if (order == null)
            throw new ArgumentNullException(nameof(order));

        if (!order.Items.Any())
            throw new InvalidOperationException("Order must have items");
    }

    private void CalculateTotals(Order order)
    {
        order.Subtotal = order.Items.Sum(i => i.Price * i.Quantity);
        order.Tax = order.Subtotal * TaxRate;
        order.Total = order.Subtotal + order.Tax;
    }

    // ... other small methods
}
```

## Non-Compliant Examples

```csharp
public class LegacyProcessor
{
    // CCS0023 - Method has 75 lines, exceeds 50 line threshold
    public void ProcessEverything(Data data)
    {
        // Line 1-10: Validation
        if (data == null) throw new ArgumentNullException();
        if (data.Items == null) throw new ArgumentException();
        // ... more validation

        // Line 11-30: Processing step 1
        foreach (var item in data.Items)
        {
            // ... complex processing
        }

        // Line 31-50: Processing step 2
        var results = new List<Result>();
        foreach (var item in data.Items)
        {
            // ... more processing
        }

        // Line 51-70: Cleanup and finalization
        foreach (var result in results)
        {
            // ... cleanup logic
        }

        // Line 71-75: Logging
        _logger.Log("Processing complete");
        // ... more logging
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
        └── MethodTooLongAnalyzer.cs
```

**Note**: No code fix provider for this rule. Refactoring long methods requires human judgment about how to split the logic.

### Analyzer Implementation

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace CodeCop.Sharp.Analyzers.Quality
{
    /// <summary>
    /// Analyzer that detects methods exceeding a line threshold.
    /// </summary>
    /// <remarks>
    /// Rule ID: CCS0023
    /// Category: Quality
    /// Severity: Warning
    ///
    /// The threshold is configurable via .editorconfig:
    /// dotnet_diagnostic.CCS0023.max_lines = 50
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MethodTooLongAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        /// The diagnostic ID for this analyzer.
        /// </summary>
        public const string DiagnosticId = "CCS0023";

        /// <summary>
        /// The default maximum number of lines allowed in a method.
        /// </summary>
        public const int DefaultMaxLines = 50;

        private static readonly LocalizableString Title = "Method too long";
        private static readonly LocalizableString MessageFormat =
            "Method '{0}' has {1} lines, exceeding the maximum of {2}";
        private static readonly LocalizableString Description =
            "Methods should be concise. Consider breaking long methods into smaller, focused methods.";
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
            context.RegisterSyntaxNodeAction(AnalyzeOperator, SyntaxKind.OperatorDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeConversionOperator, SyntaxKind.ConversionOperatorDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeAccessor, SyntaxKind.GetAccessorDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeAccessor, SyntaxKind.SetAccessorDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeAccessor, SyntaxKind.InitAccessorDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeAccessor, SyntaxKind.AddAccessorDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeAccessor, SyntaxKind.RemoveAccessorDeclaration);
        }

        private void AnalyzeMethod(SyntaxNodeAnalysisContext context)
        {
            var methodDeclaration = (MethodDeclarationSyntax)context.Node;

            // Skip methods without bodies (abstract, extern, partial)
            if (methodDeclaration.Body == null && methodDeclaration.ExpressionBody == null)
                return;

            var maxLines = GetMaxLines(context);
            var lineCount = GetLineCount(methodDeclaration);

            if (lineCount > maxLines)
            {
                var diagnostic = Diagnostic.Create(
                    Rule,
                    methodDeclaration.Identifier.GetLocation(),
                    methodDeclaration.Identifier.Text,
                    lineCount,
                    maxLines);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private void AnalyzeConstructor(SyntaxNodeAnalysisContext context)
        {
            var constructor = (ConstructorDeclarationSyntax)context.Node;

            if (constructor.Body == null && constructor.ExpressionBody == null)
                return;

            var maxLines = GetMaxLines(context);
            var lineCount = GetLineCount(constructor);

            if (lineCount > maxLines)
            {
                var diagnostic = Diagnostic.Create(
                    Rule,
                    constructor.Identifier.GetLocation(),
                    constructor.Identifier.Text,
                    lineCount,
                    maxLines);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private void AnalyzeOperator(SyntaxNodeAnalysisContext context)
        {
            var operatorDeclaration = (OperatorDeclarationSyntax)context.Node;

            if (operatorDeclaration.Body == null && operatorDeclaration.ExpressionBody == null)
                return;

            var maxLines = GetMaxLines(context);
            var lineCount = GetLineCount(operatorDeclaration);

            if (lineCount > maxLines)
            {
                var operatorName = $"operator {operatorDeclaration.OperatorToken.Text}";
                var diagnostic = Diagnostic.Create(
                    Rule,
                    operatorDeclaration.OperatorToken.GetLocation(),
                    operatorName,
                    lineCount,
                    maxLines);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private void AnalyzeConversionOperator(SyntaxNodeAnalysisContext context)
        {
            var conversionOperator = (ConversionOperatorDeclarationSyntax)context.Node;

            if (conversionOperator.Body == null && conversionOperator.ExpressionBody == null)
                return;

            var maxLines = GetMaxLines(context);
            var lineCount = GetLineCount(conversionOperator);

            if (lineCount > maxLines)
            {
                var operatorName = $"{conversionOperator.ImplicitOrExplicitKeyword.Text} operator";
                var diagnostic = Diagnostic.Create(
                    Rule,
                    conversionOperator.Type.GetLocation(),
                    operatorName,
                    lineCount,
                    maxLines);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private void AnalyzeAccessor(SyntaxNodeAnalysisContext context)
        {
            var accessor = (AccessorDeclarationSyntax)context.Node;

            if (accessor.Body == null && accessor.ExpressionBody == null)
                return;

            var maxLines = GetMaxLines(context);
            var lineCount = GetLineCount(accessor);

            if (lineCount > maxLines)
            {
                // Get the property/event name for the message
                var propertyName = GetAccessorContainerName(accessor);
                var accessorType = accessor.Keyword.Text;
                var fullName = $"{propertyName}.{accessorType}";

                var diagnostic = Diagnostic.Create(
                    Rule,
                    accessor.Keyword.GetLocation(),
                    fullName,
                    lineCount,
                    maxLines);
                context.ReportDiagnostic(diagnostic);
            }
        }

        /// <summary>
        /// Gets the maximum line count from configuration or returns default.
        /// </summary>
        private static int GetMaxLines(SyntaxNodeAnalysisContext context)
        {
            var options = context.Options.AnalyzerConfigOptionsProvider
                .GetOptions(context.Node.SyntaxTree);

            if (options.TryGetValue("dotnet_diagnostic.CCS0023.max_lines", out var value) &&
                int.TryParse(value, out var maxLines) &&
                maxLines > 0)
            {
                return maxLines;
            }

            return DefaultMaxLines;
        }

        /// <summary>
        /// Calculates the number of lines in a syntax node.
        /// </summary>
        private static int GetLineCount(SyntaxNode node)
        {
            var lineSpan = node.GetLocation().GetLineSpan();
            return lineSpan.EndLinePosition.Line - lineSpan.StartLinePosition.Line + 1;
        }

        /// <summary>
        /// Gets the name of the property or event containing the accessor.
        /// </summary>
        private static string GetAccessorContainerName(AccessorDeclarationSyntax accessor)
        {
            var parent = accessor.Parent?.Parent;
            return parent switch
            {
                PropertyDeclarationSyntax property => property.Identifier.Text,
                IndexerDeclarationSyntax _ => "this[]",
                EventDeclarationSyntax eventDecl => eventDecl.Identifier.Text,
                _ => "accessor"
            };
        }
    }
}
```

---

## Decision Tree

```
┌────────────────────────────────────┐
│ Is it a method-like member?        │
│ (method, constructor, operator,    │
│  accessor)                         │
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
│ Get max_lines from .editorconfig   │
│ or use default (50)                │
└─────────────────┬──────────────────┘
                  │
                  ▼
┌────────────────────────────────────┐
│ Calculate method line count        │
│ (end line - start line + 1)        │
└─────────────────┬──────────────────┘
                  │
                  ▼
┌────────────────────────────────────┐
│ Is line count > max_lines?         │
└─────────────────┬──────────────────┘
                  │
          ┌───────▼───────┐
          │      NO       │──────────► SKIP (within limit)
          └───────┬───────┘
                  │ YES
                  ▼
            REPORT CCS0023
       (with actual count and max)
```

---

## Line Counting Method

The line count includes:
- Method signature line(s)
- Opening brace `{`
- All lines within the body
- Closing brace `}`
- Any blank lines within the method

### Example Line Count

```csharp
public void Example()        // Line 1
{                            // Line 2
    var x = 1;               // Line 3
    var y = 2;               // Line 4
                             // Line 5 (blank)
    Console.WriteLine(x);    // Line 6
}                            // Line 7
// Total: 7 lines
```

---

## Test Cases

### Analyzer Tests - Should Trigger Diagnostic

| Test Name | Lines | Threshold | Expected |
|-----------|-------|-----------|----------|
| OverDefault | 51 | 50 (default) | CCS0023: "51 lines, exceeding 50" |
| WayOver | 100 | 50 | CCS0023: "100 lines, exceeding 50" |
| OverCustom | 35 | 30 | CCS0023: "35 lines, exceeding 30" |
| Constructor | 60 | 50 | CCS0023 on constructor |
| Operator | 55 | 50 | CCS0023 on operator |
| PropertyGetter | 60 | 50 | CCS0023 on get accessor |

### Analyzer Tests - Should NOT Trigger Diagnostic

| Test Name | Lines | Threshold | Expected |
|-----------|-------|-----------|----------|
| UnderDefault | 30 | 50 | No diagnostic |
| AtThreshold | 50 | 50 | No diagnostic |
| AtCustomThreshold | 30 | 30 | No diagnostic |
| AbstractMethod | N/A | 50 | No diagnostic (no body) |
| ExternMethod | N/A | 50 | No diagnostic (no body) |
| PartialMethod | N/A | 50 | No diagnostic (no body) |
| InterfaceMethod | N/A | 50 | No diagnostic (no body) |
| ExpressionBody | 1 | 50 | No diagnostic |

---

## Test Code Template

```csharp
using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<
    CodeCop.Sharp.Analyzers.Quality.MethodTooLongAnalyzer>;

namespace CodeCop.Sharp.Tests.Analyzers.Quality
{
    public class MethodTooLongAnalyzerTests
    {
        [Fact]
        public async Task MethodUnderThreshold_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    public void ShortMethod()
    {
        var x = 1;
        var y = 2;
        System.Console.WriteLine(x + y);
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task MethodOverThreshold_ShouldTriggerDiagnostic()
        {
            // Generate a method with 55 lines (over default 50)
            var lines = new System.Text.StringBuilder();
            lines.AppendLine(@"
public class MyClass
{
    public void {|#0:LongMethod|}()
    {");
            for (int i = 0; i < 50; i++)
            {
                lines.AppendLine($"        var line{i} = {i};");
            }
            lines.AppendLine(@"    }
}");

            var expected = VerifyCS.Diagnostic("CCS0023")
                .WithLocation(0)
                .WithArguments("LongMethod", 55, 50);
            await VerifyCS.VerifyAnalyzerAsync(lines.ToString(), expected);
        }

        [Fact]
        public async Task MethodAtThreshold_ShouldNotTriggerDiagnostic()
        {
            // Generate a method with exactly 50 lines
            var lines = new System.Text.StringBuilder();
            lines.AppendLine(@"
public class MyClass
{
    public void ExactMethod()
    {");
            for (int i = 0; i < 45; i++)
            {
                lines.AppendLine($"        var line{i} = {i};");
            }
            lines.AppendLine(@"    }
}");

            await VerifyCS.VerifyAnalyzerAsync(lines.ToString());
        }

        [Fact]
        public async Task AbstractMethod_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
public abstract class MyClass
{
    public abstract void AbstractMethod();
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task ExpressionBodyMethod_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    public int GetValue() => 42;
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task ConstructorOverThreshold_ShouldTriggerDiagnostic()
        {
            var lines = new System.Text.StringBuilder();
            lines.AppendLine(@"
public class MyClass
{
    public {|#0:MyClass|}()
    {");
            for (int i = 0; i < 50; i++)
            {
                lines.AppendLine($"        var line{i} = {i};");
            }
            lines.AppendLine(@"    }
}");

            var expected = VerifyCS.Diagnostic("CCS0023")
                .WithLocation(0)
                .WithArguments("MyClass", 55, 50);
            await VerifyCS.VerifyAnalyzerAsync(lines.ToString(), expected);
        }

        [Fact]
        public async Task PropertyGetterOverThreshold_ShouldTriggerDiagnostic()
        {
            var lines = new System.Text.StringBuilder();
            lines.AppendLine(@"
public class MyClass
{
    public int Value
    {
        {|#0:get|}
        {");
            for (int i = 0; i < 50; i++)
            {
                lines.AppendLine($"            var line{i} = {i};");
            }
            lines.AppendLine(@"            return 0;
        }
    }
}");

            var expected = VerifyCS.Diagnostic("CCS0023")
                .WithLocation(0)
                .WithArguments("Value.get", 55, 50);
            await VerifyCS.VerifyAnalyzerAsync(lines.ToString(), expected);
        }
    }
}
```

---

## Edge Cases

| Case | Behavior | Rationale |
|------|----------|-----------|
| Expression-bodied method | Usually OK | Single-line expression bodies are short |
| Method with region | Count all lines | Regions don't reduce complexity |
| Method with comments | Count all lines | Comments add to visual length |
| Method with blank lines | Count all lines | Blank lines add to visual length |
| Nested methods (local functions) | Count parent only | Local functions are separate entities |
| Partial methods | Skip if no body | May be implemented elsewhere |
| Interface methods | Skip | No implementation |

---

## Why No Code Fix?

Refactoring long methods requires human judgment:

1. **Logic Grouping**: How to split the logic meaningfully
2. **Naming**: What to call extracted methods
3. **State Management**: Which variables to pass vs. make fields
4. **Return Values**: How to handle multiple outputs
5. **Side Effects**: Understanding data flow
6. **Architecture**: May require broader refactoring

The analyzer identifies the problem; the developer must design the solution.

---

## Refactoring Suggestions

When you see CCS0023, consider:

1. **Extract Method**: Move logical chunks to separate methods
2. **Extract Class**: If method does too many things, consider new class
3. **Replace Conditional with Polymorphism**: Long switch/if chains
4. **Use Collection Methods**: Replace loops with LINQ
5. **Introduce Parameter Object**: If passing many related values

---

## Deliverable Checklist

- [ ] Create `Analyzers/Quality/MethodTooLongAnalyzer.cs`
- [ ] Implement method declaration analysis
- [ ] Implement constructor analysis
- [ ] Implement operator analysis
- [ ] Implement accessor analysis
- [ ] Implement .editorconfig threshold reading
- [ ] Calculate line count correctly
- [ ] Skip methods without bodies
- [ ] Write analyzer tests (~8 tests)
- [ ] Write configuration tests (~3 tests)
- [ ] Verify all tests pass
- [ ] Test manually in Visual Studio
