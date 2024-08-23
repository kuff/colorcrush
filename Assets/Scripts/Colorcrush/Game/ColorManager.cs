// Copyright (C) 2024 Peter Guld Leth

#region

using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

#endregion

namespace Colorcrush.Game
{
    /*
     * For now, this just generates random variations and analysis values.
     */
    public static class ColorManager
    {
        private const int VariationsPerColor = 30;
        private const float VariationRange = 0.05f;
        private static readonly Random RandomInstance = new(ProjectConfig.InstanceConfig.randomSeed);

        public static List<Color> GenerateColorVariations(Color baseColor)
        {
            var variations = new List<Color>();

            for (var i = 0; i < VariationsPerColor; i++)
            {
                // DEBUG: Generate a random color
                var variation = new Color(
                    Mathf.Clamp01(baseColor.r + (float)(RandomInstance.NextDouble() * 2 - 1) * VariationRange),
                    Mathf.Clamp01(baseColor.g + (float)(RandomInstance.NextDouble() * 2 - 1) * VariationRange),
                    Mathf.Clamp01(baseColor.b + (float)(RandomInstance.NextDouble() * 2 - 1) * VariationRange),
                    baseColor.a
                );
                variations.Add(variation);
            }

            return variations;
        }

        public static float[] GenerateColorAnalysis(List<Color> selections)
        {
            var analysis = new float[8];

            for (var i = 0; i < 8; i++)
            {
                // DEBUG: Generate a random value between 0 and 1, with a skew towards 0.5
                var u = RandomInstance.NextDouble();
                var v = RandomInstance.NextDouble();
                var skewedValue = (u + v) / 2.0;
                analysis[i] = (float)skewedValue;
            }

            return analysis;
        }
    }
}