using UnityEngine;

public class AnimalVisual : MonoBehaviour {
    [SerializeField] private SpriteRenderer _animalHighlight;
    [SerializeField] private Transform _loveIcon;
    [SerializeField] private Transform _fedIcon;
    [SerializeField] private Transform _petIcon;
    [SerializeField] private Transform _gaveIcon;
    [SerializeField] private Transform _fpgBackground;

    private const float SHOW_LOVE_TIMER_MAX = 3f;
    private float _showLoveTimer = 0f;
    private bool _showLoveIcon = false;

    private void Start() {
        if (_animalHighlight != null) _animalHighlight.gameObject.SetActive(false);
        if (_loveIcon != null) _loveIcon.gameObject.SetActive(false);
        if (_fedIcon != null) _fedIcon.gameObject.SetActive(false);
        if (_petIcon != null) _petIcon.gameObject.SetActive(false);
        if (_gaveIcon != null) _gaveIcon.gameObject.SetActive(false);
        if (_fpgBackground != null) _fpgBackground.gameObject.SetActive(false);
    }

    private void Update() {
        if (_showLoveIcon) {
            _showLoveTimer += Time.deltaTime;
            if (_showLoveTimer <= SHOW_LOVE_TIMER_MAX) {
                if (_loveIcon != null) _loveIcon.gameObject.SetActive(true);
            } else {
                if (_loveIcon != null) _loveIcon.gameObject.SetActive(false);
                _showLoveIcon = false;
                _showLoveTimer = 0f;
            }
        }
    }

    internal void ShowLoveIcon() => _showLoveIcon = true;    

    internal void ShowFPGIcon(bool fed, bool pet, bool gave, bool show) {
        if (!_showLoveIcon) {
            if (_fedIcon != null) _fedIcon.gameObject.SetActive(fed);
            if (_petIcon != null) _petIcon.gameObject.SetActive(pet);
            if (_gaveIcon != null) _gaveIcon.gameObject.SetActive(gave);
            if (_fpgBackground != null) _fpgBackground.gameObject.SetActive(show);
        } else {
            if (_fpgBackground != null) _fpgBackground.gameObject.SetActive(false);
        }
    }

    public void ShowHighlight(bool show) {
        if (_animalHighlight != null) _animalHighlight.gameObject.SetActive(show);
    }
}
