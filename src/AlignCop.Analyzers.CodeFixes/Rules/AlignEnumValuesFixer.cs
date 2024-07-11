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
    public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.AlignEnumValues);

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root       = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics[0];

        var firstSpan = diagnostic.Location.SourceSpan;
        var lastSpan  = diagnostic.AdditionalLocations.Count > 0 ? diagnostic.AdditionalLocations[diagnostic.AdditionalLocations.Count - 1].SourceSpan : firstSpan;

        if (root.FindToken(firstSpan.Start).Parent.Parent is not EnumMemberDeclarationSyntax firstEnumMember ||
            root.FindToken(lastSpan.Start).Parent.Parent is not EnumMemberDeclarationSyntax lastEnumMember)
            return;

        if (firstEnumMember.Parent is not EnumDeclarationSyntax enumDeclaration)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: CodeFixResources.AlignEnumValuesCodeFixTitle,
                createChangedDocument: cancellationToken => AlignmentFixer.FixUnalignment(context.Document, enumDeclaration.Members, firstEnumMember, lastEnumMember, GetEqualsValueClauseFunc, cancellationToken),
                equivalenceKey: nameof(CodeFixResources.AlignEnumValuesCodeFixTitle)),
            diagnostic);
    }

    private static readonly Func<EnumMemberDeclarationSyntax, EqualsValueClauseSyntax?> GetEqualsValueClauseFunc = GetEqualsValueClause;

    private static EqualsValueClauseSyntax? GetEqualsValueClause(EnumMemberDeclarationSyntax enumDeclaration)
    {
        return enumDeclaration.EqualsValue;
    }
}