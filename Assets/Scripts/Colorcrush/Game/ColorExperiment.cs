// Copyright (C) 2024 Peter Guld Leth

#region

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

// ReSharper disable UnusedType.Global

#endregion

namespace Colorcrush.Game
{
    public static partial class ColorManager
    {
        public class ColorMatrixResult
        {
            public ColorMatrixResult(List<Vector3> axisEncodings, List<ColorObject> finalColors)
            {
                AxisEncodings = axisEncodings;
                FinalColors = finalColors;
            }

            public List<Vector3> AxisEncodings { get; }
            public List<ColorObject> FinalColors { get; }
        }

        public abstract class ColorExperiment // TODO: Add more experiments by inheriting from this class
        {
            protected readonly ColorObject BaseColor;
            protected int CurrentIndex;

            protected ColorExperiment(ColorObject baseColor)
            {
                BaseColor = baseColor;
                CurrentIndex = 0;
            }

            public abstract (List<ColorObject> nextColorBatch, bool hasMore) GetNextColorBatch(List<ColorObject> selectedColors, List<ColorObject> unselectedColors);
            public abstract int GetTotalBatches();
            public abstract ColorMatrixResult GetResultingColors();
        }

        public class ColorExperiment9X8Stage1Polynomic : ColorExperiment // TODO: Use this as an example for how to create new experiments
        {
            private const float StartExpansion = 0.0005f;
            private const float CircleExpansion = 0.0013f;
            private const int BatchSize = 12;
            private const int NCircles = 9;
            private const int NDirections = 8;
            private const float PolynomicFactor = 1.1f;
            private const int ValidationSamples = 12;
            private readonly List<ColorObject> _allSelectedColors = new();
            private readonly List<ColorObject> _allUnselectedColors = new();
            private readonly int _totalBatches;
            private readonly List<ColorObject> _xyYCoordinates;

            public ColorExperiment9X8Stage1Polynomic(ColorObject baseColor)
                : base(baseColor)
            {
                _xyYCoordinates = CreateCoordinates(baseColor); // In this case the coordinates are created in the beginning and we therefore don't need to use the selected and unselected color list inputs
                _totalBatches = Mathf.CeilToInt((float)_xyYCoordinates.Count / BatchSize);
            }

            public override (List<ColorObject> nextColorBatch, bool hasMore) GetNextColorBatch(List<ColorObject> selectedColors, List<ColorObject> unselectedColors)
            {
                if (selectedColors != null)
                {
                    _allSelectedColors.AddRange(selectedColors);
                }

                if (unselectedColors != null)
                {
                    _allUnselectedColors.AddRange(unselectedColors);
                }

                var nextBatch = new List<ColorObject>();

                // If we've already gone through all coordinates, return empty list and false
                if (CurrentIndex >= _xyYCoordinates.Count)
                {
                    return (nextBatch, false);
                }

                // Add colors to next batch
                for (var i = 0; i < BatchSize && CurrentIndex < _xyYCoordinates.Count; i++, CurrentIndex++)
                {
                    nextBatch.Add(_xyYCoordinates[CurrentIndex]);
                }

                // Always return true until we've actually returned the last batch
                return (nextBatch, true);
            }

            public override int GetTotalBatches()
            {
                return _totalBatches;
            }

            public override ColorMatrixResult GetResultingColors()
            {
                return GetResultingColors(_allSelectedColors, _allUnselectedColors);
            }

