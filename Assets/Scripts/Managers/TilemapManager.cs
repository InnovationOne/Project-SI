using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class TilemapManager : NetworkBehaviour {
    public static TilemapManager Instance { get; private set; }

    private Tilemap _targetTilemap;

    private void Awake() {
        if (Instance != null) {
            throw new Exception("Found more than one Tilemap Read Manager in the scene.");
        } else {
            Instance = this;
        }

        _targetTilemap = GetComponent<Tilemap>();
    }
}
