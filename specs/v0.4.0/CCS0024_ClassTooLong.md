# CCS0024: ClassTooLong

## Overview

| Property | Value |
|----------|-------|
| Rule ID | CCS0024 |
| Category | Quality |
| Severity | Warning |
| Has Code Fix | No |
| Enabled by Default | Yes |
| Default Threshold | 500 lines |
| Configurable | Yes |

## Description

Detects classes (and structs) that exceed a configurable line threshold. Large classes often violate the Single Responsibility Principle and are harder to understand, test, and maintain.

### Why This Rule?

1. **Single Responsibility**: Large classes often do too many things
2. **Maintainability**: Changes in large classes have more risk
3. **Testability**: Large classes are harder to unit test
4. **Navigation**: Hard to find things in large files
5. **Code Review**: Large classes are difficult to review effectively

### Default Threshold

The default threshold is **500 lines**, which allows for moderately complex classes while flagging "god classes." This can be customized via `.editorconfig`.

---

## Configuration

Configure the threshold via `.editorconfig`:

```ini
[*.cs]
# CCS0024: Class line threshold (default: 500)
dotnet_diagnostic.CCS0024.max_lines = 500

# Example: More permissive threshold for legacy code
dotnet_diagnostic.CCS0024.max_lines = 1000

# Example: Stricter threshold for new code
dotnet_diagnostic.CCS0024.max_lines = 300
```

---

## Compliant Examples

```csharp
// Good - focused class with single responsibility
public class OrderValidator
{
    private readonly ILogger _logger;
    private readonly IOrderRules _rules;

    public OrderValidator(ILogger logger, IOrderRules rules)
    {
        _logger = logger;
        _rules = rules;
    }

    public ValidationResult Validate(Order order)
    {
        var errors = new List<string>();

        if (!_rules.IsValidCustomer(order.CustomerId))
            errors.Add("Invalid customer");

        if (!_rules.HasValidItems(order.Items))
            errors.Add("Invalid items");

        return new ValidationResult(errors);
    }
}

// Good - struct under threshold
public struct Point
{
    public int X { get; }
    public int Y { get; }

    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }

    public double DistanceTo(Point other)
    {
        var dx = X - other.X;
        var dy = Y - other.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}
```

## Non-Compliant Examples

```csharp
// CCS0024 - "God class" with 600+ lines
public class OrderManager  // This class is 600 lines
{
    // Fields for multiple responsibilities
    private readonly IDatabase _db;
    private readonly IEmailService _email;
    private readonly IPaymentGateway _payment;
    private readonly IInventoryService _inventory;
    private readonly IShippingService _shipping;
    private readonly IReportingService _reporting;

    // 50+ methods handling order, customer, payment, shipping, reporting...

    public void CreateOrder() { /* ... */ }
    public void UpdateOrder() { /* ... */ }
    public void DeleteOrder() { /* ... */ }
    public void ProcessPayment() { /* ... */ }
    public void RefundPayment() { /* ... */ }
    public void ShipOrder() { /* ... */ }
    public void TrackShipment() { /* ... */ }
    public void GenerateInvoice() { /* ... */ }
    public void SendConfirmationEmail() { /* ... */ }
    public void UpdateInventory() { /* ... */ }
    public void GenerateSalesReport() { /* ... */ }
    // ... hundreds more lines
}
```

---

## Implementation Specification

### File Structure

```
CodeCop.Sharp/
└── Analyzers/
    └── Quality/
        └── ClassTooLongAnalyzer.cs
```

