using System.Collections.Immutable;
using System.Composition;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AlignCop.Analyzers.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AlignVariableAssignmentsFixer)), Shared]
public sealed class AlignVariableAssignmentsFixer : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } = [RuleIdentifiers.AlignVariableAssignments];

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false) ??
                   throw new InvalidOperationException("Could not get document syntax root");

        var diagnostic = context.Diagnostics[0];
        var firstSpan  = diagnostic.Location.SourceSpan;
        var lastSpan   = diagnostic.GetLastLocation ( ).SourceSpan;

        if (root.FindToken(firstSpan.End).Parent is not LocalDeclarationStatementSyntax firstStatement ||
            root.FindToken(lastSpan .End).Parent is not LocalDeclarationStatementSyntax lastStatement  ||
            firstStatement.Parent                is not BlockSyntax                     block)
            return;

        var action = CodeAction.Create
        (
            title:                 CodeFixResources.AlignVariableAssignmentsCodeFixTitle,
            createChangedDocument: FixAlignment,
            equivalenceKey:        nameof(CodeFixResources.AlignVariableAssignmentsCodeFixTitle)
        );

        context.RegisterCodeFix(action, diagnostic);

        Task<Document> FixAlignment(CancellationToken cancellationToken)
        {
            return AlignmentFixer.FixAlignment(context.Document,
                                               block.Statements, firstStatement, lastStatement,
                                               AlignVariableAssignmentsAnalyzer.GetNodesToAlignSelector,
                                               cancellationToken);
        }
    }
}