#nullable enable
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace MathCore.WPF.Map.Infrastructure;

/// <summary>Построитель списка в стеке</summary>
/// <typeparam name="T">Тип элемента списка</typeparam>
internal ref struct ValueListBuilder<T>
{
    private Span<T> _Span;
    private T[]? _ArrayFromPool;
    private int _Pos;

    public ValueListBuilder(Span<T> InitialSpan)
    {
        _Span = InitialSpan;
        _ArrayFromPool = null;
        _Pos = 0;
    }

    public int Length
    {
        get => _Pos;
        set
        {
            Debug.Assert(value >= 0);
            Debug.Assert(value <= _Span.Length);
            _Pos = value;
        }
    }

    public ref T this[int index]
    {
        get
        {
            Debug.Assert(index < _Pos);
            return ref _Span[index];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(T item)
    {
        var pos = _Pos;
        if (pos >= _Span.Length)
            Grow();

        _Span[pos] = item;
        _Pos = pos + 1;
    }

    public ReadOnlySpan<T> AsSpan() => _Span[.._Pos];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        if (_ArrayFromPool is not { } to_return) return;
        _ArrayFromPool = null;
        ArrayPool<T>.Shared.Return(to_return);
    }

    private void Grow()
    {
        var array = ArrayPool<T>.Shared.Rent(_Span.Length * 2);

        var success = _Span.TryCopyTo(array);
        Debug.Assert(success);

        var to_return = _ArrayFromPool;
        _Span = _ArrayFromPool = array;
        if (to_return is not null) 
            ArrayPool<T>.Shared.Return(to_return);
    }
}

internal ref partial struct ValueListBuilder2<T>
{
    private Span<T> _Span;
    private T[]? _ArrayFromPool;
    private int _Pos;

    public ValueListBuilder2(Span<T> InitialSpan)
    {
        _Span = InitialSpan;
        _ArrayFromPool = null;
        _Pos = 0;
    }

    public int Length
    {
        get => _Pos;
        set
        {
            Debug.Assert(value >= 0);
            Debug.Assert(value <= _Span.Length);
            _Pos = value;
        }
    }

    public ref T this[int index]
    {
        get
        {
            Debug.Assert(index < _Pos);
            return ref _Span[index];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(T item)
    {
        var pos = _Pos;
        if (pos >= _Span.Length)
            Grow();

        _Span[pos] = item;
        _Pos = pos + 1;
    }

    public ReadOnlySpan<T> AsSpan() => _Span[.._Pos];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        if (_ArrayFromPool is not { } to_return) return;
        _ArrayFromPool = null;
        ArrayPool<T>.Shared.Return(to_return);
    }

    private void Grow()
    {
        var array = ArrayPool<T>.Shared.Rent(_Span.Length * 2);

        var success = _Span.TryCopyTo(array);
        Debug.Assert(success);

        var to_return = _ArrayFromPool;
        _Span = _ArrayFromPool = array;
        if (to_return is not null) 
            ArrayPool<T>.Shared.Return(to_return);
    }
}