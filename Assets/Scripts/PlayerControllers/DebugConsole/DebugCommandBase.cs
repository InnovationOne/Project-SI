using System;
using UnityEngine;

public class DebugCommandBase : MonoBehaviour
{
    public string CommandId { get; private set; }
    public string CommandDescription { get; private set; }
    public string CommandFormat { get; private set;}

    public DebugCommandBase(string commandId, string commandDescription, string commandFormat) {
        CommandId = commandId;
        CommandDescription = commandDescription;
        CommandFormat = commandFormat;
    }
}

public class DebugCommand : DebugCommandBase {
    private Action command;
    public DebugCommand(string commandId, string commandDescription, string commandFormat, Action command) : base(commandId, commandDescription, commandFormat) {
        this.command = command;
    }

    public void Invoke() {
        command?.Invoke();
    }
}

public class DebugCommand<T1> : DebugCommandBase {
    private Action<T1> command;
    public DebugCommand(string commandId, string commandDescription, string commandFormat, Action<T1> command) : base(commandId, commandDescription, commandFormat) {
        this.command = command;
    }

    public void Invoke(T1 value1) {
        command?.Invoke(value1);
    }
}

public class DebugCommand<T1, T2> : DebugCommandBase {
    private Action<T1, T2> command;
    public DebugCommand(string commandId, string commandDescription, string commandFormat, Action<T1, T2> command) : base(commandId, commandDescription, commandFormat) {
        this.command = command;
    }

    public void Invoke(T1 value1, T2 value2) {
        command?.Invoke(value1, value2);
    }
}

public class DebugCommand<T1, T2, T3> : DebugCommandBase {
    private Action<T1, T2, T3> command;
    public DebugCommand(string commandId, string commandDescription, string commandFormat, Action<T1, T2, T3> command) : base(commandId, commandDescription, commandFormat) {
        this.command = command;
    }

    public void Invoke(T1 value1, T2 value2, T3 value3) {
        command?.Invoke(value1, value2, value3);
    }
}


