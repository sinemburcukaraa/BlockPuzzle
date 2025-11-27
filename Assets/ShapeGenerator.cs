using System.Collections.Generic;
using UnityEngine;

public class ShapeGenerator : MonoBehaviour
{
    [Header("Presets")]
    public ShapeData[] PresetShapes;

    System.Random rnd = new System.Random();

    // Sadece SHAPE seçer → spawn işlemi GameManager’da olacak
    public ShapeData[] Generate(int count = 3)
    {
        ShapeData[] result = new ShapeData[count];

        for (int i = 0; i < count; i++)
        {
            // Rastgele bir şekil seç
            var original = PresetShapes[rnd.Next(PresetShapes.Length)];
            
            // ShapeData ScriptableObject olduğu için, çalışma zamanında değiştirmek
            // orijinal asset'i bozar. Bu yüzden kopyasını (instance) oluşturuyoruz.
            var instance = Instantiate(original);
            
            // Rastgele bir renk (BlockType) ata
            int typeCount = System.Enum.GetValues(typeof(BlockType)).Length;
            instance.Type = (BlockType)rnd.Next(typeCount);
            
            // Eğer 2x2 (Compact) şekilse, alt renkleri de rastgele ata
            if (instance.Blocks.Length == 4) // Basit kontrol, daha detaylısı GameManager'da var
            {
                // %50 ihtimalle 3 parça, %50 ihtimalle 4 parça olsun
                // 4 parça için 4 renk, 3 parça için 3 renk + 1 boş (veya aynı renk)
                // Şimdilik görsel olarak 4 parça var ama renkleri farklı olacak.
                
                instance.SubTypes = new BlockType[4];
                for (int j = 0; j < 4; j++)
                {
                    instance.SubTypes[j] = (BlockType)rnd.Next(typeCount);
                }
                
                // İsteğe bağlı: Bazen bir parçayı "boş" yapmak isterseniz,
                // BlockType'a "None" ekleyip onu render etmeyebilirsiniz.
                // Şimdilik sadece renk çeşitliliği istendi.
            }
            
            result[i] = instance;
        }

        return result;
    }
}
