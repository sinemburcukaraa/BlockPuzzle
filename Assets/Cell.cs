using UnityEngine;

[System.Serializable]
public class Cell
{
    public bool IsFilled => Block != null;
    public Block Block;
}
