using CodeCop.Sharp.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace CodeCop.Sharp.Analyzers.Naming
{
    /// <summary>
    /// Analyzer that enforces camelCase naming convention for private and internal fields.
    /// </summary>
    /// <remarks>
    /// Rule ID: CCS0004
    /// Category: Naming
    /// Severity: Warning
    ///
    /// This analyzer reports a diagnostic when a private or internal field starts with
    /// an uppercase letter. Fields starting with underscore are allowed as a valid convention.
    /// Const fields are excluded (handled by CCS0005).
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class PrivateFieldCamelCaseAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        /// The diagnostic ID for this analyzer.
        /// </summary>
        public const string DiagnosticId = "CCS0004";

        private static readonly LocalizableString Title = "Private field name should be in camelCase";
        private static readonly LocalizableString MessageFormat = "Private field '{0}' should be in camelCase. Consider: '{1}'";
        private static readonly LocalizableString Description = "Private and internal fields should use camelCase naming convention.";
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
            context.RegisterSyntaxNodeAction(AnalyzeFieldDeclaration, SyntaxKind.FieldDeclaration);
        }

        private void AnalyzeFieldDeclaration(SyntaxNodeAnalysisContext context)
        {
            var fieldDeclaration = (FieldDeclarationSyntax)context.Node;

            // Skip const fields (handled by CCS0005)
            if (fieldDeclaration.Modifiers.Any(SyntaxKind.ConstKeyword))
            {
                return;
            }

            // Only check private or internal fields
            if (!IsPrivateOrInternal(fieldDeclaration))
            {
                return;
            }

            // Check each variable in the declaration (e.g., private int a, b, c;)
            foreach (var variable in fieldDeclaration.Declaration.Variables)
            {
                var fieldName = variable.Identifier.ValueText;

                if (string.IsNullOrEmpty(fieldName))
                {
                    continue;
                }

                // Skip if starts with underscore (valid convention)
                if (fieldName[0] == '_')
                {
                    continue;
                }

                // Skip if already camelCase (starts with lowercase)
                if (char.IsLower(fieldName[0]))
                {
                    continue;
                }

                var suggestedName = NamingUtilities.ToCamelCase(fieldName);
                var diagnostic = Diagnostic.Create(
                    Rule,
                    variable.Identifier.GetLocation(),
                    fieldName,
                    suggestedName);
                context.ReportDiagnostic(diagnostic);
            }
        }

        /// <summary>
        /// Determines if a field is private or internal (but not protected internal).
        /// </summary>
        private static bool IsPrivateOrInternal(FieldDeclarationSyntax field)
        {
            var modifiers = field.Modifiers;

            // If no access modifier, it's private by default
            if (!modifiers.Any(SyntaxKind.PublicKeyword) &&
                !modifiers.Any(SyntaxKind.ProtectedKeyword) &&
                !modifiers.Any(SyntaxKind.InternalKeyword) &&
                !modifiers.Any(SyntaxKind.PrivateKeyword))
            {
                return true; // Default is private
            }

            // Explicit private (includes 'private protected')
            if (modifiers.Any(SyntaxKind.PrivateKeyword))
            {
                return true;
            }

            // Internal without protected (protected internal is more accessible)
            if (modifiers.Any(SyntaxKind.InternalKeyword) &&
                !modifiers.Any(SyntaxKind.ProtectedKeyword))
            {
                return true;
            }

            return false;
        }
    }
}
