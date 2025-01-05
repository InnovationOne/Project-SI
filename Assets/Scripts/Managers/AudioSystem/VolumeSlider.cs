using UnityEngine;
using UnityEngine.UI;

public class VolumeSlider : MonoBehaviour
{
    private enum VolumeType {
        Master,
        Music,
        Ambience,
        SFX
    }

    [Header("Type")]
    [SerializeField] private VolumeType _volumeType;
    private Slider _slider;
    private AudioManager _audioManager;

    private void Awake() {
        _slider = GetComponentInChildren<Slider>();
    }

    private void Start() {
        _audioManager = GameManager.Instance.AudioManager;
    }

    private void Update() {
        switch (_volumeType) {
            case VolumeType.Master:
                _slider.value = _audioManager.MasterVolume;
                break;
            case VolumeType.Music:
                _slider.value = _audioManager.MusicVolume;
                break;
            case VolumeType.Ambience:
                _slider.value = _audioManager.AmbienceVolume;
                break;
            case VolumeType.SFX:
                _slider.value = _audioManager.SFXVolume;
                break;
            default:
                Debug.LogWarning("Volume Type is not supported: " + _volumeType);
                break;
        }
    }

    public void OnSliderVolumeChanged() {
        switch (_volumeType) {
            case VolumeType.Master:
                _audioManager.MasterVolume = _slider.value;
                break;
            case VolumeType.Music:
                _audioManager.MusicVolume = _slider.value;
                break;
            case VolumeType.Ambience:
                _audioManager.AmbienceVolume = _slider.value;
                break;
            case VolumeType.SFX:
                _audioManager.SFXVolume = _slider.value;
                break;
            default:
                Debug.LogWarning("Volume Type is not supported: " + _volumeType);
                break;
        }
    }
}
