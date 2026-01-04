using CodeCop.Sharp.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace CodeCop.Sharp.Analyzers.Naming
{
    /// <summary>
    /// Analyzer that enforces PascalCase naming convention for C# method names.
    /// </summary>
    /// <remarks>
    /// Rule ID: CCS0001
    /// Category: Naming
    /// Severity: Warning
    ///
    /// This analyzer reports a diagnostic when a method name starts with a lowercase letter.
    /// Methods starting with underscore are ignored (private method convention).
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MethodDeclarationAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        /// The diagnostic ID for this analyzer.
        /// </summary>
        public const string DiagnosticId = "CCS0001";

        private static readonly LocalizableString Title = "Method name should be in PascalCase";
        private static readonly LocalizableString MessageFormat = "Method name '{0}' should be in PascalCase. Consider: '{1}'";
        private static readonly LocalizableString Description = "Method names should be in PascalCase.";
        private const string Category = "Naming";

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
            context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
        }

        private void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
        {
            var methodDeclaration = (MethodDeclarationSyntax)context.Node;
            var methodName = methodDeclaration.Identifier.ValueText;

            // Skip if null/empty or doesn't start with lowercase
            if (string.IsNullOrEmpty(methodName) || !char.IsLower(methodName[0]))
            {
                return;
            }

            var suggestedName = NamingUtilities.ToPascalCase(methodName);
            var diagnostic = Diagnostic.Create(Rule, methodDeclaration.Identifier.GetLocation(), methodName, suggestedName);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
