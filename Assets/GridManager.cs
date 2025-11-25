using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int Width = 10;
    public int Height = 10;
    public float CellSize = 1f;
    public Transform BlocksParent;
    public GameObject tilePrefab;

    Cell[,] grid;

    void Awake()
    {
        InitializeGrid();
        SpawnTiles();
        // CenterCameraPositionOnGrid();
    }

    #region Tile & Block Spawning
    void SpawnTiles()
    {
        float halfWidth = Width * 0.5f;
        float halfHeight = Height * 0.5f;

        for (int x = 0; x < Width; x++)
        for (int z = 0; z < Height; z++)
        {
            Vector3 pos =
                transform.position
                + new Vector3(
                    (x - halfWidth + 0.5f) * CellSize,
                    0,
                    (z - halfHeight + 0.5f) * CellSize
                );

            Instantiate(tilePrefab, pos, Quaternion.identity, transform);
        }
    }

    public void PlaceShape(ShapeData shape, int startX, int startY)
    {
        foreach (var offset in shape.Blocks)
        {
            int x = startX + offset.x;
            int y = startY + offset.y;

            var prefab = shape.BlockPrefab;
            var world = GridToWorld(x, y);
            var go = Instantiate(prefab, world, Quaternion.identity, BlocksParent);
            var block = go.GetComponent<Block>();
            if (block == null)
                block = go.AddComponent<Block>();
            block.Initialize(shape.Type);
            grid[x, y].Block = block;
        }
    }
    #endregion

    #region Grid Utilities
    public Vector3 GridToWorld(int x, int y)
    {
        float halfWidth = Width * 0.5f;
        float halfHeight = Height * 0.5f;

        return transform.position
            + new Vector3((x - halfWidth + 0.5f) * CellSize, 0, (y - halfHeight + 0.5f) * CellSize);
    }

    public (int x, int y) WorldToGrid(Vector3 worldPos)
    {
        var local = worldPos - transform.position;
        int x = Mathf.FloorToInt(local.x / CellSize + Width * 0.5f);
        int y = Mathf.FloorToInt(local.z / CellSize + Height * 0.5f);
        return (x, y);
    }

    public bool InBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

    public bool CanPlace(ShapeData shape, int startX, int startY)
    {
        foreach (var offset in shape.Blocks)
        {
            int x = startX + offset.x;
            int y = startY + offset.y;
            if (!InBounds(x, y))
                return false;
            if (grid[x, y].IsFilled)
                return false;
        }
        return true;
    }

    public Cell GetCell(int x, int y) => InBounds(x, y) ? grid[x, y] : null;

    public IEnumerable<(int x, int y, Cell cell)> AllCells()
    {
        for (int x = 0; x < Width; x++)
        for (int y = 0; y < Height; y++)
            yield return (x, y, grid[x, y]);
    }
    #endregion

    #region Grid Initialization
    void InitializeGrid()
    {
        grid = new Cell[Width, Height];
        for (int x = 0; x < Width; x++)
        for (int y = 0; y < Height; y++)
            grid[x, y] = new Cell();
    }
    #endregion

    #region Camera Centering
    void CenterCameraPositionOnGrid()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return;

        // Grid’in ortadaki hücresi
        int centerX = Width / 2;
        int centerY = Height / 2;
        Vector3 centerWorld = GridToWorld(centerX, centerY);

        // Kameranın mevcut rotasyonunu bozmadan pozisyonu ayarlıyoruz
        Vector3 offset = new Vector3(0, 10f, -10f); // yukarı ve geriye
        cam.transform.position = centerWorld + offset;
    }
    #endregion
}
