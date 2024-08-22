using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/ItemSO/ObjectSO/FenceSO")]
public class FenceSO : ObjectSO {
    public Sprite[] Sprites;
    public Vector2[][] PolygonColliderPaths = new Vector2[][] {
        new Vector2[] {
            new(0.126f, -0.5f),
            new(0.126f, -0.25f),
            new(-0.168f, -0.25f),
            new(-0.168f, -0.5f)
        },
        new Vector2[] {
            new(0.126f, -0.5f),
            new(0.126f, 0.5f),
            new(-0.168f, 0.5f),
            new(-0.168f, -0.5f)
        },
        new Vector2[] {
            new(0.5f, -0.5f),
            new(0.5f, -0.25f),
            new(-0.168f, -0.25f),
            new(-0.168f, -0.5f)
        },
        new Vector2[] {
            new(0.126f, -0.5f),
            new(0.126f, -0.25f),
            new(-0.5f, -0.25f),
            new(-0.5f, -0.5f)
        },
        new Vector2[] {
            new(0.502f, -0.5f),
            new(0.502f, -0.25f),
            new(0.126f, -0.25f),
            new(0.126f, 0.5f),
            new(-0.168f, 0.5f),
            new(-0.168f, -0.5f)
        },
        new Vector2[] {
            new(-0.5f, -0.25f),
            new(-0.5f, -0.5f),
            new(0.126f, -0.5f),
            new(0.126f, 0.5f),
            new(-0.168f, 0.5f),
            new(-0.168f, -0.25f)
        },
        new Vector2[] {
            new (-0.5f, -0.25f),
            new (-0.5f, -0.5f),
            new (0.5f, -0.5f),
            new (0.5f, -0.25f)
        },
        new Vector2[] {
            new(0.126f, -0.25f),
            new(0.126f, 0.5f),
            new(-0.168f, 0.5f),
            new(-0.168f, -0.25f),
            new(-0.5f, -0.25f),
            new(-0.5f, -0.5f),
            new(0.5f, -0.5f),
            new(0.5f, -0.25f)
        }
    };
}
