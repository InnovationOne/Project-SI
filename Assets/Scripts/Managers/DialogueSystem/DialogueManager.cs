using Ink.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// This script handels talking to an NPC
public class DialogueManager : NetworkBehaviour, IDataPersistance {
    public static DialogueManager Instance { get; private set; }


    [Header("Params")]
    [SerializeField] private float _typingSpeed = 0.04f; // Speed the letters of a text are displayed on screen - lower = faster


    [Header("Load Globals JSON")]
    [SerializeField] private TextAsset _loadGlobalsJSON; // Referenze for the globals ink file


    [Header("Dialogue UI")]
    [SerializeField] private GameObject _dialoguePanel;
    [SerializeField] private Image _continueIcon;
    [SerializeField] private TextMeshProUGUI _dialogueText;
    [SerializeField] private TextMeshProUGUI _displayNameText;
    [SerializeField] private Animator _portraitAnimator;

    private Animator _layoutAnimator;


    [Header("Choices UI")]
    [SerializeField] private Button[] _choiceButtons;

    private TextMeshProUGUI[] _choicesText;


    [Header("Audio")]
    // Referenze to the audio settings
    [SerializeField] private DialogueAudioInfoSO _defaultAudioInfo;
    [SerializeField] private DialogueAudioInfoSO[] _audioInfos;
    [SerializeField] private bool _makeSoundPredictable; // At the moment only for the same OS

    private DialogueAudioInfoSO _currentNPCAudioInfo;
    private Dictionary<string, DialogueAudioInfoSO> _audioInfoDictionary;
    private AudioSource _audioSource;

    // Is the player allowed to continue to the next line?
    private bool _canContinueToNextLine = false;
    public bool DialogueIsPlaying { get; private set; } = false;

    // Current story file of the NPC
    private Story _currentStoryFile;

    private const string SPEAKER_TAG = "speaker";
    private const string PORTRAIT_TAG = "portrait";
    private const string LAYOUT_TAG = "layout";
    private const string AUDIO_TAG = "audio";

    private Coroutine _displayLineCoroutine;
    private DialogueVariables _dialogueVariables;


    private void Awake() {
        // Check is there is already a instanciated singleton
        if (Instance != null)
            throw new Exception("Found more than one Dialogue Manager in the scene.");
        // Otherwise instanciate the singleton
        else
            Instance = this;

        _audioSource = gameObject.AddComponent<AudioSource>();
        _currentNPCAudioInfo = _defaultAudioInfo;
    }

    private void Start() {
        DialogueIsPlaying = false;
        _dialoguePanel.SetActive(false);

        // Get the layout animator
        _layoutAnimator = _dialoguePanel.GetComponent<Animator>();

        // Get all of the choices text
        _choicesText = new TextMeshProUGUI[_choiceButtons.Length];
        int index = 0;
        foreach (Button choiceButton in _choiceButtons) {
            _choicesText[index] = choiceButton.GetComponentInChildren<TextMeshProUGUI>();
            index++;
        }

        InitializeAudioInfoDictionary();
    }

    // This function cunfigurates the audio dictionary
    private void InitializeAudioInfoDictionary() {
        _audioInfoDictionary = new Dictionary<string, DialogueAudioInfoSO>
        {
            { _defaultAudioInfo.id, _defaultAudioInfo }
        };
        foreach (DialogueAudioInfoSO audioInfo in _audioInfos) {
            _audioInfoDictionary.Add(audioInfo.id, audioInfo);
        }
    }

    // This function sets the current audio info to the speaker
    private void SetCurrentAudioInfo(string id) {
        _audioInfoDictionary.TryGetValue(id, out DialogueAudioInfoSO audioInfo);
        if (audioInfo != null) {
            _currentNPCAudioInfo = audioInfo;
        } else {
            Debug.LogWarning("Failed to find audio info for id: " + id);
        }
    }

    private void Update() {
        // When no dialogue is playing, return
        if (!DialogueIsPlaying) {
            return;
        }

        // Get the mousebutton to progress the story
        if (_canContinueToNextLine
            && _currentStoryFile.currentChoices.Count == 0
            && (Input.GetMouseButtonDown(0)
            || Input.GetKeyDown(KeyCode.E))) {
            ContinueStory();
        }

    }

