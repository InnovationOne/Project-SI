using Ink.Runtime;
using System.Collections.Generic;
using UnityEngine;

// This script stores and manages the state of dialogue variables
public class DialogueVariables {
    public Dictionary<string, Ink.Runtime.Object> variables { get; private set; }

    private Story globalVariablesStory;

    public DialogueVariables(TextAsset loadGlobalsJSON, string test = null) {
        // Create the story
        globalVariablesStory = new Story(loadGlobalsJSON.text);

        if (!string.IsNullOrEmpty(test)) {
            globalVariablesStory.state.LoadJson(test);
        }

        // Initialize the dictionary
        variables = new Dictionary<string, Ink.Runtime.Object>();
        foreach (string name in globalVariablesStory.variablesState) {
            Ink.Runtime.Object value = globalVariablesStory.variablesState.GetVariableWithName(name);
            variables.Add(name, value);
        }
    }

    // This function subscribes the variableChanged function to the story, to listen for variable changes
    public void StartListening(Story story) {
        VariablesToStory(story);

        story.variablesState.variableChangedEvent += VariableChanged;
    }

    // This function unsubscribes the variableChanged function to the story, to listen for variable changes
    public void StopListening(Story story) {
        story.variablesState.variableChangedEvent -= VariableChanged;
    }

    // This function is called when a variable changed
    private void VariableChanged(string name, Ink.Runtime.Object value) {
        // Only maintain variables that were initialized from the globals ink file
        if (variables.ContainsKey(name)) {
            variables.Remove(name);
            variables.Add(name, value);
        }
    }

    // This function sets the variables in a story
    private void VariablesToStory(Story story) {
        foreach (KeyValuePair<string, Ink.Runtime.Object> variable in variables) {
            story.variablesState.SetGlobal(variable.Key, variable.Value);
        }
    }

    // This function saves the dialogue variables to game data
    public void SaveData(GameData data) {
        if (globalVariablesStory != null) {
            VariablesToStory(globalVariablesStory);

            data.inkVariables = globalVariablesStory.state.ToJson();
        }
    }
}
