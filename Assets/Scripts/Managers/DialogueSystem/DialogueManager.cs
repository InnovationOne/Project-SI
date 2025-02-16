using Ink.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEngine.Rendering.HableCurve;

[Serializable]
public class VoiceOverEntry {
    public string id;
    public AudioClip clip;
}

[Serializable]
public class EmojiEntry {
    public string id;
    public Sprite sprite;
}

public class DialogueManager : MonoBehaviour, IDataPersistance {
    public event Action DialogueComplete;

    #region -------------------- Inspector Fields --------------------

    [Header("Parameters")]
    [Tooltip("Speed at which the letters are displayed on screen. Lower = Faster")]
    [SerializeField] public float _typingSpeed = 0.04f;
    [Tooltip("JSON file containing global variables for Ink")]
    [SerializeField] TextAsset _loadGlobalsJSON;

    [Header("UI References")]
    [SerializeField] GameObject _dialoguePanel;
    [SerializeField] Image _continueIcon;
    [SerializeField] TextMeshProUGUI _dialogueText;
    [SerializeField] TextMeshProUGUI _displayNameText;
    [SerializeField] Animator _portraitAnimator;
    [SerializeField] Button[] _choiceButtons;
    [SerializeField] Image _skipCircleImage; // UI to show skipping progress

    [Header("Voice Over")]
    [SerializeField] VoiceOverEntry[] _voiceOverEntries;
    Dictionary<string, AudioClip> _voiceOverDict;

    [Header("Emoji")]
    [SerializeField] EmojiEntry[] _emojiEntries;
    Dictionary<string, Sprite> _emojiDict;
    QuickChatController _quickChatController;

    #endregion -------------------- Inspector Fields --------------------

    #region -------------------- Internal Fields --------------------

    TextMeshProUGUI[] _choicesText;
    AudioSource _voiceAudioSource;
    Story _currentStoryFile;
    DialogueVariables _dialogueVariables;
    Coroutine _displayLineCoroutine;
    Animator _layoutAnimator;
    InputManager _inputManager;

    // Typing & Skip
    bool _canContinueToNextLine = false;
    bool _continuePressedDuringTyping = false;
    bool _isHoldingContinue = false;
    float _holdContinueTime = 0f;
    const float SKIP_THRESHOLD = 5f;

    // Dialogue State
    public bool DialogueIsPlaying { get; private set; }

    // Tag keys
    const string SPEAKER_TAG = "speaker";
    const string PORTRAIT_TAG = "portrait";
    const string LAYOUT_TAG = "layout";
    const string VOICE_TAG = "voice";
    const string EMOJI_TAG = "emoji";

    #endregion -------------------- Internal Fields --------------------

    #region -------------------- Unity Lifecycle --------------------
    void Awake() {
        DialogueIsPlaying = false;
        _dialoguePanel.SetActive(false);
        _layoutAnimator = _dialoguePanel.GetComponent<Animator>(); 
        _voiceAudioSource = gameObject.AddComponent<AudioSource>();
        //_skipCircleImage.fillAmount = 0f;
        //_skipCircleImage.gameObject.SetActive(false);
    }

    void Start() {
        _inputManager = GameManager.Instance.InputManager;

        // Cache choice button texts for quick updates.
        _choicesText = new TextMeshProUGUI[_choiceButtons.Length];
        for (int i = 0; i < _choiceButtons.Length; i++) {
            _choicesText[i] = _choiceButtons[i].GetComponentInChildren<TextMeshProUGUI>();
        }

        // Subscribe to input events.
        _inputManager.Dialogue_OnContinueAction += OnContinuePressed;
        _inputManager.Dialogue_OnContinueStarted += OnContinueStarted;
        _inputManager.Dialogue_OnContinueCanceled += OnContinueEnded;
        _inputManager.Dialogue_OnResponseDown += OnResponseDown;
        _inputManager.Dialogue_OnResponseUp += OnResponseUp;

        // Initialize voice over dictionary.
        _voiceOverDict = new Dictionary<string, AudioClip>();
        foreach (var entry in _voiceOverEntries) {
            if (!_voiceOverDict.ContainsKey(entry.id)) {
                _voiceOverDict[entry.id] = entry.clip;
            } else {
                Debug.LogWarning($"Duplicate voice over id: {entry.id}");
            }
        }

        // Initialize emoji dictionary.
        _emojiDict = new Dictionary<string, Sprite>();
        foreach (var entry in _emojiEntries) {
            if (!_emojiDict.ContainsKey(entry.id)) {
                _emojiDict[entry.id] = entry.sprite;
            } else {
                Debug.LogWarning($"Duplicate emoji id: {entry.id}");
            }
        }
    }

