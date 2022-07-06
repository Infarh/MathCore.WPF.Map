namespace MathCore.WPF.Map.Infrastructure;

/// <summary>Методы-расширения для работы с задачами</summary>
internal static class TasksEx
{
    /// <summary>Когда все задачи выполнены</summary>
    /// <param name="tasks">Перечисление задач</param>
    /// <returns>Задача, выполняемая когда все задачи выполнены</returns>
    public static Task WhenAll(this IEnumerable<Task> tasks) => Task.WhenAll(tasks);

    /// <summary>Когда все задачи выполнены</summary>
    /// <typeparam name="T">Тип значения задачи</typeparam>
    /// <param name="tasks">Перечисление задач</param>
    /// <returns>Массив результатов</returns>
    public static Task<T[]> WhenAll<T>(this IEnumerable<Task<T>> tasks) => Task.WhenAll(tasks);

    /// <summary>Когда выполнена любая из перечисленных задач</summary>
    /// <param name="tasks">Перечисление задач</param>
    /// <returns>Выполненная задача</returns>
    public static Task<Task> WhenAny(this IEnumerable<Task> tasks) => Task.WhenAny(tasks);

    /// <summary>Когда выполнена любая из перечисленных задач</summary>
    /// <typeparam name="T">Тип значения задачи</typeparam>
    /// <param name="tasks">Перечисление задач</param>
    /// <returns>Выполненная задача</returns>
    public static Task<Task<T>> WhenAny<T>(this IEnumerable<Task<T>> tasks) => Task.WhenAny(tasks);
}
