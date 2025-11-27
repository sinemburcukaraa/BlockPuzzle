using System.Collections.Generic;
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
    GameObject compact2x2Prefab;

    void Start()
    {
        // Compact prefab'i hazırla
        if (Generator != null && Generator.PresetShapes != null)
        {
            Debug.Log($"Checking {Generator.PresetShapes.Length} shapes for 2x2...");
            foreach (var s in Generator.PresetShapes)
            {
                if (Is2x2Shape(s))
                {
                    Debug.Log("Found 2x2 shape: " + s.name);
                    CreateCompactPrefab(s.BlockPrefab);
                    break;
                }
            }
        }
        else
        {
            Debug.LogError("Generator or PresetShapes is null!");
        }

        // Eğer hala null ise (presetlerde yoksa), fallback oluştur
        if (compact2x2Prefab == null)
        {
            Debug.LogWarning("No 2x2 shape found in presets. Creating fallback prefab.");
            CreateCompactPrefab(null);
        }

        PopulateGrid();
        NextTurn();
    }

    void PopulateGrid()
    {
        // Tüm koordinatları listele
        List<Vector2Int> allCoords = new List<Vector2Int>();
        for (int x = 0; x < Grid.Width; x++)
        {
            for (int y = 0; y < Grid.Height; y++)
            {
                allCoords.Add(new Vector2Int(x, y));
            }
        }

        // Karıştır (Fisher-Yates Shuffle)
        System.Random rnd = new System.Random();
        int n = allCoords.Count;
        while (n > 1)
        {
            n--;
            int k = rnd.Next(n + 1);
            Vector2Int value = allCoords[k];
            allCoords[k] = allCoords[n];
            allCoords[n] = value;
        }

        // 4 tane boşluk bırak, gerisini doldur
        int emptyCount = 4;
        for (int i = 0; i < allCoords.Count - emptyCount; i++)
        {
            Vector2Int pos = allCoords[i];
            SpawnRandomBlockAt(pos.x, pos.y);
        }
    }

    void SpawnRandomBlockAt(int x, int y)
    {
        if (compact2x2Prefab == null)
        {
            Debug.LogError("CompactPrefab is null, cannot spawn block at " + x + "," + y);
            return;
        }

        // Rastgele renkler üret
        System.Random rnd = new System.Random();
        int typeCount = System.Enum.GetValues(typeof(BlockType)).Length;
        BlockType[] subTypes = new BlockType[4];
        for (int j = 0; j < 4; j++)
        {
            subTypes[j] = (BlockType)rnd.Next(typeCount);
        }

        // Bloğu oluştur
        Vector3 worldPos = Grid.GridToWorld(x, y);
        GameObject go = Instantiate(compact2x2Prefab, worldPos, Quaternion.identity, Grid.BlocksParent);
        
        Block block = go.GetComponent<Block>();
        if (block == null) block = go.AddComponent<Block>();
        
        block.InitializeCompact(subTypes);
        block.TriggerJellyEffect();

        // Grid'e kaydet
        var cell = Grid.GetCell(x, y);
        if (cell != null)
            cell.Block = block;
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

            // 2x2 Kare kontrolü (4 bloklu ve 2x2 boyutunda)
            if (Is2x2Shape(data))
            {
                CreateCompactPrefab(data.BlockPrefab);
                
                // ShapeData'yı kopyala ve modifiye et
                data = Instantiate(data);
                data.Blocks = new Vector2Int[] { Vector2Int.zero }; // Artık mantıksal olarak 1 blok
                data.BlockPrefab = compact2x2Prefab; // Görsel olarak 4 bloklu prefab
            }

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
                // CellSize'ı hesaba kat!
                Vector3 pos = shapeObj.transform.position + 
                              new Vector3(offset.x * Grid.CellSize, 0, offset.y * Grid.CellSize);

                GameObject blockObj = Instantiate(data.BlockPrefab, pos, Quaternion.identity, shapeObj.transform);
                
                // Rengi ayarla
                Block block = blockObj.GetComponent<Block>();
                if (block != null)
                {
                    if (data.SubTypes != null && data.SubTypes.Length > 0)
                    {
                        // Compact mod (çok renkli)
                        block.InitializeCompact(data.SubTypes);
                    }
                    else
                    {
                        // Normal mod (tek renk)
                        block.Initialize(data.Type);
                    }
                }
            }
            
            // Spawn Animasyonu (Pop-up)
            StartCoroutine(AnimateSpawn(shapeObj.transform));
        }
    }

    bool Is2x2Shape(ShapeData data)
    {
        if (data.Blocks.Length != 4) return false;
        
        int maxX = 0, maxY = 0;
        foreach (var b in data.Blocks)
        {
            if (b.x > maxX) maxX = b.x;
            if (b.y > maxY) maxY = b.y;
        }
        // 0 ve 1 indekslerini kullanıyorsa (boyut 2x2)
        return maxX == 1 && maxY == 1;
    }

    void CreateCompactPrefab(GameObject originalPrefab)
    {
        if (compact2x2Prefab != null) return;

        compact2x2Prefab = new GameObject("Compact2x2_Prefab");
        compact2x2Prefab.transform.position = new Vector3(-1000, -1000, 0); // Görünmez bir yere koy
        compact2x2Prefab.AddComponent<Block>(); // Block bileşeni ekle
        
        // 4 küçük blok oluştur
        float step = Grid.CellSize * 0.25f; // Merkeze göre offset
        Vector3[] offsets = new Vector3[] {
            new Vector3(-step, 0, -step),
            new Vector3(step, 0, -step),
            new Vector3(-step, 0, step),
            new Vector3(step, 0, step)
        };

        foreach(var off in offsets)
        {
            GameObject sub;
            if (originalPrefab != null)
            {
                sub = Instantiate(originalPrefab, compact2x2Prefab.transform);
            }
            else
            {
                // Fallback: Cube primitive
                sub = GameObject.CreatePrimitive(PrimitiveType.Cube);
                sub.transform.SetParent(compact2x2Prefab.transform);
                // Collider'ı kaldır (ana obje yönetecek veya trigger olacak)
                Destroy(sub.GetComponent<Collider>());
            }

            sub.transform.localPosition = off;
            sub.transform.localScale = Vector3.one * 0.45f; // Biraz boşluk bırak
            
            // Alt objelerdeki Block scriptini kaldır
            var b = sub.GetComponent<Block>();
            if (b != null) Destroy(b);
        }
        
        DontDestroyOnLoad(compact2x2Prefab); 
    }

    System.Collections.IEnumerator AnimateSpawn(Transform target)
    {
        float duration = 0.4f;
        float elapsed = 0f;
        Vector3 finalScale = target.localScale;
        target.localScale = Vector3.zero;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            // Elastic Out efekti
            float scale = Mathf.Sin(-13 * (t + 1) * Mathf.PI / 2) * Mathf.Pow(2, -10 * t) + 1;
            
            target.localScale = finalScale * scale;
            elapsed += Time.deltaTime;
            yield return null;
        }
        target.localScale = finalScale;
    }

    public bool TryPlaceShape(ShapeData shape, int startX, int startY)
    {
        if (!Grid.CanPlace(shape, startX, startY))
        {
            // invalid placement
            return false;
        }

        Grid.PlaceShape(shape, startX, startY);

        // Eşleşmeleri kontrol et ve zincirleme reaksiyonu başlat
        if (Match != null)
        {
            Match.ProcessMatches();
        }

        // Sıradaki şekiller bitti mi?
        bool allUsed = true;
        foreach (Transform t in ShapeSpawnParent)
        {
            if (t.childCount > 0) allUsed = false;
        }

        if (allUsed)
            NextTurn();

        return true;
    }
}
