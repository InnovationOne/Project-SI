using System;
using Unity.Netcode;

/// <summary>
/// This script is attached to every object that needs to be managed by time.
/// It subscribes to the TimeManager to receive minute tick events.
/// </summary>
public class TimeAgent : NetworkBehaviour {
    // Event invoked every minute tick.
    public event Action OnMinuteTimeTick;

    // Cached reference to TimeManager to reduce repeated access
    private TimeManager _timeManager;

    /// <summary>
    /// Initializes the TimeAgent and subscribes to the TimeManager.
    /// Only the server should manage TimeAgents.
    /// </summary>
    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        if (IsServer) {
            _timeManager = TimeManager.Instance;
            if (_timeManager != null) {
                SubscribeTimeAgent();
            }
        }
    }

    /// <summary>
    /// Subscribes this TimeAgent to the TimeManager.
    /// </summary>
    private void SubscribeTimeAgent() {
        if (_timeManager != null) {
            _timeManager.SubscribeTimeAgent(this);
        }
    }

    /// <summary>
    /// Unsubscribes this TimeAgent from the TimeManager.
    /// </summary>
    private void UnsubscribeTimeAgent() {
        if (_timeManager != null) {
            _timeManager.UnsubscribeTimeAgent(this);
        }        
    }

    /// <summary>
    /// Invokes the OnMinuteTimeTick event.
    /// </summary>
    public void InvokeMinute() => OnMinuteTimeTick?.Invoke();

    /// <summary>
    /// Handles the destruction of the TimeAgent by unsubscribing from the TimeManager.
    /// </summary>
    private void OnDestroy() {
        base.OnDestroy();

        if (IsServer && _timeManager != null) {
            UnsubscribeTimeAgent();
        }
    }
}
