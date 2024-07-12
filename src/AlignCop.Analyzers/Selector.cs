namespace AlignCop.Analyzers;

internal delegate void Selector<T, TResult>(T element, out TResult? selected);
internal delegate void Selector<T, TResultA, TResultB>(T element, out TResultA? selectedA, out TResultB? selectedB);