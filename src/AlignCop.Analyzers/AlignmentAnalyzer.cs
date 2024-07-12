using System.Runtime.CompilerServices;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace AlignCop.Analyzers;

internal static class AlignmentAnalyzer
{
    public static IEnumerable<Unalignment> FindUnalignments<T>(IReadOnlyList<T> nodes, Selector<T, SyntaxNode?> getNodeToAlign) where T : SyntaxNode
    {
        if (nodes.Count < 2)
            yield break;

        var previousLineSpan = default(FileLinePositionSpan);
        var startIndex       = -1;

        for (var index = 0; index < nodes.Count; index++)
        {
            var node     = nodes[index];
            var lineSpan = node.GetLineSpan();

            var alignable = !lineSpan.SpansMultipleLines();

            if (alignable)
            {
                getNodeToAlign(node, out var nodeToAlign);

                alignable = nodeToAlign is not null;
            }

            if (!alignable || index > 0 && !lineSpan.IsOnNextLineAlignedTo(previousLineSpan))
            {
                if (startIndex >= 0 && FindUnalignment(nodes, getNodeToAlign, startIndex, index - startIndex) is { } unalignment)
                    yield return unalignment;

                startIndex = alignable ? index : -1;
            }
            else if (startIndex < 0)
                startIndex = index;

            previousLineSpan = lineSpan;
        }

        if (startIndex >= 0 && FindUnalignment(nodes, getNodeToAlign, startIndex, nodes.Count - startIndex) is { } lastUnalignment)
            yield return lastUnalignment;
    }

    public static IEnumerable<Unalignment> FindUnalignments<T>(IReadOnlyList<T> nodes, Selector<T, SyntaxNode?, SyntaxNode?> getNodesToAlign) where T : SyntaxNode
    {
        if (nodes.Count < 2)
            yield break;

        var previousLineSpan = default(FileLinePositionSpan);
        var startIndex       = -1;

        for (var index = 0; index < nodes.Count; index++)
        {
            var node     = nodes[index];
            var lineSpan = node.GetLineSpan();

            var alignable = !lineSpan.SpansMultipleLines();

            if (alignable)
            {
                getNodesToAlign(node, out var nodeToAlignA, out _);

                alignable = nodeToAlignA is not null;
            }

            if (!alignable || index > 0 && !lineSpan.IsOnNextLineAlignedTo(previousLineSpan))
            {
                if (startIndex >= 0 && FindUnalignment(nodes, getNodesToAlign, startIndex, index - startIndex) is { } unalignment)
                    yield return unalignment;

                startIndex = alignable ? index : -1;
            }
            else if (startIndex < 0)
                startIndex = index;

            previousLineSpan = lineSpan;
        }

        if (startIndex >= 0 && FindUnalignment(nodes, getNodesToAlign, startIndex, nodes.Count - startIndex) is { } lastUnalignment)
            yield return lastUnalignment;
    }

    private static Unalignment? FindUnalignment<T>(IReadOnlyList<T> nodes, Selector<T, SyntaxNode?> getNodeToAlign, int startIndex, int length) where T : SyntaxNode
    {
        const int stackallocThreshold = Allocator.MaximumStackAllocationSize / sizeof(int);

        var columns     = length <= stackallocThreshold ? stackalloc int[length] : new int[length];
        var firstColumn = -1;
        var aligned     = true;

        for (var index = 0; index < length; index++)
        {
            getNodeToAlign(nodes[startIndex + index], out var nodeToAlign);

            if (nodeToAlign is null)
            {
                columns[index] = -1;
                continue;
            }

            var column = nodeToAlign.GetLineSpan().StartLinePosition.Character;

            columns[index] = column;

            if (firstColumn < 0)
                firstColumn = column;
            else if (column != firstColumn)
                aligned = false;
        }

        if (!aligned)
        {
            var unalignment = new Unalignment(length);

            for (var index = 0; index < length; index++)
            {
                if (columns[index] < 0)
                    continue;

                getNodeToAlign(nodes[startIndex + index], out var nodeToAlign);

                if (nodeToAlign is not null)
                    unalignment.Add(nodeToAlign.GetLocation());
            }

            return unalignment;
        }

        return null;
    }

