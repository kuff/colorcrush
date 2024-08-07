// Copyright (C) 2024 Peter Guld Leth

#region

using System.Collections.Generic;
using System.Linq;
using Colorcrush.Util;
using UnityEngine;

#endregion

namespace Colorcrush.Game
{
    public class SpriteColorAnalyzer : MonoBehaviour
    {
        public Sprite sprite;

        public Dictionary<Color, List<Vector2>> AnalyzeSpriteColors(Sprite s)
        {
            var texture = s.texture;

            var colorGroups = new Dictionary<Color, List<Vector2>>();
            foreach (var targetColor in ColorArray.SRGBTargetColors)
            {
                colorGroups[targetColor] = new List<Vector2>();
            }

            for (var x = 0; x < texture.width; x++)
            for (var y = 0; y < texture.height; y++)
            {
                var pixelColor = texture.GetPixel(x, y);
                var closestColor = FindClosestColor(pixelColor);
                colorGroups[closestColor].Add(new Vector2(x, y));
            }

            var doPixelBalancing = ProjectConfig.InstanceConfig.doPixelBalancing;
            if (doPixelBalancing)
            {
                BalanceColorGroups(colorGroups); // Comment this out for more "true" color groups
            }

            return colorGroups;
        }

        private Color FindClosestColor(Color color)
        {
            var closestColor = ColorArray.SRGBTargetColors[0];
            var closestDistance = ColorDistance(color, closestColor);

            foreach (var targetColor in ColorArray.SRGBTargetColors)
            {
                var distance = ColorDistance(color, targetColor);
                if (distance < closestDistance)
                {
                    closestColor = targetColor;
                    closestDistance = distance;
                }
            }

            return closestColor;
        }

        private float ColorDistance(Color a, Color b)
        {
            var rDiff = a.r - b.r;
            var gDiff = a.g - b.g;
            var bDiff = a.b - b.b;
            return Mathf.Sqrt(rDiff * rDiff + gDiff * gDiff + bDiff * bDiff);
        }

        private void BalanceColorGroups(Dictionary<Color, List<Vector2>> colorGroups)
        {
            var totalPixels = colorGroups.Values.Sum(group => group.Count);
            var averageGroupSize = totalPixels / colorGroups.Count;

            var targetColors = ColorArray.SRGBTargetColors.ToList();

            foreach (var targetColor in targetColors)
            {
                if (colorGroups[targetColor].Count < averageGroupSize)
                {
                    var pixelsNeeded = averageGroupSize - colorGroups[targetColor].Count;

                    while (pixelsNeeded > 0)
                    {
                        var foundDonor = FindClosestLargerGroup(colorGroups, targetColor, averageGroupSize,
                            out var donorColor);

                        if (!foundDonor)
                        {
                            break;
                        }

                        var donorPixels = colorGroups[donorColor];
                        var pixelsToMove = Mathf.Min(pixelsNeeded, donorPixels.Count - averageGroupSize);

                        for (var i = 0; i < pixelsToMove; i++)
                        {
                            colorGroups[targetColor].Add(donorPixels[0]);
                            donorPixels.RemoveAt(0);
                        }

                        pixelsNeeded -= pixelsToMove;
                    }
                }
            }
        }

        private bool FindClosestLargerGroup(Dictionary<Color, List<Vector2>> colorGroups,
            Color targetColor, int averageGroupSize, out Color closestColor)
        {
            closestColor = new Color();
            var closestDistance = float.MaxValue;
            var found = false;

            foreach (var entry in colorGroups)
            {
                if (entry.Value.Count > averageGroupSize)
                {
                    var distance = ColorDistance(targetColor, entry.Key);
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