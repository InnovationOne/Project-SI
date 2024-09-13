using System;
using UnityEngine;

[RequireComponent(typeof(TimeAgent))]
public class NPC : Interactable {
    public NPCDailyRoutine[] DailyRoutines;

    private int _locationId;
    private int _weekDay => TimeAndWeatherManager.Instance.CurrentDay.Value % TimeAndWeatherManager.DAYS_PER_WEEK;
    private float _moveSpeed = 1f;

    private void Start() {
        GetComponent<TimeAgent>().onMinuteTimeTick += CheckAndUpdateLocation;
    }

    private void OnDestroy() {
        GetComponent<TimeAgent>().onMinuteTimeTick -= CheckAndUpdateLocation;
    }

    private void CheckAndUpdateLocation() {
        if (DailyRoutines[_weekDay].Locations[_locationId].LeaveTimeInvoke > TimeAndWeatherManager.Instance.TotalTimeAgentInvokesThisDay) {
            _locationId++;
        }

        //GetComponent<Pathfinding>().MoveTo(_moveSpeed, DailyRoutines[_weekDay].Locations[_locationId].Position);
    }
}
