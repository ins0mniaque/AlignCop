using Microsoft.CodeAnalysis;

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

    private static List<Location>? FindUnalignment<T>(IReadOnlyList<T> elements, Func<T, SyntaxNode?> getNodeToAlign, int startIndex, int length) where T : SyntaxNode
    {
        var aligned = true;

        var columns   = new int[length];
        var maxColumn = -1;

        for (var index = 0; index < length; index++)
        {
            if (getNodeToAlign(elements[startIndex + index]) is not { } nodeToAlign)
            {
                columns[index] = -1;
                continue;
            }

            var column = columns[index] = nodeToAlign.GetLineSpan().StartLinePosition.Character;
            if (maxColumn > 0 && column != maxColumn)
                aligned = false;

            if (column > maxColumn)
                maxColumn = column;
        }

        if (aligned)
            return null;

        var locations = new List<Location>(length);
        for (var index = 0; index < length; index++)
            if (columns[index] >= 0 && getNodeToAlign(elements[startIndex + index]) is { } nodeToAlign)
                locations.Add(nodeToAlign.GetLocation());

        return locations;
    }
}