# CCS0026: TooManyParameters

## Overview

| Property | Value |
|----------|-------|
| Rule ID | CCS0026 |
| Category | Quality |
| Severity | Warning |
| Has Code Fix | No |
| Enabled by Default | Yes |
| Default Threshold | 5 |
| Configurable | Yes |

## Description

Detects methods and constructors with too many parameters. Methods with many parameters are hard to call correctly, difficult to remember, and often indicate that the method is doing too much or that related parameters should be grouped.

### Why This Rule?

1. **Usability**: Hard to remember parameter order
2. **Readability**: Long parameter lists are hard to read
3. **Maintenance**: Adding/removing parameters affects many call sites
4. **Design Smell**: Often indicates missing abstractions
5. **Testing**: More parameters = more test combinations

### Default Threshold

The default threshold is **5 parameters**, which is a commonly accepted maximum. This can be customized via `.editorconfig`.

---

## Configuration

Configure the threshold via `.editorconfig`:

```ini
[*.cs]
# CCS0026: Parameter count threshold (default: 5)
dotnet_diagnostic.CCS0026.max_parameters = 5

# Example: More permissive for legacy code
dotnet_diagnostic.CCS0026.max_parameters = 7

# Example: Stricter for new code
dotnet_diagnostic.CCS0026.max_parameters = 4
```

---

## Compliant Examples

```csharp
public class OrderService
{
    // Good - 3 parameters is reasonable
    public Order CreateOrder(Customer customer, IEnumerable<OrderItem> items, decimal discount)
    {
        return new Order(customer, items, discount);
    }

    // Good - uses parameter object instead of many params
    public Order CreateOrder(CreateOrderRequest request)
    {
        return new Order(request.Customer, request.Items, request.Discount);
    }

    // Good - reasonable constructor
    public OrderService(ILogger logger, IOrderRepository repository, IPaymentService payment)
    {
        _logger = logger;
        _repository = repository;
        _payment = payment;
    }
}

// Good - parameter object groups related data
public class CreateOrderRequest
{
    public Customer Customer { get; set; }
    public IEnumerable<OrderItem> Items { get; set; }
    public decimal Discount { get; set; }
    public string CouponCode { get; set; }
    public Address ShippingAddress { get; set; }
    public Address BillingAddress { get; set; }
}
```

## Non-Compliant Examples

```csharp
public class OrderService
{
    // CCS0026 - 8 parameters, exceeds 5
    public Order CreateOrder(
        int customerId,
        string customerName,
        string customerEmail,
        List<OrderItem> items,
        decimal subtotal,
        decimal tax,
        decimal discount,
        string couponCode)
    {
        // ...
    }

    // CCS0026 - 7 parameters
    public void ProcessPayment(
        int orderId,
        decimal amount,
        string cardNumber,
        string cardName,
        string expiryDate,
        string cvv,
        string billingZip)
    {
        // ...
    }

    // CCS0026 - 6 parameters
    public OrderService(
        ILogger logger,
        IOrderRepository orderRepo,
        ICustomerRepository customerRepo,
        IPaymentService payment,
        IShippingService shipping,
        IEmailService email)
    {
        // ...
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
        └── TooManyParametersAnalyzer.cs
```

