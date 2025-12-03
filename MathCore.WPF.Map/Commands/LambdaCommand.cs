using MathCore.WPF.Map.Commands.Base;

namespace MathCore.WPF.Map.Commands;

internal sealed class LambdaCommand : Command
{
    /// <summary>Команда, выполняющая делегат Action и проверяющая возможность через Func</summary>
    private readonly Action<object?> _ExecuteAction; // делегат выполнения
    private readonly Func<object?, bool>? _CanExecuteFunc; // делегат проверки выполнения

    /// <summary>Создаёт команду на основе делегатов</summary>
    /// <param name="ExecuteAction">Делегат выполнения команды</param>
    /// <param name="CanExecuteFunc">Делегат проверки возможности выполнения</param>
    public LambdaCommand(Action<object?> ExecuteAction, Func<object?, bool>? CanExecuteFunc = null)
    {
        if (ExecuteAction is null) // проверка аргумента
            throw new ArgumentNullException(nameof(ExecuteAction));

        _ExecuteAction = ExecuteAction;
        _CanExecuteFunc = CanExecuteFunc;
    }

    /// <summary>Выполняет действие команды</summary>
    /// <param name="p">Параметр команды</param>
    protected override void Execute(object? p) => _ExecuteAction(p); // вызываем переданный Action

    /// <summary>Определяет, можно ли выполнить команду</summary>
    /// <param name="p">Параметр команды</param>
    /// <returns>True если команда может быть выполнена</returns>
    protected override bool CanExecute(object? p) => _CanExecuteFunc?.Invoke(p) ?? true; // проверка через Func или true по умолчанию
}
