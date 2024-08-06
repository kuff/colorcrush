// Copyright (C) 2024 Peter Guld Leth

#region

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;
using Colorcrush.Util;

#endregion

namespace Colorcrush.Game
{
    public class ColorController : MonoBehaviour
    {
        private const int VariationsPerColor = 30;
        private const float VariationRange = 0.05f; // 5% variation
        private readonly Queue<Color> _colorQueue = new();
        private int _randomSeed;

        private void Start()
        {
            _randomSeed = ProjectConfig.InstanceConfig.randomSeed;
            GenerateColorVariations();
        }

        private void GenerateColorVariations()
        {
            var allVariations = new List<Color>();
            var random = new Random(_randomSeed);

            foreach (var targetColor in ColorArray.SRGBTargetColors)
            {
                for (var i = 0; i < VariationsPerColor; i++)
                {
                    var variation = new Color(
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
            foreach (var variation in allVariations)
            {
                _colorQueue.Enqueue(variation);
            }
        }

        public Color GetNextColor()
        {
            if (_colorQueue.Count == 0)
            {
                GenerateColorVariations(); // Regenerate if queue is empty
            }

            var nextColor = _colorQueue.Dequeue();
            _colorQueue.Enqueue(nextColor); // Add back to the end for wrapping
            return nextColor;
        }
    }
}