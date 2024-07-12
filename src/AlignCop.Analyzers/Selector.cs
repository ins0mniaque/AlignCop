namespace AlignCop.Analyzers;

internal delegate void Selector<T, TResult>(T source, out TResult? selected);
internal delegate void Selector<T, TResultA, TResultB>(T source, out TResultA? selectedA, out TResultB? selectedB);