using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AlignCop.Analyzers.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AlignVariableAssignmentsFixer)), Shared]
public class AlignVariableAssignmentsFixer : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.AlignVariableAssignments);

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root       = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics.First();

        var firstSpan = diagnostic.Location.SourceSpan;
        var lastSpan  = diagnostic.AdditionalLocations.LastOrDefault()?.SourceSpan ?? firstSpan;

        var firstVariableAssignment = root.FindToken(firstSpan.Start).Parent.Parent.Parent.Parent as LocalDeclarationStatementSyntax;
        var lastVariableAssignment  = root.FindToken(lastSpan.Start).Parent.Parent.Parent.Parent as LocalDeclarationStatementSyntax;

        if (firstVariableAssignment is null || lastVariableAssignment is null)
            return;

        var block = firstVariableAssignment.Parent as BlockSyntax;
        if (block is null)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: CodeFixResources.AlignVariableAssignmentsCodeFixTitle,
                createChangedDocument: cancellationToken => AlignmentFixer.FixUnalignment(context.Document, block.Statements, firstVariableAssignment, lastVariableAssignment, GetEqualsValueClauseFunc, cancellationToken),
                equivalenceKey: nameof(CodeFixResources.AlignVariableAssignmentsCodeFixTitle)),
            diagnostic);
    }

    private static readonly Func<StatementSyntax, EqualsValueClauseSyntax?> GetEqualsValueClauseFunc = GetEqualsValueClause;

    private static EqualsValueClauseSyntax? GetEqualsValueClause(StatementSyntax statementSyntax)
    {
        if (statementSyntax is LocalDeclarationStatementSyntax localDeclarationStatement && localDeclarationStatement.Declaration.Variables.Count is 1)
            return localDeclarationStatement.Declaration.Variables[0].Initializer;

        return null;
    }
}