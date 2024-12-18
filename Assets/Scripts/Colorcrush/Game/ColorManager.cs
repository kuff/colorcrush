// Copyright (C) 2024 Peter Guld Leth

#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using Random = System.Random;

// ReSharper disable UnusedMember.Global

#endregion

namespace Colorcrush.Game
{
    public static partial class ColorManager
    {
        public enum ColorFormat
        {
            DisplayP3ZeroToOne,
            DisplayP3ZeroTo255,
            SrgbZeroToOne,
            SrgbZeroTo255,
            XYZ,
            Xyy,
        }

        private const int VariationsPerColor = 30;
        private const float VariationRange = 0.05f;
        private static readonly Random RandomInstance = new(ProjectConfig.InstanceConfig.randomSeed);
        private static readonly Dictionary<Color, float[]> ColorAnalysisCache = new();

        // Conversion matrices

        // CIE XYZ to sRGB matrix
        private static Matrix4x4 _xyzToSrgb = new(
            new Vector4(3.174569687f, -1.437132245f, -0.533239074f, 0f),
            new Vector4(-0.978559662f, 1.851015357f, 0.0734006f, 0f),
            new Vector4(0.071795226f, -0.224002081f, 1.061208354f, 0f),
            new Vector4(0f, 0f, 0f, 1f)
        );

        // sRGB to XYZ matrix
        private static Matrix4x4 _srgbToXYZ = _xyzToSrgb.inverse;

        // sRGB to Display P3 matrix
        private static readonly Matrix4x4 SrgbToDisplayP3 = new(
            new Vector4(0.8225f, 0.1774f, 0f, 0f),
            new Vector4(0.0332f, 0.9669f, 0f, 0f),
            new Vector4(0.0171f, 0.0724f, 0.9108f, 0f),
            new Vector4(0f, 0f, 0f, 1f)
        );

        // Display P3 to sRGB matrix (inverse of SRGBToDisplayP3)
        private static readonly Matrix4x4 DisplayP3TosRGB = SrgbToDisplayP3.inverse;

        // Display P3 to XYZ matrix
        private static Matrix4x4 _displayP3ToXYZ = _srgbToXYZ * DisplayP3TosRGB;

        // XYZ to Display P3 matrix
        private static Matrix4x4 _xyzToDisplayP3 = SrgbToDisplayP3 * _xyzToSrgb;

        private static Color[] _targetColors;
        public static bool ApplyGammaCorrection => true;
        public static ColorExperiment CurrentColorExperiment { get; private set; }

        // Define the array of colors
        public static Color[] TargetColors
        {
            get
            {
                if (_targetColors == null)
                {
                    var loader = new ColorDataLoader(
                        ProjectConfig.InstanceConfig.colorDataFilePath,
                        ProjectConfig.InstanceConfig.colorSplitRegex,
                        ProjectConfig.InstanceConfig.colorDataFormat
                    );

                    var colorObjects = loader.LoadColors();
                    _targetColors = new Color[colorObjects.Count];

                    for (var i = 0; i < colorObjects.Count; i++)
                    {
                        var srgbColor = colorObjects[i].ToColorFormat(ColorFormat.SrgbZeroToOne);
                        _targetColors[i] = new Color(srgbColor.Vector.x, srgbColor.Vector.y, srgbColor.Vector.z);
                    }
                }

                return _targetColors;
            }
        }

        public static ColorExperiment BeginColorExperiment(ColorObject baseColor)
        {
            var experimentName = ProjectConfig.InstanceConfig.colorExperimentName;

            // Get the containing type (ColorManager) first
            var managerType = typeof(ColorManager);
            // Then find the nested type by name
            var experimentType = managerType.GetNestedType(experimentName);

            if (experimentType == null)
            {
                throw new InvalidOperationException($"Unknown color experiment name: {experimentName}. Make sure the class exists in the Colorcrush.Game.ColorManager namespace.");
            }

            if (!typeof(ColorExperiment).IsAssignableFrom(experimentType))
            {
                throw new InvalidOperationException($"Type {experimentName} must inherit from ColorExperiment.");
            }

            try
            {
                CurrentColorExperiment = (ColorExperiment)Activator.CreateInstance(experimentType, baseColor);
                return CurrentColorExperiment;
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Failed to create instance of {experimentName}: {e.Message}", e);
            }
        }

        public static ColorObject ConvertToDisplayP3(ColorObject color)
        {
            return color.ToColorFormat(ColorFormat.DisplayP3ZeroToOne);
        }

        public static ColorObject ConvertToSrgb(ColorObject color)
        {
            return color.ToColorFormat(ColorFormat.SrgbZeroToOne);
        }

        public static ColorObject ConvertToXyY(ColorObject color)
        {
            return color.ToColorFormat(ColorFormat.Xyy);
        }

