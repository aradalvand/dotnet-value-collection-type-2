using System.Collections;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.ImmutableCollection;

namespace Avesta.Common;

/// <summary>
/// An immutable list of values with structural value semantics, and a sane `.ToString()` implementation.
/// </summary>
/// <typeparam name="T">The type of the array elements.</typeparam>
[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(typeof(Sequence<>.DebugView))]
[CollectionBuilder(typeof(Sequence), nameof(Sequence.Create))]
[MessagePackFormatter(typeof(SequenceMessagePackFormatter<>))]
public sealed class Sequence<T> :
    IImmutableList<T>,
    IEquatable<Sequence<T>>,
    IReadOnlyList<T>
    where T : IEquatable<T>
{
    /// <summary>
    /// An empty <see cref="Sequence{T}"/> instance.
    /// </summary>
    public static readonly Sequence<T> Empty = new([]);

    private readonly ImmutableArray<T> _values;
    public ImmutableArray<T> ToImmutableArray() => _values;

    /// <summary>
    /// Gets the element at the specified index.
    /// </summary>
    public T this[int index] => _values[index];

    /// <summary>
    /// Gets the number of elements in the array.
    /// </summary>
    public int Count => _values.Length;

    public Sequence(ImmutableArray<T> values)
        => _values = values;

    /// <inheritdoc/>
    public bool Equals(Sequence<T>? other)
        => ReferenceEquals(this, other) || (other is not null && _values.AsSpan().SequenceEqual(other._values.AsSpan()));

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is Sequence<T> other && Equals(other);

    public static bool operator ==(Sequence<T>? left, Sequence<T>? right) =>
        left is not null
            ? left.Equals(right)
            : ReferenceEquals(left, right);

    public static bool operator !=(Sequence<T>? left, Sequence<T>? right) =>
        !(left == right);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        int hash = 0;
        foreach (T value in _values)
            hash = CombineHashCodes(hash, value is null ? 0 : value.GetHashCode());
        return hash;
    }

    static int CombineHashCodes(int h1, int h2)
    {
        // RyuJIT optimizes this to use the ROL instruction
        // Related GitHub pull request: https://github.com/dotnet/coreclr/pull/1830
        uint rol5 = ((uint)h1 << 5) | ((uint)h1 >> 27);
        return ((int)rol5 + h1) ^ h2;
    }

    public Sequence<T> Add(T item) => new(_values.Add(item));
    public Sequence<T> AddRange(params T[] items) => new(_values.AddRange(items));
    public Sequence<T> Insert(int index, T item) => new(_values.Insert(index, item));
    public Sequence<T> InsertRange(int index, params T[] items) => new(_values.InsertRange(index, items));
    public Sequence<T> RemoveAt(int index) => new(_values.RemoveAt(index));
    public Sequence<T> Remove(T itemToRemove) => new(_values.Remove(itemToRemove));
    public Sequence<T> RemoveAll(Predicate<T> match) => new(_values.RemoveAll(match));
    public Sequence<T> SetItem(int index, T value) =>
        new(_values.SetItem(index, value));
    public Sequence<T> Clear() => [];

    public Sequence<T> Pop() => RemoveAt(Count - 1);

    /// <inheritdoc/>
    public Enumerator GetEnumerator() => new(_values);

    /// <summary>
    /// Defines an enumerator for the <see cref="Sequence{T}"/> type.
    /// </summary>
    public struct Enumerator(ImmutableArray<T> _values) : IEnumerator<T>
    {
        private int _index = -1;

        /// <summary>
        /// Advances the enumerator to the next element of the array.
        /// </summary>
        public bool MoveNext() => ++_index < _values.Length;

        /// <summary>
        /// Gets the element at the current position of the enumerator.
        /// </summary>
        public readonly T Current => _values[_index];

        readonly T IEnumerator<T>.Current => _values[_index];
        readonly object IEnumerator.Current => _values[_index];
        void IEnumerator.Reset() => throw new NotImplementedException();
        readonly void IDisposable.Dispose() { }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static Sequence<T> UnsafeCreateFromArray(T[] values)
        => new([.. values]);

    public override string ToString() =>
        $"({_values.Length}) [{string.Join(", ", _values)}]";

    private sealed class DebugView(Sequence<T> array)
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items => array.ToArray();
    }

    #region Explicit Interface Impls
    IEnumerator<T> IEnumerable<T>.GetEnumerator() =>
        GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();

    IImmutableList<T> IImmutableList<T>.Add(T value) =>
        Add(value);

    IImmutableList<T> IImmutableList<T>.AddRange(IEnumerable<T> items) =>
        AddRange([.. items]);

    IImmutableList<T> IImmutableList<T>.Clear() =>
        Clear();

    IImmutableList<T> IImmutableList<T>.Insert(int index, T element) =>
        Insert(index, element);

    IImmutableList<T> IImmutableList<T>.InsertRange(int index, IEnumerable<T> items) =>
        InsertRange(index, [.. items]);

    IImmutableList<T> IImmutableList<T>.RemoveAll(Predicate<T> match) =>
        RemoveAll(match);

    public int IndexOf(T item, int index, int count, IEqualityComparer<T>? equalityComparer = null) =>
        _values.IndexOf(item, index, count, equalityComparer);
    int IImmutableList<T>.IndexOf(T item, int index, int count, IEqualityComparer<T>? equalityComparer) =>
        IndexOf(item, index, count, equalityComparer);

    public int LastIndexOf(T item, int index, int count, IEqualityComparer<T>? equalityComparer = null) =>
        _values.LastIndexOf(item, index, count, equalityComparer);
    int IImmutableList<T>.LastIndexOf(T item, int index, int count, IEqualityComparer<T>? equalityComparer) =>
        LastIndexOf(item, index, count, equalityComparer);

    IImmutableList<T> IImmutableList<T>.Remove(T value, IEqualityComparer<T>? equalityComparer) =>
        Remove(value);

    IImmutableList<T> IImmutableList<T>.RemoveAt(int index) =>
        RemoveAt(index);

    public Sequence<T> RemoveRange(IEnumerable<T> items, IEqualityComparer<T>? equalityComparer = null) =>
        new(_values.RemoveRange(items, equalityComparer));
    IImmutableList<T> IImmutableList<T>.RemoveRange(IEnumerable<T> items, IEqualityComparer<T>? equalityComparer) =>
        RemoveRange(items, equalityComparer);

    public Sequence<T> RemoveRange(int index, int count) =>
        new(_values.RemoveRange(index, count));
    IImmutableList<T> IImmutableList<T>.RemoveRange(int index, int count) =>
        RemoveRange(index, count);

    public Sequence<T> Replace(T oldValue, T newValue, IEqualityComparer<T>? equalityComparer = null) =>
        new(_values.Replace(oldValue, newValue, equalityComparer));

    IImmutableList<T> IImmutableList<T>.Replace(T oldValue, T newValue, IEqualityComparer<T>? equalityComparer) =>
        Replace(oldValue, newValue, equalityComparer);

    IImmutableList<T> IImmutableList<T>.SetItem(int index, T value) =>
        SetItem(index, value);
    #endregion
}
/// <summary>
/// Provides extension methods for the <see cref="Sequence{T}"/> type.
/// </summary>
public static class Sequence
{
    /// <summary>
    /// Creates an <see cref="Sequence{T}"/> instance from the specified values.
    /// </summary>
    /// <typeparam name="T">The element type for the array.</typeparam>
    /// <param name="values">The source enumerable from which to populate the array.</param>
    /// <returns>A new <see cref="Sequence{T}"/> instance.</returns>
    public static Sequence<T> ToSequence<T>(this IEnumerable<T> values) where T : IEquatable<T>
        => values is ICollection<T> { Count: 0 }
            ? Sequence<T>.Empty
            : new Sequence<T>([.. values]);

    /// <summary>
    /// Creates an <see cref="Sequence{T}"/> instance from the specified values.
    /// </summary>
    /// <typeparam name="T">The element type for the array.</typeparam>
    /// <param name="values">The source span from which to populate the array.</param>
    /// <returns>A new <see cref="Sequence{T}"/> instance.</returns>
    public static Sequence<T> Create<T>(ReadOnlySpan<T> values) where T : IEquatable<T>
        => values.IsEmpty ? Sequence<T>.Empty : new Sequence<T>([.. values]);
}

public sealed class SequenceMessagePackFormatter<T> : IMessagePackFormatter<Sequence<T>>
    where T : IEquatable<T>
{
    private readonly static ImmutableArrayFormatter<T> _underlyingFormatter = new();

    public Sequence<T> Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) =>
        new(_underlyingFormatter.Deserialize(ref reader, options));

    public void Serialize(ref MessagePackWriter writer, Sequence<T> value, MessagePackSerializerOptions options) =>
        _underlyingFormatter.Serialize(ref writer, value.ToImmutableArray(), options);
}
