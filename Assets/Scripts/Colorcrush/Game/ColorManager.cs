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
        private static readonly Dictionary<Color, float[]> ColorAnalysisCache = new();

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

        public static float[] GenerateColorAnalysis(Color targetColor, List<Color> selections)
        {
            if (ColorAnalysisCache.TryGetValue(targetColor, out var cachedAnalysis))
            {
                return cachedAnalysis;
            }

            var analysis = new float[8];

            for (var i = 0; i < 8; i++)
            {
                // DEBUG: Generate a random value between 0 and 1, with a skew towards 0.5
                var u = RandomInstance.NextDouble();
                var v = RandomInstance.NextDouble();
                var skewedValue = (u + v) / 2.0;
                analysis[i] = (float)skewedValue;
            }

            ColorAnalysisCache[targetColor] = analysis;
            return analysis;
        }

        public static List<Color> GetColorMatrixEdges(Color targetColor)
        {
            var edges = new List<Color>();

            for (var r = -1; r <= 1; r += 2)
            {
                for (var g = -1; g <= 1; g += 2)
                {
                    for (var b = -1; b <= 1; b += 2)
                    {
                        var edgeColor = new Color(
                            Mathf.Clamp01(targetColor.r + r * VariationRange),
                            Mathf.Clamp01(targetColor.g + g * VariationRange),
                            Mathf.Clamp01(targetColor.b + b * VariationRange),
                            targetColor.a
                        );
                        edges.Add(edgeColor);
                    }
                }
            }

            return edges;
        }
    }
}