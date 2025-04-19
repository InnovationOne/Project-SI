using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class AnimalManager : NetworkBehaviour {
    public static AnimalManager Instance { get; private set; }

    [Header("Prefabs per AnimalId")]
    [Tooltip("Jedes Element entspricht einer AnimalId, index = AnimalId.")]
    [SerializeField] private AnimalBase[] animalPrefabs;

    private readonly string[] randomNames = new string[] {
        "Bella", "Max", "Charlie", "Luna", "Lucy", "Cooper", "Daisy", "Milo", "Lola", "Buddy",
        "Sadie", "Rocky", "Bailey", "Maggie", "Zoey", "Harley", "Sophie", "Teddy", "Chloe", "Oliver",
        "Lily", "Bear", "Coco", "Duke", "Nala", "Jack", "Rosie", "Leo", "Ruby", "Jake",
        "Mia", "Oscar", "Stella", "Tucker", "Molly", "Winston", "Hazel", "Finn", "Roxy", "Simba",
        "Ellie", "Scout", "Gus", "Olive", "Jasper", "Poppy", "Remy", "Ivy", "Louie", "Pepper",
        "Peanut", "Oreo", "Angel", "Blue", "Zeus", "Penny", "Frankie", "George", "Marley", "Willow",
        "Dexter", "Lulu", "Izzy", "Otis", "Mimi", "Bluebell", "Mocha", "Chief", "Pearl", "Sassy",
        "Ziggy", "Harvey", "Sienna", "Bandit", "Pippin", "Mischief", "Blossom", "Blaze", "Phoebe", "Pugsy",
        "Kodiak", "Buttercup", "Nibbles", "Cinnamon", "Fudge", "Marble", "Toffee", "Ember", "Frosty", "Rani",
        "Tango", "Velvet", "Trinket", "Widget", "Yuki", "Zephyr", "Zorro", "Nova", "Whisper", "Echo"
    };

    void Awake() {
        if (Instance != null) {
            Debug.LogError("More than one AnimalManager instance found!");
            return;
        }
        Instance = this;
    }

    public void RequestSpawnJuvenile(int animalId, Vector3 pos) {
        if (IsServer) SpawnJuvenileInternal(animalId, pos);
        else SpawnJuvenileServerRpc(animalId, pos);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnJuvenileServerRpc(int animalId, Vector3 pos) {
        SpawnJuvenileInternal(animalId, pos);
    }

    private void SpawnJuvenileInternal(int animalId, Vector3 pos) {
        // 1) Validierung
        if (animalId < 0 || animalId >= animalPrefabs.Length) return;

        // 2) Prefab instanziieren & network-spawnen
        var prefab = animalPrefabs[animalId];
        var go = Instantiate(prefab.gameObject, pos, Quaternion.identity);
        var netObj = go.GetComponent<NetworkObject>() ?? go.AddComponent<NetworkObject>();
        netObj.Spawn();

        // 3) Name setzen
        if (go.TryGetComponent<AnimalBase>(out var animal)) {
            string finalName = randomNames[Random.Range(0, randomNames.Length)];
            animal.SetNameServer(finalName);
        }
    }
}