            private ColorMatrixResult GetResultingColors(List<ColorObject> selectedColors, List<ColorObject> unselectedColors)
            {
                var directions = new List<List<(Vector3, bool)>>
                {
                    new(), // direction0
                    new(), // direction1
                    new(), // direction2
                    new(), // direction3
                    new(), // direction4
                    new(), // direction5
                    new(), // direction6
                    new(), // direction7
                };
                var baseColorVector = BaseColor.ToColorFormat(ColorFormat.Xyy).Vector;

                // Filter out the base color from selected and unselected colors
                selectedColors = selectedColors?.Where(c => !VectorsEqual(c.ToColorFormat(ColorFormat.Xyy).Vector, baseColorVector)).ToList() ?? new List<ColorObject>();
                unselectedColors = unselectedColors?.Where(c => !VectorsEqual(c.ToColorFormat(ColorFormat.Xyy).Vector, baseColorVector)).ToList() ?? new List<ColorObject>();

                AddToDirectionList(selectedColors, true);
                AddToDirectionList(unselectedColors, false);

                var finalColors = new List<ColorObject>();
                var axis = new List<Vector3>();

                for (var i = 0; i < directions.Count; i++)
                {
                    var finalPoint = CalculateFinalPointForDirection(directions[i], baseColorVector);
                    var directionVector = finalPoint - baseColorVector;
                    var maxDistance = CalculateMaxDistanceForDirection(i, baseColorVector);
                    var normalizedDistance = directionVector.magnitude / maxDistance;
                    axis.Add(directionVector.normalized * normalizedDistance);
                    finalColors.Add(new ColorObject(finalPoint, ColorFormat.Xyy));
                }

                return new ColorMatrixResult(axis, finalColors);

                void AddToDirectionList(List<ColorObject> colors, bool wasSelected)
                {
                    foreach (var colorObject in colors)
                    {
                        var directionIndex = colorObject.DirectionIndex;
                        directions[directionIndex].Add((colorObject.Vector, wasSelected));
                    }
                }

                bool VectorsEqual(Vector3 a, Vector3 b)
                {
                    const float epsilon = 0.0001f;
                    return Vector3.Distance(a, b) < epsilon;
                }
            }

            private Vector3 CalculateFinalPointForDirection(List<(Vector3, bool)> direction, Vector3 baseColor)
            {
                var ordered = direction.OrderBy(item => Vector3.Distance(item.Item1, baseColor)).ToList();

                if (!ordered.Any())
                {
                    throw new InvalidOperationException("Direction list is empty");
                }

                var startPoint = Vector3.zero;
                var endPoint = Vector3.zero;

                foreach (var (point, isSelected) in ordered)
                {
                    if (isSelected && startPoint == Vector3.zero)
                    {
                        startPoint = point;
                    }
                    else if (!isSelected)
                    {
                        endPoint = point;
                    }
                }

                if (startPoint == Vector3.zero)
                {
                    return ordered.Last().Item1;
                }

                if (endPoint == Vector3.zero)
                {
                    return ordered.First().Item1;
                }

                return Vector3.Lerp(startPoint, endPoint, 0.5f);
            }

            private float CalculateMaxDistanceForDirection(int directionIndex, Vector3 baseColor)
            {
                var expansion = StartExpansion + CircleExpansion * Mathf.Pow(NCircles - 1, PolynomicFactor);

                var x = Mathf.Cos(2 * Mathf.PI / NDirections * directionIndex) * expansion + baseColor.x;
                var y = Mathf.Sin(2 * Mathf.PI / NDirections * directionIndex) * expansion + baseColor.y;
                var maxPoint = new Vector3(x, y, baseColor.z);

                return Vector3.Distance(baseColor, maxPoint);
            }

