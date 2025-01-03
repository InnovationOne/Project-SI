using Ink.Runtime;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


[RequireComponent(typeof(NetworkObject))]
public class DialogueManager : NetworkBehaviour, IDataPersistance {
    public static DialogueManager Instance { get; private set; }

    [Header("Parameters")]
    [SerializeField] float _typingSpeed = 0.04f; // Speed the letters of a text are displayed on screen - lower = faster
    [SerializeField] TextAsset _loadGlobalsJSON;

    [Header("UI References")]
    [SerializeField] GameObject _dialoguePanel;
    [SerializeField] Image _continueIcon;
    [SerializeField] TextMeshProUGUI _dialogueText;
    [SerializeField] TextMeshProUGUI _displayNameText;
    [SerializeField] Animator _portraitAnimator;
    Animator _layoutAnimator;
    [SerializeField] Button[] _choiceButtons;
    [SerializeField] Image _skipCircleImage; // UI to show skipping progress

    [Header("Audio")]
    [SerializeField] DialogueAudioInfoSO _defaultAudioInfo;
    [SerializeField] DialogueAudioInfoSO[] _audioInfos;
    [SerializeField] bool _makeSoundPredictable; // At the moment only for the same OS

    // Internal fields
    TextMeshProUGUI[] _choicesText;
    AudioSource _audioSource;
    Story _currentStoryFile;
    DialogueVariables _dialogueVariables;
    InputManager _inputManager;
    Dictionary<string, DialogueAudioInfoSO> _audioInfoDictionary;
    DialogueAudioInfoSO _currentNPCAudioInfo;

    bool _canContinueToNextLine = false;
    public bool DialogueIsPlaying { get; private set; } = false;

    Coroutine _displayLineCoroutine;

    // Skipping feature
    float _holdContinueTime = 0f;
    const float SKIP_THRESHOLD = 5f;
    bool _isHoldingContinue = false;

    // Flag to indicate if continue was pressed during typing
    bool _continuePressedDuringTyping = false;

    // Tag keys
    const string SPEAKER_TAG = "speaker";
    const string PORTRAIT_TAG = "portrait";
    const string LAYOUT_TAG = "layout";
    const string AUDIO_TAG = "audio";


    void Awake() {
        if (Instance != null) {
            Debug.LogError("More than one DialogueManager in the scene");
            return;
        }
        Instance = this;

        _audioSource = gameObject.AddComponent<AudioSource>();
        _currentNPCAudioInfo = _defaultAudioInfo;
    }

    void Start() {
        DialogueIsPlaying = false;
        _dialoguePanel.SetActive(false);

        _layoutAnimator = _dialoguePanel.GetComponent<Animator>();

        // Cache choices text
        _choicesText = new TextMeshProUGUI[_choiceButtons.Length];

        for (int i = 0; i < _choiceButtons.Length; i++) {
            _choicesText[i] = _choiceButtons[i].GetComponentInChildren<TextMeshProUGUI>();
        }

        _inputManager = InputManager.Instance;
        // Input events from new input system
        _inputManager.Dialogue_OnContinueAction += OnContinuePressed;
        _inputManager.Dialogue_OnContinueStarted += OnContinueStarted;
        _inputManager.Dialogue_OnContinueCanceled += OnContinueEnded;
        _inputManager.Dialogue_OnResponseDown += OnResponseDown;
        _inputManager.Dialogue_OnResponseUp += OnResponseUp;

        InitializeAudioInfoDictionary();

        // Initialize skip circle
        if (_skipCircleImage != null) {
            _skipCircleImage.fillAmount = 0f;
            _skipCircleImage.gameObject.SetActive(false);
        }
    }

    // Sets up the audio dictionary for quick lookups
    void InitializeAudioInfoDictionary() {
        _audioInfoDictionary = new Dictionary<string, DialogueAudioInfoSO>
        {
            { _defaultAudioInfo.id, _defaultAudioInfo }
        };
        foreach (var audioInfo in _audioInfos) {
            _audioInfoDictionary.Add(audioInfo.id, audioInfo);
        }
    }