    private void OnDestroy() {
        if (_inputManager != null) {
            _inputManager.Dialogue_OnContinueAction -= OnContinuePressed;
            _inputManager.Dialogue_OnContinueStarted -= OnContinueStarted;
            _inputManager.Dialogue_OnContinueCanceled -= OnContinueEnded;
            _inputManager.Dialogue_OnResponseDown -= OnResponseDown;
            _inputManager.Dialogue_OnResponseUp -= OnResponseUp;
        }
    }

    void Update() {
        if (_isHoldingContinue && DialogueIsPlaying && _currentStoryFile != null) {
            _holdContinueTime += Time.deltaTime;
            _skipCircleImage.fillAmount = Mathf.Clamp01(_holdContinueTime / SKIP_THRESHOLD);
            if (_holdContinueTime >= SKIP_THRESHOLD) {
                _isHoldingContinue = false;
                _skipCircleImage.fillAmount = 0f;
                _skipCircleImage.gameObject.SetActive(false);
                StartCoroutine(ExitDialogueMode());
            }
        }
    }

    #endregion -------------------- Unity Lifecycle --------------------

    #region -------------------- Dialogue Control --------------------


    public void StartTextBubble(string text, GameObject target) {
        StartCoroutine(target.GetComponent<QuickChatController>().SetChatBubble(_typingSpeed, text));
    }

    public void EnterDialogueMode(TextAsset inkJSON) {
        _currentStoryFile = new Story(inkJSON.text);
        DialogueIsPlaying = true;
        _dialoguePanel.SetActive(true);
        _inputManager.EnableDialogueActionMap();

        _dialogueVariables.StartListening(_currentStoryFile);

        // Reset UI state.
        _displayNameText.text = "???";
        _portraitAnimator.Play("default");
        _layoutAnimator.Play("right");

        ContinueStory();
    }

    IEnumerator ExitDialogueMode() {
        yield return new WaitForSeconds(0.2f);
        _inputManager.EnablePlayerActionMap();
        _dialogueVariables.StopListening(_currentStoryFile);
        DialogueIsPlaying = false;
        _dialoguePanel.SetActive(false);
        _dialogueText.text = string.Empty;
        DialogueComplete?.Invoke();
    }

    void ContinueStory() {
        StopVoiceOver();
        if (!_currentStoryFile.canContinue) {
            StartCoroutine(ExitDialogueMode());
            return;
        }

        if (_displayLineCoroutine != null) {
            StopCoroutine(_displayLineCoroutine);
        }

        string nextLine = _currentStoryFile.Continue();
        HandleTags(_currentStoryFile.currentTags);
        _displayLineCoroutine = StartCoroutine(DisplayLine(nextLine));
    }

    #endregion -------------------- Dialogue Control --------------------

    #region -------------------- Input Handling --------------------

    private void OnContinuePressed() {
        if (!DialogueIsPlaying) return;
        if (_displayLineCoroutine != null && !_canContinueToNextLine) {
            _continuePressedDuringTyping = true;
            return;
        }
        if (!_canContinueToNextLine || _currentStoryFile.currentChoices.Count != 0) return;
        ContinueStory();
    }

    void OnContinueStarted() {
        if (!DialogueIsPlaying) return;
        _isHoldingContinue = true;
        _holdContinueTime = 0f;
        _skipCircleImage.gameObject.SetActive(true);
        _skipCircleImage.fillAmount = 0f;
    }

    void OnContinueEnded() {
        _isHoldingContinue = false;
        _skipCircleImage.fillAmount = 0f;
        _skipCircleImage.gameObject.SetActive(false);
    }

    private void OnResponseDown() {
        // TODO Placeholder for choice navigation (e.g. arrow key down).
    }

    private void OnResponseUp() {
        // TODO Placeholder for choice navigation (e.g. arrow key up).
    }

    #endregion -------------------- Input Handling --------------------

    #region -------------------- Text Display --------------------

    IEnumerator DisplayLine(string line) {
        _dialogueText.text = line;
        _dialogueText.maxVisibleCharacters = 0;
        _continueIcon.gameObject.SetActive(false);
        HideChoices();

        if (_quickChatController == null) _quickChatController = PlayerController.LocalInstance.QuickChatController;
        _quickChatController.ClearEmoji();

        _canContinueToNextLine = false;
        _continuePressedDuringTyping = false;
        bool isAddingRichTextTag = false;

        foreach (char letter in line.ToCharArray()) {
            if (_continuePressedDuringTyping) {
                _dialogueText.maxVisibleCharacters = line.Length;
                break;
            }
            if (letter == '<' || isAddingRichTextTag) {
                isAddingRichTextTag = letter != '>';
            } else {
                _dialogueText.maxVisibleCharacters++;
                yield return new WaitForSeconds(_typingSpeed);
            }
        }

        _continueIcon.gameObject.SetActive(true);
        DisplayChoices();
        _canContinueToNextLine = true;
    }

