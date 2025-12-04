# BitmapPixelAccessor - Документация

## Описание

`BitmapPixelAccessor` - класс для эффективной работы с пикселями `WriteableBitmap` в WPF. Предоставляет удобный API для чтения и записи пикселей с автоматическим применением изменений.

## Архитектурное решение

### Выбор типа: Класс vs Структура

**Выбрано: `class` (ссылочный тип)**

#### Обоснование:
1. **Асинхронный контекст** - используется в `async` методах с `await`, где происходит захват переменных в state machine
2. **Передача между методами** - может передаваться и возвращаться из функций без копирования
3. **Хранение в полях** - может быть закреплён как поле экземпляра класса
4. **Время жизни** - управление ресурсами через `IDisposable` с автоматическим flush при освобождении

### Структура Pixel

**Выбрано: `readonly ref struct`**

#### Обоснование:
1. **Производительность** - отсутствие аллокаций в куче при доступе к пикселям
2. **Безопасность** - `readonly` предотвращает случайные изменения ссылки на буфер
3. **Ограничения ref struct** - не может использоваться:
   - В полях класса/структуры
   - В делегатах и лямбда-выражениях
   - Как параметр типа в generics
   - В async-методах (за пределами одного блока до await)

## Использование

### Базовый пример

```csharp
using var accessor = tile.CreatePixelAccessor();

for (var y = 0; y < accessor.Height; y++)
    for (var x = 0; x < accessor.Width; x++)
    {
        var pixel = accessor[x, y];
        pixel.R = 255;
        pixel.G = 128;
        pixel.B = 0;
        pixel.A = 255;
    }

return accessor.Bitmap;
```

### Использование метода Set

```csharp
using var accessor = tile.CreatePixelAccessor();

for (var y = 0; y < accessor.Height; y++)
    for (var x = 0; x < accessor.Width; x++)
        accessor[x, y].Set(R: 255, G: 128, B: 0, A: 255);

return accessor.Bitmap;
```

### Асинхронная обработка

```csharp
TileFunc = async (tile, cancel) =>
{
    using var accessor = tile.CreatePixelAccessor();
    
    for (var y = 0; y < accessor.Height; y++)
    {
        cancel.ThrowIfCancellationRequested();
        
        for (var x = 0; x < accessor.Width; x++)
        {
            // Вычисление цвета пикселя
            var color = ComputeColor(x, y);
            accessor[x, y].Set(color.R, color.G, color.B, color.A);
        }
    }
    
    return accessor.Bitmap;
};
```

### Очистка изображения

```csharp
using var accessor = tile.CreatePixelAccessor();

// Прозрачный фон
accessor.Clear(R: 0, G: 0, B: 0, A: 0);

// Или непрозрачный белый
accessor.Clear(R: 255, G: 255, B: 255, A: 255);
```

### Пример из MainWindowViewModel

```csharp
TileFunc = async (tile, cancel) =>
{
    var center = _FunctionTileSourceCenter;
    
    var lat_min = tile.Min.Latitude;
    var lat_max = tile.Max.Latitude;
    var lon_min = tile.Min.Longitude;
    var lon_max = tile.Max.Longitude;

    using var accessor = tile.CreatePixelAccessor();
    var tile_size = tile.TilePixelSize;

    for (var y = 0; y < tile_size; y++)
    {
        cancel.ThrowIfCancellationRequested();
        var lat = lat_max - (lat_max - lat_min) * y / (tile_size - 1.0);
        
        for (var x = 0; x < tile_size; x++)
        {
            var lon = lon_min + (lon_max - lon_min) * x / (tile_size - 1.0);
            var loc = new Location(lat, lon);

            var distance = CalculateDistance(center, loc);
            var (b, g, r) = HeatColor(distance);
            
            accessor[x, y].Set(r, g, b, 200);
        }
    }

    return accessor.Bitmap;
};
```

## API Reference

### BitmapPixelAccessor

#### Конструктор
```csharp
public BitmapPixelAccessor(WriteableBitmap Bitmap)
```

#### Свойства
- `int Width` - ширина изображения в пикселях
- `int Height` - высота изображения в пикселях
- `int Stride` - шаг строки в байтах
- `WriteableBitmap Bitmap` - исходное изображение

#### Индексатор
```csharp
public Pixel this[int X, int Y]
```

#### Методы
- `void Flush()` - применяет изменения к WriteableBitmap
- `void Clear(byte R = 0, byte G = 0, byte B = 0, byte A = 0)` - очищает все пиксели
- `void Dispose()` - автоматически вызывает Flush и освобождает ресурсы

### Pixel (ref struct)

#### Свойства
- `byte R` - красный канал (0-255)
- `byte G` - зелёный канал (0-255)
- `byte B` - синий канал (0-255)
- `byte A` - альфа-канал/прозрачность (0-255)

#### Методы
```csharp
public void Set(byte R, byte G, byte B, byte A)
```

## Важные замечания

1. **Автоматический Flush** - при использовании `using` оператора, `Flush()` вызывается автоматически
2. **Формат пикселей** - BGRA32 (Blue, Green, Red, Alpha), каждый канал - 1 байт
3. **Порядок байтов** - в памяти: [B][G][R][A]
4. **Производительность** - прямой доступ к буферу без промежуточных копирований
5. **Потокобезопасность** - класс НЕ потокобезопасен, требуется внешняя синхронизация

## Преимущества решения

1. ✅ Чистый и понятный API
2. ✅ Автоматическое управление ресурсами через IDisposable
3. ✅ Высокая производительность за счёт ref struct для Pixel
4. ✅ Работает в асинхронном контексте
5. ✅ Можно передавать между методами и возвращать из функций
6. ✅ Может храниться в полях класса
7. ✅ Безопасность типов на уровне компилятора

## Возможные расширения (для будущего)

- Поддержка других форматов пикселей (RGB24, RGBA64, и т.д.)
- Batch-операции для оптимизации (FillRect, DrawLine, etc.)
- Unsafe-версия для критичных по производительности сценариев
- Интеграция с Span<T> и Memory<T> для .NET 5+
