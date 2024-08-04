using System.Collections.Generic;
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
    }
}