        private static Color ConvertColor(Color color, ColorFormat fromFormat, ColorFormat toFormat, bool disableGammaExpansion = false)
        {
            var vector = new Vector3(color.r, color.g, color.b);
            var convertedVector = ConvertColor(vector, fromFormat, toFormat, disableGammaExpansion);
            return new Color(convertedVector.x, convertedVector.y, convertedVector.z, color.a);
        }

        private static Vector3 ConvertColor(Vector3 vector, ColorFormat fromFormat, ColorFormat toFormat, bool disableGammaExpansion = false)
        {
            // Normalize input if needed
            vector = NormalizeInput(vector, fromFormat);

            // Apply gamma correction to linear space if needed
            if (ApplyGammaCorrection && !disableGammaExpansion && IsGammaEncodedFormat(fromFormat))
            {
                vector = ApplyGammaCorrectionToVector(vector, true);
            }

            // Convert from source color space to XYZ
            var xyzVector = ToXYZ(vector, fromFormat);

            // Convert from XYZ to target color space
            var resultVector = FromXYZ(xyzVector, toFormat);

            // Apply gamma correction from linear space if needed
            if (ApplyGammaCorrection && IsGammaEncodedFormat(toFormat))
            {
                resultVector = ApplyGammaCorrectionToVector(resultVector, false);
            }

            // Denormalize output if needed
            resultVector = DenormalizeOutput(resultVector, toFormat);

            // Clamp the result to valid ranges
            resultVector = ClampVector(resultVector, toFormat);

            return resultVector;
        }

        private static Vector3 NormalizeInput(Vector3 vector, ColorFormat format)
        {
            switch (format)
            {
                case ColorFormat.SrgbZeroTo255:
                case ColorFormat.DisplayP3ZeroTo255:
                    return vector / 255f;
                default:
                    return vector;
            }
        }

        private static Vector3 DenormalizeOutput(Vector3 vector, ColorFormat format)
        {
            switch (format)
            {
                case ColorFormat.SrgbZeroTo255:
                case ColorFormat.DisplayP3ZeroTo255:
                    return vector * 255f;
                default:
                    return vector;
            }
        }

        private static Vector3 ToXYZ(Vector3 vector, ColorFormat format)
        {
            switch (format)
            {
                case ColorFormat.SrgbZeroToOne:
                case ColorFormat.SrgbZeroTo255:
                    return _srgbToXYZ.MultiplyPoint3x4(vector);
                case ColorFormat.DisplayP3ZeroToOne:
                case ColorFormat.DisplayP3ZeroTo255:
                    return _displayP3ToXYZ.MultiplyPoint3x4(vector);
                case ColorFormat.Xyy:
                    return XyYToXYZ(vector);
                case ColorFormat.XYZ:
                    return vector;
                default:
                    return vector; // Assume it's already XYZ
            }
        }

        private static Vector3 FromXYZ(Vector3 xyzVector, ColorFormat format)
        {
            switch (format)
            {
                case ColorFormat.SrgbZeroToOne:
                case ColorFormat.SrgbZeroTo255:
                    return _xyzToSrgb.MultiplyPoint3x4(xyzVector);
                case ColorFormat.DisplayP3ZeroToOne:
                case ColorFormat.DisplayP3ZeroTo255:
                    return _xyzToDisplayP3.MultiplyPoint3x4(xyzVector);
                case ColorFormat.Xyy:
                    return XYZToXyY(xyzVector);
                case ColorFormat.XYZ:
                    return xyzVector;
                default:
                    return xyzVector; // Assume XYZ
            }
        }

        private static bool IsGammaEncodedFormat(ColorFormat format)
        {
            return format is ColorFormat.SrgbZeroToOne or ColorFormat.SrgbZeroTo255 or ColorFormat.DisplayP3ZeroToOne or ColorFormat.DisplayP3ZeroTo255;
        }

        private static Vector3 ClampVector(Vector3 vector, ColorFormat format)
        {
            switch (format)
            {
                case ColorFormat.SrgbZeroToOne:
                case ColorFormat.DisplayP3ZeroToOne:
                    vector.x = Mathf.Clamp01(vector.x);
                    vector.y = Mathf.Clamp01(vector.y);
                    vector.z = Mathf.Clamp01(vector.z);
                    break;
                case ColorFormat.SrgbZeroTo255:
                case ColorFormat.DisplayP3ZeroTo255:
                    vector.x = Mathf.Clamp(vector.x, 0f, 255f);
                    vector.y = Mathf.Clamp(vector.y, 0f, 255f);
                    vector.z = Mathf.Clamp(vector.z, 0f, 255f);
                    break;
                case ColorFormat.XYZ:
                    // XYZ components can be outside [0,1], especially Y (luminance)
                    vector.x = Mathf.Max(0f, vector.x);
                    vector.y = Mathf.Max(0f, vector.y);
                    vector.z = Mathf.Max(0f, vector.z);
                    break;
                case ColorFormat.Xyy:
                    // x and y chromaticity coordinates are typically in [0,1], but z (Y) can be greater
                    vector.x = Mathf.Clamp01(vector.x);
                    vector.y = Mathf.Clamp01(vector.y);
                    vector.z = Mathf.Max(0f, vector.z);
                    break;
            }

            return vector;
        }

