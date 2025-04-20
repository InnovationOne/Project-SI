using System;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(TimeAgent))]
public class DoorController : PlaceableObject, IInteractable {
    [SerializeField] private bool _canBeWeatherControlled;
    [SerializeField] private bool _canBeTimeControlled;
    [SerializeField] private Transform _exitPoint;
    private Collider2D _doorCollider;

    public static float OpenHour = 6f;
    public static float CloseHour = 18f;

    // Player setting on the door itself
    private NetworkVariable<bool> _weatherActive = new(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> _timeActive = new(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private TimeManager _timeManager;
    private WeatherManager _weatherManager;

    public Transform ExitPoint => _exitPoint;
    public bool IsOpen => !_doorCollider.enabled;

    public override float MaxDistanceToPlayer => 1.5f;
    public override bool CircleInteract => false;

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        _doorCollider = GetComponent<Collider2D>();
        _timeManager = GameManager.Instance.TimeManager;
        _weatherManager = GameManager.Instance.WeatherManager;
        if (IsServer) { 
            _timeManager.OnNextDayStarted += UpdateDoorState;
            GetComponent<TimeAgent>().OnMinuteTimeTick += UpdateDoorState; 
        }
    }

    public override void OnNetworkDespawn() {
        if (IsServer) {
            _timeManager.OnNextDayStarted -= UpdateDoorState;
            GetComponent<TimeAgent>().OnMinuteTimeTick -= UpdateDoorState; 
        }
        base.OnNetworkDespawn();
    }

    public override void Interact(PlayerController player) {
        if (_doorCollider.enabled) Close();
        else Open();
    }

    public void Open() => _doorCollider.enabled = false;
    public void Close() => _doorCollider.enabled = true;

    private void UpdateDoorState() {
        if (!IsServer) return;
        bool closed = false;
        if (_canBeTimeControlled && _timeActive.Value) {
            float hour = _timeManager.GetHours();
            if (hour < OpenHour || hour >= CloseHour) closed = true;
        }
        if (_canBeWeatherControlled && _weatherActive.Value && _weatherManager.RainThunderSnow()) {
            closed = true;
        }

        if (closed) Close(); 
        else Open();
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetWeatherActiveServerRpc(bool weatherControlled) {
        _weatherActive.Value = weatherControlled;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetTimeActiveServerRpc(bool timeControlled) {
        _timeActive.Value = timeControlled;
    }

    [Serializable]
    private struct DoorData {
        public bool WeatherActive;
        public bool TimeActive;
    }

    public override string SaveObject() {
        DoorData data = new() {
            WeatherActive = _weatherActive.Value,
            TimeActive = _timeActive.Value
        };
        return JsonUtility.ToJson(data);
    }
    public override void LoadObject(string data) {
        if (string.IsNullOrEmpty(data)) return;
        DoorData doorData = JsonUtility.FromJson<DoorData>(data);
        _weatherActive.Value = doorData.WeatherActive;
        _timeActive.Value = doorData.TimeActive;
    }

    public override void PickUpItemsInPlacedObject(PlayerController player) { }
    public override void InitializePreLoad(int itemId) { }
    public override void InitializePostLoad() { }
    public override void OnStateReceivedCallback(string callbackName) { }
    
}
