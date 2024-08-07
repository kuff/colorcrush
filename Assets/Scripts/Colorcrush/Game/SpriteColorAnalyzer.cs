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
        private Dictionary<Color, int> _colorIndexMap;
        private Color[] _targetColors;

        private void Awake()
        {
            InitializeColorData();
        }

        private void InitializeColorData()
        {
            _targetColors = ColorArray.SRGBTargetColors;
            _colorIndexMap = new Dictionary<Color, int>();
            for (var i = 0; i < _targetColors.Length; i++)
            {
                _colorIndexMap[_targetColors[i]] = i;
            }
        }

        public Dictionary<Color, List<Vector2>> AnalyzeSpriteColors(Sprite s)
        {
            if (_targetColors == null || _colorIndexMap == null)
            {
                InitializeColorData();
            }

            var texture = s.texture;
            var pixels = texture.GetPixels32();
            var width = texture.width;
            var height = texture.height;

            var colorGroups = new Dictionary<Color, List<Vector2>>();
            foreach (var color in _targetColors)
            {
                colorGroups[color] = new List<Vector2>();
            }

            var groupCounts = new int[_targetColors.Length];

            for (var i = 0; i < pixels.Length; i++)
            {
                var pixelColor = pixels[i];
                if (ShouldConsiderPixel(pixelColor))
                {
                    var closestColorIndex = FindClosestColorIndex(pixelColor);
                    colorGroups[_targetColors[closestColorIndex]].Add(new Vector2(i % width, i / width));
                    groupCounts[closestColorIndex]++;
                }
            }

            if (ProjectConfig.InstanceConfig.doPixelBalancing)
            {
                BalanceColorGroups(colorGroups, groupCounts);
            }

            return colorGroups;
        }

        private bool ShouldConsiderPixel(Color32 color)
        {
            if (color.a < 3)
            {
                return false;
            }

            const byte threshold = 13;
            return !(color.r <= threshold && color.g <= threshold && color.b <= threshold) &&
                   !(color.r >= 255 - threshold && color.g >= 255 - threshold && color.b >= 255 - threshold);
        }

        private int FindClosestColorIndex(Color32 color)
        {
            var closestIndex = 0;
            var closestDistance = float.MaxValue;

            for (var i = 0; i < _targetColors.Length; i++)
            {
                var distance = ColorDistance(color, _targetColors[i]);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestIndex = i;
                }
            }

            return closestIndex;
        }

        private float ColorDistance(Color32 a, Color b)
        {
            var rDiff = a.r / 255f - b.r;
            var gDiff = a.g / 255f - b.g;
            var bDiff = a.b / 255f - b.b;
            return rDiff * rDiff + gDiff * gDiff + bDiff * bDiff;
        }

        private void BalanceColorGroups(Dictionary<Color, List<Vector2>> colorGroups, int[] groupCounts)
        {
            var totalPixels = groupCounts.Sum();
            var averageGroupSize = totalPixels / _targetColors.Length;

            for (var i = 0; i < _targetColors.Length; i++)
            {
                if (groupCounts[i] < averageGroupSize)
                {
                    var pixelsNeeded = averageGroupSize - groupCounts[i];
                    while (pixelsNeeded > 0)
                    {
                        var donorIndex = FindLargestGroup(groupCounts, averageGroupSize);
                        if (donorIndex == -1)
                        {
                            break;
                        }

                        var donorColor = _targetColors[donorIndex];
                        var targetColor = _targetColors[i];
                        var donorPixels = colorGroups[donorColor];
                        var pixelsToMove = Mathf.Min(pixelsNeeded, groupCounts[donorIndex] - averageGroupSize);

                        colorGroups[targetColor].AddRange(donorPixels.GetRange(0, pixelsToMove));
                        donorPixels.RemoveRange(0, pixelsToMove);

                        groupCounts[i] += pixelsToMove;
                        groupCounts[donorIndex] -= pixelsToMove;
                        pixelsNeeded -= pixelsToMove;
                    }
                }
            }
        }

        private int FindLargestGroup(int[] groupCounts, int averageGroupSize)
        {
            var largestIndex = -1;
            var largestCount = averageGroupSize;

            for (var i = 0; i < groupCounts.Length; i++)
            {
                if (groupCounts[i] > largestCount)
                {
                    largestCount = groupCounts[i];
                    largestIndex = i;
                }
            }

            return largestIndex;
        }
    }
}