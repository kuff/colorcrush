// Copyright (C) 2024 Peter Guld Leth

#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

// ReSharper disable InconsistentNaming

#endregion

namespace Colorcrush.Game
{
    /*
     * Not used in this version of the game.
     */
    public static partial class ColorManager
    {
        public class ColorDataLoader
        {
            private readonly ColorFormat _colorFormat;
            private readonly string _colorSplitRegex;
            private readonly string _filePath;

            public ColorDataLoader(string filePath, string colorSplitRegex, ColorFormat colorFormat)
            {
                _filePath = filePath;
                _colorSplitRegex = colorSplitRegex;
                _colorFormat = colorFormat;
            }

            public ColorData LoadColors()
            {
                var colors = new List<Vector3>();
                var linesProcessed = 0;
                var colorsAdded = 0;
                Vector3? exampleColor = null;

                try
                {
                    var lines = File.ReadAllLines(_filePath);

                    foreach (var line in lines)
                    {
                        linesProcessed++;
                        var colorValues = Regex.Split(line.Trim(), _colorSplitRegex);

                        if (colorValues.Length >= 3)
                        {
                            var c1 = ParseColorComponent(colorValues[0]);
                            var c2 = ParseColorComponent(colorValues[1]);
                            var c3 = ParseColorComponent(colorValues[2]);

                            var newColor = new Vector3(c1, c2, c3);
                            colors.Add(newColor);
                            colorsAdded++;

                            if (exampleColor == null)
                            {
                                exampleColor = newColor;
                            }
                        }
                    }

                    Debug.Log($"Color data loading successful. Processed {linesProcessed} lines, added {colorsAdded} colors.");
                    if (exampleColor.HasValue)
                    {
                        Debug.Log($"Example color added: ({exampleColor.Value.x}, {exampleColor.Value.y}, {exampleColor.Value.z})");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error loading colors from file: {e.Message}");
                    throw new InvalidOperationException("Color data loading failed. Partial loads are not allowed.", e);
                }

                return new ColorData(colors.ToArray(), _colorFormat);
            }

            private float ParseColorComponent(string value)
            {
                if (float.TryParse(value, out var parsedValue))
                {
                    switch (_colorFormat)
                    {
                        case ColorFormat.SrgbZeroTo255:
                        case ColorFormat.DisplayP3ZeroTo255:
                            return Mathf.Clamp(parsedValue, 0f, 255f) / 255f;
                        case ColorFormat.SrgbZeroToOne:
                        case ColorFormat.DisplayP3ZeroToOne:
                            return Mathf.Clamp01(parsedValue);
                        case ColorFormat.Xyy:
                        case ColorFormat.XYZ:
                            return parsedValue; // XYY and XYZ values are not clamped
                        default:
                            return 0f;
                    }
                }

                return 0f;
            }

            public struct ColorData
            {
                public Vector3[] Colors;
                public ColorFormat Format;

                public ColorData(Vector3[] colors, ColorFormat format)
                {
                    Colors = colors;
                    Format = format;
                }
            }
        }
    }
}