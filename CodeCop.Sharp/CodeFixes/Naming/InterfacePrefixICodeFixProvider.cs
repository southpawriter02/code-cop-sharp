using CodeCop.Sharp.Analyzers.Naming;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCop.Sharp.CodeFixes.Naming
{
    /// <summary>
    /// Code fix provider for CCS0003 (InterfacePrefixI).
    /// Renames interfaces to include 'I' prefix and updates all references.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(InterfacePrefixICodeFixProvider)), Shared]
    public class InterfacePrefixICodeFixProvider : CodeFixProvider
    {
        /// <inheritdoc/>
        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(InterfacePrefixIAnalyzer.DiagnosticId);

        /// <inheritdoc/>
        public sealed override FixAllProvider GetFixAllProvider()
            => WellKnownFixAllProviders.BatchFixer;

        /// <inheritdoc/>
        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var declaration = root.FindToken(diagnosticSpan.Start)
                .Parent
                .AncestorsAndSelf()
                .OfType<InterfaceDeclarationSyntax>()
                .First();

            var interfaceName = declaration.Identifier.ValueText;
            var newName = InterfacePrefixIAnalyzer.SuggestInterfaceName(interfaceName);

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: $"Rename to '{newName}'",
                    createChangedSolution: c => RenameInterfaceAsync(context.Document, declaration, newName, c),
                    equivalenceKey: nameof(InterfacePrefixICodeFixProvider)),
                diagnostic);
        }

        private async Task<Solution> RenameInterfaceAsync(
            Document document,
            InterfaceDeclarationSyntax interfaceDeclaration,
            string newName,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var interfaceSymbol = semanticModel.GetDeclaredSymbol(interfaceDeclaration, cancellationToken);

            var solution = document.Project.Solution;
            var newSolution = await Renamer.RenameSymbolAsync(
                solution,
                interfaceSymbol,
                newName,
                solution.Workspace.Options,
                cancellationToken).ConfigureAwait(false);

            return newSolution;
        }
    }
}
