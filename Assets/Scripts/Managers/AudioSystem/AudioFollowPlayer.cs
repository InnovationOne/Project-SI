using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public class AudioFollowPlayer : MonoBehaviour {
    [SerializeField] EventReference _audioClip;

    Collider2D _areaToFollowInside;
    Vector3 _audioPos;
    AudioManager _audioManager;
    EventInstance _eventInstance;

    private void Awake() {
        _areaToFollowInside = GetComponent<Collider2D>();
    }

    private void Start() {
        _audioManager = GameManager.Instance.AudioManager;
        _eventInstance = _audioManager.CreateEventInstance(_audioClip);
        _eventInstance.start();
    }

    private void Update() {
        if (PlayerController.LocalInstance == null) return;

        // 2D position to give the collider
        Vector2 playerPos2D = PlayerController.LocalInstance.transform.position;

        // Check inside/outside in *2D*
        if (_areaToFollowInside.OverlapPoint(playerPos2D)) {
            _audioPos = new Vector3(playerPos2D.x, playerPos2D.y, -10);
        } else {
            Vector2 closest2D = _areaToFollowInside.ClosestPoint(playerPos2D);
            _audioPos = new Vector3(closest2D.x, closest2D.y, -10);
        }

        _eventInstance.set3DAttributes(RuntimeUtils.To3DAttributes(_audioPos));
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(_audioPos, 0.25f);
        Gizmos.color = Color.green;
        _eventInstance.getMinMaxDistance(out float min, out float max);
        Gizmos.DrawWireSphere(_audioPos, min);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(_audioPos, max);
    }

    private void OnDestroy() {
        _eventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
    }
}
