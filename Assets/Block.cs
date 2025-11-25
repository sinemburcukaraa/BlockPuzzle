using UnityEngine;

public class Block : MonoBehaviour
{
    public BlockType Type { get; private set; }

    public void Initialize(BlockType type)
    {
        Type = type;
        // Optional: update visuals here (sprite, color, animator)
    }
}

public enum BlockType
{
    Red,
    Blue,
    Green,
    Yellow,
    Purple,
}

[CreateAssetMenu(fileName = "ShapeData", menuName = "JellyField/ShapeData")]
public class ShapeData : ScriptableObject
{
    // Relative cell offsets from pivot (usually bottom-left or custom pivot)
    public Vector2Int[] Blocks;
    public BlockType Type;
    public GameObject BlockPrefab; // prefab used when placing
}
