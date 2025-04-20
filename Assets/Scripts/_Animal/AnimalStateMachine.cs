using System;
using UnityEngine;

public enum AnimalState {
    Idle,
    SearchingFood,
    Moving,
    Eating,
    Sleeping
}

public class AnimalStateMachine : MonoBehaviour {
    [Header("Timers & Ranges")]
    [SerializeField] private float wanderRadius = 5f;
    [SerializeField] private float wanderInterval = 10f;
    [SerializeField] private float eatingDuration = 3f;
    [SerializeField] private float matingDuration = 5f;

    // Laufzeit‑Variablen
    private AnimalState currentState;
    private AnimalState previousState;
    private float stateTimer;
    private Vector3 targetPosition;

    // Referenzen
    private AnimalSO animalSO;
    private AnimalBase controller;
    private Transform animalTransform;
    private TimeManager timeManager;
    private WeatherManager weatherManager;
    private NPCMovementController movement;

    /// <summary>
    /// Wird vom AnimalController beim Spawn aufgerufen.
    /// </summary>
    public void Initialize(AnimalSO so, Transform transformRef, AnimalBase animalController, NPCMovementController movementController) {
        animalSO = so;
        animalTransform = transformRef;
        controller = animalController;
        movement = movementController;
        timeManager = TimeManager.Instance;
        weatherManager = WeatherManager.Instance;
        SetStateIdle();
    }

    /// <summary>
    /// Muss pro Frame aufgerufen werden (z.B. im Update des Controllers).
    /// </summary>
    public void Tick() {
        switch (currentState) {
            case AnimalState.Idle: UpdateIdle(); break;
            case AnimalState.SearchingFood: UpdateSearchingFood(); break;
            case AnimalState.Moving: UpdateMoving(); break;
            case AnimalState.Eating: UpdateEating(); break;
            case AnimalState.Sleeping: UpdateSleeping(); break;
        }
    }

    #region --- State Entry Methods ---

    public void SetStateIdle() {
        currentState = AnimalState.Idle;
        stateTimer = 0f;
        movement.MoveTo(transform.position);
    }

    private void SetStateSearchingFood() {
        currentState = AnimalState.SearchingFood;
        stateTimer = 0f;
    }

    private void SetStateMoving(Vector3 dest) {
        previousState = currentState;
        currentState = AnimalState.Moving;
        targetPosition = dest;
        movement.MoveTo(dest);
    }

    private void SetStateEating() {
        currentState = AnimalState.Eating;
        stateTimer = 0f;
    }

    private void SetStateSleeping() {
        currentState = AnimalState.Sleeping;
        stateTimer = 0f;
    }

    #endregion

    #region --- State Update Methods ---

    private void UpdateIdle() {
        float hour = timeManager.GetHours();
        var building = controller.GetComponentInParent<AnimalBuilding>();

        // Morgens: Stall verlassen, wenn Tür offen und Zeit >= wakeUp
        if (hour >= DoorController.OpenHour && building != null && building.IsDoorOpen) {
            SetStateMoving(building.DoorPosition);
            return;
        }

        // Abends: Schlafen im Stall, wenn Zeit >= sleepTime und Tier draußen oder im Stall
        if (hour >= DoorController.CloseHour && building != null) {
            SetStateMoving(building.DoorPosition);
            return;
        }

        // 1) Nachts schlafen
        if (hour >= DoorController.CloseHour || hour < DoorController.OpenHour) {
            SetStateSleeping();
            return;
        }

        // 2) Wenn noch nicht gefüttert, Futter suchen
        if (!controller.WasFed) {
            SetStateSearchingFood();
            return;
        }

        // 3) Zufälliges Herumwandern
        stateTimer += Time.deltaTime;
        if (stateTimer >= UnityEngine.Random.Range(2f, wanderInterval)) {
            Vector2 rand = UnityEngine.Random.insideUnitCircle * wanderRadius;
            Vector3 dest = animalTransform.position + new Vector3(rand.x, rand.y, 0f);
            SetStateMoving(dest);
        }
    }

    private void UpdateSearchingFood() {
        // Prüfe Futtertrog im Stall
        var building = controller.GetComponentInParent<AnimalBuilding>();
        if (building != null) {
            var trough = building.GetComponentInChildren<FeedingTrough>();
            if (trough != null && trough.HasHay) {
                SetStateMoving(trough.transform.position);
                return;
            }
        }

        // Sonst draußen Gras suchen
        var grasses = GameObject.FindGameObjectsWithTag("Grass");
        GameObject nearest = null;
        float minDist = float.MaxValue;
        Vector3 pos = animalTransform.position;
        foreach (var g in grasses) {
            float d = Vector3.Distance(pos, g.transform.position);
            if (d < minDist) {
                minDist = d;
                nearest = g;
            }
        }
        if (nearest != null) SetStateMoving(nearest.transform.position); 
        else SetStateIdle();
    }

    private void UpdateMoving() {
        if (!movement.IsMoving()) {
            // Wenn Ziel Stall-Tür und Zeit zum Schlafen
            float hour = timeManager.GetHours();
            var building = controller.GetComponentInParent<AnimalBuilding>();
            if (building != null && Vector3.Distance(animalTransform.position, building.DoorPosition) < 0.5f) {
                if (hour >= DoorController.CloseHour) { SetStateSleeping(); return; }
                if (hour >= DoorController.OpenHour) { SetStateIdle(); return; }
            }

            // ... vorherige Logik: Essen oder Idle ...
            if (previousState == AnimalState.SearchingFood) SetStateEating();
            else SetStateIdle();
        }
    }

    private void UpdateEating() {
        stateTimer += Time.deltaTime;
        if (stateTimer >= eatingDuration) {
            controller.Feed();
            SetStateIdle();
        }
    }

    private void UpdateSleeping() {
        float hour = timeManager.GetHours();
        // Morgens aufwachen
        if (hour >= DoorController.OpenHour && hour < DoorController.CloseHour) {
            SetStateIdle();
        }
    }

    #endregion
}
