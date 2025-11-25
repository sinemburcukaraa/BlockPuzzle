using UnityEngine;

public class Cell : MonoBehaviour
{
    public bool IsFilled => Block != null;
    public Block Block;
}
