# CCS0025: CyclomaticComplexity

## Overview

| Property | Value |
|----------|-------|
| Rule ID | CCS0025 |
| Category | Quality |
| Severity | Warning |
| Has Code Fix | No |
| Enabled by Default | Yes |
| Default Threshold | 10 |
| Configurable | Yes |

## Description

Detects methods with high cyclomatic complexity. Cyclomatic complexity is a software metric that measures the number of linearly independent paths through a method's source code. Methods with high complexity are harder to understand, test, and maintain.

### Why This Rule?

1. **Testability**: High complexity requires more test cases
2. **Maintainability**: Complex methods are harder to modify safely
3. **Bug Risk**: Studies show complexity correlates with defect density
4. **Readability**: Simple methods are easier to understand
5. **Code Review**: Complex methods are harder to review

### Cyclomatic Complexity Formula

Complexity = Number of decision points + 1

Decision points include:
- `if` statements
- `else if` branches
- `case` labels in switch
- `for` loops
- `foreach` loops
- `while` loops
- `do-while` loops
- `&&` operators
- `||` operators
- `?:` ternary operators
- `?.` null-conditional operators
- `??` null-coalescing operators
- `catch` clauses

---

## Configuration

Configure the threshold via `.editorconfig`:

```ini
[*.cs]
# CCS0025: Cyclomatic complexity threshold (default: 10)
dotnet_diagnostic.CCS0025.max_complexity = 10

# Example: More permissive for legacy code
dotnet_diagnostic.CCS0025.max_complexity = 15

# Example: Stricter for new code
dotnet_diagnostic.CCS0025.max_complexity = 7
```

---

## Complexity Examples

### Low Complexity (1-5)

```csharp
// Complexity: 1 (no decision points)
public int Add(int a, int b)
{
    return a + b;
}

// Complexity: 2 (1 if)
public int Abs(int value)
{
    if (value < 0)
        return -value;
    return value;
}

// Complexity: 3 (1 if + 1 &&)
public bool IsValidAge(int age)
{
    if (age >= 0 && age <= 120)
        return true;
    return false;
}
```

### Medium Complexity (6-10)

```csharp
// Complexity: 6 (5 cases + 1 base)
public string GetDayName(int day)
{
    switch (day)
    {
        case 1: return "Monday";
        case 2: return "Tuesday";
        case 3: return "Wednesday";
        case 4: return "Thursday";
        case 5: return "Friday";
        default: return "Weekend";
    }
}
```

### High Complexity (> 10) - Non-Compliant

```csharp
// CCS0025 - Complexity: 15
public decimal CalculateDiscount(Order order)
{
    decimal discount = 0;

    if (order.Customer.IsPremium)           // +1 = 2
    {
        discount += 0.1m;
    }

    if (order.Total > 100)                  // +1 = 3
    {
        discount += 0.05m;
    }
    else if (order.Total > 50)              // +1 = 4
    {
        discount += 0.02m;
    }

    foreach (var item in order.Items)       // +1 = 5
    {
        if (item.IsOnSale)                  // +1 = 6
        {
            discount += 0.01m;
        }

        if (item.Category == "Electronics" && item.Price > 500)  // +1 +1 = 8
        {
            discount += 0.03m;
        }
    }

    if (order.CouponCode != null)           // +1 = 9
    {
        switch (order.CouponCode)
        {
            case "SAVE10": discount += 0.1m; break;    // +1 = 10
            case "SAVE20": discount += 0.2m; break;    // +1 = 11
            case "SAVE30": discount += 0.3m; break;    // +1 = 12
        }
    }

    return discount > 0.5m ? 0.5m : discount;  // +1 = 13 (ternary)
}
```

---

## Implementation Specification

### File Structure

```
CodeCop.Sharp/
└── Analyzers/
    └── Quality/
        └── CyclomaticComplexityAnalyzer.cs
```