**Note**: No code fix provider. Splitting a large class requires architectural decisions.

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
    /// Analyzer that detects classes/structs exceeding a line threshold.
    /// </summary>
    /// <remarks>
    /// Rule ID: CCS0024
    /// Category: Quality
    /// Severity: Warning
    ///
    /// The threshold is configurable via .editorconfig:
    /// dotnet_diagnostic.CCS0024.max_lines = 500
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ClassTooLongAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        /// The diagnostic ID for this analyzer.
        /// </summary>
        public const string DiagnosticId = "CCS0024";

        /// <summary>
        /// The default maximum number of lines allowed in a class.
        /// </summary>
        public const int DefaultMaxLines = 500;

        private static readonly LocalizableString Title = "Class too long";
        private static readonly LocalizableString MessageFormat =
            "{0} '{1}' has {2} lines, exceeding the maximum of {3}";
        private static readonly LocalizableString Description =
            "Classes should be concise. Consider breaking large classes into smaller, focused classes.";
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

            context.RegisterSyntaxNodeAction(AnalyzeClass, SyntaxKind.ClassDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeStruct, SyntaxKind.StructDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeRecord, SyntaxKind.RecordDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeRecordStruct, SyntaxKind.RecordStructDeclaration);
        }

        private void AnalyzeClass(SyntaxNodeAnalysisContext context)
        {
            var classDeclaration = (ClassDeclarationSyntax)context.Node;
            AnalyzeTypeDeclaration(context, classDeclaration, classDeclaration.Identifier, "Class");
        }

        private void AnalyzeStruct(SyntaxNodeAnalysisContext context)
        {
            var structDeclaration = (StructDeclarationSyntax)context.Node;
            AnalyzeTypeDeclaration(context, structDeclaration, structDeclaration.Identifier, "Struct");
        }

        private void AnalyzeRecord(SyntaxNodeAnalysisContext context)
        {
            var recordDeclaration = (RecordDeclarationSyntax)context.Node;

            // RecordDeclarationSyntax is used for both 'record' and 'record class'
            var typeKind = recordDeclaration.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword)
                ? "Record struct"
                : "Record";

            AnalyzeTypeDeclaration(context, recordDeclaration, recordDeclaration.Identifier, typeKind);
        }

        private void AnalyzeRecordStruct(SyntaxNodeAnalysisContext context)
        {
            var recordStructDeclaration = (RecordDeclarationSyntax)context.Node;
            AnalyzeTypeDeclaration(context, recordStructDeclaration, recordStructDeclaration.Identifier, "Record struct");
        }

        private void AnalyzeTypeDeclaration(
            SyntaxNodeAnalysisContext context,
            TypeDeclarationSyntax typeDeclaration,
            SyntaxToken identifier,
            string typeKind)
        {
            var maxLines = GetMaxLines(context);
            var lineCount = GetLineCount(typeDeclaration);

            if (lineCount > maxLines)
            {
                var diagnostic = Diagnostic.Create(
                    Rule,
                    identifier.GetLocation(),
                    typeKind,
                    identifier.Text,
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

            if (options.TryGetValue("dotnet_diagnostic.CCS0024.max_lines", out var value) &&
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
    }
}
```

---

## Decision Tree

```
┌────────────────────────────────────┐
│ Is it a type declaration?          │
│ (class, struct, record)            │
└─────────────────┬──────────────────┘
                  │
          ┌───────▼───────┐
          │      NO       │──────────► SKIP
          └───────┬───────┘
                  │ YES
                  ▼
┌────────────────────────────────────┐
│ Get max_lines from .editorconfig   │
│ or use default (500)               │
└─────────────────┬──────────────────┘
                  │
                  ▼
┌────────────────────────────────────┐
│ Calculate class line count         │
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
            REPORT CCS0024
       (with type kind, name, count)
```

---

## Test Cases

### Analyzer Tests - Should Trigger Diagnostic

| Test Name | Lines | Threshold | Expected |
|-----------|-------|-----------|----------|
| ClassOverDefault | 501 | 500 | CCS0024: "Class 'X' has 501 lines" |
| ClassWayOver | 1000 | 500 | CCS0024: "Class 'X' has 1000 lines" |
| StructOverDefault | 501 | 500 | CCS0024: "Struct 'X' has 501 lines" |
| RecordOverDefault | 501 | 500 | CCS0024: "Record 'X' has 501 lines" |
| OverCustomThreshold | 350 | 300 | CCS0024: "350 lines, exceeding 300" |

### Analyzer Tests - Should NOT Trigger Diagnostic

| Test Name | Lines | Threshold | Expected |
|-----------|-------|-----------|----------|
| ClassUnderThreshold | 100 | 500 | No diagnostic |
| ClassAtThreshold | 500 | 500 | No diagnostic |
| SmallClass | 20 | 500 | No diagnostic |
| Interface | Any | 500 | No diagnostic (interfaces excluded) |
| Enum | Any | 500 | No diagnostic (enums excluded) |
| PartialClassPart | 300 | 500 | No diagnostic (each part separate) |

---

## Test Code Template

```csharp
using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<
    CodeCop.Sharp.Analyzers.Quality.ClassTooLongAnalyzer>;

namespace CodeCop.Sharp.Tests.Analyzers.Quality
{
    public class ClassTooLongAnalyzerTests
    {
        [Fact]
        public async Task ClassUnderThreshold_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
public class SmallClass
{
    private int _value;

    public int Value => _value;

    public void SetValue(int value)
    {
        _value = value;
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task ClassOverThreshold_ShouldTriggerDiagnostic()
        {
            // Generate a class with 510 lines (over default 500)
            var lines = new System.Text.StringBuilder();
            lines.AppendLine(@"
public class {|#0:LargeClass|}
{");
            for (int i = 0; i < 505; i++)
            {
                lines.AppendLine($"    private int _field{i};");
            }
            lines.AppendLine(@"}");

            var expected = VerifyCS.Diagnostic("CCS0024")
                .WithLocation(0)
                .WithArguments("Class", "LargeClass", 510, 500);
            await VerifyCS.VerifyAnalyzerAsync(lines.ToString(), expected);
        }

        [Fact]
        public async Task ClassAtThreshold_ShouldNotTriggerDiagnostic()
        {
            // Generate a class with exactly 500 lines
            var lines = new System.Text.StringBuilder();
            lines.AppendLine(@"
public class ExactClass
{");
            for (int i = 0; i < 495; i++)
            {
                lines.AppendLine($"    private int _field{i};");
            }
            lines.AppendLine(@"}");

            await VerifyCS.VerifyAnalyzerAsync(lines.ToString());
        }

        [Fact]
        public async Task StructOverThreshold_ShouldTriggerDiagnostic()
        {
            var lines = new System.Text.StringBuilder();
            lines.AppendLine(@"
public struct {|#0:LargeStruct|}
{");
            for (int i = 0; i < 505; i++)
            {
                lines.AppendLine($"    private int _field{i};");
            }
            lines.AppendLine(@"}");

            var expected = VerifyCS.Diagnostic("CCS0024")
                .WithLocation(0)
                .WithArguments("Struct", "LargeStruct", 510, 500);
            await VerifyCS.VerifyAnalyzerAsync(lines.ToString(), expected);
        }

        [Fact]
        public async Task RecordOverThreshold_ShouldTriggerDiagnostic()
        {
            var lines = new System.Text.StringBuilder();
            lines.AppendLine(@"
public record {|#0:LargeRecord|}
{");
            for (int i = 0; i < 505; i++)
            {
                lines.AppendLine($"    public int Property{i} {{ get; init; }}");
            }
            lines.AppendLine(@"}");

            var expected = VerifyCS.Diagnostic("CCS0024")
                .WithLocation(0)
                .WithArguments("Record", "LargeRecord", 510, 500);
            await VerifyCS.VerifyAnalyzerAsync(lines.ToString(), expected);
        }

        [Fact]
        public async Task InterfaceOverThreshold_ShouldNotTriggerDiagnostic()
        {
            // Interfaces are not analyzed
            var lines = new System.Text.StringBuilder();
            lines.AppendLine(@"
public interface ILargeInterface
{");
            for (int i = 0; i < 505; i++)
            {
                lines.AppendLine($"    void Method{i}();");
            }
            lines.AppendLine(@"}");

            await VerifyCS.VerifyAnalyzerAsync(lines.ToString());
        }

        [Fact]
        public async Task NestedClass_EachAnalyzedSeparately()
        {
            var testCode = @"
public class OuterClass
{
    public class {|#0:InnerClass|}
    {
        // This inner class is 600 lines...
    }
}";
            // Test verifies nested classes are analyzed independently
        }
    }
}
```

---

## Edge Cases

| Case | Behavior | Rationale |
|------|----------|-----------|
| Partial classes | Each part separate | Combined size not tracked in one place |
| Nested classes | Each class separate | Inner class is independent unit |
| Interfaces | Not analyzed | Interfaces don't contain implementation |
| Enums | Not analyzed | Enums are data definitions |
| Delegates | Not analyzed | Delegates are type definitions |
| Empty class | Under threshold | 2-3 lines is fine |
| Class with regions | Count all lines | Regions don't reduce complexity |

---

## Why No Code Fix?

Splitting a large class requires architectural decisions:

1. **Responsibility Identification**: What are the distinct responsibilities?
2. **Dependency Analysis**: How do parts depend on each other?
3. **Interface Design**: What abstractions should be introduced?
4. **Naming**: What to call the extracted classes?
5. **Namespace Organization**: Where should new classes go?
6. **Migration Strategy**: How to refactor incrementally?

The analyzer identifies the problem; architects must design the solution.

---

## Refactoring Suggestions

When you see CCS0024, consider:

1. **Extract Class**: Move related methods/fields to new class
2. **Extract Interface**: Define contracts for dependencies
3. **Composition over Inheritance**: Delegate to helper classes
4. **Facade Pattern**: Hide complexity behind simpler interface
5. **Strategy Pattern**: Extract varying behaviors
6. **Repository Pattern**: Separate data access
7. **Service Layer**: Extract business logic

---

## Deliverable Checklist

- [ ] Create `Analyzers/Quality/ClassTooLongAnalyzer.cs`
- [ ] Implement class declaration analysis
- [ ] Implement struct declaration analysis
- [ ] Implement record declaration analysis
- [ ] Implement .editorconfig threshold reading
- [ ] Calculate line count correctly
- [ ] Write analyzer tests (~6 tests)
- [ ] Write configuration tests (~2 tests)
- [ ] Verify all tests pass
- [ ] Test manually in Visual Studio