    // Changes current audio based on speaker tag
    void SetCurrentAudioInfo(string id) {
        if (_audioInfoDictionary.TryGetValue(id, out var audioInfo)) {
            _currentNPCAudioInfo = audioInfo;
        } else {
            Debug.LogWarning("Audio info not found for ID: " + id);
        }
    }

    // Called from outside to start a dialogue
    public void EnterDialogueMode(TextAsset inkJSON) {
        _currentStoryFile = new Story(inkJSON.text);
        DialogueIsPlaying = true;
        _dialoguePanel.SetActive(true);
        _inputManager.EnableDialogueActionMap();

        // Start listening to variables
        _dialogueVariables.StartListening(_currentStoryFile);

        // Reset UI states
        _displayNameText.text = "???";
        _portraitAnimator.Play("default");
        _layoutAnimator.Play("right");

        // Start story
        ContinueStory();
    }

    // Gracefully end dialogue
    IEnumerator ExitDialogueMode() {
        yield return new WaitForSeconds(0.2f);

        _inputManager.EnablePlayerActionMap();
        _dialogueVariables.StopListening(_currentStoryFile);
        DialogueIsPlaying = false;
        _dialoguePanel.SetActive(false);
        _dialogueText.text = string.Empty;
        SetCurrentAudioInfo(_defaultAudioInfo.id);
    }

    // Called when the player tries to continue the story
    private void OnContinuePressed() {
        if (!DialogueIsPlaying) {
            return;
        }

        if (_displayLineCoroutine != null && !_canContinueToNextLine) {
            // If typing is in progress, set flag to display full line
            _continuePressedDuringTyping = true;
            return;
        }

        if (!_canContinueToNextLine || _currentStoryFile.currentChoices.Count != 0) {
            return;
        }

        ContinueStory();
    }

    // Called when the player holds the continue action
    void OnContinueStarted() {
        if (!DialogueIsPlaying) return;
        _isHoldingContinue = true;
        _holdContinueTime = 0f;
        if (_skipCircleImage != null) {
            _skipCircleImage.gameObject.SetActive(true);
            _skipCircleImage.fillAmount = 0f;
        }
    }

    // Called when the player releases the continue action
    void OnContinueEnded() {
        _isHoldingContinue = false;
        if (_skipCircleImage != null) {
            _skipCircleImage.fillAmount = 0f;
            _skipCircleImage.gameObject.SetActive(false);
        }
    }

    void Update() {
        if (_isHoldingContinue && DialogueIsPlaying && _currentStoryFile != null) {
            _holdContinueTime += Time.deltaTime;
            if (_skipCircleImage != null) {
                _skipCircleImage.fillAmount = Mathf.Clamp01(_holdContinueTime / SKIP_THRESHOLD);
            }

            if (_holdContinueTime >= SKIP_THRESHOLD) {
                // Skip entire conversation
                _isHoldingContinue = false;
                if (_skipCircleImage != null) {
                    _skipCircleImage.fillAmount = 0f;
                    _skipCircleImage.gameObject.SetActive(false);
                }

                // End dialogue immediately
                StartCoroutine(ExitDialogueMode());
            }
        }
    }

    private void OnResponseDown() {
        // Example TODO: Navigating choices down (custom logic can be implemented)
    }

    private void OnResponseUp() {
        // Example TODO: Navigating choices up (custom logic can be implemented)
    }

    // Displays text with typing effect
    IEnumerator DisplayLine(string line) {
        // Set the text to the full line, but set the visible letters to 0
        _dialogueText.text = line;
        _dialogueText.maxVisibleCharacters = 0;
        _continueIcon.gameObject.SetActive(false);
        HideChoices();

        _canContinueToNextLine = false;
        _continuePressedDuringTyping = false;

        bool isAddingRichTextTag = false;

        // Display each letter one at a time
        foreach (char letter in line.ToCharArray()) {
            if (_continuePressedDuringTyping) {
                _dialogueText.maxVisibleCharacters = line.Length;
                break;
            }

            // Check for rich text tag, if found, add it without waiting
            if (letter == '<' || isAddingRichTextTag) {
                isAddingRichTextTag = true;
                if (letter == '>') {
                    isAddingRichTextTag = false;
                }
            } else {
                //PlayDialogueSound(dialogueText.maxVisibleCharacters, dialogueText.text[dialogueText.maxVisibleCharacters]);
                _dialogueText.maxVisibleCharacters++;
                yield return new WaitForSeconds(_typingSpeed);
            }
        }

        _continueIcon.gameObject.SetActive(true);
        DisplayChoices();
        _canContinueToNextLine = true;
    }

