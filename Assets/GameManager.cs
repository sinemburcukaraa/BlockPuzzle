using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GridManager Grid;
    public ShapeGenerator Generator;
    public MatchManager Match;

    [Header("Spawn Settings")]
    public Transform ShapeSpawnParent;
    public Vector3[] SpawnPositions = new Vector3[3];

    ShapeData[] currentShapes;

    void Start()
    {
        NextTurn();
    }

    void NextTurn()
    {
        currentShapes = Generator.Generate(3);
        SpawnShapes(currentShapes);
    }

    void SpawnShapes(ShapeData[] shapes)
    {
        // eski shape'leri temizle
        foreach (Transform t in ShapeSpawnParent)
            Destroy(t.gameObject);

        for (int i = 0; i < shapes.Length; i++)
        {
            ShapeData data = shapes[i];

            // SHAPE OBJESİ
            GameObject shapeObj = new GameObject("Shape_" + i);
            shapeObj.transform.SetParent(ShapeSpawnParent);
            shapeObj.transform.position = SpawnPositions[i];

            // DRAG SCRIPT EKLE
            var drag = shapeObj.AddComponent<ShapeDragSystem>();
            drag.Data = data;
            drag.Game = this;

            // BLOKLARI SHAPE İÇİNE KOY
            foreach (var offset in data.Blocks)
            {
                Vector3 pos = shapeObj.transform.position + new Vector3(offset.x, 0, offset.y);

                Instantiate(data.BlockPrefab, pos, Quaternion.identity, shapeObj.transform);
            }
        }
    }

    public bool TryPlaceShape(ShapeData shape, int startX, int startY)
    {
        if (!Grid.CanPlace(shape, startX, startY))
        {
            // invalid placement
            return false;
        }

        Grid.PlaceShape(shape, startX, startY);

        var groups = Match.EvaluateMatches();
        if (groups.Count > 0)
            Match.RemoveGroups(groups);

        NextTurn();
        return true;
    }
}
