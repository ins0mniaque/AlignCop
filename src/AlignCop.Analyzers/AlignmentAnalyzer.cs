using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace AlignCop.Analyzers;

internal static class AlignmentAnalyzer
{
    public static IEnumerable<List<Location>> FindUnalignments<T>(IReadOnlyList<T> elements, Selector<T, SyntaxNode?> getNodeToAlign) where T : SyntaxNode
    {
        if (elements.Count < 2)
            yield break;

        var previousLineSpan = default(FileLinePositionSpan);
        var startIndex       = -1;

        for (var index = 0; index < elements.Count; index++)
        {
            var element  = elements[index];
            var lineSpan = element.GetLineSpan();

            getNodeToAlign(element, out var nodeToAlign);

            var spansMultipleLine = lineSpan.StartLinePosition.Line != lineSpan.EndLinePosition.Line;
            var isNotOnNextLine   = index > 0 && (previousLineSpan.StartLinePosition.Character != lineSpan.StartLinePosition.Character ||
                                                  previousLineSpan.StartLinePosition.Line + 1  != lineSpan.StartLinePosition.Line);

            if (spansMultipleLine || isNotOnNextLine || nodeToAlign is null)
            {
                if (startIndex >= 0 && FindUnalignment(elements, getNodeToAlign, startIndex, index - startIndex) is { } unalignment)
                    yield return unalignment;

                startIndex = spansMultipleLine || nodeToAlign is null ? -1 : index;
            }
            else if (startIndex < 0)
                startIndex = index;

            previousLineSpan = lineSpan;
        }

        if (startIndex >= 0 && FindUnalignment(elements, getNodeToAlign, startIndex, elements.Count - startIndex) is { } lastUnalignment)
            yield return lastUnalignment;
    }

    public static IEnumerable<List<Location>> FindUnalignments<T>(IReadOnlyList<T> elements, Selector<T, SyntaxNode?, SyntaxNode?> getNodesToAlign) where T : SyntaxNode
    {
        if (elements.Count < 2)
            yield break;

        var previousLineSpan = default(FileLinePositionSpan);
        var startIndex       = -1;

        for (var index = 0; index < elements.Count; index++)
        {
            var element  = elements[index];
            var lineSpan = element.GetLineSpan();

            getNodesToAlign(element, out var nodeToAlignA, out _);

            var spansMultipleLine = lineSpan.StartLinePosition.Line != lineSpan.EndLinePosition.Line;
            var isNotOnNextLine   = index > 0 && (previousLineSpan.StartLinePosition.Character != lineSpan.StartLinePosition.Character ||
                                                  previousLineSpan.StartLinePosition.Line + 1  != lineSpan.StartLinePosition.Line);

            if (spansMultipleLine || isNotOnNextLine || nodeToAlignA is null)
            {
                if (startIndex >= 0 && FindUnalignment(elements, getNodesToAlign, startIndex, index - startIndex) is { } unalignment)
                    yield return unalignment;

                startIndex = spansMultipleLine || nodeToAlignA is null ? -1 : index;
            }
            else if (startIndex < 0)
                startIndex = index;

            previousLineSpan = lineSpan;
        }

        if (startIndex >= 0 && FindUnalignment(elements, getNodesToAlign, startIndex, elements.Count - startIndex) is { } lastUnalignment)
            yield return lastUnalignment;
    }

    private static List<Location>? FindUnalignment<T>(IReadOnlyList<T> elements, Selector<T, SyntaxNode?> getNodeToAlign, int startIndex, int length) where T : SyntaxNode
    {
        var aligned = true;

        var columns     = new int[length];
        var firstColumn = -1;

        for (var index = 0; index < length; index++)
        {
            var element = elements[startIndex + index];

            getNodeToAlign(element, out var nodeToAlign);

            if (nodeToAlign is null)
            {
                columns[index] = -1;
                continue;
            }

            var column = columns[index] = nodeToAlign.GetLineSpan().StartLinePosition.Character;

            if (firstColumn < 0)
                firstColumn = column;
            else if (column != firstColumn)
                aligned = false;
        }

        if (!aligned)
        {
            var locations = new List<Location>(length);
            for (var index = 0; index < length; index++)
            {
                if (columns[index] < 0)
                    continue;

                getNodeToAlign(elements[startIndex + index], out var nodeToAlign);

                if (nodeToAlign is not null)
                    locations.Add(nodeToAlign.GetLocation());
            }

            return locations;
        }

        return null;
    }

    private static List<Location>? FindUnalignment<T>(IReadOnlyList<T> elements, Selector<T, SyntaxNode?, SyntaxNode?> getNodesToAlign, int startIndex, int length) where T : SyntaxNode
    {
        var alignedA = true;
        var alignedB = true;

        var columnsA     = new int[length];
        var columnsB     = new int[length];
        var firstColumnA = -1;
        var firstColumnB = -1;

        for (var index = 0; index < length; index++)
        {
            var element = elements[startIndex + index];

            getNodesToAlign(element, out var nodeToAlignA, out var nodeToAlignB);

            if (nodeToAlignA is null)
            {
                columnsA[index] = -1;
                columnsB[index] = -1;
                continue;
            }

            var columnA = columnsA[index] = nodeToAlignA.GetLineSpan().StartLinePosition.Character;

            if (firstColumnA < 0)
                firstColumnA = columnA;
            else if (columnA != firstColumnA)
                alignedA = false;

            if (nodeToAlignB is null)
            {
                columnsB[index] = -1;
                continue;
            }

            var columnB = columnsB[index] = nodeToAlignB.GetLineSpan().StartLinePosition.Character;

            if (firstColumnB < 0)
                firstColumnB = columnB;
            else if (columnB != firstColumnB)
                alignedB = false;
        }

        if (!alignedA)
        {
            var locations = new List<Location>(length);
            for (var index = 0; index < length; index++)
            {
                var element = elements[startIndex + index];

                getNodesToAlign(element, out var nodeToAlignA, out var nodeToAlignB);

                if (columnsA[index] >= 0 && nodeToAlignA is not null)
                {
                    if (columnsB[index] >= 0 && nodeToAlignB is not null)
                        locations.Add(Location.Create(nodeToAlignA.SyntaxTree,
                                                      TextSpan.FromBounds(nodeToAlignA.GetLocation().SourceSpan.Start,
                                                                          nodeToAlignB.GetLocation().SourceSpan.End)));
                    else
                        locations.Add(nodeToAlignA.GetLocation());
                }
            }

            return locations;
        }
        else if (!alignedB)
        {
            var locations = new List<Location>(length);
            for (var index = 0; index < length; index++)
            {
                var element = elements[startIndex + index];

                getNodesToAlign(element, out _, out var nodeToAlignB);

                if (columnsB[index] >= 0 && nodeToAlignB is not null)
                    locations.Add(nodeToAlignB.GetLocation());
            }

            return locations;
        }

        return null;
    }

    /// <summary>
    /// Gets the location in terms of path, line and column for a given node.
    /// </summary>
    /// <param name="node">The node to use.</param>
    /// <returns>The location in terms of path, line and column for a given node.</returns>
    public static FileLinePositionSpan GetLineSpan(this SyntaxNode node)
    {
        return node.SyntaxTree.GetLineSpan(node.Span);
    }
}