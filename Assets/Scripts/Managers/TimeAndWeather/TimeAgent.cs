using System;
using UnityEngine;

// This script is attatched to every object that needs to get managed by time
public class TimeAgent : MonoBehaviour {
    public Action onMinuteTimeTick;

    private void Start() {
        SubscribeTimeAgentToTimeManager();
    }

    public void SubscribeTimeAgentToTimeManager() {
        TimeAndWeatherManager.Instance.SubscribeTimeAgent(this);
    }

    public void InvokeMinute() {
        onMinuteTimeTick?.Invoke();
    }

    private void OnDestroy() {
        TimeAndWeatherManager.Instance.UnsubscribeTimeAgent(this);
    }
}
