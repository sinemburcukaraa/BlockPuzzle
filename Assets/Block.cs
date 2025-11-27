using System.Collections.Generic;
using UnityEngine;

public class Block : MonoBehaviour
{
    public BlockType Type { get; private set; }
    public BlockType[] SubTypes; // Alt blokların renkleri (Compact mod için)
    public bool IsCompact { get; private set; }
    public bool[] ActiveSubBlocks; // Hangi alt blokların aktif olduğu

    public void Initialize(BlockType type)
    {
        Type = type;
        IsCompact = false;
        UpdateVisuals();
    }

    // Compact mod için çoklu renk atama
    public void InitializeCompact(BlockType[] subTypes)
    {
        SubTypes = subTypes;
        IsCompact = true;
        ActiveSubBlocks = new bool[4] { true, true, true, true };
        
        // Ana tip ilk renk olsun (referans için)
        if (subTypes.Length > 0) Type = subTypes[0];
        
        UpdateVisualsCompact();
    }
    
    public void DestroySubBlock(int index)
    {
        if (!IsCompact)
        {
            // Normal bloksa tamamen yok ol
            Destroy(gameObject);
            return;
        }

        if (index >= 0 && index < ActiveSubBlocks.Length)
        {
            ActiveSubBlocks[index] = false;
            UpdateVisualsCompact();
        }
        
        // Hepsi yok olduysa objeyi sil
        bool anyActive = false;
        foreach (bool active in ActiveSubBlocks)
            if (active) anyActive = true;
            
        if (!anyActive)
            Destroy(gameObject);
    }

    public bool ExpandToFill()
    {
        if (!IsCompact) return false;

        bool changed = false;
        
        // Boş olan (aktif olmayan) alt blokları bul
        List<int> emptyIndices = new List<int>();
        for (int i = 0; i < 4; i++)
        {
            if (!ActiveSubBlocks[i]) emptyIndices.Add(i);
        }

        if (emptyIndices.Count == 0) return false; // Zaten dolu
        if (emptyIndices.Count == 4) return false; // Tamamen boş (yok olmalıydı)

        // Her boşluk için rastgele bir dolu komşu seç ve oraya "büyü"
        // 0: Sol-Alt, 1: Sağ-Alt, 2: Sol-Üst, 3: Sağ-Üst
        // Komşuluklar:
        // 0: 1, 2
        // 1: 0, 3
        // 2: 0, 3
        // 3: 1, 2
        
        foreach (int emptyIdx in emptyIndices)
        {
            List<int> neighbors = new List<int>();
            if (emptyIdx == 0) { neighbors.Add(1); neighbors.Add(2); }
            else if (emptyIdx == 1) { neighbors.Add(0); neighbors.Add(3); }
            else if (emptyIdx == 2) { neighbors.Add(0); neighbors.Add(3); }
            else if (emptyIdx == 3) { neighbors.Add(1); neighbors.Add(2); }

            // Sadece aktif komşuları al
            List<int> activeNeighbors = new List<int>();
            foreach (int n in neighbors)
            {
                if (ActiveSubBlocks[n]) activeNeighbors.Add(n);
            }

            if (activeNeighbors.Count > 0)
            {
                // Rastgele bir komşu seç
                int sourceIdx = activeNeighbors[Random.Range(0, activeNeighbors.Count)];
                
                // Rengi kopyala (Büyüme efekti)
                SubTypes[emptyIdx] = SubTypes[sourceIdx];
                ActiveSubBlocks[emptyIdx] = true;
                changed = true;
            }
        }

        if (changed)
        {
            UpdateVisualsCompact();
        }

        return changed;
    }

    void UpdateVisuals()
    {
        var renderers = GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            renderer.material.color = GetColorForType(Type);
        }
    }

    void UpdateVisualsCompact()
    {
        var renderers = GetComponentsInChildren<Renderer>();
        // Renderers sırası ile SubTypes sırasının eşleştiğini varsayıyoruz
        for (int i = 0; i < renderers.Length; i++)
        {
            if (i < SubTypes.Length)
            {
                if (ActiveSubBlocks[i])
                {
                    renderers[i].enabled = true;
                    renderers[i].material.color = GetColorForType(SubTypes[i]);
                }
                else
                {
                    renderers[i].enabled = false;
                }
            }
        }
    }

    public static Color GetColorForType(BlockType type)
    {
        // Jelly Field benzeri parlak ve tatlı renkler
        switch (type)
        {
            case BlockType.Red: return new Color(1f, 0.4f, 0.4f); // Yumuşak Kırmızı
            case BlockType.Blue: return new Color(0.2f, 0.6f, 1f); // Parlak Mavi
            case BlockType.Green: return new Color(0.4f, 0.9f, 0.4f); // Fıstık Yeşili
            case BlockType.Yellow: return new Color(1f, 0.85f, 0.3f); // Altın Sarısı
            case BlockType.Purple: return new Color(0.7f, 0.4f, 1f); // Açık Mor
            default: return Color.white;
        }
    }

    public void TriggerJellyEffect()
    {
        StopAllCoroutines();
        StartCoroutine(DoWobble());
    }

    System.Collections.IEnumerator DoWobble()
    {
        float duration = 0.5f;
        float elapsed = 0f;
        Vector3 originalScale = Vector3.one; // Prefab scale'i 1 varsayıyoruz

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            // Sönümlenen sinüs dalgası (Jöle titremesi)
            float wobble = Mathf.Sin(t * 20f) * (1f - t) * 0.3f;
            
            // X ve Z genişlerken Y basılır (Hacim koruma hissi)
            transform.localScale = originalScale + new Vector3(wobble, -wobble, wobble);

            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale = originalScale;
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
    public BlockType[] SubTypes; // Compact mod için alt renkler
    public GameObject BlockPrefab; // prefab used when placing
}
