using System;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;

[CreateAssetMenu(menuName = "Tools/Custom Tiles/Advanced Rule Tile")]
public class AdvancedRuleTile : RuleTile<AdvancedRuleTile.Neighbor> {
    public bool AlwaysConnect;
    public TileBase[] TilesToConnect;
    public bool CheckSelf;

    public class Neighbor : TilingRuleOutput.Neighbor {
        public const int This = 1;
        public const int NotThis = 2;
        public const int Any = 3;
        public const int Specified = 4;
        public const int Nothing = 5;
    }

    public override bool RuleMatch(int neighbor, TileBase tile) {
        switch (neighbor) {
            case Neighbor.This: return Check_This(tile);
            case Neighbor.NotThis: return Check_NotThis(tile);
            case Neighbor.Any: return Check_Any(tile);
            case Neighbor.Specified: return Check_Specified(tile);
            case Neighbor.Nothing: return Check_Nothing(tile);
        }
        return base.RuleMatch(neighbor, tile);
    }

    private bool Check_This(TileBase tile) {
        if (!AlwaysConnect) {
            return tile == this;
        } else {
            return TilesToConnect.Contains(tile) || tile == this;
        }
    }

    private bool Check_NotThis(TileBase tile) {
        return tile != this;
    }

    private bool Check_Any(TileBase tile) {
        if (CheckSelf) {
            return tile != null;
        } else {
            return tile != null && tile != this;
        }
    }

    private bool Check_Specified(TileBase tile) {
        return TilesToConnect.Contains(tile);
    }

    private bool Check_Nothing(TileBase tile) {
        return tile == null;
    }
}