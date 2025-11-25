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
            result[i] = PresetShapes[rnd.Next(PresetShapes.Length)];
        }

        return result;
    }
}
