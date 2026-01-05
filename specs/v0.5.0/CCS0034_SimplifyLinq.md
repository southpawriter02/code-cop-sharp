# CCS0034: SimplifyLinq

## Overview

| Property | Value |
|----------|-------|
| Rule ID | CCS0034 |
| Category | BestPractices |
| Severity | Info |
| Has Code Fix | Yes |
| Enabled by Default | Yes |

## Description

Detects LINQ expressions that can be simplified for better performance and readability.

## Patterns Detected

| Pattern | Verbose | Simplified |
|---------|---------|------------|
| Count > 0 | `.Count() > 0` | `.Any()` |
| Count == 0 | `.Count() == 0` | `!.Any()` |
| Count != 0 | `.Count() != 0` | `.Any()` |
| Where.Count | `.Where(x).Count()` | `.Count(x)` |
| Where.First | `.Where(x).First()` | `.First(x)` |
| Where.FirstOrDefault | `.Where(x).FirstOrDefault()` | `.FirstOrDefault(x)` |
| Where.Any | `.Where(x).Any()` | `.Any(x)` |
| Select.ToList | `.Select(x).ToList()` | `.ToList()` or `.ConvertAll(x)` |
| OrderBy.First | `.OrderBy(x).First()` | `.MinBy(x)` (.NET 6+) |
| OrderByDescending.First | `.OrderByDescending(x).First()` | `.MaxBy(x)` (.NET 6+) |

## Compliant Examples

```csharp
// Good - simplified
bool hasItems = items.Any();
bool hasActive = items.Any(x => x.IsActive);
int activeCount = items.Count(x => x.IsActive);
var first = items.FirstOrDefault(x => x.Id == id);
```

## Non-Compliant Examples

```csharp
// CCS0034 - can simplify
bool hasItems = items.Count() > 0;           // Use Any()
bool hasActive = items.Where(x => x.IsActive).Any();  // Use Any(predicate)
int activeCount = items.Where(x => x.IsActive).Count();  // Use Count(predicate)
var first = items.Where(x => x.Id == id).FirstOrDefault();  // Use FirstOrDefault(predicate)
```

