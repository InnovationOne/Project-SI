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

    private void Awake() {
        _slider = GetComponentInChildren<Slider>();
    }

    private void Update() {
        switch (_volumeType) {
            case VolumeType.Master:
                _slider.value = AudioManager.Instance.MasterVolume;
                break;
            case VolumeType.Music:
                _slider.value = AudioManager.Instance.MusicVolume;
                break;
            case VolumeType.Ambience:
                _slider.value = AudioManager.Instance.AmbienceVolume;
                break;
            case VolumeType.SFX:
                _slider.value = AudioManager.Instance.SFXVolume;
                break;
            default:
                Debug.LogWarning("Volume Type is not supported: " + _volumeType);
                break;
        }
    }

    public void OnSliderVolumeChanged() {
        switch (_volumeType) {
            case VolumeType.Master:
                AudioManager.Instance.MasterVolume = _slider.value;
                break;
            case VolumeType.Music:
                AudioManager.Instance.MusicVolume = _slider.value;
                break;
            case VolumeType.Ambience:
                AudioManager.Instance.AmbienceVolume = _slider.value;
                break;
            case VolumeType.SFX:
                AudioManager.Instance.SFXVolume = _slider.value;
                break;
            default:
                Debug.LogWarning("Volume Type is not supported: " + _volumeType);
                break;
        }
    }
}
