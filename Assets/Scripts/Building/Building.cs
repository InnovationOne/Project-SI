using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
using System;

/// <summary>
/// Abstract base class for all buildings with construction/upgrade logic.
/// </summary>
public class Building : PlaceableObject {
    [Header("Konfiguration")]
    [SerializeField] protected BuildingSO buildingSO;

    protected NetworkVariable<int> _buildingLevel = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> _underConstruction = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> _daysRemaining = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public bool IsUnderConstruction => _underConstruction.Value;

    public override float MaxDistanceToPlayer => 0f;
    public override bool CircleInteract => false;

    protected virtual void Start() {
        if (IsServer) TimeManager.Instance.OnNextDayStarted += HandleNextDay;
    }

    protected virtual void OnDestroy() {
        if (IsServer) TimeManager.Instance.OnNextDayStarted -= HandleNextDay;
    }

    /// <summary>
    /// Starts the first build (level 0 → finished).
    /// </summary>
    public void StartConstruction() {
        if (!IsServer || _underConstruction.Value) return;
        _underConstruction.Value = true;
        _daysRemaining.Value = GetBuildTime(_buildingLevel.Value);
        OnConstructionStarted();
    }

    /// <summary>
    /// Startet ein Upgrade auf das nächste Level.
    /// </summary>
    public void Upgrade() {
        if (!IsServer || _underConstruction.Value) return;
        _underConstruction.Value = true;
        _buildingLevel.Value++;
        _daysRemaining.Value = GetBuildTime(_buildingLevel.Value);
        OnConstructionStarted();
    }

    private void HandleNextDay() {
        if (!IsServer || !_underConstruction.Value) return;

        _daysRemaining.Value--;
        if (_daysRemaining.Value <= 0) FinishConstruction();
    }

    private void FinishConstruction() {
        _underConstruction.Value = false;
        OnConstructionFinished();
    }

    /// <summary>
    /// Returns the construction time for the given level (can be overwritten in subclasses).
    /// </summary>
    protected virtual int GetBuildTime(int level) {
        return buildingSO.BuildTimeDays;
    }

    /// <summary>
    /// Hook for visual effects or similar at the start of construction.
    /// </summary>
    protected virtual void OnConstructionStarted() {
        Debug.Log($"Baustart: {buildingSO.name} (Level {_buildingLevel.Value}), Tage verbleibend: {_daysRemaining.Value}");
    }

    /// <summary>
    /// Hook for final initialization (prefab swap, collider, capacities).
    /// </summary>
    protected virtual void OnConstructionFinished() {
        Debug.Log($"Bau abgeschlossen: {buildingSO.name} (Level {_buildingLevel.Value})");
        // z.B. hier das korrekte Model für Level wechseln
    }

    #region Save & Load

    [Serializable]
    public struct BuildingData {
        public int BuildingLevel;
        public bool UnderConstruction;
        public int DaysRemaining;
    }

    public override string SaveObject() {
        return JsonUtility.ToJson(new BuildingData {
            BuildingLevel = _buildingLevel.Value,
            UnderConstruction = _underConstruction.Value,
            DaysRemaining = _daysRemaining.Value
        });
    }

    public override void LoadObject(string data) {
        if (string.IsNullOrEmpty(data)) return;
        var buildingData = JsonUtility.FromJson<BuildingData>(data);
        _buildingLevel.Value = buildingData.BuildingLevel;
        _underConstruction.Value = buildingData.UnderConstruction;
        _daysRemaining.Value = buildingData.DaysRemaining;
        if (_underConstruction.Value) OnConstructionStarted();
        else OnConstructionFinished();
    }

    #endregion

    public override void InitializePreLoad(int itemId) { }
    public override void InitializePostLoad() { }
    public override void Interact(PlayerController player) { }
    public override void PickUpItemsInPlacedObject(PlayerController player) { }
    public override void OnStateReceivedCallback(string callbackName) { }
}
