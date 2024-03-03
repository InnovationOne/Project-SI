using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;

// This script visualizes the placed objects
public class PlaceableObjectsManager : NetworkBehaviour, IDataPersistance {
    public static PlaceableObjectsManager Instance { get; private set; }

    [SerializeField] private RecipeDatabaseSO recipeDatabase;
    [SerializeField] private PlaceableObjectsContainer placeableObjectsContainer;

    private Tilemap _targetTilemap;


    private void Awake() {
        if (Instance != null) {
            throw new Exception("Found more than one Placeable Objects Manager in the scene.");
        } else {
            Instance = this;
        }

        _targetTilemap = GetComponent<Tilemap>();

        SetRecipeIdInDatabase();
    }

    private void Start() {
        VisualizeAllObjectsOnMap();
    }

    private void VisualizeAllObjectsOnMap() {
        for (int i = 0; i < placeableObjectsContainer.placeableObjects.Count; i++) {
            VisualizeObjectOnMap(placeableObjectsContainer.placeableObjects[i]);
        }
    }

    private void VisualizeObjectOnMap(PlaceableObject objectToPlace) {
        GameObject prefabGameObject = Instantiate(objectToPlace.placedObject.ObjectPrefabToPlace);

        prefabGameObject.transform.position = TilemapManager.Instance.FixPositionOnGrid(_targetTilemap.CellToWorld(objectToPlace.objectPositionOnGrid));

        IObjectDataPersistence persistance = prefabGameObject.GetComponent<IObjectDataPersistence>();
        persistance?.LoadObject(objectToPlace.objectState);

        objectToPlace.targetObject = prefabGameObject.transform;
    }

    public void PlaceObjectOnMap(Vector3Int positionOnGrid) {
        ItemSO objectToPlace = PlayerToolbeltController.LocalInstance.GetCurrentlySelectedToolbeltItemSlot().Item;

        PlaceableObject placeableObject = new(objectToPlace, positionOnGrid);

        VisualizeObjectOnMap(placeableObject);

        placeableObjectsContainer.placeableObjects.Add(placeableObject);
    }

    public bool IsPositionPlaced(Vector3Int position) {
        return placeableObjectsContainer.Get(position) != null;
    }

    public void PickUpObject(Vector3Int gridPosition) {
        PlaceableObject placedObject = placeableObjectsContainer.Get(gridPosition);

        if (placedObject == null || placedObject.placedObject.ItemSOToPickUpObject.ItemID != PlayerToolbeltController.LocalInstance.GetCurrentlySelectedToolbeltItemSlot().Item.ItemID) {
            return;
        }

        ItemSpawnManager.Instance.SpawnItemAtPosition(_targetTilemap.CellToWorld(gridPosition), PlayerMovementController.LocalInstance.LastMotionDirection, placedObject.placedObject, 1, 0, SpreadType.Circle);

        if (placedObject.targetObject.gameObject.GetComponent<Interactable>() != null) {
            placedObject.targetObject.gameObject.GetComponent<Interactable>().PickUpItemsInPlacedObject(Player.LocalInstance);
        } else if (placedObject.targetObject.gameObject.GetComponent<FenceBehaviour>() != null) {
            placedObject.targetObject.gameObject.GetComponent<FenceBehaviour>().PickUp();
        }

        Destroy(placedObject.targetObject.gameObject);
        placeableObjectsContainer.placeableObjects.Remove(placedObject);
    }

    private void SetRecipeIdInDatabase() {
        // Set the itemID for each item in the itemDatabase using LINQ's Select function
        recipeDatabase.Recipes = recipeDatabase.Recipes.Select((item, index) => {
            item.recipeID = index;
            return item;
        }).ToList();
    }

    public RecipeDatabaseSO GetRecipeDatabase() {
        return recipeDatabase;
    }


    #region Save & Load
    [Serializable]
    public class SaveObjectData {
        public int placedObjectID;
        public Transform targetObject;
        public Vector3Int objectPositionOnGrid;
        public string objectState;

        public SaveObjectData(int placedObjectID, Transform targetObject, Vector3Int objectPositionOnGrid, string objectState) {
            this.placedObjectID = placedObjectID;
            this.targetObject = targetObject;
            this.objectPositionOnGrid = objectPositionOnGrid;
            this.objectState = objectState;
        }
    }

    [Serializable]
    public class ToSaveObjectData {
        public List<SaveObjectData> toSaveData;

        public ToSaveObjectData() {
            toSaveData = new List<SaveObjectData>();
        }
    }

    public void LoadData(GameData data) {
        if (string.IsNullOrEmpty(data.PlacedObjects)) {
            return;
        }

        placeableObjectsContainer.placeableObjects.Clear();

        ToSaveObjectData toLoadObjectData = JsonUtility.FromJson<ToSaveObjectData>(data.PlacedObjects);

        foreach (SaveObjectData savedObject in toLoadObjectData.toSaveData) {
            PlaceableObject placeableObject = new() {
                placedObject = ItemManager.Instance.ItemDatabase.Items[savedObject.placedObjectID],
                targetObject = savedObject.targetObject,
                objectPositionOnGrid = savedObject.objectPositionOnGrid,
                objectState = savedObject.objectState
            };

            placeableObjectsContainer.placeableObjects.Add(placeableObject);
        }
    }

    public void SaveData(GameData data) {
        ToSaveObjectData toSaveObjectData = new();

        foreach (PlaceableObject placeableObject in placeableObjectsContainer.placeableObjects) {
            placeableObject.objectState = placeableObject.targetObject.GetComponent<IObjectDataPersistence>()?.SaveObject();

            toSaveObjectData.toSaveData.Add(new SaveObjectData(
                placeableObject.placedObject.ItemID,
                placeableObject.targetObject,
                placeableObject.objectPositionOnGrid,
                placeableObject.objectState));
        }

        data.PlacedObjects = JsonUtility.ToJson(toSaveObjectData);
    }
    #endregion
}
