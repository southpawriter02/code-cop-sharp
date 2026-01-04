using CodeCop.Sharp.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace CodeCop.Sharp.Analyzers.Naming
{
    /// <summary>
    /// Analyzer that enforces PascalCase naming convention for C# class names.
    /// </summary>
    /// <remarks>
    /// Rule ID: CCS0002
    /// Category: Naming
    /// Severity: Warning
    ///
    /// This analyzer reports a diagnostic when a class name starts with a lowercase letter.
    /// Classes starting with underscore are ignored.
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ClassPascalCaseAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        /// The diagnostic ID for this analyzer.
        /// </summary>
        public const string DiagnosticId = "CCS0002";

        private static readonly LocalizableString Title = "Class name should be in PascalCase";
        private static readonly LocalizableString MessageFormat = "Class name '{0}' should be in PascalCase. Consider: '{1}'";
        private static readonly LocalizableString Description = "Class names should be in PascalCase.";
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
            context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
        }

        private void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
        {
            var classDeclaration = (ClassDeclarationSyntax)context.Node;
            var className = classDeclaration.Identifier.ValueText;

            // Skip if null/empty or doesn't start with lowercase
            if (string.IsNullOrEmpty(className) || !char.IsLower(className[0]))
            {
                return;
            }

            var suggestedName = NamingUtilities.ToPascalCase(className);
            var diagnostic = Diagnostic.Create(Rule, classDeclaration.Identifier.GetLocation(), className, suggestedName);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
