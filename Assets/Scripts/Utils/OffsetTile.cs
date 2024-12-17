using UnityEngine;
using UnityEngine.Tilemaps;

public class OffsetTile : Tile {
    public Vector3 Offset;

    public override void GetTileData(Vector3Int location, ITilemap tilemap, ref TileData tileData) {
        base.GetTileData(location, tilemap, ref tileData);
        tileData.transform = Matrix4x4.TRS(Offset, Quaternion.identity, Vector3.one);
    }
}
