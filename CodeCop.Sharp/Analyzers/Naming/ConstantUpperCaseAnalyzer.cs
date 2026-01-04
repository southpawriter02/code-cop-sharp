using CodeCop.Sharp.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace CodeCop.Sharp.Analyzers.Naming
{
    /// <summary>
    /// Analyzer that enforces UPPER_CASE or PascalCase naming convention for constants.
    /// </summary>
    /// <remarks>
    /// Rule ID: CCS0005
    /// Category: Naming
    /// Severity: Info
    ///
    /// This analyzer reports a diagnostic when a constant field starts with a lowercase letter.
    /// Both PascalCase and UPPER_SNAKE_CASE are considered valid conventions.
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ConstantUpperCaseAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        /// The diagnostic ID for this analyzer.
        /// </summary>
        public const string DiagnosticId = "CCS0005";

        private static readonly LocalizableString Title = "Constant should use UPPER_CASE or PascalCase";
        private static readonly LocalizableString MessageFormat = "Constant '{0}' should use UPPER_CASE or PascalCase. Consider: '{1}'";
        private static readonly LocalizableString Description = "Constants should use UPPER_CASE (SCREAMING_SNAKE_CASE) or PascalCase naming.";
        private const string Category = "Naming";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Info,
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
            context.RegisterSyntaxNodeAction(AnalyzeFieldDeclaration, SyntaxKind.FieldDeclaration);
        }

        private void AnalyzeFieldDeclaration(SyntaxNodeAnalysisContext context)
        {
            var fieldDeclaration = (FieldDeclarationSyntax)context.Node;

            // Only check const fields
            if (!fieldDeclaration.Modifiers.Any(SyntaxKind.ConstKeyword))
            {
                return;
            }

            // Check each variable in the declaration (e.g., const int a = 1, b = 2;)
            foreach (var variable in fieldDeclaration.Declaration.Variables)
            {
                var constName = variable.Identifier.ValueText;

                if (string.IsNullOrEmpty(constName))
                {
                    continue;
                }

                // Valid if starts with uppercase (either PascalCase or UPPER_CASE)
                if (char.IsUpper(constName[0]))
                {
                    continue;
                }

                // Starts with lowercase - violation
                var suggestedName = NamingUtilities.ToPascalCase(constName);
                var diagnostic = Diagnostic.Create(
                    Rule,
                    variable.Identifier.GetLocation(),
                    constName,
                    suggestedName);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
