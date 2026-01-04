using CodeCop.Sharp.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace CodeCop.Sharp.Analyzers.Naming
{
    /// <summary>
    /// Analyzer that enforces interface names start with 'I' prefix.
    /// </summary>
    /// <remarks>
    /// Rule ID: CCS0003
    /// Category: Naming
    /// Severity: Warning
    ///
    /// This analyzer reports a diagnostic when an interface name doesn't start with
    /// uppercase 'I' followed by another uppercase letter (or just 'I' alone).
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class InterfacePrefixIAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        /// The diagnostic ID for this analyzer.
        /// </summary>
        public const string DiagnosticId = "CCS0003";

        private static readonly LocalizableString Title = "Interface name should start with 'I'";
        private static readonly LocalizableString MessageFormat = "Interface name '{0}' should start with 'I'. Consider: '{1}'";
        private static readonly LocalizableString Description = "Interface names should start with 'I' followed by PascalCase.";
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
            context.RegisterSyntaxNodeAction(AnalyzeInterfaceDeclaration, SyntaxKind.InterfaceDeclaration);
        }

        private void AnalyzeInterfaceDeclaration(SyntaxNodeAnalysisContext context)
        {
            var interfaceDeclaration = (InterfaceDeclarationSyntax)context.Node;
            var interfaceName = interfaceDeclaration.Identifier.ValueText;

            if (string.IsNullOrEmpty(interfaceName))
            {
                return;
            }

            // Check if starts with valid 'I' prefix
            if (StartsWithValidIPrefix(interfaceName))
            {
                return;
            }

            var suggestedName = SuggestInterfaceName(interfaceName);
            var diagnostic = Diagnostic.Create(Rule, interfaceDeclaration.Identifier.GetLocation(), interfaceName, suggestedName);
            context.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        /// Checks if the interface name starts with a valid 'I' prefix.
        /// Valid: "I", "IService", "IMyClass" (I + uppercase)
        /// Invalid: "Service", "iService", "Iservice" (I + lowercase)
        /// </summary>
        private static bool StartsWithValidIPrefix(string name)
        {
            if (name.Length == 1)
            {
                return name == "I";
            }

            // Must start with 'I' followed by uppercase letter or digit
            return name[0] == 'I' && (char.IsUpper(name[1]) || char.IsDigit(name[1]));
        }

        /// <summary>
        /// Suggests a valid interface name by adding or fixing the 'I' prefix.
        /// </summary>
        public static string SuggestInterfaceName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }

            // If starts with lowercase 'i', replace with 'I' and ensure next char is upper
            if (name[0] == 'i')
            {
                if (name.Length == 1)
                {
                    return "I";
                }
                return "I" + char.ToUpperInvariant(name[1]) + name.Substring(2);
            }

            // Otherwise, prepend 'I' and ensure PascalCase
            // This handles: "Service" -> "IService", "Inner" -> "IInner", "Iservice" -> "IIservice"
            return "I" + NamingUtilities.ToPascalCase(name);
        }
    }
}