### Analyzer Implementation

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCop.Sharp.Analyzers.Quality
{
    /// <summary>
    /// Analyzer that detects methods with high cyclomatic complexity.
    /// </summary>
    /// <remarks>
    /// Rule ID: CCS0025
    /// Category: Quality
    /// Severity: Warning
    ///
    /// Cyclomatic complexity measures the number of linearly independent
    /// paths through a method. High complexity indicates code that is
    /// hard to test and maintain.
    ///
    /// The threshold is configurable via .editorconfig:
    /// dotnet_diagnostic.CCS0025.max_complexity = 10
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CyclomaticComplexityAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        /// The diagnostic ID for this analyzer.
        /// </summary>
        public const string DiagnosticId = "CCS0025";

        /// <summary>
        /// The default maximum cyclomatic complexity allowed.
        /// </summary>
        public const int DefaultMaxComplexity = 10;

        private static readonly LocalizableString Title = "High cyclomatic complexity";
        private static readonly LocalizableString MessageFormat =
            "Method '{0}' has cyclomatic complexity of {1}, exceeding the maximum of {2}";
        private static readonly LocalizableString Description =
            "Methods with high complexity are harder to understand and test. Consider refactoring into smaller methods.";
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
            context.RegisterSyntaxNodeAction(AnalyzeLocalFunction, SyntaxKind.LocalFunctionStatement);
        }

        private void AnalyzeMethod(SyntaxNodeAnalysisContext context)
        {
            var method = (MethodDeclarationSyntax)context.Node;

            if (method.Body == null && method.ExpressionBody == null)
                return;

            var complexity = CalculateComplexity(method);
            var maxComplexity = GetMaxComplexity(context);

            if (complexity > maxComplexity)
            {
                var diagnostic = Diagnostic.Create(
                    Rule,
                    method.Identifier.GetLocation(),
                    method.Identifier.Text,
                    complexity,
                    maxComplexity);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private void AnalyzeConstructor(SyntaxNodeAnalysisContext context)
        {
            var constructor = (ConstructorDeclarationSyntax)context.Node;

            if (constructor.Body == null && constructor.ExpressionBody == null)
                return;

            var complexity = CalculateComplexity(constructor);
            var maxComplexity = GetMaxComplexity(context);

            if (complexity > maxComplexity)
            {
                var diagnostic = Diagnostic.Create(
                    Rule,
                    constructor.Identifier.GetLocation(),
                    constructor.Identifier.Text,
                    complexity,
                    maxComplexity);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private void AnalyzeOperator(SyntaxNodeAnalysisContext context)
        {
            var operatorDecl = (OperatorDeclarationSyntax)context.Node;

            if (operatorDecl.Body == null && operatorDecl.ExpressionBody == null)
                return;

            var complexity = CalculateComplexity(operatorDecl);
            var maxComplexity = GetMaxComplexity(context);

            if (complexity > maxComplexity)
            {
                var operatorName = $"operator {operatorDecl.OperatorToken.Text}";
                var diagnostic = Diagnostic.Create(
                    Rule,
                    operatorDecl.OperatorToken.GetLocation(),
                    operatorName,
                    complexity,
                    maxComplexity);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private void AnalyzeLocalFunction(SyntaxNodeAnalysisContext context)
        {
            var localFunction = (LocalFunctionStatementSyntax)context.Node;

            if (localFunction.Body == null && localFunction.ExpressionBody == null)
                return;

            var complexity = CalculateComplexity(localFunction);
            var maxComplexity = GetMaxComplexity(context);

            if (complexity > maxComplexity)
            {
                var diagnostic = Diagnostic.Create(
                    Rule,
                    localFunction.Identifier.GetLocation(),
                    localFunction.Identifier.Text,
                    complexity,
                    maxComplexity);
                context.ReportDiagnostic(diagnostic);
            }
        }

        /// <summary>
        /// Calculates the cyclomatic complexity of a syntax node.
        /// </summary>
        public static int CalculateComplexity(SyntaxNode node)
        {
            // Start at 1 (the method itself represents one path)
            int complexity = 1;

            var descendantNodes = node.DescendantNodes().ToList();

            foreach (var descendant in descendantNodes)
            {
                complexity += GetComplexityContribution(descendant);
            }

            return complexity;
        }

        /// <summary>
        /// Gets the complexity contribution of a syntax node.
        /// </summary>
        private static int GetComplexityContribution(SyntaxNode node)
        {
            switch (node)
            {
                // Control flow statements
                case IfStatementSyntax _:
                    return 1;

                case ConditionalExpressionSyntax _:  // ?:
                    return 1;

                case CaseSwitchLabelSyntax _:
                    return 1;

                case CasePatternSwitchLabelSyntax _:
                    return 1;

                case ForStatementSyntax _:
                    return 1;

                case ForEachStatementSyntax _:
                    return 1;

                case WhileStatementSyntax _:
                    return 1;

                case DoStatementSyntax _:
                    return 1;

                case CatchClauseSyntax _:
                    return 1;

                case ConditionalAccessExpressionSyntax _:  // ?.
                    return 1;

                case CoalesceExpressionSyntax _:  // ??
                    return 1;

                case BinaryExpressionSyntax binary:
                    // && and || add complexity (short-circuit evaluation)
                    if (binary.IsKind(SyntaxKind.LogicalAndExpression) ||
                        binary.IsKind(SyntaxKind.LogicalOrExpression))
                    {
                        return 1;
                    }
                    return 0;

                case SwitchExpressionSyntax switchExpr:
                    // Switch expressions: each arm (except default) adds complexity
                    return switchExpr.Arms.Count - 1; // -1 because one path is "free"

                case WhenClauseSyntax _:  // Pattern matching when clause
                    return 1;

                default:
                    return 0;
            }
        }

        /// <summary>
        /// Gets the maximum complexity from configuration or returns default.
        /// </summary>
        private static int GetMaxComplexity(SyntaxNodeAnalysisContext context)
        {
            var options = context.Options.AnalyzerConfigOptionsProvider
                .GetOptions(context.Node.SyntaxTree);

            if (options.TryGetValue("dotnet_diagnostic.CCS0025.max_complexity", out var value) &&
                int.TryParse(value, out var maxComplexity) &&
                maxComplexity > 0)
            {
                return maxComplexity;
            }

            return DefaultMaxComplexity;
        }
    }
}
```

---

## Complexity Calculation Reference

| Construct | Complexity Added | Example |
|-----------|------------------|---------|
| Method entry | +1 (base) | Every method starts at 1 |
| `if` | +1 | `if (x) { }` |
| `else if` | +1 | `else if (y) { }` |
| `case` (switch statement) | +1 per case | `case 1:` |
| `for` | +1 | `for (;;) { }` |
| `foreach` | +1 | `foreach (var x in y) { }` |
| `while` | +1 | `while (x) { }` |
| `do` | +1 | `do { } while (x);` |
| `&&` | +1 | `a && b` |
| `\|\|` | +1 | `a \|\| b` |
| `?:` | +1 | `x ? a : b` |
| `?.` | +1 | `obj?.Method()` |
| `??` | +1 | `x ?? default` |
| `catch` | +1 | `catch (Exception) { }` |
| `when` (pattern) | +1 | `case int x when x > 0:` |
| Switch expression arm | +1 per arm | `_ => default` (one is free) |

---

## Decision Tree

```
┌────────────────────────────────────┐
│ Is it a method-like member?        │
│ (method, constructor, operator,    │
│  local function)                   │
└─────────────────┬──────────────────┘
                  │
          ┌───────▼───────┐
          │      NO       │──────────► SKIP
          └───────┬───────┘
                  │ YES
                  ▼
┌────────────────────────────────────┐
│ Does it have a body?               │
└─────────────────┬──────────────────┘
                  │
          ┌───────▼───────┐
          │      NO       │──────────► SKIP
          └───────┬───────┘
                  │ YES
                  ▼
┌────────────────────────────────────┐
│ Get max_complexity from config     │
│ or use default (10)                │
└─────────────────┬──────────────────┘
                  │
                  ▼
┌────────────────────────────────────┐
│ Calculate complexity:              │
│ 1 (base) + decision points         │
└─────────────────┬──────────────────┘
                  │
                  ▼
┌────────────────────────────────────┐
│ Is complexity > max?               │
└─────────────────┬──────────────────┘
                  │
          ┌───────▼───────┐
          │      NO       │──────────► SKIP
          └───────┬───────┘
                  │ YES
                  ▼
            REPORT CCS0025
```

---

## Test Cases

### Analyzer Tests - Should Trigger Diagnostic

| Test Name | Complexity | Threshold | Expected |
|-----------|------------|-----------|----------|
| OverDefault | 11 | 10 | CCS0025 |
| WayOver | 25 | 10 | CCS0025 |
| OverCustom | 8 | 7 | CCS0025 |
| NestedConditions | 15 | 10 | CCS0025 |
| ManyLogicalOperators | 12 | 10 | CCS0025 |

### Analyzer Tests - Should NOT Trigger Diagnostic

| Test Name | Complexity | Threshold | Expected |
|-----------|------------|-----------|----------|
| Simple | 1 | 10 | No diagnostic |
| AtThreshold | 10 | 10 | No diagnostic |
| Moderate | 5 | 10 | No diagnostic |
| AbstractMethod | N/A | 10 | No diagnostic |

### Complexity Calculation Tests

| Test Name | Input | Expected Complexity |
|-----------|-------|---------------------|
| EmptyMethod | `void M() { }` | 1 |
| SingleIf | `if (x) { }` | 2 |
| IfElse | `if (x) { } else { }` | 2 |
| IfElseIf | `if (x) { } else if (y) { }` | 3 |
| Switch3Cases | 3 case labels | 4 |
| ForLoop | `for (;;) { }` | 2 |
| NestedFor | 2 nested for loops | 3 |
| LogicalAnd | `if (a && b)` | 3 |
| LogicalOr | `if (a \|\| b)` | 3 |
| Ternary | `x ? a : b` | 2 |
| NullConditional | `obj?.Method()` | 2 |
| NullCoalescing | `x ?? default` | 2 |
| TryCatch | `try { } catch { }` | 2 |

---

## Test Code Template

```csharp
using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<
    CodeCop.Sharp.Analyzers.Quality.CyclomaticComplexityAnalyzer>;

namespace CodeCop.Sharp.Tests.Analyzers.Quality
{
    public class CyclomaticComplexityAnalyzerTests
    {
        [Fact]
        public async Task SimpleMethod_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    public int Add(int a, int b)
    {
        return a + b;  // Complexity: 1
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task MethodAtThreshold_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    public int Process(int value)
    {
        // Complexity: 10 (1 base + 9 if statements)
        if (value > 0) return 1;
        if (value > 1) return 2;
        if (value > 2) return 3;
        if (value > 3) return 4;
        if (value > 4) return 5;
        if (value > 5) return 6;
        if (value > 6) return 7;
        if (value > 7) return 8;
        if (value > 8) return 9;
        return 0;
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task MethodOverThreshold_ShouldTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    public int {|#0:Process|}(int value)
    {
        // Complexity: 11 (1 base + 10 if statements)
        if (value > 0) return 1;
        if (value > 1) return 2;
        if (value > 2) return 3;
        if (value > 3) return 4;
        if (value > 4) return 5;
        if (value > 5) return 6;
        if (value > 6) return 7;
        if (value > 7) return 8;
        if (value > 8) return 9;
        if (value > 9) return 10;
        return 0;
    }
}";

            var expected = VerifyCS.Diagnostic("CCS0025")
                .WithLocation(0)
                .WithArguments("Process", 11, 10);
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task LogicalOperators_CountAsComplexity()
        {
            var testCode = @"
public class MyClass
{
    public bool {|#0:Check|}(int a, int b, int c, int d, int e,
                           int f, int g, int h, int i, int j, int k)
    {
        // Complexity: 12 (1 base + 11 && operators)
        return a > 0 && b > 0 && c > 0 && d > 0 && e > 0 &&
               f > 0 && g > 0 && h > 0 && i > 0 && j > 0 && k > 0;
    }
}";

            var expected = VerifyCS.Diagnostic("CCS0025")
                .WithLocation(0)
                .WithArguments("Check", 12, 10);
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task SwitchWithManyCases_CountsCorrectly()
        {
            var testCode = @"
public class MyClass
{
    public string {|#0:GetName|}(int day)
    {
        // Complexity: 12 (1 base + 11 cases including default)
        switch (day)
        {
            case 1: return ""Mon"";
            case 2: return ""Tue"";
            case 3: return ""Wed"";
            case 4: return ""Thu"";
            case 5: return ""Fri"";
            case 6: return ""Sat"";
            case 7: return ""Sun"";
            case 8: return ""Eight"";
            case 9: return ""Nine"";
            case 10: return ""Ten"";
            default: return ""Unknown"";
        }
    }
}";

            var expected = VerifyCS.Diagnostic("CCS0025")
                .WithLocation(0)
                .WithArguments("GetName", 12, 10);
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task NullOperators_CountAsComplexity()
        {
            var testCode = @"
public class MyClass
{
    private string _a, _b, _c, _d, _e, _f, _g, _h, _i, _j, _k;

    public string {|#0:GetValue|}()
    {
        // Complexity: 12 (1 base + 11 ?? operators)
        return _a ?? _b ?? _c ?? _d ?? _e ?? _f ??
               _g ?? _h ?? _i ?? _j ?? _k ?? ""default"";
    }
}";

            var expected = VerifyCS.Diagnostic("CCS0025")
                .WithLocation(0)
                .WithArguments("GetValue", 12, 10);
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Theory]
        [InlineData(1, "void M() { }")]
        [InlineData(2, "void M() { if (true) { } }")]
        [InlineData(3, "void M() { if (true && false) { } }")]
        [InlineData(2, "void M() { foreach (var x in new int[0]) { } }")]
        [InlineData(2, "void M() { try { } catch { } }")]
        public void CalculateComplexity_ReturnsCorrectValue(int expected, string methodBody)
        {
            // Unit test for the complexity calculation helper
        }
    }
}
```

---

## Edge Cases

| Case | Behavior | Rationale |
|------|----------|-----------|
| Empty method | Complexity = 1 | Base path |
| Expression body | Analyze expression | `=>` can contain complexity |
| Nested lambdas | Include in parent | Lambda adds to containing method |
| Local functions | Separate analysis | Analyzed as independent units |
| Pattern matching | +1 per pattern | Each pattern is a decision |
| Switch expressions | Arms - 1 | Modern switch syntax |
| LINQ query syntax | Not counted | Not direct control flow |
| Recursive calls | Not counted | Not additional paths in this method |

---

## Why No Code Fix?

Reducing complexity requires design decisions:

1. **Extract Method**: Which code blocks to extract?
2. **Strategy Pattern**: Extract varying behavior?
3. **State Pattern**: Convert conditionals to polymorphism?
4. **Lookup Tables**: Replace switch with dictionary?
5. **Guard Clauses**: Flatten nested conditions?

The analyzer identifies complexity; architects must refactor appropriately.

---

## Refactoring Suggestions

When you see CCS0025, consider:

1. **Extract Method**: Break into smaller, focused methods
2. **Replace Conditional with Polymorphism**: Use inheritance
3. **Introduce Lookup Table**: Dictionary instead of switch
4. **Guard Clauses**: Early returns to reduce nesting
5. **Decompose Conditional**: Extract complex conditions
6. **Replace Nested Conditional with Guard Clauses**
7. **Replace Parameter with Explicit Methods**: Eliminate flag parameters

---

## Deliverable Checklist

- [ ] Create `Analyzers/Quality/CyclomaticComplexityAnalyzer.cs`
- [ ] Implement method complexity calculation
- [ ] Count all decision point types correctly
- [ ] Implement .editorconfig threshold reading
- [ ] Analyze methods, constructors, operators, local functions
- [ ] Write analyzer tests (~10 tests)
- [ ] Write complexity calculation unit tests (~10 tests)
- [ ] Write configuration tests (~2 tests)
- [ ] Verify all tests pass
- [ ] Test manually in Visual Studio