## Implementation Specification

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace CodeCop.Sharp.Analyzers.BestPractices
{
    /// <summary>
    /// Analyzer that detects LINQ expressions that can be simplified.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SimplifyLinqAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CCS0034";

        private static readonly LocalizableString Title = "Simplify LINQ expression";
        private static readonly LocalizableString MessageFormat =
            "Replace '{0}' with '{1}'";
        private static readonly LocalizableString Description =
            "LINQ expressions should use the most direct method for clarity and performance.";
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

            context.RegisterSyntaxNodeAction(AnalyzeBinaryExpression, SyntaxKind.GreaterThanExpression);
            context.RegisterSyntaxNodeAction(AnalyzeBinaryExpression, SyntaxKind.EqualsExpression);
            context.RegisterSyntaxNodeAction(AnalyzeBinaryExpression, SyntaxKind.NotEqualsExpression);
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private void AnalyzeBinaryExpression(SyntaxNodeAnalysisContext context)
        {
            var binary = (BinaryExpressionSyntax)context.Node;

            // Check for Count() > 0, Count() == 0, Count() != 0
            if (IsCountComparison(binary, out var suggestion))
            {
                var diagnostic = Diagnostic.Create(
                    Rule,
                    binary.GetLocation(),
                    binary.ToString(),
                    suggestion);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            // Check for Where().X() patterns
            if (IsWhereChain(invocation, out var original, out var simplified))
            {
                var diagnostic = Diagnostic.Create(
                    Rule,
                    invocation.GetLocation(),
                    original,
                    simplified);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static bool IsCountComparison(BinaryExpressionSyntax binary, out string suggestion)
        {
            suggestion = "";

            // Check for .Count() on left side
            if (binary.Left is InvocationExpressionSyntax invocation &&
                invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Name.Identifier.Text == "Count" &&
                invocation.ArgumentList.Arguments.Count == 0)
            {
                // Check right side is 0
                if (binary.Right is LiteralExpressionSyntax literal &&
                    literal.Token.ValueText == "0")
                {
                    var collection = memberAccess.Expression.ToString();

                    if (binary.IsKind(SyntaxKind.GreaterThanExpression))
                    {
                        suggestion = $"{collection}.Any()";
                        return true;
                    }
                    else if (binary.IsKind(SyntaxKind.EqualsExpression))
                    {
                        suggestion = $"!{collection}.Any()";
                        return true;
                    }
                    else if (binary.IsKind(SyntaxKind.NotEqualsExpression))
                    {
                        suggestion = $"{collection}.Any()";
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsWhereChain(
            InvocationExpressionSyntax invocation,
            out string original,
            out string simplified)
        {
            original = "";
            simplified = "";

            if (invocation.Expression is not MemberAccessExpressionSyntax outerAccess)
                return false;

            var outerMethod = outerAccess.Name.Identifier.Text;

            // Check if the inner expression is .Where()
            if (outerAccess.Expression is InvocationExpressionSyntax innerInvocation &&
                innerInvocation.Expression is MemberAccessExpressionSyntax innerAccess &&
                innerAccess.Name.Identifier.Text == "Where" &&
                innerInvocation.ArgumentList.Arguments.Count == 1)
            {
                var collection = innerAccess.Expression.ToString();
                var predicate = innerInvocation.ArgumentList.Arguments[0].ToString();

                switch (outerMethod)
                {
                    case "Any" when invocation.ArgumentList.Arguments.Count == 0:
                        original = $".Where({predicate}).Any()";
                        simplified = $".Any({predicate})";
                        return true;

                    case "Count" when invocation.ArgumentList.Arguments.Count == 0:
                        original = $".Where({predicate}).Count()";
                        simplified = $".Count({predicate})";
                        return true;

                    case "First" when invocation.ArgumentList.Arguments.Count == 0:
                        original = $".Where({predicate}).First()";
                        simplified = $".First({predicate})";
                        return true;

                    case "FirstOrDefault" when invocation.ArgumentList.Arguments.Count == 0:
                        original = $".Where({predicate}).FirstOrDefault()";
                        simplified = $".FirstOrDefault({predicate})";
                        return true;

                    case "Single" when invocation.ArgumentList.Arguments.Count == 0:
                        original = $".Where({predicate}).Single()";
                        simplified = $".Single({predicate})";
                        return true;

                    case "SingleOrDefault" when invocation.ArgumentList.Arguments.Count == 0:
                        original = $".Where({predicate}).SingleOrDefault()";
                        simplified = $".SingleOrDefault({predicate})";
                        return true;

                    case "Last" when invocation.ArgumentList.Arguments.Count == 0:
                        original = $".Where({predicate}).Last()";
                        simplified = $".Last({predicate})";
                        return true;

                    case "LastOrDefault" when invocation.ArgumentList.Arguments.Count == 0:
                        original = $".Where({predicate}).LastOrDefault()";
                        simplified = $".LastOrDefault({predicate})";
                        return true;
                }
            }

            return false;
        }
    }
}
```

## Test Cases

| Test Name | Input | Expected |
|-----------|-------|----------|
| CountGreaterThanZero | `list.Count() > 0` | CCS0034: "Any()" |
| CountEqualsZero | `list.Count() == 0` | CCS0034: "!Any()" |
| CountNotEqualsZero | `list.Count() != 0` | CCS0034: "Any()" |
| WhereAny | `.Where(x).Any()` | CCS0034: ".Any(x)" |
| WhereCount | `.Where(x).Count()` | CCS0034: ".Count(x)" |
| WhereFirst | `.Where(x).First()` | CCS0034: ".First(x)" |
| WhereFirstOrDefault | `.Where(x).FirstOrDefault()` | CCS0034: ".FirstOrDefault(x)" |
| AlreadySimplified | `.Any(x => x.IsActive)` | No diagnostic |
| CountWithPredicate | `.Count(x => x.IsActive)` | No diagnostic |