            private List<ColorObject> CreateCoordinates(ColorObject centerColor)
            {
                // Convert center color to XYY color space coordinates
                var xyYCoordinates = new List<ColorObject>();
                var centerCoordinateXyy = centerColor.ToColorFormat(ColorFormat.Xyy);

                // Create validation samples - copies of the center color used to check participant consistency
                var validationSamples = new List<ColorObject>();
                for (var i = 0; i < ValidationSamples; i++)
                {
                    validationSamples.Add(new ColorObject(centerCoordinateXyy.Vector, ColorFormat.Xyy));
                }

                // Store colors with their distances from center for later sorting
                var colorsByDistance = new List<(ColorObject color, float distance)>();

                // Create a circular pattern of color coordinates around the center
                // NDirections controls number of "spokes", NCircles controls number of points along each spoke
                for (var direction = 0; direction < NDirections; direction++)
                {
                    for (var circle = 0; circle < NCircles; circle++)
                    {
                        // Calculate expansion distance using polynomial growth for non-linear spacing
                        // Higher circles will be spaced further apart than lower ones
                        var expansion = StartExpansion + CircleExpansion * Mathf.Pow(circle, PolynomicFactor);

                        // Convert polar coordinates to XY coordinates and offset by center position
                        var x = Mathf.Cos(2 * Mathf.PI / NDirections * direction) * expansion + centerCoordinateXyy.Vector.x;
                        var y = Mathf.Sin(2 * Mathf.PI / NDirections * direction) * expansion + centerCoordinateXyy.Vector.y;
                        var currentCoordinate = new Vector3(x, y, centerCoordinateXyy.Vector.z);

                        var color = new ColorObject(currentCoordinate, ColorFormat.Xyy, direction);
                        var distance = Vector3.Distance(currentCoordinate, centerCoordinateXyy.Vector);
                        colorsByDistance.Add((color, distance));
                    }
                }

                // Sort colors by distance from center, farthest first
                colorsByDistance.Sort((a, b) => b.distance.CompareTo(a.distance));

                // Create deterministic but seemingly random seed based on center color
                // This ensures consistent randomization for the same color but variation between trials
                var seedVector = centerCoordinateXyy.Vector * 1000;
                var colorSeed = (int)(seedVector.x + seedVector.y + seedVector.z);
                var random = new Random(colorSeed);
                var totalColors = colorsByDistance.Count;

                var sortedColors = colorsByDistance.Select(x => x.color).ToList();

                // Organize colors into batches, with one validation sample per batch
                // BatchSize - 1 regular colors + 1 validation sample = BatchSize total per batch
                var completeBatches = totalColors / (BatchSize - 1);
                var remainingColors = totalColors % (BatchSize - 1);
                var validationSamplesUsed = 0;

                // Create complete batches with validation samples randomly inserted
                for (var i = 0; i < completeBatches; i++)
                {
                    var start = i * (BatchSize - 1);
                    // Randomize order of colors within each batch
                    var batchColors = sortedColors.GetRange(start, BatchSize - 1).OrderBy(x => random.Next()).ToList();

                    // Insert validation sample at random position in batch
                    var insertPosition = random.Next(batchColors.Count + 1);
                    batchColors.Insert(insertPosition, validationSamples[validationSamplesUsed++]);

                    xyYCoordinates.AddRange(batchColors);
                }

                // Handle remaining colors and unused validation samples in final batch
                if (remainingColors > 0 || validationSamplesUsed < validationSamples.Count)
                {
                    var finalBatchColors = new List<ColorObject>();
                    
                    if (remainingColors > 0)
                    {
                        var start = completeBatches * (BatchSize - 1);
                        finalBatchColors.AddRange(sortedColors.GetRange(start, remainingColors));
                    }

                    // Insert any remaining validation samples at random positions
                    while (validationSamplesUsed < validationSamples.Count)
                    {
                        var insertPosition = random.Next(finalBatchColors.Count + 1);
                        finalBatchColors.Insert(insertPosition, validationSamples[validationSamplesUsed++]);
                    }

                    xyYCoordinates.AddRange(finalBatchColors);
                }

                // Verify we have the expected number of colors (NCircles * NDirections regular colors + ValidationSamples)
                var expectedColorCount = NCircles * NDirections + ValidationSamples; // 9 * 8 + 12 = 84
                if (xyYCoordinates.Count != expectedColorCount)
                {
                    throw new InvalidOperationException(
                        $"Expected {expectedColorCount} colors but got {xyYCoordinates.Count} colors");
                }

                return xyYCoordinates;
            }
        }
    }
}