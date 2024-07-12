using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

using AlignCop.Analyzers.Internal;

namespace AlignCop.Analyzers;

internal static class AlignmentAnalyzer
{
    public static IEnumerable<List<Location>> FindUnalignments<T>(IReadOnlyList<T> elements, Func<T, SyntaxNode?> getNodeToAlign) where T : SyntaxNode
    {
        if (elements.Count < 2)
            yield break;

        var previousLineSpan = default(FileLinePositionSpan);
        var startIndex       = -1;

        for (var index = 0; index < elements.Count; index++)
        {
            var element     = elements[index];
            var lineSpan    = element.GetLineSpan();
            var nodeToAlign = getNodeToAlign(element);

            var isNotOnNextLine = index > 0 && (previousLineSpan.StartLinePosition.Character != lineSpan.StartLinePosition.Character ||
                                                previousLineSpan.StartLinePosition.Line + 1  != lineSpan.StartLinePosition.Line);

            if (isNotOnNextLine || nodeToAlign is null)
            {
                if (startIndex >= 0 && FindUnalignment(elements, getNodeToAlign, startIndex, index - startIndex) is { } unalignment)
                    yield return unalignment;

                startIndex = nodeToAlign is null ? -1 : index;
            }
            else if (startIndex < 0)
                startIndex = index;

            previousLineSpan = lineSpan;
        }

        if (startIndex >= 0 && FindUnalignment(elements, getNodeToAlign, startIndex, elements.Count - startIndex) is { } lastUnalignment)
            yield return lastUnalignment;
    }

    public static IEnumerable<List<Location>> FindUnalignments<T>(IReadOnlyList<T> elements, Func<T, SyntaxNode?> getLeftNodeToAlign, Func<T, SyntaxNode?> getRightNodeToAlign) where T : SyntaxNode
    {
        if (elements.Count < 2)
            yield break;

        var previousLineSpan = default(FileLinePositionSpan);
        var startIndex       = -1;

        for (var index = 0; index < elements.Count; index++)
        {
            var element         = elements[index];
            var lineSpan        = element.GetLineSpan();
            var leftNodeToAlign = getLeftNodeToAlign(element);

            var isNotOnNextLine = index > 0 && (previousLineSpan.StartLinePosition.Character != lineSpan.StartLinePosition.Character ||
                                                previousLineSpan.StartLinePosition.Line + 1  != lineSpan.StartLinePosition.Line);

            if (isNotOnNextLine || leftNodeToAlign is null)
            {
                if (startIndex >= 0 && FindUnalignment(elements, getLeftNodeToAlign, getRightNodeToAlign, startIndex, index - startIndex) is { } unalignment)
                    yield return unalignment;

                startIndex = leftNodeToAlign is null ? -1 : index;
            }
            else if (startIndex < 0)
                startIndex = index;

            previousLineSpan = lineSpan;
        }

        if (startIndex >= 0 && FindUnalignment(elements, getLeftNodeToAlign, getRightNodeToAlign, startIndex, elements.Count - startIndex) is { } lastUnalignment)
            yield return lastUnalignment;
    }

    private static List<Location>? FindUnalignment<T>(IReadOnlyList<T> elements, Func<T, SyntaxNode?> getNodeToAlign, int startIndex, int length) where T : SyntaxNode
    {
        var aligned = true;

        var columns     = new int[length];
        var firstColumn = -1;

        for (var index = 0; index < length; index++)
        {
            var element = elements[startIndex + index];

            if (getNodeToAlign(element) is not { } nodeToAlign)
            {
                columns[index] = -1;
                continue;
            }

            var column = columns[index] = nodeToAlign.GetLineSpan().StartLinePosition.Character;

            if (firstColumn < 0)
                firstColumn = column;
            else if(column != firstColumn)
                aligned = false;
        }

        if (!aligned)
        {
            var locations = new List<Location>(length);
            for (var index = 0; index < length; index++)
                if (columns[index] >= 0 && getNodeToAlign(elements[startIndex + index]) is { } nodeToAlign)
                    locations.Add(nodeToAlign.GetLocation());

            return locations;
        }

        return null;
    }

    private static List<Location>? FindUnalignment<T>(IReadOnlyList<T> elements, Func<T, SyntaxNode?> getLeftNodeToAlign, Func<T, SyntaxNode?> getRightNodeToAlign, int startIndex, int length) where T : SyntaxNode
    {
        var leftAligned  = true;
        var rightAligned = true;

        var leftColumns      = new int[length];
        var rightColumns     = new int[length];
        var firstLeftColumn  = -1;
        var firstRightColumn = -1;

        for (var index = 0; index < length; index++)
        {
            var element = elements[startIndex + index];

            if (getLeftNodeToAlign(element) is not { } leftNodeToAlign)
            {
                leftColumns[index]  = -1;
                rightColumns[index] = -1;
                continue;
            }

            var leftColumn = leftColumns[index] = leftNodeToAlign.GetLineSpan().StartLinePosition.Character;

            if (firstLeftColumn < 0)
                firstLeftColumn = leftColumn;
            else if(leftColumn != firstLeftColumn)
                leftAligned = false;

            if (getRightNodeToAlign(element) is not { } rightNodeToAlign)
            {
                rightColumns[index] = -1;
                continue;
            }

            var rightColumn = rightColumns[index] = rightNodeToAlign.GetLineSpan().StartLinePosition.Character;

            if (firstRightColumn < 0)
                firstRightColumn = rightColumn;
            else if(rightColumn != firstRightColumn)
                rightAligned = false;
        }

        if (!leftAligned)
        {
            var locations = new List<Location>(length);
            for (var index = 0; index < length; index++)
            {
                var element = elements[startIndex + index];

                if (leftColumns[index] >= 0 && getLeftNodeToAlign(element) is { } leftNodeToAlign)
                {
                    if (rightColumns[index] >= 0 && getRightNodeToAlign(element) is { } rightNodeToAlign)
                        locations.Add(Location.Create(leftNodeToAlign.SyntaxTree,
                                                      TextSpan.FromBounds(leftNodeToAlign.GetLocation().SourceSpan.Start,
                                                                          rightNodeToAlign.GetLocation().SourceSpan.End)));
                    else
                        locations.Add(leftNodeToAlign.GetLocation());
                }
            }

            return locations;
        }
        else if(!rightAligned)
        {
            var locations = new List<Location>(length);
            for (var index = 0; index < length; index++)
                if (rightColumns[index] >= 0 && getRightNodeToAlign(elements[startIndex + index]) is { } rightNodeToAlign)
                    locations.Add(rightNodeToAlign.GetLocation());

            return locations;
        }

        return null;
    }
}