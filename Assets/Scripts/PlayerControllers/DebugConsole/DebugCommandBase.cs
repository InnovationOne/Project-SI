using System;

/// <summary>
/// Represents a debug command.
/// </summary>
public interface IDebugCommand {
    /// <summary>
    /// Gets the unique identifier of the command.
    /// </summary>
    string CommandId { get; }

    /// <summary>
    /// Gets the description of the command.
    /// </summary>
    string CommandDescription { get; }

    /// <summary>
    /// Gets the format of the command.
    /// </summary>
    string CommandFormat { get; }

    /// <summary>
    /// Invokes the command with the specified arguments.
    /// </summary>
    /// <param name="args">The arguments for the command.</param>
    void Invoke(params object[] args);
}

public abstract class DebugCommandBase : IDebugCommand {
    public string CommandId { get; private set; }
    public string CommandDescription { get; private set; }
    public string CommandFormat { get; private set; }

    protected DebugCommandBase(string commandId, string commandDescription, string commandFormat) {
        CommandId = commandId;
        CommandDescription = commandDescription;
        CommandFormat = commandFormat;
    }

    public abstract void Invoke(params object[] args);
}

// Cheat command with no parameters
public class DebugCommand : DebugCommandBase {
    private readonly Action _command;

    public DebugCommand(string commandId, string commandDescription, string commandFormat, Action command)
        : base(commandId, commandDescription, commandFormat) {
        _command = command ?? throw new ArgumentNullException(nameof(command));
    }

    public override void Invoke(params object[] args) {
        if (args.Length != 0) throw new ArgumentException("This command does not accept parameters.");
        _command.Invoke();
    }
}

// Cheat command with parameters
public class DebugCommand<T1> : DebugCommandBase {
    private readonly Action<T1> _command;

    public DebugCommand(string commandId, string commandDescription, string commandFormat, Action<T1> command)
        : base(commandId, commandDescription, commandFormat) {
        _command = command ?? throw new ArgumentNullException(nameof(command));
    }

    public override void Invoke(params object[] args) {
        if (args.Length != 1 || !(args[0] is T1)) throw new ArgumentException("This command requires exactly one parameter of type " + typeof(T1));
        _command.Invoke((T1)args[0]);
    }
}

// Cheat command with 2 parameters
public class DebugCommand<T1, T2> : DebugCommandBase {
    private readonly Action<T1, T2> _command;

    public DebugCommand(string commandId, string commandDescription, string commandFormat, Action<T1, T2> command)
        : base(commandId, commandDescription, commandFormat) {
        _command = command ?? throw new ArgumentNullException(nameof(command));
    }

    public override void Invoke(params object[] args) {
        if (args.Length != 2 || !(args[0] is T1) || !(args[1] is T2)) throw new ArgumentException("This command requires exactly two parameters of types " + typeof(T1) + " and " + typeof(T2));
        _command.Invoke((T1)args[0], (T2)args[1]);
    }
}

// Cheat command with 3 parameters
public class DebugCommand<T1, T2, T3> : DebugCommandBase {
    private readonly Action<T1, T2, T3> _command;

    public DebugCommand(string commandId, string commandDescription, string commandFormat, Action<T1, T2, T3> command)
        : base(commandId, commandDescription, commandFormat) {
        _command = command ?? throw new ArgumentNullException(nameof(command));
    }

    public override void Invoke(params object[] args) {
        if (args.Length != 3 || !(args[0] is T1) || !(args[1] is T2) || !(args[2] is T3)) throw new ArgumentException("This command requires exactly three parameters of types " + typeof(T1) + ", " + typeof(T2) + ", and " + typeof(T3));
        _command.Invoke((T1)args[0], (T2)args[1], (T3)args[2]);
    }
}