    // This function is called when the player talks to an NPC
    public void EnterDialogueMode(TextAsset inkJSON) {
        // Set the dialogue parameter
        _currentStoryFile = new Story(inkJSON.text);
        DialogueIsPlaying = true;
        _dialoguePanel.SetActive(true);

        _dialogueVariables.StartListening(_currentStoryFile);

        // Reset name, portrait and layout
        _displayNameText.text = "???";
        _portraitAnimator.Play("default");
        _layoutAnimator.Play("right");

        // Start the story
        ContinueStory();
    }

    // This function is called when the dialogue ends
    private IEnumerator ExitDialogueMode() {
        // Wait for time to pass befor exiting
        yield return new WaitForSeconds(0.2f);

        _dialogueVariables.StopListening(_currentStoryFile);

        // Reset the dialogue parameter
        DialogueIsPlaying = false;
        _dialoguePanel.SetActive(false);
        _dialogueText.text = string.Empty;

        // Set current audio to default
        SetCurrentAudioInfo(_defaultAudioInfo.id);
    }

    // This function is called to progress with the story
    private void ContinueStory() {
        // Display the next line of text
        if (_currentStoryFile.canContinue) {
            // Set the text for the current dialogue line
            if (_displayLineCoroutine != null)
                StopCoroutine(_displayLineCoroutine);
            string nextLine = _currentStoryFile.Continue();

            // Handels all of the tags in the story
            HandleTags(_currentStoryFile.currentTags);

            _displayLineCoroutine = StartCoroutine(DisplayLine(nextLine));
        }
        // Otherwise end the dialogue
        else
            StartCoroutine(ExitDialogueMode());
    }

    // This function displays a line of text on screen with typing effect
    private IEnumerator DisplayLine(string line) {
        // Set the text to the full line, but set the visible letters to 0
        _dialogueText.text = line;
        _dialogueText.maxVisibleCharacters = 0;
        // Hide the continue icon and choices
        _continueIcon.gameObject.SetActive(false);
        HideChoices();

        _canContinueToNextLine = false;

        bool isAddingRichTextTag = false;

        // Display each letter one at a time
        foreach (char letter in line.ToCharArray()) {
            // If the button was pressed show all text instantly
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.E)) {
                _dialogueText.maxVisibleCharacters = line.Length;
                break;
            }

