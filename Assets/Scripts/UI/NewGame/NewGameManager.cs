using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.Analytics;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using Unity.Collections;
using System.Linq;

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
    [SerializeField] private Button _readyButton;
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

    public static Dictionary<ulong, PlayerData> ReceivedPlayerData = new Dictionary<ulong, PlayerData>();

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
            _startButton.gameObject.SetActive(false);
            _skipIntroToggle.gameObject.SetActive(false);
            _readyButton.onClick.AddListener(SubmitLocalPlayerData);
        } else {
            _readyButton.gameObject.SetActive(false);
            SubmitLocalPlayerData();
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

    private void Update() {
        CheckAllPlayersReady();
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
        if (!NetworkManager.Singleton.IsHost) return;
        _playerData.Name = _nameInput.text;

        // Stelle sicher, dass der Host auch seine eigenen Daten übermittelt hat.
        if (!ReceivedPlayerData.ContainsKey(NetworkManager.Singleton.LocalClientId)) {
            SubmitLocalPlayerData();
        }

        int lobbyCount = LobbyManager.Instance.GetLobbyPlayers().Count;
        if (ReceivedPlayerData.Count < lobbyCount) {
            Debug.LogWarning("Nicht alle Spieler haben ihre Daten übermittelt!");
            return;
        }

        // Aggregiere die gesammelten Daten in eine Liste.
        List<PlayerData> allPlayerData = ReceivedPlayerData.Values.ToList();

        // Erstelle ein GameData-Objekt und übernehme die Spielerinformationen.
        var newGame = new GameData {
            Players = allPlayerData
        };
        DataPersistenceManager.Instance.NewGame(newGame);

        if (NetworkManager.Singleton.IsHost) NetworkManager.Singleton.SceneManager.LoadScene(LoadSceneManager.SceneName.GameScene.ToString(), LoadSceneMode.Single);
        else LoadSceneManager.Instance.LoadSceneAsync(LoadSceneManager.SceneName.GameScene);
    }

    public void SubmitLocalPlayerData() {
        string playerName = _nameInput.text;
        bool skipIntro = _skipIntroToggle.isOn;
        int gender = (int)_playerData.Gender;
        // Übertrage die Daten zum Host.
        SubmitPlayerDataServerRpc(playerName, gender, skipIntro);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitPlayerDataServerRpc(string playerName, int gender, bool skipIntro, ServerRpcParams rpcParams = default) {
        ulong senderId = rpcParams.Receive.SenderClientId;
        var data = new PlayerData(senderId) {
            Name = new FixedString64Bytes(playerName),
            Gender = (Gender)gender,
            SkipIntro = skipIntro
        };

        if (ReceivedPlayerData.ContainsKey(senderId)) ReceivedPlayerData[senderId] = data;
        else ReceivedPlayerData.Add(senderId, data);

        Debug.Log("Server: Daten von Client " + senderId + " empfangen: " + playerName);
    }

    private void CheckAllPlayersReady() {
        bool allReady = true;
        foreach (var player in LobbyManager.Instance.GetLobbyPlayers()) {
            if (!player.IsReady) { 
                allReady = false;
                break;
            }
        }

        if (NetworkManager.Singleton.IsHost) {
            _startButton.interactable = allReady;
        }
    }
}