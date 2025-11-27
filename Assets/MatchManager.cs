using System.Collections.Generic;
using UnityEngine;

public class MatchManager : MonoBehaviour
{
    public GridManager Grid;
    public int MinMatchSize = 2;

    public List<List<(int vx, int vy)>> EvaluateMatches()
    {
        // Sanal Grid oluştur (2x çözünürlük)
        int vWidth = Grid.Width * 2;
        int vHeight = Grid.Height * 2;
        BlockType?[,] virtualGrid = new BlockType?[vWidth, vHeight];
        bool[,] visited = new bool[vWidth, vHeight];

        // Grid'i tara ve sanal grid'i doldur
        for (int x = 0; x < Grid.Width; x++)
        {
            for (int y = 0; y < Grid.Height; y++)
            {
                var cell = Grid.GetCell(x, y);
                if (cell != null && cell.IsFilled && cell.Block != null)
                {
                    Block b = cell.Block;
                    // 4 alt bloğu sanal grid'e işle
                    for (int i = 0; i < 4; i++)
                    {
                        // Alt blok aktif mi?
                        if (b.IsCompact && !b.ActiveSubBlocks[i]) continue;

                        int vx = x * 2 + (i % 2); // 0->0, 1->1, 2->0, 3->1
                        int vy = y * 2 + (i / 2); // 0->0, 1->0, 2->1, 3->1
                        
                        // Renk belirle
                        BlockType type = b.Type;
                        if (b.IsCompact && i < b.SubTypes.Length)
                            type = b.SubTypes[i];
                            
                        virtualGrid[vx, vy] = type;
                    }
                }
            }
        }

        var removedGroups = new List<List<(int vx, int vy)>>();

        // Sanal grid üzerinde FloodFill yap
        for (int vx = 0; vx < vWidth; vx++)
        {
            for (int vy = 0; vy < vHeight; vy++)
            {
                if (visited[vx, vy]) continue;
                if (virtualGrid[vx, vy] == null) continue;

                var group = FloodFillVirtual(vx, vy, virtualGrid[vx, vy].Value, virtualGrid, visited, vWidth, vHeight);
                if (group.Count >= MinMatchSize)
                {
                    removedGroups.Add(group);
                }
            }
        }

        return removedGroups;
    }

    public void ProcessMatches()
    {
        StartCoroutine(ProcessMatchesRoutine());
    }

    System.Collections.IEnumerator ProcessMatchesRoutine()
    {
        bool anyMatch = true;
        while (anyMatch)
        {
            // 1. Eşleşmeleri bul
            var groups = EvaluateMatches();
            if (groups.Count == 0)
            {
                anyMatch = false;
                break;
            }

            // 2. Eşleşenleri yok et
            RemoveGroups(groups);
            
            // Animasyon için bekle
            yield return new WaitForSeconds(0.3f);

            // 3. Kalanları büyüt (Jelly Effect)
            bool anyExpanded = false;
            foreach (var (x, y, cell) in Grid.AllCells())
            {
                if (cell != null && cell.IsFilled && cell.Block != null)
                {
                    if (cell.Block.ExpandToFill())
                        anyExpanded = true;
                }
            }

            // Eğer büyüme olduysa yeni eşleşmeler olabilir, döngü devam etsin
            if (anyExpanded)
            {
                yield return new WaitForSeconds(0.2f); // Büyüme animasyonu için bekle
                anyMatch = true;
            }
            else
            {
                // Büyüme olmadıysa ve eşleşme bittiyse çık
                anyMatch = false; 
                
                // Ancak EvaluateMatches tekrar çalışmalı mı? 
                // RemoveGroups sonrası yeni durum oluştu ama büyüme olmadı.
                // Normalde büyüme olmazsa yeni eşleşme de olmaz (düşme yok).
            }
        }
    }

    List<(int vx, int vy)> FloodFillVirtual(int sx, int sy, BlockType type, BlockType?[,] grid, bool[,] visited, int w, int h)
    {
        var result = new List<(int vx, int vy)>();
        var stack = new Stack<(int vx, int vy)>();
        stack.Push((sx, sy));

        int[,] dirs = new int[,] { { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 } };

        while (stack.Count > 0)
        {
            var (x, y) = stack.Pop();
            
            if (x < 0 || x >= w || y < 0 || y >= h) continue;
            if (visited[x, y]) continue;
            if (grid[x, y] != type) continue;

            visited[x, y] = true;
            result.Add((x, y));

            for (int i = 0; i < 4; i++)
            {
                int nx = x + dirs[i, 0];
                int ny = y + dirs[i, 1];
                if (nx >= 0 && nx < w && ny >= 0 && ny < h && !visited[nx, ny])
                    stack.Push((nx, ny));
            }
        }
        return result;
    }

    // Removes groups and returns total removed count
    public int RemoveGroups(List<List<(int vx, int vy)>> groups)
    {
        int removedCount = 0;
        
        foreach (var group in groups)
        {
            foreach (var pos in group)
            {
                // Sanal koordinattan gerçek koordinata dön
                int cx = pos.vx / 2;
                int cy = pos.vy / 2;
                
                // Alt blok indeksi (0-3)
                int subIndex = (pos.vy % 2) * 2 + (pos.vx % 2);

                var cell = Grid.GetCell(cx, cy);
                if (cell != null && cell.IsFilled && cell.Block != null)
                {
                    // Alt bloğu yok et
                    cell.Block.DestroySubBlock(subIndex);
                    removedCount++;
                    
                    // Eğer blok tamamen yok olduysa (DestroySubBlock içinde Destroy çağrıldıysa)
                    // Cell referansını temizlememiz lazım.
                    // Ancak Block hemen null olmaz (Unity Destroy frame sonu çalışır).
                    // Bu yüzden Block'un "tamamen aktif değil" durumunu kontrol etmeliyiz.
                    
                    if (cell.Block == null) // Destroy hemen çalışmaz ama referans kontrolü
                    {
                        cell.Block = null;
                    }
                    else if (cell.Block.IsCompact)
                    {
                        bool anyActive = false;
                        foreach (bool active in cell.Block.ActiveSubBlocks)
                            if (active) anyActive = true;
                        
                        if (!anyActive) cell.Block = null;
                    }
                }
            }
        }
        
        return removedCount;
    }

    System.Collections.IEnumerator AnimateAndDestroy(GameObject obj)
    {
        float duration = 0.4f;
        float elapsed = 0f;
        Vector3 startScale = obj.transform.localScale;

        while (elapsed < duration)
        {
            if (obj == null) yield break;
            
            float t = elapsed / duration;
            
            // Önce biraz şişsin (Anticipation), sonra hızla küçülsün
            float scaleCurve = Mathf.Sin(t * Mathf.PI) * 0.2f; // Şişme
            float shrink = 1f - t; // Küçülme
            
            Vector3 effectScale = startScale * shrink;
            effectScale += new Vector3(scaleCurve, -scaleCurve, scaleCurve); // Squash & Stretch
            
            obj.transform.localScale = effectScale;

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (obj != null)
            Destroy(obj);
    }
}
