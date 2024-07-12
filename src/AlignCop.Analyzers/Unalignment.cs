using Microsoft.CodeAnalysis;

namespace AlignCop.Analyzers;

internal sealed class Unalignment : List<Location>
{
    public Unalignment(int capacity) : base(capacity) { }

    public Location?              Location            => this[0];
    public IEnumerable<Location>? AdditionalLocations => this.Skip(1);
}