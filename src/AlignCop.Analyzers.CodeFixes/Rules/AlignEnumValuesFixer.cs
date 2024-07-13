using System.Collections.Immutable;
using System.Composition;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AlignCop.Analyzers.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AlignEnumValuesFixer)), Shared]
public sealed class AlignEnumValuesFixer : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } = [RuleIdentifiers.AlignEnumValues];

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false) ??
                   throw new InvalidOperationException("Could not get document syntax root");

        var diagnostic = context.Diagnostics[0];
        var firstSpan  = diagnostic.Location.SourceSpan;
        var lastSpan   = diagnostic.GetLastLocation ( ).SourceSpan;

        if (root.FindToken(firstSpan.Start).Parent?.Parent is not EnumMemberDeclarationSyntax firstEnumMember ||
            root.FindToken(lastSpan .Start).Parent?.Parent is not EnumMemberDeclarationSyntax lastEnumMember  ||
            firstEnumMember.Parent                         is not EnumDeclarationSyntax       enumDeclaration)
            return;

        var action = CodeAction.Create
        (
            title:                 CodeFixResources.AlignEnumValuesCodeFixTitle,
            createChangedDocument: FixAlignment,
            equivalenceKey:        nameof(CodeFixResources.AlignEnumValuesCodeFixTitle)
        );

        context.RegisterCodeFix(action, diagnostic);

        Task<Document> FixAlignment(CancellationToken cancellationToken)
        {
            return AlignmentFixer.FixAlignment(context.Document,
                                               enumDeclaration.Members, firstEnumMember, lastEnumMember,
                                               AlignEnumValuesAnalyzer.GetNodeToAlignSelector,
                                               cancellationToken);
        }
    }
}