using System;
using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using AlignCop.Analyzers.Internal;

namespace AlignCop.Analyzers;

public static class AlignmentAnalyzer
{
    public static IEnumerable<List<Location>> FindUnalignments<T>(SyntaxNodeAnalysisContext context, IReadOnlyList<T> elements, Func<T, EqualsValueClauseSyntax?> getEqualsValueClause) where T : SyntaxNode
    {
        if (elements.Count < 2)
            yield break;

        var previousLineSpan = default(FileLinePositionSpan);
        var blockIndex       = -1;

        for (var index = 0; index < elements.Count; index++)
        {
            var currentElement  = elements[index];
            var currentLineSpan = currentElement.GetLineSpan();
            var equal           = getEqualsValueClause(currentElement);

            var isNotOnNextLine = index > 0 && (previousLineSpan.StartLinePosition.Character != currentLineSpan.StartLinePosition.Character ||
                                                previousLineSpan.StartLinePosition.Line + 1  != currentLineSpan.StartLinePosition.Line);

            if (isNotOnNextLine || equal is null)
            {
                if (blockIndex >= 0 && FindUnalignment(context, elements, getEqualsValueClause, blockIndex, index) is { } unalignment)
                    yield return unalignment;

                blockIndex = equal is null ? -1 : index;
            }
            else if (blockIndex < 0)
                blockIndex = index;

            previousLineSpan = currentLineSpan;
        }

        if (blockIndex >= 0 && FindUnalignment(context, elements, getEqualsValueClause, blockIndex, elements.Count) is { } lastUnalignment)
            yield return lastUnalignment;
    }

    private static List<Location>? FindUnalignment<T>(SyntaxNodeAnalysisContext context, IReadOnlyList<T> elements, Func<T, EqualsValueClauseSyntax?> getEqualsValueClause, int startIndex, int endIndex) where T : SyntaxNode
    {
        var aligned = true;

        var equalColumns   = new int[endIndex - startIndex];
        var maxEqualColumn = -1;

        for (var index = startIndex; index < endIndex; index++)
        {
            if (getEqualsValueClause(elements[index]) is not { } equal)
            {
                equalColumns[index] = -1;
                continue;
            }

            var equalColumn = equalColumns[index - startIndex] = equal.EqualsToken.GetLineSpan().StartLinePosition.Character;
            if (maxEqualColumn > 0 && equalColumn != maxEqualColumn)
                aligned = false;

            if (equalColumn > maxEqualColumn)
                maxEqualColumn = equalColumn;
        }

        if (aligned)
            return null;

        var locations = new List<Location>(endIndex - startIndex);
        for (var index = startIndex; index < endIndex; index++)
            if (equalColumns[index - startIndex] >= 0)
                locations.Add(getEqualsValueClause(elements[index]).GetLocation());

        return locations;
    }
}