    private static unsafe Unalignment? FindUnalignment<T>(IReadOnlyList<T> nodes, Selector<T, SyntaxNode?, SyntaxNode?> getNodesToAlign, int startIndex, int length) where T : SyntaxNode
    {
        const int stackallocThreshold = Allocator.MaximumStackAllocationSize / sizeof(int) / 2;

        var columnsA     = length <= stackallocThreshold ? stackalloc int[length] : new int[length];
        var columnsB     = length <= stackallocThreshold ? stackalloc int[length] : new int[length];
        var firstColumnA = -1;
        var firstColumnB = -1;
        var alignedA     = true;
        var alignedB     = true;

        for (var index = 0; index < length; index++)
        {
            getNodesToAlign(nodes[startIndex + index], out var nodeToAlignA, out var nodeToAlignB);

            if (nodeToAlignA is null)
            {
                columnsA[index] = -1;
                columnsB[index] = -1;
                continue;
            }

            var columnA = nodeToAlignA.GetLineSpan().StartLinePosition.Character;

            columnsA[index] = columnA;

            if (firstColumnA < 0)
                firstColumnA = columnA;
            else if (columnA != firstColumnA)
                alignedA = false;

            if (nodeToAlignB is null)
            {
                columnsB[index] = -1;
                continue;
            }

            var columnB = nodeToAlignB.GetLineSpan().StartLinePosition.Character;

            columnsB[index] = columnB;

            if (firstColumnB < 0)
                firstColumnB = columnB;
            else if (columnB != firstColumnB)
                alignedB = false;
        }

        if (!alignedA)
        {
            var unalignment = new Unalignment(length);

            for (var index = 0; index < length; index++)
            {
                getNodesToAlign(nodes[startIndex + index], out var nodeToAlignA, out var nodeToAlignB);

                if (columnsA[index] >= 0 && nodeToAlignA is not null)
                {
                    if (columnsB[index] >= 0 && nodeToAlignB is not null)
                        unalignment.Add(nodeToAlignA.GetLocation().CombineWith(nodeToAlignB.GetLocation()));
                    else
                        unalignment.Add(nodeToAlignA.GetLocation());
                }
            }

            return unalignment;
        }
        else if (!alignedB)
        {
            var unalignment = new Unalignment(length);

            for (var index = 0; index < length; index++)
            {
                getNodesToAlign(nodes[startIndex + index], out _, out var nodeToAlignB);

                if (columnsB[index] >= 0 && nodeToAlignB is not null)
                    unalignment.Add(nodeToAlignB.GetLocation());
            }

            return unalignment;
        }

        return null;
    }

    /// <summary>
    /// Gets the location in terms of path, line and column for a given node.
    /// </summary>
    /// <param name="node">The node to use.</param>
    /// <returns>The location in terms of path, line and column for a given node.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FileLinePositionSpan GetLineSpan(this SyntaxNode node)
    {
        return node.SyntaxTree.GetLineSpan(node.Span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Location CombineWith(this Location location, Location otherLocation)
    {
        return Location.Create(location.SourceTree ?? throw new ArgumentException("Location must be in a syntax tree", nameof(location)),
                               TextSpan.FromBounds(location     .SourceSpan.Start,
                                                   otherLocation.SourceSpan.End));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsOnNextLineAlignedTo(this FileLinePositionSpan lineSpan, FileLinePositionSpan previousLineSpan)
    {
        return lineSpan.StartLinePosition.Character == previousLineSpan.StartLinePosition.Character &&
               lineSpan.StartLinePosition.Line      == previousLineSpan.StartLinePosition.Line + 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool SpansMultipleLines(this FileLinePositionSpan lineSpan)
    {
        return lineSpan.StartLinePosition.Line != lineSpan.EndLinePosition.Line;
    }
}