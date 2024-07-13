using System.Runtime.CompilerServices;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace AlignCop.Analyzers;

internal static class AlignmentFixer
{
    public static async Task<Document> FixAlignment<T>(Document document, IReadOnlyList<T> nodes, T firstNode, T lastNode, Selector<T, SyntaxNode?> getNodeToAlign, CancellationToken cancellationToken) where T : SyntaxNode
    {
        if (GenerateAlignmentFix(nodes, firstNode, lastNode, getNodeToAlign) is not { } changes)
            return document;

        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

        return document.WithText(text.WithChanges(changes));
    }

    private static unsafe List<TextChange>? GenerateAlignmentFix<T>(IReadOnlyList<T> nodes, T firstNode, T lastNode, Selector<T, SyntaxNode?> getNodeToAlign) where T : SyntaxNode
    {
        IndexOfStartAndLength(nodes, firstNode, lastNode, out var startIndex, out var length);

        if (length < 0)
            return null;

        const int stackallocThreshold = Allocator.MaximumStackAllocationSize / sizeof(int);

        var columns   = length <= stackallocThreshold ? stackalloc int[length] : new int[length];
        var maxColumn = -1;

        for (var index = 0; index < length; index++)
        {
            getNodeToAlign(nodes[startIndex + index], out var nodeToAlign);

            if (nodeToAlign is null)
            {
                columns[index] = -1;
                continue;
            }

            var column = columns[index] = nodeToAlign.GetLineSpan().StartLinePosition.Character;
            if (column > maxColumn)
                maxColumn = column;
        }

        if (maxColumn < 0)
            return null;

        var changes = new List<TextChange>(length);

        for (var index = 0; index < length; index++)
        {
            getNodeToAlign(nodes[startIndex + index], out var nodeToAlign);

            if (nodeToAlign is null)
                continue;

            var column = columns[index];
            if (column < maxColumn)
                changes.Add(GenerateAlignmentChange(nodeToAlign, maxColumn - column));
        }

        if (changes.Count is 0)
            return null;

        return changes;
    }

    public static async Task<Document> FixAlignment<T>(Document document, IReadOnlyList<T> nodes, T firstNode, T lastNode, Selector<T, SyntaxNode?, SyntaxNode?> getNodesToAlign, CancellationToken cancellationToken) where T : SyntaxNode
    {
        if (GenerateAlignmentFix(nodes, firstNode, lastNode, getNodesToAlign) is not { } changes)
            return document;

        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

        return document.WithText(text.WithChanges(changes));
    }

    private static unsafe List<TextChange>? GenerateAlignmentFix<T>(IReadOnlyList<T> nodes, T firstNode, T lastNode, Selector<T, SyntaxNode?, SyntaxNode?> getNodesToAlign) where T : SyntaxNode
    {
        IndexOfStartAndLength(nodes, firstNode, lastNode, out var startIndex, out var length);

        if (length < 0)
            return null;

        const int stackallocThreshold = Allocator.MaximumStackAllocationSize / sizeof(int) / 2;

        var columnsA   = length <= stackallocThreshold ? stackalloc int[length] : new int[length];
        var columnsB   = length <= stackallocThreshold ? stackalloc int[length] : new int[length];
        var maxColumnA = -1;
        var maxColumnB = -1;

        for (var index = 0; index < length; index++)
        {
            getNodesToAlign(nodes[startIndex + index], out var nodeToAlignA, out var nodeToAlignB);

            if (nodeToAlignA is null)
            {
                columnsA[index] = -1;
                columnsB[index] = -1;
                continue;
            }

            var columnA = columnsA[index] = nodeToAlignA.GetLineSpan().StartLinePosition.Character;

            if (columnA > maxColumnA)
                maxColumnA = columnA;

            if (nodeToAlignB is null)
            {
                columnsB[index] = -1;
                continue;
            }

            var columnB = columnsB[index] = nodeToAlignB.GetLineSpan().StartLinePosition.Character;

            if (columnB > maxColumnB)
                maxColumnB = columnB;
        }

        if (maxColumnA < 0)
            return null;

        var changes = new List<TextChange>(length);

        for (var index = 0; index < length; index++)
        {
            getNodesToAlign(nodes[startIndex + index], out var nodeToAlignA, out var nodeToAlignB);

            if (nodeToAlignA is null)
                continue;

            var changeA = 0;
            var columnA = columnsA[index];
            if (columnA < maxColumnA)
                changes.Add(GenerateAlignmentChange(nodeToAlignA, changeA = maxColumnA - columnA));

            if (nodeToAlignB is null)
                continue;

            var columnB = columnsB[index];
            if (columnB >= 0 && columnB + changeA < maxColumnB)
                changes.Add(GenerateAlignmentChange(nodeToAlignB, maxColumnB - columnB - changeA));
        }

        if (changes.Count is 0)
            return null;

        return changes;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static TextChange GenerateAlignmentChange(SyntaxNode node, int spaces)
    {
        return new TextChange(new TextSpan(node.Span.Start, 0), new string(' ', spaces));
    }

    private static void IndexOfStartAndLength<T>(IReadOnlyList<T> nodes, T firstNode, T lastNode, out int startIndex, out int length) where T : SyntaxNode
    {
        startIndex = -1;
        length     = -1;

        for (var index = 0; index < nodes.Count; index++)
        {
            var node = nodes[index];

            if (startIndex < 0)
            {
                if (node == firstNode)
                    startIndex = index;
                else
                    continue;
            }

            if (node == lastNode)
            {
                length = index + 1 - startIndex;
                break;
            }
        }
    }
}