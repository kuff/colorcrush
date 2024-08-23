// Copyright (C) 2024 Peter Guld Leth

#region

using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

#endregion

namespace Colorcrush.Game
{
    public static class ColorManager
    {
        private const int VariationsPerColor = 30;
        private const float VariationRange = 0.05f;
        private static int _currentTargetColorIndex;
        private static Color _currentTargetColor;
        private static readonly Queue<Color> CurrentColorVariations = new();
        private static int _randomSeed;

        private static void GenerateColorVariations()
        {
            CurrentColorVariations.Clear();
            var random = new Random(ProjectConfig.InstanceConfig.randomSeed);

            _currentTargetColor = ColorArray.SRGBTargetColors[_currentTargetColorIndex];

            for (var i = 0; i < VariationsPerColor; i++)
            {
                var variation = new Color(
                    Mathf.Clamp01(_currentTargetColor.r + (float)(random.NextDouble() * 2 - 1) * VariationRange),
                    Mathf.Clamp01(_currentTargetColor.g + (float)(random.NextDouble() * 2 - 1) * VariationRange),
                    Mathf.Clamp01(_currentTargetColor.b + (float)(random.NextDouble() * 2 - 1) * VariationRange),
                    _currentTargetColor.a
                );
                CurrentColorVariations.Enqueue(variation);
            }
        }

        public static Color GetNextColor()
        {
            if (CurrentColorVariations.Count == 0)
            {
                GenerateColorVariations();
            }

            if (CurrentColorVariations.Count > 0)
            {
                var nextColor = CurrentColorVariations.Dequeue();
                CurrentColorVariations.Enqueue(nextColor);
                return nextColor;
            }

            Debug.LogError("Failed to get next color. Returning default color.");
            return Color.white;
        }

        public static void AdvanceToNextTargetColor()
        {
            _currentTargetColorIndex++;
            if (_currentTargetColorIndex >= ColorArray.SRGBTargetColors.Length)
            {
                _currentTargetColorIndex = 0;
            }

            GenerateColorVariations();
        }

        public static int GetCurrentTargetColorIndex()
        {
            if (CurrentColorVariations.Count == 0)
            {
                GenerateColorVariations();
            }

            return _currentTargetColorIndex;
        }

        public static Color GetCurrentTargetColor()
        {
            if (CurrentColorVariations.Count == 0)
            {
                GenerateColorVariations();
            }

            return _currentTargetColor;
        }
    }
}