using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace MathCore.WPF.Map.Infrastructure;

/// <summary>Построитель строк на основе структуры в стеке</summary>
internal ref struct StringBuilderValued
{
    private char[] _ArrayToReturnToPool;
    private Span<char> _Chars;
    private int _Pos;

    public bool ClearOnConsume { get; init; }

    public StringBuilderValued(Span<char> InitialBuffer)
    {
        _ArrayToReturnToPool = null;
        _Chars = InitialBuffer;
        _Pos = 0;
        ClearOnConsume = false;
    }

    public StringBuilderValued(string str)
    {
        var capacity = str.Length;
        _ArrayToReturnToPool = ArrayPool<char>.Shared.Rent(capacity);
        _Chars = _ArrayToReturnToPool;
#if NET5_0_OR_GREATER
        str.CopyTo(_Chars);
#else
        str.AsSpan().CopyTo(_Chars);
#endif
        _Pos = capacity;
        ClearOnConsume = false;
    }

    public StringBuilderValued(int Capacity)
    {
        _ArrayToReturnToPool = ArrayPool<char>.Shared.Rent(Capacity);
        _Chars = _ArrayToReturnToPool;
        _Pos = 0;
        ClearOnConsume = false;
    }

    public StringBuilderValued(int Capacity, string str)
    {
        var capacity = Math.Max(str.Length, Capacity);
        _ArrayToReturnToPool = ArrayPool<char>.Shared.Rent(capacity);
        _Chars = _ArrayToReturnToPool;
        _Pos = 0;
        ClearOnConsume = false;
    }

    public int Length
    {
        get => _Pos;
        set
        {
            var delta = value - _Pos;
            if (delta > 0)
                Append('\0', delta);
            else
                _Pos = value;
        }
    }

    public override string ToString()
    {
#if NET5_0_OR_GREATER
        var s = new string(_Chars[.._Pos]);
#else
        var s = new string(_Chars[.._Pos].ToArray());
#endif
        if (ClearOnConsume) Clear();
        return s;
    }

    public bool TryCopyTo(Span<char> Destination, out int CharsWritten)
    {
        if (_Chars[.._Pos].TryCopyTo(Destination))
        {
            CharsWritten = _Pos;
            if (ClearOnConsume) Clear();
            return true;
        }

        CharsWritten = 0;
        if (ClearOnConsume) Clear();
        return false;
    }

    public StringBuilderValued Insert(int index, char value, int count)
    {
        if (_Pos > _Chars.Length - count) Grow(count);

        var remaining = _Pos - index;
        _Chars.Slice(index, remaining).CopyTo(_Chars[(index + count)..]);
        _Chars.Slice(index, count).Fill(value);
        _Pos += count;

        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StringBuilderValued Append(char c)
    {
        var pos = _Pos;
        if (pos < _Chars.Length)
        {
            _Chars[pos] = c;
            _Pos = pos + 1;
        }
        else
            GrowAndAppend(c);

        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StringBuilderValued Append(string str)
    {
        var pos = _Pos;
        if (str.Length == 1 && pos < _Chars.Length)
            // очень распространенный случай, например, добавление строк из
            // NumberFormatInfo, таких как разделители, символы процента и т.д.
            return Append(str[0]);

        AppendSlow(str);

        return this;
    }

    public StringBuilderValued Append(byte value, IFormatProvider FormatProvider = null)
    {
#if NET5_0_OR_GREATER
        const int double_chars_buffer_size = 3;
        Span<char> buffer = stackalloc char[double_chars_buffer_size];
        if(!value.TryFormat(buffer, out var write_chars_count, provider: FormatProvider))
            throw new InvalidOperationException("Не удалось выполнить преобразование целого числа в массив символов");
#else
        var buffer = value.ToString(FormatProvider).AsSpan();
        var write_chars_count = buffer.Length;
#endif

        var pos = _Pos;
        if (pos > _Chars.Length - write_chars_count)
            Grow(write_chars_count);

        buffer[..write_chars_count].CopyTo(_Chars[pos..]);
        _Pos += write_chars_count;

        return this;
    }

    public StringBuilderValued Append(sbyte value, IFormatProvider FormatProvider = null)
    {
#if NET5_0_OR_GREATER
        const int double_chars_buffer_size = 4;
        Span<char> buffer = stackalloc char[double_chars_buffer_size];
        if(!value.TryFormat(buffer, out var write_chars_count, provider: FormatProvider))
            throw new InvalidOperationException("Не удалось выполнить преобразование целого числа в массив символов");
#else
        var buffer = value.ToString(FormatProvider).AsSpan();
        var write_chars_count = buffer.Length;
#endif

        var pos = _Pos;
        if (pos > _Chars.Length - write_chars_count)
            Grow(write_chars_count);

        buffer[..write_chars_count].CopyTo(_Chars[pos..]);
        _Pos += write_chars_count;

        return this;
    }

    public StringBuilderValued Append(int value, IFormatProvider FormatProvider = null)
    {
#if NET5_0_OR_GREATER
        const int double_chars_buffer_size = 16;
        Span<char> buffer = stackalloc char[double_chars_buffer_size];
        if(!value.TryFormat(buffer, out var write_chars_count, provider: FormatProvider))
            throw new InvalidOperationException("Не удалось выполнить преобразование целого числа в массив символов");
#else
        var buffer = value.ToString(FormatProvider).AsSpan();
        var write_chars_count = buffer.Length;
#endif

        var pos = _Pos;
        if (pos > _Chars.Length - write_chars_count)
            Grow(write_chars_count);

        buffer[..write_chars_count].CopyTo(_Chars[pos..]);
        _Pos += write_chars_count;

        return this;
    }

    public StringBuilderValued Append(long value, IFormatProvider FormatProvider = null)
    {
#if NET5_0_OR_GREATER
        const int double_chars_buffer_size = 27;
        Span<char> buffer = stackalloc char[double_chars_buffer_size];
        if(!value.TryFormat(buffer, out var write_chars_count, provider: FormatProvider))
            throw new InvalidOperationException("Не удалось выполнить преобразование целого числа в массив символов");
#else
        var buffer = value.ToString(FormatProvider).AsSpan();
        var write_chars_count = buffer.Length;
#endif

        var pos = _Pos;
        if (pos > _Chars.Length - write_chars_count)
            Grow(write_chars_count);

        buffer[..write_chars_count].CopyTo(_Chars[pos..]);
        _Pos += write_chars_count;

        return this;
    }

    public StringBuilderValued Append(uint value, IFormatProvider FormatProvider = null)
    {
#if NET5_0_OR_GREATER
        const int double_chars_buffer_size = 16;
        Span<char> buffer = stackalloc char[double_chars_buffer_size];
        if(!value.TryFormat(buffer, out var write_chars_count, provider: FormatProvider))
            throw new InvalidOperationException("Не удалось выполнить преобразование целого числа в массив символов");
#else
        var buffer = value.ToString(FormatProvider).AsSpan();
        var write_chars_count = buffer.Length;
#endif

        var pos = _Pos;
        if (pos > _Chars.Length - write_chars_count)
            Grow(write_chars_count);

        buffer[..write_chars_count].CopyTo(_Chars[pos..]);
        _Pos += write_chars_count;

        return this;
    }

    public StringBuilderValued Append(ulong value, IFormatProvider FormatProvider = null)
    {
#if NET5_0_OR_GREATER
        const int double_chars_buffer_size = 27;
        Span<char> buffer = stackalloc char[double_chars_buffer_size];
        if(!value.TryFormat(buffer, out var write_chars_count, provider: FormatProvider))
            throw new InvalidOperationException("Не удалось выполнить преобразование целого числа в массив символов");
#else
        var buffer = value.ToString(FormatProvider).AsSpan();
        var write_chars_count = buffer.Length;
#endif

        var pos = _Pos;
        if (pos > _Chars.Length - write_chars_count)
            Grow(write_chars_count);

        buffer[..write_chars_count].CopyTo(_Chars[pos..]);
        _Pos += write_chars_count;

        return this;
    }

    public StringBuilderValued Append(double value, IFormatProvider FormatProvider = null)
    {
#if NET5_0_OR_GREATER
        const int double_chars_buffer_size = 32;
        Span<char> buffer = stackalloc char[double_chars_buffer_size];
        if(!value.TryFormat(buffer, out var write_chars_count, provider: FormatProvider))
            throw new InvalidOperationException("Не удалось выполнить преобразование вещественного числа в массив символов");
#else
        var buffer = value.ToString(FormatProvider).AsSpan();
        var write_chars_count = buffer.Length;
#endif

        var pos = _Pos;
        if (pos > _Chars.Length - write_chars_count)
            Grow(write_chars_count);

        buffer[..write_chars_count].CopyTo(_Chars[pos..]);
        _Pos += write_chars_count;

        return this;
    }

    public StringBuilderValued Append(double value, ReadOnlySpan<char> format, IFormatProvider FormatProvider = null)
    {
#if NET5_0_OR_GREATER
        const int double_chars_buffer_size = 32;
        Span<char> buffer = stackalloc char[double_chars_buffer_size];
        if(!value.TryFormat(buffer, out var write_chars_count, provider: FormatProvider))
            throw new InvalidOperationException("Не удалось выполнить преобразование вещественного числа в массив символов");
#else
        var buffer = value.ToString(FormatProvider).AsSpan();
        var write_chars_count = buffer.Length;
#endif

        var pos = _Pos;
        if (pos > _Chars.Length - write_chars_count)
            Grow(write_chars_count);

        buffer[..write_chars_count].CopyTo(_Chars[pos..]);
        _Pos += write_chars_count;

        return this;
    }

    public StringBuilderValued Append(float value, IFormatProvider FormatProvider = null)
    {
#if NET5_0_OR_GREATER
        const int double_chars_buffer_size = 32;
        Span<char> buffer = stackalloc char[double_chars_buffer_size];
        if(!value.TryFormat(buffer, out var write_chars_count, provider: FormatProvider))
            throw new InvalidOperationException("Не удалось выполнить преобразование вещественного числа в массив символов");
#else
        var buffer = value.ToString(FormatProvider).AsSpan();
        var write_chars_count = buffer.Length;
#endif

        var pos = _Pos;
        if (pos > _Chars.Length - write_chars_count)
            Grow(write_chars_count);

        buffer[..write_chars_count].CopyTo(_Chars[pos..]);
        _Pos += write_chars_count;

        return this;
    }

    public StringBuilderValued Append(bool value)
    {
#if NET5_0_OR_GREATER
        const int double_chars_buffer_size = 5;
        Span<char> buffer = stackalloc char[double_chars_buffer_size];
        if(!value.TryFormat(buffer, out var write_chars_count))
            throw new InvalidOperationException("Не удалось выполнить преобразование bool-значения в массив символов");
#else
        var buffer = value.ToString().AsSpan();
        var write_chars_count = buffer.Length;
#endif

        var pos = _Pos;
        if (pos > _Chars.Length - write_chars_count)
            Grow(write_chars_count);

        buffer[..write_chars_count].CopyTo(_Chars[pos..]);
        _Pos += write_chars_count;

        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StringBuilderValued Append(object obj) => Append(obj.ToString());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StringBuilderValued Append(ReadOnlySpan<char> str)
    {
        var pos = _Pos;
        if (str.Length == 1 && pos < _Chars.Length)
        {
            _Chars[pos] = str[0];
            _Pos = pos + 1;
        }
        else
            AppendSlow(str);

        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StringBuilderValued Append(Span<char> str)
    {
        var pos = _Pos;
        if (str.Length == 1 && pos < _Chars.Length)
        {
            _Chars[pos] = str[0];
            _Pos = pos + 1;
        }
        else
            AppendSlow(str);

        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StringBuilderValued Append(Memory<char> str) => Append(str.Span);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StringBuilderValued Append(ReadOnlyMemory<char> str) => Append(str.Span);

    private void AppendSlow(string s)
    {
        var pos = _Pos;
        if (pos > _Chars.Length - s.Length)
            Grow(s.Length);

#if NET5_0_OR_GREATER
        s.CopyTo(_Chars[pos..]);
#else
        s.AsSpan().CopyTo(_Chars[pos..]);
#endif
        _Pos += s.Length;
    }

    private void AppendSlow(ReadOnlySpan<char> s)
    {
        var pos = _Pos;
        if (pos > _Chars.Length - s.Length)
            Grow(s.Length);

        s.CopyTo(_Chars[pos..]);
        _Pos += s.Length;
    }

    private void AppendSlow(Span<char> s)
    {
        var pos = _Pos;
        if (pos > _Chars.Length - s.Length)
            Grow(s.Length);

        s.CopyTo(_Chars[pos..]);
        _Pos += s.Length;
    }

    public StringBuilderValued Append(char c, int count)
    {
        if (_Pos > _Chars.Length - count)
            Grow(count);

        var dst = _Chars.Slice(_Pos, count);
        for (var i = 0; i < dst.Length; i++)
            dst[i] = c;
        _Pos += count;

        return this;
    }

    //public unsafe StringBuilderValued Append(char* value, int length)
    //{
    //    int pos = _pos;
    //    if (pos > _chars.Length - length)
    //        Grow(length);
    //
    //    Span<char> dst = _chars.Slice(_pos, length);
    //    for (int i = 0; i < dst.Length; i++)
    //        dst[i] = *value++;
    //    _pos += length;
    //
    //    return this;
    //}

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<char> AppendSpan(int length)
    {
        var orig_pos = _Pos;
        if (orig_pos > _Chars.Length - length)
            Grow(length);

        _Pos = orig_pos + length;
        return _Chars.Slice(orig_pos, length);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GrowAndAppend(char c)
    {
        Grow(1);
        Append(c);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Grow(int RequiredAdditionalCapacity)
    {
        Debug.Assert(RequiredAdditionalCapacity > _Chars.Length - _Pos);

        var pool_array = ArrayPool<char>.Shared.Rent(Math.Max(_Pos + RequiredAdditionalCapacity, _Chars.Length * 2));

        _Chars.CopyTo(pool_array);

        var to_return = _ArrayToReturnToPool;
        _Chars = _ArrayToReturnToPool = pool_array;
        if (to_return is not null)
            ArrayPool<char>.Shared.Return(to_return);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        var to_return = _ArrayToReturnToPool;
        this = default; // for safety, to avoid using pooled array if this instance is erroneously appended to again
        if (to_return is not null)
            ArrayPool<char>.Shared.Return(to_return);
    }

    public static implicit operator string(StringBuilderValued str) => str.ToString();

    public static implicit operator StringBuilderValued(string str) => new(str);
}