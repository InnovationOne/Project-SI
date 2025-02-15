using Ink.Runtime;
using System.Collections.Generic;
using UnityEngine;

// Manages global Ink variables and synchronizes them with stories.
public class DialogueVariables {
    public Dictionary<string, Ink.Runtime.Object> Variables { get; private set; }
    Story _globalVariablesStory;

    public DialogueVariables(TextAsset loadGlobalsJSON, string loadedGlobalsJson = null) {
        _globalVariablesStory = new Story(loadGlobalsJSON.text);

        if (!string.IsNullOrEmpty(loadedGlobalsJson)) {
            _globalVariablesStory.state.LoadJson(loadedGlobalsJson);
        }

        Variables = new Dictionary<string, Ink.Runtime.Object>();
        foreach (string name in _globalVariablesStory.variablesState) {
            Variables[name] = _globalVariablesStory.variablesState.GetVariableWithName(name);
        }
    }

    public void StartListening(Story story) {
        SetVariablesToStory(story);
        story.variablesState.variableChangedEvent += VariableChanged;
    }

    public void StopListening(Story story) {
        story.variablesState.variableChangedEvent -= VariableChanged;
    }

    void VariableChanged(string name, Ink.Runtime.Object value) {
        if (Variables.ContainsKey(name)) {
            Variables[name] = value;
        }
    }

    private void SetVariablesToStory(Story story) {
        foreach (var variable in Variables) {
            story.variablesState.SetGlobal(variable.Key, variable.Value);
        }
    }

    // This function saves the dialogue variables to game data
    public void SaveData(GameData data) {
        if (_globalVariablesStory != null) {
            SetVariablesToStory(_globalVariablesStory);
            data.inkVariables = _globalVariablesStory.state.ToJson();
        }
    }
}
