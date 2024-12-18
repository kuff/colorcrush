﻿// Copyright (C) 2024 Peter Guld Leth

#region

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

        public class ColorExperiment8X6Stage1Solo : ColorExperiment // TODO: Use this as an example for how to create new experiments
        {
            private const float StartExpansion = 0.0005f;
            private const float CircleExpansion = 0.0013f;
            private const int BatchSize = 12;
            private const int NCircles = 6;
            private const int NDirections = 8;
            private readonly List<ColorObject> _allSelectedColors = new();
            private readonly List<ColorObject> _allUnselectedColors = new();
            private readonly int _totalBatches;
            private readonly List<ColorObject> _xyYCoordinates;

            public ColorExperiment8X6Stage1Solo(ColorObject baseColor)
                : base(baseColor)
            {
                _xyYCoordinates = CreateCoordinates(baseColor);
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
                /*var sortedXyYCoordinates = _xyYCoordinates.OrderBy(c => c.ToColorFormat(ColorFormat.XYY).Vector.magnitude).ToList();
                var combinedColors = selectedColors.Concat(unselectedColors)
                    .OrderBy(c => c.ToColorFormat(ColorFormat.XYY).Vector.magnitude)
                    .ToList();*/

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
                const float maxExpansion = NCircles * CircleExpansion + StartExpansion;

                var x = Mathf.Cos(2 * Mathf.PI / NDirections * directionIndex) * maxExpansion + baseColor.x;
                var y = Mathf.Sin(2 * Mathf.PI / NDirections * directionIndex) * maxExpansion + baseColor.y;
                var maxPoint = new Vector3(x, y, baseColor.z);

                return Vector3.Distance(baseColor, maxPoint);
            }

            private List<ColorObject> CreateCoordinates(ColorObject centerColor)
            {
                var xyYCoordinates = new List<ColorObject>();
                var centerCoordinateXyy = centerColor.ToColorFormat(ColorFormat.Xyy);

                for (var direction = 0; direction < NDirections; direction++)
                {
                    for (var circle = 0; circle < NCircles; circle++)
                    {
                        var x = Mathf.Cos(2 * Mathf.PI / NDirections * direction) * (circle * CircleExpansion + StartExpansion) + centerCoordinateXyy.Vector.x;
                        var y = Mathf.Sin(2 * Mathf.PI / NDirections * direction) * (circle * CircleExpansion + StartExpansion) + centerCoordinateXyy.Vector.y;
                        var currentCoordinate = new Vector3(x, y, centerCoordinateXyy.Vector.z);

                        xyYCoordinates.Add(new ColorObject(currentCoordinate, ColorFormat.Xyy, direction));
                    }
                }

                return xyYCoordinates;
            }
        }
    }
}