// Copyright (C) 2024 Peter Guld Leth

#region

using System.Collections.Generic;
using Colorcrush.Util;
using UnityEngine;
using Random = System.Random;

#endregion

namespace Colorcrush.Game
{
    public class ColorController : MonoBehaviour
    {
        private const int VariationsPerColor = 30;
        private const float VariationRange = 0.05f;
        private static int _currentTargetColorIndex;
        private static Color _currentTargetColor;
        private readonly Queue<Color> _currentColorVariations = new();
        private int _randomSeed;

        private void Start()
        {
            _randomSeed = ProjectConfig.InstanceConfig.randomSeed;
            GenerateColorVariations();
        }

        private void GenerateColorVariations()
        {
            _currentColorVariations.Clear();
            var random = new Random(_randomSeed);

            _currentTargetColor = ColorArray.SRGBTargetColors[_currentTargetColorIndex];

            for (var i = 0; i < VariationsPerColor; i++)
            {
                var variation = new Color(
                    Mathf.Clamp01(_currentTargetColor.r + (float)(random.NextDouble() * 2 - 1) * VariationRange),
                    Mathf.Clamp01(_currentTargetColor.g + (float)(random.NextDouble() * 2 - 1) * VariationRange),
                    Mathf.Clamp01(_currentTargetColor.b + (float)(random.NextDouble() * 2 - 1) * VariationRange),
                    _currentTargetColor.a
                );
                _currentColorVariations.Enqueue(variation);
            }
        }

        public Color GetNextColor()
        {
            if (_currentColorVariations.Count == 0)
            {
                GenerateColorVariations();
            }

            if (_currentColorVariations.Count > 0)
            {
                var nextColor = _currentColorVariations.Dequeue();
                _currentColorVariations.Enqueue(nextColor);
                return nextColor;
            }

            Debug.LogError("Failed to get next color. Returning default color.");
            return Color.white;
        }

        public void AdvanceToNextTargetColor()
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
            return _currentTargetColorIndex;
        }

        public static Color GetCurrentTargetColor()
        {
            return _currentTargetColor;
        }
    }
}