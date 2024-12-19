using System;
using Unity.Netcode;

/// <summary>
/// Attaches to objects that need to react to in-game minute ticks.
/// Only the server should manage TimeAgents.
/// </summary>
public class TimeAgent : NetworkBehaviour {
    public event Action OnMinuteTimeTick;
    TimeManager _timeManager;

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        if (IsServer) {
            _timeManager = TimeManager.Instance;
            if (_timeManager != null) {
                SubscribeToTimeManager();
            }
        }
    }

    // Subscribes this agent to TimeManager's tick events.
    void SubscribeToTimeManager() => _timeManager.SubscribeTimeAgent(this);

    // Unsubscribes this agent from TimeManager's tick events.
    void UnsubscribeFromTimeManager() => _timeManager.UnsubscribeTimeAgent(this);

    // Invoked by TimeManager every minute.
    public void InvokeMinute() => OnMinuteTimeTick?.Invoke();

    new void OnDestroy() {
        if (IsServer && _timeManager != null) {
            UnsubscribeFromTimeManager();
        }
        base.OnDestroy();
    }
}