**Note**: No code fix provider. Refactoring parameter lists requires design decisions.

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
    /// Analyzer that detects methods/constructors with too many parameters.
    /// </summary>
    /// <remarks>
    /// Rule ID: CCS0026
    /// Category: Quality
    /// Severity: Warning
    ///
    /// The threshold is configurable via .editorconfig:
    /// dotnet_diagnostic.CCS0026.max_parameters = 5
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TooManyParametersAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        /// The diagnostic ID for this analyzer.
        /// </summary>
        public const string DiagnosticId = "CCS0026";

        /// <summary>
        /// The default maximum number of parameters allowed.
        /// </summary>
        public const int DefaultMaxParameters = 5;

        private static readonly LocalizableString Title = "Too many parameters";
        private static readonly LocalizableString MessageFormat =
            "Method '{0}' has {1} parameters, exceeding the maximum of {2}. Consider using a parameter object.";
        private static readonly LocalizableString Description =
            "Methods with many parameters are hard to call correctly. Consider grouping related parameters into an object.";
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
            context.RegisterSyntaxNodeAction(AnalyzeDelegateDeclaration, SyntaxKind.DelegateDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeIndexer, SyntaxKind.IndexerDeclaration);
        }

        private void AnalyzeMethod(SyntaxNodeAnalysisContext context)
        {
            var method = (MethodDeclarationSyntax)context.Node;
            AnalyzeParameterList(context, method.Identifier, method.ParameterList);
        }

        private void AnalyzeConstructor(SyntaxNodeAnalysisContext context)
        {
            var constructor = (ConstructorDeclarationSyntax)context.Node;
            AnalyzeParameterList(context, constructor.Identifier, constructor.ParameterList);
        }

        private void AnalyzeLocalFunction(SyntaxNodeAnalysisContext context)
        {
            var localFunction = (LocalFunctionStatementSyntax)context.Node;
            AnalyzeParameterList(context, localFunction.Identifier, localFunction.ParameterList);
        }

        private void AnalyzeDelegateDeclaration(SyntaxNodeAnalysisContext context)
        {
            var delegateDecl = (DelegateDeclarationSyntax)context.Node;
            AnalyzeParameterList(context, delegateDecl.Identifier, delegateDecl.ParameterList);
        }

        private void AnalyzeIndexer(SyntaxNodeAnalysisContext context)
        {
            var indexer = (IndexerDeclarationSyntax)context.Node;

            var maxParams = GetMaxParameters(context);
            var paramCount = indexer.ParameterList?.Parameters.Count ?? 0;

            if (paramCount > maxParams)
            {
                var diagnostic = Diagnostic.Create(
                    Rule,
                    indexer.ThisKeyword.GetLocation(),
                    "indexer",
                    paramCount,
                    maxParams);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private void AnalyzeParameterList(
            SyntaxNodeAnalysisContext context,
            SyntaxToken identifier,
            ParameterListSyntax? parameterList)
        {
            if (parameterList == null)
                return;

            var maxParams = GetMaxParameters(context);
            var paramCount = parameterList.Parameters.Count;

            if (paramCount > maxParams)
            {
                var diagnostic = Diagnostic.Create(
                    Rule,
                    identifier.GetLocation(),
                    identifier.Text,
                    paramCount,
                    maxParams);
                context.ReportDiagnostic(diagnostic);
            }
        }

        /// <summary>
        /// Gets the maximum parameter count from configuration or returns default.
        /// </summary>
        private static int GetMaxParameters(SyntaxNodeAnalysisContext context)
        {
            var options = context.Options.AnalyzerConfigOptionsProvider
                .GetOptions(context.Node.SyntaxTree);

            if (options.TryGetValue("dotnet_diagnostic.CCS0026.max_parameters", out var value) &&
                int.TryParse(value, out var maxParams) &&
                maxParams > 0)
            {
                return maxParams;
            }

            return DefaultMaxParameters;
        }
    }
}
```

---

## Decision Tree

```
┌────────────────────────────────────┐
│ Is it a member with parameters?    │
│ (method, constructor, local func,  │
│  delegate, indexer)                │
└─────────────────┬──────────────────┘
                  │
          ┌───────▼───────┐
          │      NO       │──────────► SKIP
          └───────┬───────┘
                  │ YES
                  ▼
┌────────────────────────────────────┐
│ Get max_parameters from config     │
│ or use default (5)                 │
└─────────────────┬──────────────────┘
                  │
                  ▼
┌────────────────────────────────────┐
│ Count parameters in list           │
└─────────────────┬──────────────────┘
                  │
                  ▼
┌────────────────────────────────────┐
│ Is parameter count > max?          │
└─────────────────┬──────────────────┘
                  │
          ┌───────▼───────┐
          │      NO       │──────────► SKIP (within limit)
          └───────┬───────┘
                  │ YES
                  ▼
            REPORT CCS0026
```

---

## Test Cases

### Analyzer Tests - Should Trigger Diagnostic

| Test Name | Params | Threshold | Expected |
|-----------|--------|-----------|----------|
| MethodOverDefault | 6 | 5 | CCS0026 |
| MethodWayOver | 10 | 5 | CCS0026 |
| ConstructorOver | 7 | 5 | CCS0026 |
| OverCustomThreshold | 5 | 4 | CCS0026 |
| LocalFunctionOver | 6 | 5 | CCS0026 |
| DelegateOver | 6 | 5 | CCS0026 |
| IndexerOver | 4 | 3 | CCS0026 |

### Analyzer Tests - Should NOT Trigger Diagnostic

| Test Name | Params | Threshold | Expected |
|-----------|--------|-----------|----------|
| MethodUnder | 3 | 5 | No diagnostic |
| MethodAtThreshold | 5 | 5 | No diagnostic |
| EmptyParams | 0 | 5 | No diagnostic |
| SingleParam | 1 | 5 | No diagnostic |
| ParamsKeyword | Any | 5 | Still counts each |
| OptionalParams | 6 optional | 5 | CCS0026 (still counts) |

---

## Test Code Template

```csharp
using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<
    CodeCop.Sharp.Analyzers.Quality.TooManyParametersAnalyzer>;

