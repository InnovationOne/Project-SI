using UnityEngine;

public class AnimalVisual : MonoBehaviour
{
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
        _animalHighlight.gameObject.SetActive(false);

        _loveIcon.gameObject.SetActive(false);
        _fedIcon.gameObject.SetActive(false);
        _petIcon.gameObject.SetActive(false);
        _gaveIcon.gameObject.SetActive(false);
        _fpgBackground.gameObject.SetActive(false);
    }

    private void Update() {
        if (_showLoveIcon) {
            _showLoveTimer += Time.deltaTime;

            if (_showLoveTimer <= SHOW_LOVE_TIMER_MAX) {
                _loveIcon.gameObject.SetActive(true);
            } else {
                _loveIcon.gameObject.SetActive(false);
                _showLoveIcon = false;
                _showLoveTimer = 0f;
            }
        }
    }

    internal void ShowLoveIcon() {
        _showLoveIcon = true;
    }

    internal void ShowFPGIcon(bool fed, bool pet, bool gave, bool show) {
        if (!_showLoveIcon) {
            _fedIcon.gameObject.SetActive(fed);
            _petIcon.gameObject.SetActive(pet);
            _gaveIcon.gameObject.SetActive(gave);

            _fpgBackground.gameObject.SetActive(show);
        } else {
            _fpgBackground.gameObject.SetActive(false);
        }
    }

    public void ShowHighlight(bool show) {
        _animalHighlight.gameObject.SetActive(show);
    }
}
