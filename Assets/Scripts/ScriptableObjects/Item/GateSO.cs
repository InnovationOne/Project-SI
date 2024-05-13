using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/ItemSO/ObjectSO/GateSO")]
public class GateSO : ObjectSO {
    public Sprite[] Sprites;
    public Vector2[][] ClosePolygonColliderPaths = new Vector2[][] {
        new Vector2[] { // Horizontal-Closed
            new(-0.5f, -0.25f),
            new(-0.5f, -0.5f),
            new(0.5f, -0.5f),
            new(0.5f, -0.25f)
        },
        new Vector2[] { // Vertical-Closed
            new(0.126f, -0.5f),
            new(0.126f, 0.5f),
            new(-0.168f, 0.5f),
            new(-0.168f, -0.5f)
        },
    };

    public Vector2[][] OpenPolygonColliderPaths = new Vector2[][] {
        new Vector2[] { // Horizontal-Open-Path1
            new(-0.332f, -0.5f),
            new(-0.332f, -0.25f),
            new(-0.5f, -0.25f),
            new(-0.5f, -0.5f)
        },
        new Vector2[] { // Horizontal-Open-Path2
            new(0.5f, -0.5f),
            new(0.5f, -0.25f),
            new(0.332f, -0.25f),
            new(0.332f, -0.5f)
        },
        new Vector2[] { // Vertical-Open-Left-To-Right-Path1
            new(0.334f, 0.292f),
            new(0.334f, 0.5f),
            new(-0.168f, 0.5f),
            new(-0.168f, 0.292f)
        },
        new Vector2[] { // Vertical-Open-Left-To-Right-Path2
            new(0.334f, -0.666f),
            new(0.334f, -0.458f),
            new(-0.168f, -0.458f),
            new(-0.168f, -0.666f)
        },
        new Vector2[] { // Vertical-Open-Right-To-Left-Path1
            new(0.168f, 0.292f),
            new(0.168f, 0.5f),
            new(-0.334f, 0.5f),
            new(-0.334f, 0.292f)
        },
        new Vector2[] { // Vertical-Open-Right-To-Left-Path2
            new(0.168f, -0.666f),
            new(0.168f, -0.458f),
            new(-0.334f, -0.458f),
            new(-0.334f, -0.666f)
        },
    }; 
}
