using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace MathCore.WPF.Map.Infrastructure;

/// <summary>Построитель строк на основе структуры в стеке</summary>
internal ref struct StringBuilderValued
{
    /// <summary>Массив символов, выделенный из пула для возврата</summary>
    private char[]? _ArrayToReturnToPool;

    /// <summary>Текущий буфер символов</summary>
    private Span<char> _Chars;

    /// <summary>Текущая позиция записи символов</summary>
    private int _Pos;

    /// <summary>Флаг очистки буфера при потреблении результата</summary>
    public bool ClearOnConsume { get; init; }

    /// <summary>Инициализирует построитель указанным начальным буфером</summary>
    /// <param name="InitialBuffer">Начальный буфер для записи</param>
    public StringBuilderValued(Span<char> InitialBuffer)
    {
        _ArrayToReturnToPool = null;
        _Chars = InitialBuffer;
        _Pos = 0;
        ClearOnConsume = false;
    }

    /// <summary>Инициализирует построитель строкой, копируя её в внутренний буфер</summary>
    /// <param name="str">Исходная строка</param>
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

    /// <summary>Инициализирует построитель с заданной ёмкостью буфера</summary>
    /// <param name="Capacity">Начальная ёмкость буфера</param>
    public StringBuilderValued(int Capacity)
    {
        _ArrayToReturnToPool = ArrayPool<char>.Shared.Rent(Capacity);
        _Chars = _ArrayToReturnToPool;
        _Pos = 0;
        ClearOnConsume = false;
    }

    /// <summary>Инициализирует построитель с заданной ёмкостью и строкой</summary>
    /// <param name="Capacity">Минимальная ёмкость буфера</param>
    /// <param name="str">Строка для возможной записи</param>
    public StringBuilderValued(int Capacity, string str)
    {
        var capacity = Math.Max(str.Length, Capacity);
        _ArrayToReturnToPool = ArrayPool<char>.Shared.Rent(capacity);
        _Chars = _ArrayToReturnToPool;
        _Pos = 0;
        ClearOnConsume = false;
    }

    /// <summary>Текущая длина записанных символов</summary>
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

    /// <summary>Возвращает собранную строку</summary>
    /// <returns>Сформированная строка</returns>
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

    /// <summary>Пытается скопировать содержимое в указанный буфер</summary>
    /// <param name="Destination">Буфер назначения</param>
    /// <param name="CharsWritten">Количество записанных символов</param>
    /// <returns>Признак успешности копирования</returns>
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

    /// <summary>Вставляет повторяющийся символ в указанной позиции</summary>
    /// <param name="index">Индекс вставки</param>
    /// <param name="value">Символ для вставки</param>
    /// <param name="count">Количество повторов</param>
    /// <returns>Текущий экземпляр построителя</returns>
    public StringBuilderValued Insert(int index, char value, int count)
    {
        if (_Pos > _Chars.Length - count) Grow(count);

        var remaining = _Pos - index;
        _Chars.Slice(index, remaining).CopyTo(_Chars[(index + count)..]);
        _Chars.Slice(index, count).Fill(value);
        _Pos += count;

        return this;
    }

    /// <summary>Добавляет один символ</summary>
    /// <param name="c">Добавляемый символ</param>
    /// <returns>Текущий экземпляр построителя</returns>
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

    /// <summary>Добавляет строку</summary>
    /// <param name="str">Строка для добавления</param>
    /// <returns>Текущий экземпляр построителя</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StringBuilderValued Append(string? str)
    {
        if (str is not { Length: > 0 }) return this;
        var pos = _Pos;
        if (str.Length == 1 && pos < _Chars.Length)
            // очень распространенный случай, например, добавление строк из
            // NumberFormatInfo, таких, как разделители, символы процента и т.д.
            return Append(str[0]);

        AppendSlow(str);

        return this;
    }

    /// <summary>Добавляет значение типа byte</summary>
    /// <param name="value">Значение для добавления</param>
    /// <param name="FormatProvider">Формат-провайдер</param>
    /// <returns>Текущий экземпляр построителя</returns>
    public StringBuilderValued Append(byte value, IFormatProvider? FormatProvider = null)
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

    /// <summary>Добавляет значение типа sbyte</summary>
    /// <param name="value">Значение для добавления</param>
    /// <param name="FormatProvider">Формат-провайдер</param>
    /// <returns>Текущий экземпляр построителя</returns>
    public StringBuilderValued Append(sbyte value, IFormatProvider? FormatProvider = null)
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

    /// <summary>Добавляет значение типа int</summary>
    /// <param name="value">Значение для добавления</param>
    /// <param name="FormatProvider">Формат-провайдер</param>
    /// <returns>Текущий экземпляр построителя</returns>
    public StringBuilderValued Append(int value, IFormatProvider? FormatProvider = null)
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

    /// <summary>Добавляет значение типа long</summary>
    /// <param name="value">Значение для добавления</param>
    /// <param name="FormatProvider">Формат-провайдер</param>
    /// <returns>Текущий экземпляр построителя</returns>
    public StringBuilderValued Append(long value, IFormatProvider? FormatProvider = null)
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

    /// <summary>Добавляет значение типа uint</summary>
    /// <param name="value">Значение для добавления</param>
    /// <param name="FormatProvider">Формат-провайдер</param>
    /// <returns>Текущий экземпляр построителя</returns>
    public StringBuilderValued Append(uint value, IFormatProvider? FormatProvider = null)
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

    /// <summary>Добавляет значение типа ulong</summary>
    /// <param name="value">Значение для добавления</param>
    /// <param name="FormatProvider">Формат-провайдер</param>
    /// <returns>Текущий экземпляр построителя</returns>
    public StringBuilderValued Append(ulong value, IFormatProvider? FormatProvider = null)
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

    /// <summary>Добавляет значение типа double</summary>
    /// <param name="value">Значение для добавления</param>
    /// <param name="FormatProvider">Формат-провайдер</param>
    /// <returns>Текущий экземпляр построителя</returns>
    public StringBuilderValued Append(double value, IFormatProvider? FormatProvider = null)
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

    /// <summary>Добавляет значение типа double с форматом</summary>
    /// <param name="value">Значение для добавления</param>
    /// <param name="format">Формат вывода</param>
    /// <param name="FormatProvider">Формат-провайдер</param>
    /// <returns>Текущий экземпляр построителя</returns>
    public StringBuilderValued Append(double value, ReadOnlySpan<char> format, IFormatProvider? FormatProvider = null)
    {
#if NET5_0_OR_GREATER
        const int double_chars_buffer_size = 32;
        Span<char> buffer = stackalloc char[double_chars_buffer_size];
        if(!value.TryFormat(buffer, out var write_chars_count, provider: FormatProvider, format: format))
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

    /// <summary>Добавляет значение типа float</summary>
    /// <param name="value">Значение для добавления</param>
    /// <param name="FormatProvider">Формат-провайдер</param>
    /// <returns>Текущий экземпляр построителя</returns>
    public StringBuilderValued Append(float value, IFormatProvider? FormatProvider = null)
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

    /// <summary>Добавляет значение типа bool</summary>
    /// <param name="value">Значение для добавления</param>
    /// <returns>Текущий экземпляр построителя</returns>
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

    /// <summary>Добавляет объект через его строковое представление</summary>
    /// <param name="obj">Объект для добавления</param>
    /// <returns>Текущий экземпляр построителя</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StringBuilderValued Append(object? obj) => Append(obj?.ToString());

    /// <summary>Добавляет диапазон символов только для чтения</summary>
    /// <param name="str">Диапазон символов</param>
    /// <returns>Текущий экземпляр построителя</returns>
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

    /// <summary>Добавляет изменяемый диапазон символов</summary>
    /// <param name="str">Диапазон символов</param>
    /// <returns>Текущий экземпляр построителя</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StringBuilderValued Append(Span<char> str)
    {
        if (str.Length == 0) return this;
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

    /// <summary>Добавляет память символов</summary>
    /// <param name="str">Память с символами</param>
    /// <returns>Текущий экземпляр построителя</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StringBuilderValued Append(Memory<char> str) => Append(str.Span);

    /// <summary>Добавляет память символов только для чтения</summary>
    /// <param name="str">Память с символами</param>
    /// <returns>Текущий экземпляр построителя</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StringBuilderValued Append(ReadOnlyMemory<char> str) => Append(str.Span);

    /// <summary>Медленный путь добавления строки с проверкой роста буфера</summary>
    /// <param name="s">Строка для добавления</param>
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

    /// <summary>Медленный путь добавления диапазона символов</summary>
    /// <param name="s">Диапазон символов</param>
    private void AppendSlow(ReadOnlySpan<char> s)
    {
        var pos = _Pos;
        if (pos > _Chars.Length - s.Length)
            Grow(s.Length);

        s.CopyTo(_Chars[pos..]);
        _Pos += s.Length;
    }

    /// <summary>Медленный путь добавления изменяемого диапазона символов</summary>
    /// <param name="s">Диапазон символов</param>
    private void AppendSlow(Span<char> s)
    {
        var pos = _Pos;
        if (pos > _Chars.Length - s.Length)
            Grow(s.Length);

        s.CopyTo(_Chars[pos..]);
        _Pos += s.Length;
    }

    /// <summary>Добавляет символ указанное количество раз</summary>
    /// <param name="c">Символ для добавления</param>
    /// <param name="count">Количество повторов</param>
    /// <returns>Текущий экземпляр построителя</returns>
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

    //[summary]Добавляет указатель на символы (небезопасный код)
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

    /// <summary>Выделяет диапазон для прямой записи указанной длины</summary>
    /// <param name="length">Длина диапазона</param>
    /// <returns>Диапазон символов для записи</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<char> AppendSpan(int length)
    {
        var orig_pos = _Pos;
        if (orig_pos > _Chars.Length - length)
            Grow(length);

        _Pos = orig_pos + length;
        return _Chars.Slice(orig_pos, length);
    }

    /// <summary>Расширяет буфер и добавляет символ</summary>
    /// <param name="c">Символ для добавления</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GrowAndAppend(char c)
    {
        Grow(1);
        Append(c);
    }

    /// <summary>Увеличивает внутренний буфер до требуемой ёмкости</summary>
    /// <param name="RequiredAdditionalCapacity">Требуемая дополнительная ёмкость</param>
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

    /// <summary>Очищает и возвращает буфер в пул</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        var to_return = _ArrayToReturnToPool;
        this = default; // for safety, to avoid using pooled array if this instance is erroneously appended to again
        if (to_return is not null)
            ArrayPool<char>.Shared.Return(to_return);
    }

    /// <summary>Неявно преобразует построитель в строку</summary>
    /// <param name="str">Построитель для преобразования</param>
    /// <returns>Строковое представление</returns>
    public static implicit operator string(StringBuilderValued str) => str.ToString();

    /// <summary>Неявно преобразует строку в построитель</summary>
    /// <param name="str">Строка для инициализации</param>
    /// <returns>Экземпляр построителя</returns>
    public static implicit operator StringBuilderValued(string str) => new(str);
}