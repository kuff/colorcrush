using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Colorcrush.Color
{
    public class SpriteColorAnalyzer : MonoBehaviour
    {
        public Sprite sprite;

        public Dictionary<UnityEngine.Color, List<Vector2>> AnalyzeSpriteColors(Sprite sprite)
        {
            Texture2D texture = sprite.texture;

            Dictionary<UnityEngine.Color, List<Vector2>> colorGroups = new Dictionary<UnityEngine.Color, List<Vector2>>();
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
                    colorGroups[closestColor].Add(new Vector2(x, y));
                }
            }

            BalanceColorGroups(colorGroups); // Comment this out for more "true" color groups

            return colorGroups;
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

        void BalanceColorGroups(Dictionary<UnityEngine.Color, List<Vector2>> colorGroups)
        {
            int totalPixels = colorGroups.Values.Sum(group => group.Count);
            int averageGroupSize = totalPixels / colorGroups.Count;

            List<UnityEngine.Color> targetColors = ColorArray.sRGBTargetColors.ToList();

            foreach (var targetColor in targetColors)
            {
                if (colorGroups[targetColor].Count < averageGroupSize)
                {
                    int pixelsNeeded = averageGroupSize - colorGroups[targetColor].Count;

                    while (pixelsNeeded > 0)
                    {
                        bool foundDonor = FindClosestLargerGroup(colorGroups, targetColor, averageGroupSize, out var donorColor);

                        if (!foundDonor)
                            break;

                        List<Vector2> donorPixels = colorGroups[donorColor];
                        int pixelsToMove = Mathf.Min(pixelsNeeded, donorPixels.Count - averageGroupSize);

                        for (int i = 0; i < pixelsToMove; i++)
                        {
                            colorGroups[targetColor].Add(donorPixels[0]);
                            donorPixels.RemoveAt(0);
                        }

                        pixelsNeeded -= pixelsToMove;
                    }
                }
            }
        }

        bool FindClosestLargerGroup(Dictionary<UnityEngine.Color, List<Vector2>> colorGroups, UnityEngine.Color targetColor, int averageGroupSize, out UnityEngine.Color closestColor)
        {
            closestColor = new UnityEngine.Color();
            float closestDistance = float.MaxValue;
            bool found = false;

            foreach (var entry in colorGroups)
            {
                if (entry.Value.Count > averageGroupSize)
                {
                    float distance = ColorDistance(targetColor, entry.Key);
                    if (distance < closestDistance)
                    {
                        closestColor = entry.Key;
                        closestDistance = distance;
                        found = true;
                    }
                }
            }

            return found;
        }
    }
}
