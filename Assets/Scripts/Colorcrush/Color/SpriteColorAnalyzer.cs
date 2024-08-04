using System.Collections.Generic;
using UnityEngine;

namespace Colorcrush.Color
{
    public class SpriteColorAnalyzer : MonoBehaviour
    {
        public Sprite sprite;

        private Dictionary<UnityEngine.Color, List<Vector2>> colorGroups;
        private Dictionary<Vector2, UnityEngine.Color> pixelAssignments;

        void Start()
        {
            AnalyzeSpriteColors(sprite);
            BalanceColorGroups();
        }

        void AnalyzeSpriteColors(Sprite sprite)
        {
            Texture2D texture = sprite.texture;
            UnityEngine.Color[] pixels = texture.GetPixels();

            colorGroups = new Dictionary<UnityEngine.Color, List<Vector2>>();
            pixelAssignments = new Dictionary<Vector2, UnityEngine.Color>();
            foreach (var targetColor in ColorArray.sRGBTargetColors)
            {
                colorGroups[targetColor] = new List<Vector2>();
            }

            for (int x = 0; x < texture.width; x++)
            {
                for (int y = 0; y < texture.height; y++)
                {
                    UnityEngine.Color pixelColor = texture.GetPixel(x, y);
                    UnityEngine.Color closestColor = FindClosestColor(pixelColor);
                    Vector2 pixelCoord = new Vector2(x, y);
                    colorGroups[closestColor].Add(pixelCoord);
                    pixelAssignments[pixelCoord] = closestColor;
                }
            }

            // Example usage: print out the number of pixels for each color group
            foreach (var targetColor in ColorArray.sRGBTargetColors)
            {
                Debug.Log($"Color {targetColor}: {colorGroups[targetColor].Count} pixels");
            }
        }

        UnityEngine.Color FindClosestColor(UnityEngine.Color color)
        {
            UnityEngine.Color closestColor = ColorArray.sRGBTargetColors[0];
            float closestDistance = ColorDistance(color, closestColor);

            foreach (var targetColor in ColorArray.sRGBTargetColors)
            {
                float distance = ColorDistance(color, targetColor);
                if (distance < closestDistance)
                {
                    closestColor = targetColor;
                    closestDistance = distance;
                }
            }

            return closestColor;
        }

        float ColorDistance(UnityEngine.Color a, UnityEngine.Color b)
        {
            float rDiff = a.r - b.r;
            float gDiff = a.g - b.g;
            float bDiff = a.b - b.b;
            return Mathf.Sqrt(rDiff * rDiff + gDiff * gDiff + bDiff * bDiff);
        }

        void BalanceColorGroups()
        {
            int totalPixels = 0;
            foreach (var targetColor in ColorArray.sRGBTargetColors)
            {
                totalPixels += colorGroups[targetColor].Count;
            }

            int targetGroupSize = totalPixels / ColorArray.sRGBTargetColors.Length;

            // List to hold excess pixels that need reassignment
            List<Vector2> excessPixels = new List<Vector2>();

            // Collect excess pixels from overpopulated groups
            foreach (var targetColor in ColorArray.sRGBTargetColors)
            {
                while (colorGroups[targetColor].Count > targetGroupSize)
                {
                    Vector2 pixelCoord = colorGroups[targetColor][0];
                    colorGroups[targetColor].RemoveAt(0);
                    excessPixels.Add(pixelCoord);
                }
            }

            // Redistribute excess pixels to underpopulated groups
            foreach (var targetColor in ColorArray.sRGBTargetColors)
            {
                while (colorGroups[targetColor].Count < targetGroupSize && excessPixels.Count > 0)
                {
                    Vector2 pixelCoord = excessPixels[0];
                    excessPixels.RemoveAt(0);
                    colorGroups[targetColor].Add(pixelCoord);
                    pixelAssignments[pixelCoord] = targetColor;
                }
            }
        }

        // Additional helper methods can be added here for further functionality
    }
}
