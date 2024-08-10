// Copyright (C) 2024 Peter Guld Leth

#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

// ReSharper disable InconsistentNaming

#endregion

namespace Colorcrush.Colorspace
{
    public class ColorDataLoader
    {
        public enum ColorFormat
        {
            SRGBZeroToOne,
            SRGBZeroTo255,
            XYY,
        }

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

            try
            {
                var lines = File.ReadAllLines(_filePath);

                foreach (var line in lines)
                {
                    var colorValues = Regex.Split(line.Trim(), _colorSplitRegex);

                    if (colorValues.Length >= 3)
                    {
                        var c1 = ParseColorComponent(colorValues[0]);
                        var c2 = ParseColorComponent(colorValues[1]);
                        var c3 = ParseColorComponent(colorValues[2]);

                        colors.Add(new Vector3(c1, c2, c3));
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading colors from file: {e.Message}");
            }

            return new ColorData(colors.ToArray(), _colorFormat);
        }

        private float ParseColorComponent(string value)
        {
            if (float.TryParse(value, out var parsedValue))
            {
                return _colorFormat == ColorFormat.SRGBZeroTo255 ? Mathf.Clamp(parsedValue, 0f, 255f) / 255f : Mathf.Clamp01(parsedValue);
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