namespace CodeCop.Sharp.Tests.Analyzers.Quality
{
    public class TooManyParametersAnalyzerTests
    {
        [Fact]
        public async Task MethodUnderThreshold_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    public void Method(int a, int b, int c)
    {
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
    public void Method(int a, int b, int c, int d, int e)
    {
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
    public void {|#0:Method|}(int a, int b, int c, int d, int e, int f)
    {
    }
}";

            var expected = VerifyCS.Diagnostic("CCS0026")
                .WithLocation(0)
                .WithArguments("Method", 6, 5);
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task ConstructorOverThreshold_ShouldTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    public {|#0:MyClass|}(int a, int b, int c, int d, int e, int f, int g)
    {
    }
}";

            var expected = VerifyCS.Diagnostic("CCS0026")
                .WithLocation(0)
                .WithArguments("MyClass", 7, 5);
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task LocalFunctionOverThreshold_ShouldTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    public void Method()
    {
        void {|#0:LocalFunc|}(int a, int b, int c, int d, int e, int f)
        {
        }
    }
}";

            var expected = VerifyCS.Diagnostic("CCS0026")
                .WithLocation(0)
                .WithArguments("LocalFunc", 6, 5);
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task DelegateOverThreshold_ShouldTriggerDiagnostic()
        {
            var testCode = @"
public delegate void {|#0:MyDelegate|}(int a, int b, int c, int d, int e, int f);
";

            var expected = VerifyCS.Diagnostic("CCS0026")
                .WithLocation(0)
                .WithArguments("MyDelegate", 6, 5);
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task OptionalParametersStillCount()
        {
            var testCode = @"
public class MyClass
{
    public void {|#0:Method|}(int a, int b = 0, int c = 0, int d = 0, int e = 0, int f = 0)
    {
    }
}";

            var expected = VerifyCS.Diagnostic("CCS0026")
                .WithLocation(0)
                .WithArguments("Method", 6, 5);
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task ParamsArrayStillCounts()
        {
            var testCode = @"
public class MyClass
{
    public void {|#0:Method|}(int a, int b, int c, int d, int e, params int[] rest)
    {
    }
}";

            var expected = VerifyCS.Diagnostic("CCS0026")
                .WithLocation(0)
                .WithArguments("Method", 6, 5);
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task NoParameters_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    public void Method()
    {
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task InterfaceMethod_ShouldStillAnalyze()
        {
            var testCode = @"
public interface IMyInterface
{
    void {|#0:Method|}(int a, int b, int c, int d, int e, int f);
}";

            var expected = VerifyCS.Diagnostic("CCS0026")
                .WithLocation(0)
                .WithArguments("Method", 6, 5);
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }
    }
}
```

---

## Edge Cases

| Case | Behavior | Rationale |
|------|----------|-----------|
| Optional parameters | Count each | Still add to cognitive load |
| `params` array | Counts as 1 | It's a single parameter syntactically |
| Extension method `this` | Counts as 1 | It's a parameter |
| Out/ref parameters | Count each | Still parameters |
| Generic type params | Don't count | Type parameters, not value parameters |
| Primary constructor | Count each | C# 12 feature - analyze same as constructor |
| Interface methods | Analyze | Interfaces define contracts |
| Abstract methods | Analyze | Define signature to implement |

---

## Why No Code Fix?

Refactoring parameters requires design decisions:

1. **Parameter Object**: What to name it? What properties?
2. **Builder Pattern**: When is this appropriate?
3. **Overloads**: Add overloads with fewer params?
4. **Fluent Interface**: Use method chaining?
5. **Optional Parameters**: Which can be optional?
6. **Dependency Injection**: Constructor parameters for DI?

The analyzer identifies the problem; developers must design the solution.

---

## Refactoring Suggestions

When you see CCS0026, consider:

1. **Introduce Parameter Object**: Group related parameters
   ```csharp
   // Before
   void CreateUser(string firstName, string lastName, string email,
                   string phone, string address, string city);

   // After
   void CreateUser(UserInfo userInfo);
   ```

2. **Builder Pattern**: For complex object construction
   ```csharp
   var order = new OrderBuilder()
       .WithCustomer(customer)
       .WithItems(items)
       .WithDiscount(discount)
       .Build();
   ```

3. **Method Chaining / Fluent Interface**:
   ```csharp
   query.Where(x => x.Active)
        .OrderBy(x => x.Name)
        .Take(10);
   ```

4. **Split Method**: Maybe the method does too much
5. **Optional Parameters**: With defaults for less-common cases
6. **Overloads**: Provide simplified versions

---

## Deliverable Checklist

- [ ] Create `Analyzers/Quality/TooManyParametersAnalyzer.cs`
- [ ] Implement method parameter counting
- [ ] Implement constructor analysis
- [ ] Implement local function analysis
- [ ] Implement delegate analysis
- [ ] Implement indexer analysis
- [ ] Implement .editorconfig threshold reading
- [ ] Write analyzer tests (~8 tests)
- [ ] Write configuration tests (~2 tests)
- [ ] Verify all tests pass
- [ ] Test manually in Visual Studio
