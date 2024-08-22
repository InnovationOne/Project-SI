using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

// This class is a container for all placeable objects
public class PlaceableObjectsContainer {
    // Store placeable objects by their position for fast lookup
    private Dictionary<Vector3Int, PlaceableObject> _placeableObjects = new();
    public IReadOnlyDictionary<Vector3Int, PlaceableObject> PlaceableObjects => _placeableObjects;


    /// <summary>
    /// Indexer to access placeable objects by their position
    /// </summary>
    public PlaceableObject this[Vector3Int position] {
        get {
            if (_placeableObjects.TryGetValue(position, out var placeableObject)) {
                return placeableObject;
            } else {
                return null;
            }
        }
    }

    /// <summary>
    /// Adds a placeable object to the container at the specified position.
    /// </summary>
    /// <param name="position">The position where the placeable object should be added.</param>
    /// <param name="placeableObject">The placeable object to be added.</param>
    /// <exception cref="KeyNotFoundException">Thrown if a placeable object already exists at the specified position.</exception>
    public void Add(Vector3Int position, PlaceableObject placeableObject) {
        if (!_placeableObjects.ContainsKey(position)) {
            _placeableObjects.Add(position, placeableObject);
        } else {
            throw new KeyNotFoundException($"Placeable object already exists at position {position}.");
        }
    }

    /// <summary>
    /// Removes a placeable object from the container based on its position.
    /// </summary>
    /// <param name="position">The position of the placeable object to remove.</param>
    /// <exception cref="KeyNotFoundException">Thrown if the placeable object with the specified position does not exist in the container.</exception>
    public void Remove(Vector3Int position) {
        if (_placeableObjects.ContainsKey(position)) {
            _placeableObjects.Remove(position);
        } else {
            throw new KeyNotFoundException($"Placeable object with position {position} does not exist.");
        }
    }

    /// <summary>
    /// Serializes the placeable object container into a list of JSON strings.
    /// </summary>
    /// <returns>A list of serialized JSON strings representing the placeable object container.</returns>
    public string SerializePlaceableObjectsContainer() {
        var placeableObjectsContainerJSON = new List<string>();
        foreach (var placeableObject in _placeableObjects.Values) {
            var placeableObjectData = new PlaceableObject.PlaceableObjectData {
                ObjectId = placeableObject.ObjectId,
                Position = placeableObject.Position,
                State = placeableObject.State
            };
            placeableObjectsContainerJSON.Add(JsonConvert.SerializeObject(placeableObjectData));
        }
        return JsonConvert.SerializeObject(placeableObjectsContainerJSON);
    }

    /// <summary>
    /// Deserializes a JSON string into a list of placeable object instances.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>A list of deserialized placeable object instances.</returns>
    public List<PlaceableObject> DeserializePlaceableObjecteContainer(string json) {
        var placeableObjects = new List<PlaceableObject>();
        var placeableObjectsContainerJSON = JsonConvert.DeserializeObject<List<string>>(json);
        foreach (var placeableObjectsJSON in placeableObjectsContainerJSON) {
            var placeableObjectData = JsonConvert.DeserializeObject<PlaceableObject.PlaceableObjectData>(placeableObjectsJSON);
            placeableObjects.Add(
                new PlaceableObject {
                    ObjectId = placeableObjectData.ObjectId,
                    Position = new Vector3Int(
                        (int)placeableObjectData.Position.x,
                        (int)placeableObjectData.Position.y,
                        (int)placeableObjectData.Position.z),
                    State = placeableObjectData.State
                });
        }

        return placeableObjects;
    }
}