    // Plays dialogue typing sound
    void PlayDialogueSound(int currentDisplayedCharacterCount, char currentCharacter) {
        var soundClips = _currentNPCAudioInfo.dialogueTypingSoundClips;
        int frequencyLevel = _currentNPCAudioInfo.frequencyLevel;
        float minPitch = _currentNPCAudioInfo.minPitch;
        float maxPitch = _currentNPCAudioInfo.maxPitch;
        bool stopAudioSource = _currentNPCAudioInfo.stopAudioSource;

        // Plays the sound on every 2 characters, change modulo to change sound to another rythm
        if (currentDisplayedCharacterCount % frequencyLevel == 0) {
            if (stopAudioSource) {
                _audioSource.Stop();
            }

            AudioClip soundClip;

            if (_makeSoundPredictable) {
                int hashCode = currentCharacter.GetHashCode();
                int index = Mathf.Abs(hashCode) % soundClips.Length;
                soundClip = soundClips[index];

                // Pitch
                int minPitchInt = (int)(minPitch * 100);
                int maxPitchInt = (int)(maxPitch * 100);
                int pitchRange = maxPitchInt - minPitchInt;
                _audioSource.pitch = pitchRange != 0
                    ? ((hashCode % pitchRange) + minPitchInt) / 100f
                    : minPitch;
            } else {
                int randomIndex = UnityEngine.Random.Range(0, soundClips.Length);
                soundClip = soundClips[randomIndex];
                _audioSource.pitch = UnityEngine.Random.Range(maxPitch, maxPitch);
            }

            _audioSource.PlayOneShot(soundClip);
        }
    }

    // This function gets the tag of the story and displays it on screen
    void HandleTags(List<string> currentTags) {
        foreach (string tag in currentTags) {
            string[] splitTag = tag.Split(':');
            if (splitTag.Length != 2) {
                Debug.LogError("Could not parse tag: " + tag);
                continue;
            }

            string key = splitTag[0].Trim();
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
                case AUDIO_TAG:
                    SetCurrentAudioInfo(value);
                    break;
                default:
                    Debug.LogWarning("Unhandled tag: " + tag);
                    break;
            }
        }
    }

    // Displays current Ink choices
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

    // Hides all choices
    void HideChoices() {
        foreach (var choiceButton in _choiceButtons) {
            choiceButton.gameObject.SetActive(false);
        }
    }

    // Auto-selects the first choice button for navigation
    IEnumerator SelectFirstChoice() {
        EventSystem.current.SetSelectedGameObject(null);
        yield return new WaitForEndOfFrame();
        if (_choiceButtons.Length > 0 && _choiceButtons[0].gameObject.activeSelf) {
            EventSystem.current.SetSelectedGameObject(_choiceButtons[0].gameObject);
        }
    }

    // Player makes a choice
    public void MakeChoice(int choiceIndex) {
        if (_canContinueToNextLine && _currentStoryFile.currentChoices.Count > choiceIndex) {
            _currentStoryFile.ChooseChoiceIndex(choiceIndex);
            ContinueStory();
        }
    }

    // Called to continue the story
    void ContinueStory() {
        Debug.Log("Continue story");
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

    // Get a global variable from Ink
    public Ink.Runtime.Object GetVariableState(string variableName) {
        _dialogueVariables.variables.TryGetValue(variableName, out var variableValue);
        if (variableValue == null) {
            Debug.LogWarning("Ink variable null: " + variableName);
        }
        return variableValue;
    }


    #region Save & Load
    public void LoadData(GameData data) {
        if (string.IsNullOrEmpty(data.inkVariables)) {
            _dialogueVariables = new DialogueVariables(_loadGlobalsJSON);
        } else {
            _dialogueVariables = new DialogueVariables(_loadGlobalsJSON, data.inkVariables);
        }
    }

    public void SaveData(GameData data) {
        _dialogueVariables.SaveData(data);
    }
    #endregion
}
