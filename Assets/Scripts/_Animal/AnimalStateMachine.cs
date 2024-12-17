using System.Collections;
using UnityEngine;

/// <summary>
/// State machine for an animal. Controls which state it is in.
/// Additional logic to make state changes dependent on external influences (hunger, weather, time).
/// </summary>
public enum AnimalState {
    Idle,
    Searching,
    Eating,
    Moving,
    Sleeping
}

[RequireComponent(typeof(AnimalNavigation))]
public class AnimalStateMachine : MonoBehaviour {
    private AnimalState _currentState = AnimalState.Idle;
    private AnimalNavigation _navigation;
    private AnimalController _controller;

    private bool _hasEatenToday = false;
    private bool _isNightTime = false;

    [SerializeField] private float _wanderRadius = 5f;
    [SerializeField] private float _wanderInterval = 10f;
    private float _wanderTimer = 0f;


    private TimeManager _tM;
    private WeatherManager _wM;

    /*
    private void Awake() {
        _navigation = GetComponent<AnimalNavigation>();
        _controller = GetComponent<AnimalController>();
    }

    private void Start() {
        _tM = TimeManager.Instance;
        _wM = WeatherManager.Instance;
    }

    // In Update oder Coroutine könnte man abhängig vom State Aktionen ausführen:
    private void Update() {
        UpdateDayNightStatus();

        switch (_currentState) {
            case AnimalState.Idle:
                // Tier steht einfach rum. Nach einiger Zeit möchte es sich vielleicht bewegen.
                _wanderTimer += Time.deltaTime;
                if (_wanderTimer >= Random.Range(2, _wanderInterval + 1) && !_isNightTime) {
                    // Neues Zufallsziel festlegen
                    Vector3 randomDestination = GetRandomDestination();
                    SetStateMoving(randomDestination);
                    _wanderTimer = 0f;
                }

                // Wenn Tier noch nicht gegessen hat und Tag ist -> Suchen nach Grass
                if (!_hasEatenToday && !_isNightTime) {
                    SetStateSearching();
                }

                // Wenn Nacht -> Schlafen
                if (_isNightTime) {
                    SetStateSleeping();
                }
                break;

            case AnimalState.Searching:
                // Suche nach Grass
                GameObject grass = FindNearestGrass();
                if (grass != null) {
                    SetStateMoving(grass.transform.position);
                } else {
                    // Kein Grass gefunden, versuche später nochmal -> zurück zu Idle
                    SetStateIdle();
                }
                break;

            case AnimalState.Eating:
                StartCoroutine(EatingRoutine());
                break;

            case AnimalState.Moving:
                if (_navigation.HasReachedDestination()) {
                    if (!_hasEatenToday && !_isNightTime && IsAtGrass()) {
                        SetStateEating();
                    } else {
                        // Ansonsten einfach Idle stehen bleiben
                        SetStateIdle();
                    }
                }
                break;

            case AnimalState.Sleeping:
                if (!_isNightTime) {
                    SetStateIdle();
                    _hasEatenToday = false;
                }
                break;
        }
    }

    private IEnumerator EatingRoutine() {
        yield return new WaitForSeconds(3f);
        _hasEatenToday = true;
        SetStateIdle();
    }

    public void SetStateIdle() {
        _currentState = AnimalState.Idle;
    }

    public void SetStateSearching() {
        _currentState = AnimalState.Searching;
    }

    public void SetStateEating() {
        _currentState = AnimalState.Eating;
    }

    public void SetStateMoving(Vector3 target) {
        _currentState = AnimalState.Moving;
        _navigation.SetDestination(target);
    }

    public void SetStateSleeping() {
        _currentState = AnimalState.Sleeping;
    }

    private void UpdateDayNightStatus() {
        float hour = _tM.GetHours();
        _isNightTime = (hour >= 22 || hour < 6);
    }

    private Vector3 GetRandomDestination() {
        // Zufallsziel in der Nähe
        Vector3 randomDirection = Random.insideUnitCircle * _wanderRadius;
        return transform.position + new Vector3(randomDirection.x, randomDirection.y, 0f);
    }

    private GameObject FindNearestGrass() {
        GameObject[] allGrass = FindFirstObjectByType<Grass>();
        if (allGrass.Length == 0) return null;
        GameObject nearest = null;
        float minDist = Mathf.Infinity;
        Vector3 currentPos = transform.position;
        foreach (var go in allGrass) {
            float dist = Vector3.Distance(currentPos, go.transform.position);
            if (dist < minDist) {
                minDist = dist;
                nearest = go.gameObject;
            }
        }
        return nearest;
    }

    private bool IsAtGrass() {
        // Prüfe, ob Tier bei einem Grass-Objekt steht
        GameObject[] allGrass = FindFirstObjectByType<Grass>();
        foreach (var go in allGrass) {
            if (Vector3.Distance(go.transform.position, transform.position) < 1f) {
                return true;
            }
        }
        return false;
    }
    */
}
