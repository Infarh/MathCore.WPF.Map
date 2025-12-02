# Dark Menu Styles for WPF

Полноценная система стилей для тёмных меню в WPF, решающая проблему с отображением границ при задании собственного Background.

## Проблема

Стандартные шаблоны WPF меню (Menu и ContextMenu) имеют проблему: при задании собственного цвета фона (Background) рамка меню сохраняет исходный цвет, что выглядит непрофессионально в тёмных темах.

## Решение

Система `DarkMenus.xaml` предоставляет полностью переработанные шаблоны для всех элементов меню с полной поддержкой настройки цветов.

## Возможности

- ✓ Полная поддержка настройки Background/Foreground
- ✓ Единообразный внешний вид фона и границ
- ✓ Поддержка вложенных подменю
- ✓ Чекбоксы и иконки в пунктах меню
- ✓ Разделители с правильным стилем
- ✓ Горячие клавиши (InputGestureText)
- ✓ Состояния Hover, Selected, Disabled
- ✓ Оптимизировано для тёмной темы Visual Studio

## Использование

### 1. Подключение ресурсов

```xml
<Window.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="Styles/DarkMenus.xaml"/>
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Window.Resources>
```

### 2. Применение стилей

#### Главное меню

```xml
<Menu Style="{StaticResource DarkMenuStyle}">
    <MenuItem Header="_Файл">
        <MenuItem Header="_Создать" InputGestureText="Ctrl+N"/>
        <MenuItem Header="_Открыть" InputGestureText="Ctrl+O"/>
        <Separator Style="{StaticResource DarkMenuSeparatorStyle}"/>
        <MenuItem Header="_Выход" InputGestureText="Alt+F4"/>
    </MenuItem>
</Menu>
```

#### Контекстное меню

```xml
<TextBox>
    <TextBox.ContextMenu>
        <ContextMenu Style="{StaticResource DarkContextMenuStyle}">
            <MenuItem Header="_Вырезать" InputGestureText="Ctrl+X"/>
            <MenuItem Header="_Копировать" InputGestureText="Ctrl+C"/>
            <MenuItem Header="В_ставить" InputGestureText="Ctrl+V"/>
            <Separator Style="{StaticResource DarkMenuSeparatorStyle}"/>
            <MenuItem Header="Дополнительно">
                <MenuItem Header="Вложенный пункт 1"/>
                <MenuItem Header="Вложенный пункт 2"/>
            </MenuItem>
        </ContextMenu>
    </TextBox.ContextMenu>
</TextBox>
```

#### Чекбоксы в меню

```xml
<MenuItem Header="Панель инструментов" IsCheckable="True" IsChecked="True"/>
<MenuItem Header="Строка состояния" IsCheckable="True" IsChecked="False"/>
```

## Цветовая палитра

Система использует цвета, совместимые с тёмной темой Visual Studio:

- **Фон**: `#2D2D30`
- **Текст**: `#F1F1F1`
- **Граница**: `#3F3F46`
- **Hover фон**: `#3E3E42`
- **Выделение**: `#007ACC`
- **Отключено**: `#656565`

## Настройка цветов

Вы можете переопределить цвета, изменив значения кистей в `DarkMenus.xaml`:

```xml
<SolidColorBrush x:Key="DarkMenu.Background" Color="#YourColor"/>
<SolidColorBrush x:Key="DarkMenu.Foreground" Color="#YourColor"/>
```

## Демо

Запустите `DarkStyleWindow` для просмотра всех возможностей системы.

## Файлы

- `Styles/DarkMenus.xaml` - основной файл стилей
- `DarkStyleWindow.xaml` - демонстрационное окно
- `DarkStyleWindow.xaml.cs` - код-behind демо

## Лицензия

MIT License