            // Check for rich text tag, if found, add it without waiting
            if (letter == '<' || isAddingRichTextTag) {
                isAddingRichTextTag = true;
                if (letter == '>') {
                    isAddingRichTextTag = false;
                }
            }
            // Otherwise add the next letter and wait a small timer
            else {
                //PlayDialogueSound(dialogueText.maxVisibleCharacters, dialogueText.text[dialogueText.maxVisibleCharacters]);
                _dialogueText.maxVisibleCharacters++;
                yield return new WaitForSeconds(_typingSpeed);
            }
        }

        // Show the continue icon and choices
        _continueIcon.gameObject.SetActive(true);
        DisplayChoices();

        _canContinueToNextLine = true;
    }

    // This function plays the sound for each letter in the text
    private void PlayDialogueSound(int currentDisplayedCharacterCount, char currentCharacter) {
        AudioClip[] dialogueTypingSoundClips = _currentNPCAudioInfo.dialogueTypingSoundClips;
        int frequencyLevel = _currentNPCAudioInfo.frequencyLevel;
        float minPitch = _currentNPCAudioInfo.minPitch;
        float maxPitch = _currentNPCAudioInfo.maxPitch;
        bool stopAudioSource = _currentNPCAudioInfo.stopAudioSource;

        // Plays the sound on every 2 characters, change modulo to change sound to another rythm
        if (currentDisplayedCharacterCount % frequencyLevel == 0) {
            // Stop a audiosource when its playing to avoid overlapping
            if (stopAudioSource)
                _audioSource.Stop();

            AudioClip soundClip = null;

            // Create predictable audio from hashing (not the same on every OS)
            if (_makeSoundPredictable) {
                int hashCode = currentCharacter.GetHashCode();

                // Select sound clip
                int predictableIndex = hashCode % dialogueTypingSoundClips.Length;
                soundClip = dialogueTypingSoundClips[predictableIndex];

                // Pitch
                int minPitchInt = (int)(minPitch * 100);
                int maxPitchInt = (int)(maxPitch * 100);
                int pitchRange = maxPitchInt - minPitchInt;
                // Cannot div by 0
                if (pitchRange != 0) {
                    int predictablePitchInt = (hashCode % maxPitchInt) + minPitchInt;
                    float predictablePitch = predictablePitchInt / 100f;
                    _audioSource.pitch = predictablePitch;
                } else
                    _audioSource.pitch = minPitch;
            }
            // Otherwise, randomice the audio
            else {
                // Select random sound clip
                int randomIndex = UnityEngine.Random.Range(0, dialogueTypingSoundClips.Length);
                soundClip = dialogueTypingSoundClips[randomIndex];

                // Pitch
                _audioSource.pitch = UnityEngine.Random.Range(maxPitch, maxPitch);


            }
            // Play sound clip
            _audioSource.PlayOneShot(soundClip);
        }
    }

    // This function gets the tag of the story and displays it on screen
    private void HandleTags(List<string> currentTags) {
        // Loop through each tag and handle it accordingly
        foreach (string tag in currentTags) {
            // Parse the tag
            string[] splitTag = tag.Split(':');
            if (splitTag.Length != 2)
                Debug.LogError("Tag could not be appropriately parsed: " + tag);

            string tagKey = splitTag[0].Trim();
            string tagValue = splitTag[1].Trim();

            // Handle the tag
            switch (tagKey) {
                case SPEAKER_TAG:
                    _displayNameText.text = tagValue;
                    break;
                case PORTRAIT_TAG:
                    _portraitAnimator.Play(tagValue);
                    break;
                case LAYOUT_TAG:
                    _layoutAnimator.Play(tagValue);
                    break;
                case AUDIO_TAG:
                    SetCurrentAudioInfo(tagValue);
                    break;
                default:
                    Debug.LogWarning("Tag came in but is not currently being handled " + tag);
                    break;
            }
        }
    }

    // This function enables and disable the choices buttons based on the current story
    private void DisplayChoices() {
        List<Choice> currentChoices = _currentStoryFile.currentChoices;

        // Check if the UI can support the amount of choices in the story
        if (currentChoices.Count > _choiceButtons.Length) {
            Debug.LogError("More choices were given than the UI can support. Number of choices given: "
                + currentChoices.Count);
        }

        int index = 0;
        // Enable and initialize the choices up to the amount of choices for this line of dialogue
        foreach (Choice choice in currentChoices) {
            _choiceButtons[index].gameObject.SetActive(true);
            _choicesText[index].text = choice.text;
            index++;
        }
        // Hide the not needed choice buttons
        for (int i = index; i < _choiceButtons.Length; i++)
            _choiceButtons[i].gameObject.SetActive(false);

        StartCoroutine(SelectFirstChoice());
    }

    // Hides all the choice buttons
    private void HideChoices() {
        foreach (Button choiceButton in _choiceButtons) {
            choiceButton.gameObject.SetActive(false);
        }
    }

    // This function sets the first selected gameobject to the first choice
    private IEnumerator SelectFirstChoice() {
        // Eventsystem needs to be cleared first, for one frame
        EventSystem.current.SetSelectedGameObject(null);
        yield return new WaitForEndOfFrame();
        EventSystem.current.SetSelectedGameObject(_choiceButtons[0].gameObject);
    }

    // This function is called when you choose a choice
    public void MakeChoice(int choiceIndex) {
        if (_canContinueToNextLine) {
            _currentStoryFile.ChooseChoiceIndex(choiceIndex);
            ContinueStory();
        }
    }

    // This function returns a variable from the globals ink file
    public Ink.Runtime.Object GetVariableState(string variableName) {
        _dialogueVariables.variables.TryGetValue(variableName, out Ink.Runtime.Object variableValue);
        if (variableValue == null) {
            Debug.LogWarning("Ink variable was found to be null: " + variableName);
        }
        return variableValue;
    }


    #region Save & Load
    public void LoadData(GameData data) {
        if (string.IsNullOrEmpty(data.inkVariables)) {
            _dialogueVariables = new DialogueVariables(_loadGlobalsJSON);
            return;
        }
        _dialogueVariables = new DialogueVariables(_loadGlobalsJSON, data.inkVariables);
    }

    public void SaveData(GameData data) {
        return;
        _dialogueVariables.SaveData(data);
    }
    #endregion
}