    #endregion -------------------- Text Display --------------------

    #region -------------------- Tag Handling --------------------

    void HandleTags(List<string> currentTags) {
        foreach (string tag in currentTags) {
            string[] splitTag = tag.Split(':');
            if (splitTag.Length != 2) {
                Debug.LogError("Could not parse tag: " + tag);
                continue;
            }
            string key = splitTag[0].Trim().ToLower();
            string value = splitTag[1].Trim();

            switch (key) {
                case SPEAKER_TAG:
                    _displayNameText.text = value;
                    break;
                case PORTRAIT_TAG:
                    _portraitAnimator.Play(value);
                    break;
                case LAYOUT_TAG:
                    _layoutAnimator.Play(value);
                    break;
                case VOICE_TAG:
                    PlayVoiceOver(value);
                    break;
                case EMOJI_TAG:
                    SetEmoji(value);
                    break;
                default:
                    Debug.LogWarning("Unhandled tag: " + tag);
                    break;
            }
        }
    }

    // Plays the voice over clip corresponding to the given id.
    void PlayVoiceOver(string id) {
        if (_voiceOverDict != null && _voiceOverDict.TryGetValue(id, out AudioClip clip)) {
            _voiceAudioSource.clip = clip;
            _voiceAudioSource.Play();
        } else {
            Debug.LogWarning("Voice over clip not found for id: " + id);
        }
    }

    // Stops any currently playing voice over.
    void StopVoiceOver() {
        if (_voiceAudioSource != null && _voiceAudioSource.isPlaying) {
            _voiceAudioSource.Stop();
        }
    }

    // Sets the emoji image using the given id.
    void SetEmoji(string id) {
        if (_quickChatController == null) _quickChatController = PlayerController.LocalInstance.QuickChatController;

        if (_emojiDict.TryGetValue(id, out Sprite emojiSprite)) {
            _quickChatController.SetEmoji(emojiSprite);
        } else {
            Debug.LogWarning("Emoji not found for id: " + id);
            _quickChatController.ClearEmoji();
        }
    }

    public void SetEmoji(GameObject character, string id) {
        if (character == null || !character.TryGetComponent<QuickChatController>(out var quickChatController)) return;
        if (_emojiDict != null && _emojiDict.TryGetValue(id, out Sprite emojiSprite)) {
            quickChatController.SetEmoji(emojiSprite);
        } else {
            Debug.LogWarning("Emoji not found for id: " + id);
            quickChatController.ClearEmoji();
        }
    }

    #endregion -------------------- Tag Handling --------------------

    #region -------------------- Choice Handling --------------------

    void DisplayChoices() {
        if (_currentStoryFile == null) return;

        List<Choice> currentChoices = _currentStoryFile.currentChoices;
        if (currentChoices.Count > _choiceButtons.Length) {
            Debug.LogError("More choices than UI supports: " + currentChoices.Count);
            return;
        }

        for (int i = 0; i < currentChoices.Count; i++) {
            _choiceButtons[i].gameObject.SetActive(true);
            _choicesText[i].text = currentChoices[i].text;
        }

        for (int i = currentChoices.Count; i < _choiceButtons.Length; i++) {
            _choiceButtons[i].gameObject.SetActive(false);
        }

        StartCoroutine(SelectFirstChoice());
    }

    void HideChoices() {
        foreach (var btn in _choiceButtons) {
            btn.gameObject.SetActive(false);
        }
    }

    IEnumerator SelectFirstChoice() {
        EventSystem.current.SetSelectedGameObject(null);
        yield return new WaitForEndOfFrame();
        if (_choiceButtons.Length > 0 && _choiceButtons[0].gameObject.activeSelf) {
            EventSystem.current.SetSelectedGameObject(_choiceButtons[0].gameObject);
        }
    }

    public void MakeChoice(int choiceIndex) {
        if (_canContinueToNextLine && _currentStoryFile.currentChoices.Count > choiceIndex) {
            _currentStoryFile.ChooseChoiceIndex(choiceIndex);
            ContinueStory();
        }
    }

    #endregion -------------------- Choice Handling --------------------

    public Ink.Runtime.Object GetVariableState(string variableName) {
        _dialogueVariables.Variables.TryGetValue(variableName, out var variableValue);
        if (variableValue == null) {
            Debug.LogWarning("Ink variable null: " + variableName);
        }
        return variableValue;
    }

    #region -------------------- Data Persistance --------------------

    public void LoadData(GameData data) {
        _dialogueVariables = string.IsNullOrEmpty(data.inkVariables)
            ? new DialogueVariables(_loadGlobalsJSON)
            : new DialogueVariables(_loadGlobalsJSON, data.inkVariables);
    }

    public void SaveData(GameData data) {
        _dialogueVariables.SaveData(data);
    }

    #endregion -------------------- Data Persistance --------------------
}