        private static Vector3 ApplyGammaCorrectionToVector(Vector3 color, bool toLinear)
        {
            if (toLinear)
            {
                return new Vector3(
                    GammaExpansion(color.x),
                    GammaExpansion(color.y),
                    GammaExpansion(color.z)
                );
            }

            return new Vector3(
                GammaCompression(color.x),
                GammaCompression(color.y),
                GammaCompression(color.z)
            );
        }

        private static float GammaExpansion(float c)
        {
            if (c <= 0.04045f)
            {
                return c / 12.92f;
            }

            return Mathf.Pow((c + 0.055f) / 1.055f, 2.4f);
        }

        private static float GammaCompression(float cLin)
        {
            if (cLin <= 0.0031308f)
            {
                return cLin * 12.92f;
            }

            return 1.055f * Mathf.Pow(cLin, 1.0f / 2.4f) - 0.055f;
        }

        private static Vector3 XyYToXYZ(Vector3 xyY)
        {
            if (xyY.y == 0)
            {
                return Vector3.zero; // Avoid division by zero
            }

            var x = xyY.x * xyY.z / xyY.y;
            var y = xyY.z;
            var z = (1 - xyY.x - xyY.y) * xyY.z / xyY.y;
            return new Vector3(x, y, z);
        }

        private static Vector3 XYZToXyY(Vector3 xyz)
        {
            var sum = xyz.x + xyz.y + xyz.z;
            if (sum == 0)
            {
                return new Vector3(0, 0, xyz.y); // Avoid division by zero
            }

            var x = xyz.x / sum;
            var y = xyz.y / sum;
            return new Vector3(x, y, xyz.y);
        }

        public class ColorObject
        {
            public ColorObject(Vector3 colorVector, ColorFormat format, int directionIndex = -1)
            {
                Vector = colorVector;
                Format = format;
                DirectionIndex = directionIndex;
            }

            public ColorObject(Color unityColor, int directionIndex = -1)
            {
                Vector = new Vector3(unityColor.r, unityColor.g, unityColor.b);
                Format = ColorFormat.SrgbZeroToOne;
                DirectionIndex = directionIndex;
            }

            public Vector3 Vector { get; }

            public Vector3 Vector255
            {
                get
                {
                    if (Format is ColorFormat.DisplayP3ZeroToOne or ColorFormat.SrgbZeroToOne)
                    {
                        return Vector * 255f;
                    }

                    throw new InvalidOperationException("Vector255 is only allowed for DisplayP3 and sRGB formats.");
                }
            }

            public ColorFormat Format { get; }
            public int DirectionIndex { get; }

            public ColorObject ToColorFormat(ColorFormat targetFormat)
            {
                return new ColorObject(ConvertColor(Vector, Format, targetFormat), targetFormat);
            }

            public Color ToDisplayColor()
            {
                // TODO: Convert to Display P3 if that setting is enabled

                // Convert to sRGB format if needed
                var srgbColor = ToColorFormat(ColorFormat.SrgbZeroToOne).Vector;

                return new Color(srgbColor.x, srgbColor.y, srgbColor.z, 1f);
            }
        }

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

            public List<ColorObject> LoadColors()
            {
                var colors = new List<ColorObject>();
                var linesProcessed = 0;
                var colorsAdded = 0;

                var lines = File.ReadAllLines(_filePath);

                foreach (var line in lines)
                {
                    linesProcessed++;

                    var match = Regex.Match(line.Trim(), _colorSplitRegex);
                    if (!match.Success)
                    {
                        throw new FormatException($"Invalid color data format at line {linesProcessed}. Expected 3 integer color components at the end of the line.");
                    }

                    if (!float.TryParse(match.Groups[1].Value, out var x) ||
                        !float.TryParse(match.Groups[2].Value, out var y) ||
                        !float.TryParse(match.Groups[3].Value, out var z))
                    {
                        throw new FormatException($"Failed to parse color components at line {linesProcessed}. Values must be valid numbers.");
                    }

                    var newColor = new ColorObject(
                        new Vector3(
                            x,
                            y,
                            z
                        ),
                        _colorFormat
                    );
                    colors.Add(newColor);
                    colorsAdded++;
                }

                if (colors.Count == 0)
                {
                    throw new InvalidOperationException("No valid colors were loaded from the file.");
                }

                Debug.Log($"Color data loading successful. Processed {linesProcessed} lines, added {colorsAdded} colors.");

                return colors;
            }
        }
    }
}