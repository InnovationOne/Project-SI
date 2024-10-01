using System;
using UnityEngine;

[RequireComponent(typeof(TimeAgent))]
public class NPC : MonoBehaviour, IInteractable {
    public NPCDailyRoutine[] DailyRoutines;

    private int _locationId;
    private int _weekDay => TimeManager.Instance.CurrentDate.Value.Day % TimeManager.DAYS_PER_WEEK;

    public float MaxDistanceToPlayer => 0f;

    private float _moveSpeed = 1f;

    private void Start() {
        GetComponent<TimeAgent>().OnMinuteTimeTick += CheckAndUpdateLocation;
    }

    private void OnDestroy() {
        GetComponent<TimeAgent>().OnMinuteTimeTick -= CheckAndUpdateLocation;
    }

    private void CheckAndUpdateLocation() {
        if (DailyRoutines[_weekDay].Locations[_locationId].LeaveTimeInvoke > TimeManager.Instance.TotalTimeAgentInvokesThisDay) {
            _locationId++;
        }

        //GetComponent<Pathfinding>().MoveTo(_moveSpeed, DailyRoutines[_weekDay].Locations[_locationId].Position);
    }

    public void Interact(Player player) { }

    public void PickUpItemsInPlacedObject(Player player) { }

    public void InitializePreLoad(int itemId) { }
}
