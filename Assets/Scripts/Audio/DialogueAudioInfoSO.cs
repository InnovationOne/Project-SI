using UnityEngine;

[CreateAssetMenu(fileName = "DialogueAudioInfo", menuName = "Dialogue Audio", order = 1)]
public class DialogueAudioInfoSO : ScriptableObject
{
    public string id;

    // Referenze to the audio sound clip for the npc's
    public AudioClip[] dialogueTypingSoundClips;
    // How often the sound should be played, 2 = every 2 letters
    [Range(1, 5)]
    public int frequencyLevel = 2;
    // Randomize pitch variables
    [Range(-3, 3)]
    public float minPitch = 0.5f;
    [Range(-3, 3)]
    public float maxPitch = 3f;
    public bool stopAudioSource;
}
