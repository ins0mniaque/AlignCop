using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using AlignCop.Analyzers.Internal;

namespace AlignCop.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AlignEnumValuesAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        id: RuleIdentifiers.AlignEnumValues,
        title: new LocalizableResourceString(nameof(Resources.AlignEnumValuesTitle), Resources.ResourceManager, typeof(Resources)),
        messageFormat: new LocalizableResourceString(nameof(Resources.AlignEnumValuesMessageFormat), Resources.ResourceManager, typeof(Resources)),
        category: RuleCategories.Readability,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: new LocalizableResourceString(nameof(Resources.AlignEnumValuesDescription), Resources.ResourceManager, typeof(Resources)),
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.AlignEnumValues));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeEnumDeclarationAction, SyntaxKind.EnumDeclaration);
    }

    private static readonly Action<SyntaxNodeAnalysisContext> AnalyzeEnumDeclarationAction = AnalyzeEnumDeclaration;

    private static void AnalyzeEnumDeclaration(SyntaxNodeAnalysisContext context)
    {
        var enumDeclaration = (EnumDeclarationSyntax)context.Node;
        if (enumDeclaration.Members.Count < 2)
            return;

        var previousLineSpan = default(FileLinePositionSpan);
        var blockIndex       = -1;

        for (var index = 0; index < enumDeclaration.Members.Count; index++)
        {
            var currentMember   = enumDeclaration.Members[index];
            var currentLineSpan = currentMember.GetLineSpan();

            var isNotOnNextLine = index > 0 && (previousLineSpan.StartLinePosition.Character != currentLineSpan.StartLinePosition.Character ||
                                                previousLineSpan.StartLinePosition.Line + 1  != currentLineSpan.StartLinePosition.Line);

            if (isNotOnNextLine || currentMember.EqualsValue is null)
            {
                if (blockIndex >= 0)
                    AnalyzeEnumDeclarationBlock(context, enumDeclaration, blockIndex, index);

                blockIndex = -1;
            }
            else if (blockIndex < 0)
                blockIndex = index;

            previousLineSpan = currentLineSpan;
        }

        if (blockIndex >= 0)
            AnalyzeEnumDeclarationBlock(context, enumDeclaration, blockIndex, enumDeclaration.Members.Count);
    }

    private static void AnalyzeEnumDeclarationBlock(SyntaxNodeAnalysisContext context, EnumDeclarationSyntax enumDeclaration, int startIndex, int endIndex)
    {
        var aligned = true;

        var equalColumns   = new int[endIndex - startIndex];
        var maxEqualColumn = -1;
        
        for (var index = startIndex; index < endIndex; index++)
        {
            if (enumDeclaration.Members[index].EqualsValue is not { } equal)
            {
                equalColumns[index] = -1;
                continue;
            }

            var equalColumn = equalColumns[index] = equal.EqualsToken.GetLineSpan().StartLinePosition.Character;
            if (maxEqualColumn > 0 && equalColumn != maxEqualColumn)
                aligned = false;

            if (equalColumn > maxEqualColumn)
                maxEqualColumn = equalColumn;
        }

        if (aligned)
            return;

        var locations = new List<Location>(endIndex - startIndex);
        for (var index = startIndex; index < endIndex; index++)
            if (equalColumns[index] >= 0)
                locations.Add(enumDeclaration.Members[index].EqualsValue.GetLocation());

        context.ReportDiagnostic(Diagnostic.Create(Rule, locations[0], locations.Skip(1), enumDeclaration.Identifier.Text));
    }
}