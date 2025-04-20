using TMPro;
using UnityEngine;

[RequireComponent(typeof(AnimalBase))]
public class AnimalVisual : MonoBehaviour {
    [Header("Highlight & Name Tag")]
    [SerializeField] private SpriteRenderer _highlightSprite;
    [SerializeField] private Canvas _nameCanvas;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private Vector3 _nameOffset = new(0, 1.5f, 0);

    private AnimalBase _animal;
    private Camera _mainCamera;

    void Awake() {
        _animal = GetComponent<AnimalBase>();
        _mainCamera = Camera.main;
        if (_highlightSprite) _highlightSprite.enabled = false;
    }

    void LateUpdate() {
        // Position name tag above animal
        if (_nameCanvas) {
            // Face the camera
            _nameCanvas.transform.SetPositionAndRotation(transform.position + _nameOffset, Quaternion.LookRotation(
                _nameCanvas.transform.position - _mainCamera.transform.position));
        }
        // Update name text
        if (_nameText != null)
            _nameText.text = _animal.AnimalName;
    }

    /// <summary>Show or hide interaction highlight.</summary>
    public void ShowHighlight(bool show) {
        if (_highlightSprite) _highlightSprite.enabled = show;
    }
}