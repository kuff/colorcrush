using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Colorcrush.Color;

public class ColorController : MonoBehaviour
{
    private Queue<Color> colorQueue = new Queue<Color>();
    private const int VariationsPerColor = 30;
    private const float VariationRange = 0.05f; // 5% variation
    private int randomSeed = 42; // Specify the random seed here

    private void Start()
    {
        GenerateColorVariations();
    }

    private void GenerateColorVariations()
    {
        List<Color> allVariations = new List<Color>();
        System.Random random = new System.Random(randomSeed);

        foreach (Color targetColor in ColorArray.sRGBTargetColors)
        {
            for (int i = 0; i < VariationsPerColor; i++)
            {
                Color variation = new Color(
                    Mathf.Clamp01(targetColor.r + (float)(random.NextDouble() * 2 - 1) * VariationRange),
                    Mathf.Clamp01(targetColor.g + (float)(random.NextDouble() * 2 - 1) * VariationRange),
                    Mathf.Clamp01(targetColor.b + (float)(random.NextDouble() * 2 - 1) * VariationRange),
                    targetColor.a
                );
                allVariations.Add(variation);
            }
        }

        // Shuffle the variations
        allVariations = allVariations.OrderBy(x => random.NextDouble()).ToList();

        // Add all variations to the queue
        foreach (Color variation in allVariations)
        {
            colorQueue.Enqueue(variation);
        }
    }

    public Color GetNextColor()
    {
        if (colorQueue.Count == 0)
        {
            GenerateColorVariations(); // Regenerate if queue is empty
        }

        Color nextColor = colorQueue.Dequeue();
        colorQueue.Enqueue(nextColor); // Add back to the end for wrapping
        return nextColor;
    }
}
