using UnityEngine;

// This script is for testing with global ink variables only !!!
public class PokemonNPC : MonoBehaviour
{
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color charmanderColor = Color.red;
    [SerializeField] private Color bulbasaurColor = Color.green;
    [SerializeField] private Color squirtleColor = Color.blue;

    [SerializeField] private SpriteRenderer spriteRenderer;


    private void Update()
    {
        /*
        // Get the pokemon name
        string pokemonName = ((Ink.Runtime.StringValue)DialogueManager.Instance.GetVariableState("pokemon_name")).value;

        // Change the color of the cue
        switch (pokemonName)
        {
            case "":
                spriteRenderer.color = defaultColor;
                break;
            case "Charmander":
                spriteRenderer.color = charmanderColor;
                break;
            case "Bulbasaur":
                spriteRenderer.color = bulbasaurColor;
                break;
            case "Squirtle":
                spriteRenderer.color = squirtleColor;
                break;
            default:
                Debug.LogWarning("Pokemon name not handelt by switch statement: " + pokemonName);
                break;
        }*/
    }
}
