using System.Collections.Generic;
using UnityEngine;

//XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
//Script for the LoadGamePanel
//XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX

public class LoadGamePanel : MonoBehaviour
{
    public static LoadGamePanel instance { get; private set; }

    [Header("Prefab")]
    [SerializeField] private SaveSlot saveSlotPrefab;
    [SerializeField] private Transform prefabParent;

    private List<SaveSlot> saveSlots = new List<SaveSlot>();

    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogError("Found more than one Load Game Panel in the scene.");
            return;
        }

        instance = this;
    }

    public void ActivateMenu()
    {
        //Load all the profiles that exist
        Dictionary<string, GameData> profilesGameData = DataPersistenceManager.Instance.GetAllProfilesGameData();

        //Destroy all children and reload
        foreach (Transform child in prefabParent)
            Destroy(child.gameObject);

        //Create the save slots for all profiles
        foreach (KeyValuePair<string, GameData> pair in profilesGameData)
        {
            SaveSlot saveSlotReference = Instantiate(saveSlotPrefab, prefabParent);
            saveSlotReference.profileId = pair.Key;
            saveSlotReference.SetData(pair.Value);
            saveSlots.Add(saveSlotReference);
        }
    }
}
