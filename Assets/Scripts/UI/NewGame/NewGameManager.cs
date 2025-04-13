using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.Analytics;
using Unity.Netcode;

public class NewGameManager : MonoBehaviour {
    [Header("Name & Gender")]
    [SerializeField] private TMP_InputField _nameInput;
    [SerializeField] private Button _randomizeNameButton;
    [SerializeField] private Button _maleButton;
    [SerializeField] private Button _femaleButton;

    [Header("Character Rotation")]
    [SerializeField] private Button _rotateLeftButton;
    [SerializeField] private Button _rotateRightButton;

    [Header("Grids")]
    [SerializeField] private Transform _hairStylesGrid;
    [SerializeField] private Transform _bodyColorGrid;
    [SerializeField] private Transform _headColorGrid;
    [SerializeField] private Transform _hairColorGrid;
    [SerializeField] private Transform _eyeColorGrid;

    [Header("Grid Button Prefab")]
    [SerializeField] private GameObject _gridButtonPrefab;

    [Header("Preview & Start")]
    [SerializeField] private Toggle _skipIntroToggle;
    [SerializeField] private Button _startButton;
    [SerializeField] private Transform _previewAnchor;

    [Header("Preview Sub-Objects (UI Images)")]
    [SerializeField] private Image _bodyImage;
    [SerializeField] private Image _headImage;
    [SerializeField] private Image _feetImage;
    [SerializeField] private Image _legsImage;
    [SerializeField] private Image _torsoImage;
    [SerializeField] private Image _beltImage;
    [SerializeField] private Image _hairImage;
    [SerializeField] private Image _helmetImage;
    [SerializeField] private Image _handsImage;

    private PlayerData _playerData;
    private float _currentRotation;

    private readonly string[] _maleNames = {
            "Arthur", "Edward", "George", "Henry", "Charles", "William", "Thomas", "James", "John", "Robert",
            "Albert", "Frederick", "Alfred", "Benjamin", "Samuel", "Joseph", "Richard", "Walter", "Ernest", "Francis",
            "Harold", "Edwin", "Eugene", "Clarence", "Leonard", "Oscar", "Sidney", "Theodore", "Victor", "Raymond",
            "Lewis", "Cecil", "Gilbert", "Herbert", "Lawrence", "Percy", "Reginald", "Stanley", "Vincent", "Warren",
            "Edgar", "Hugh", "Isaac", "Julian", "Leon", "Milton", "Norman", "Philip", "Ralph", "Stephen"
        };
    private readonly string[] _femaleNames = {
            "Mary", "Elizabeth", "Margaret", "Sarah", "Emma", "Alice", "Clara", "Florence", "Eleanor", "Edith",
            "Grace", "Helen", "Jane", "Julia", "Laura", "Louisa", "Martha", "Matilda", "Rebecca", "Ruth",
            "Agnes", "Annie", "Beatrice", "Caroline", "Catherine", "Charlotte", "Daisy", "Dorothy", "Ellen", "Emily",
            "Esther", "Frances", "Hannah", "Harriet", "Irene", "Isabel", "Josephine", "Lillian", "Lucy", "Mabel",
            "Nora", "Olive", "Phoebe", "Rose", "Sophia", "Stella", "Susan", "Sylvia", "Violet", "Winifred"
        };

    private void Start() {
        _playerData = new PlayerData(NetworkManager.Singleton.LocalClientId);

        _randomizeNameButton.onClick.AddListener(GenerateRandomName);
        if (!NetworkManager.Singleton.IsHost) {
            _startButton.interactable = false;
        }
        _startButton.onClick.AddListener(OnStartGame);

        _maleButton.onClick.AddListener(() => { _playerData.Gender = Gender.Male; UpdateVisuals(); });
        _femaleButton.onClick.AddListener(() => { _playerData.Gender = Gender.Female; UpdateVisuals(); });

        _rotateLeftButton.onClick.AddListener(() => RotatePreview(-45f));
        _rotateRightButton.onClick.AddListener(() => RotatePreview(45f));

        _skipIntroToggle.onValueChanged.AddListener(val => _playerData.SkipIntro = val);

        //GenerateHairStyleButtons();
        //GenerateColorButtons(_bodyColorGrid, color => { _playerData.HairColor = color; UpdateVisuals(); }); // Beispiel
        //GenerateColorButtons(_headColorGrid, color => { _playerData.EyeColor = color; UpdateVisuals(); }); // Beispiel
        // TODO: Je nach Zuweisungslogik Body/Hair/Eye separieren

        UpdateVisuals();
    }

    private void GenerateRandomName() {
        var pool = _playerData.Gender == Gender.Male ? _maleNames : _femaleNames;
        _nameInput.text = pool[Random.Range(0, pool.Length)];
        _playerData.Name = _nameInput.text;
    }

    private void RotatePreview(float angle) {
        Debug.Log($"RotatePreview: {angle}");
        // TODO: Hier die Rotation des Preview-Objekts umsetzen
    }

    private void GenerateHairStyleButtons() {
        int hairCount = 5; // Platzhalter – später über Datenquelle
        for (int i = 0; i < hairCount; i++) {
            GameObject buttonGO = Instantiate(_gridButtonPrefab, _hairStylesGrid);
            int index = i;
            buttonGO.GetComponentInChildren<TMP_Text>().text = $"H{i + 1}";
            buttonGO.GetComponent<Button>().onClick.AddListener(() => {
                _playerData.HairStyleIndex = index;
                UpdateVisuals();
            });
        }
    }

    private void GenerateColorButtons(Transform grid, System.Action<Color> onClick) {
        foreach (Transform child in grid) Destroy(child.gameObject); // clear
        Color[] colors = new Color[] {
            Color.black, Color.white, Color.red, Color.yellow, Color.blue, Color.green, Color.cyan, Color.magenta
        };
        foreach (Color color in colors) {
            GameObject btn = Instantiate(_gridButtonPrefab, grid);
            var img = btn.GetComponent<Image>();
            img.color = color;
            btn.GetComponent<Button>().onClick.AddListener(() => onClick.Invoke(color));
        }
    }

    private void UpdateVisuals() {
        _playerData.Name = _nameInput.text;

        // TODO: Aktuelle Vorschau-Logik: Einfach Haarfarbe auf HairImage anwenden
        _hairImage.color = _playerData.HairColor;
        _headImage.color = _playerData.EyeColor;

        // Weiter ausbauen z. B. mit HairStyle-Sprites oder SpriteLibrary-Anbindung
    }

    private void OnStartGame() {
        _playerData.Name = _nameInput.text;

        // Neues GameData vorbereiten
        GameData newGame = new() {
            Players = new List<PlayerData> { _playerData }
        };

        // Spiel initialisieren
        DataPersistenceManager.Instance.NewGame(newGame);

        // Szene laden (asynchron über Ladebildschirm)
        LoadSceneManager.Instance.LoadSceneAsync(LoadSceneManager.SceneName.GameScene);
    }
}