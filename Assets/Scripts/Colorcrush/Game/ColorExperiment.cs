// Copyright (C) 2024 Peter Guld Leth

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Colorcrush.Game
{
    public static partial class ColorManager
    {
        public abstract class ColorExperiment
        {
            protected readonly ColorObject _baseColor;
            protected int _currentIndex;

            protected ColorExperiment(ColorObject baseColor)
            {
                _baseColor = baseColor;
                _currentIndex = 0;
            }

            public abstract (List<ColorObject> nextColorBatch, bool hasMore) GetNextColorVariantBatch();
            public abstract (List<ColorObject> nextColorBatch, bool hasMore) GetNextColorVariantBatch(List<ColorObject> selectedColors, List<ColorObject> unselectedColors);
            public abstract int GetTotalBatches();
            public abstract ColorMatrixResult GetFinalColors();
            public abstract ColorMatrixResult GetFinalColors(List<ColorObject> selectedColors, List<ColorObject> unselectedColors);
        }

        public class ColorExperiment8x6Stage1Solo : ColorExperiment
        {
            private readonly List<ColorObject> _xyYCoordinates;
            private readonly int _totalBatches;
            private readonly int _batchSize = 8;
            private readonly List<ColorObject> _allSelectedColors = new List<ColorObject>();
            private readonly List<ColorObject> _allUnselectedColors = new List<ColorObject>();

            // Experiment configuration
            private readonly int _nDirections = 8;
            private readonly int _nCircles = 6;
            private readonly float _startExpansion = 0.0005f;
            private readonly float _circleExpansion = 0.0013f;

            public ColorExperiment8x6Stage1Solo(ColorObject baseColor)
                : base(baseColor)
            {
                _xyYCoordinates = CreateCoordinates(baseColor);
                _totalBatches = Mathf.CeilToInt((float)_xyYCoordinates.Count / _batchSize);
            }

            public override (List<ColorObject> nextColorBatch, bool hasMore) GetNextColorVariantBatch()
            {
                var nextBatch = new List<ColorObject>();

                for (var i = 0; i < _batchSize && _currentIndex < _xyYCoordinates.Count; i++, _currentIndex++)
                {
                    nextBatch.Add(_xyYCoordinates[_currentIndex]);
                }

                var hasMore = _currentIndex < _xyYCoordinates.Count;
                return (nextBatch, hasMore);
            }

            public override (List<ColorObject> nextColorBatch, bool hasMore) GetNextColorVariantBatch(List<ColorObject> selectedColors, List<ColorObject> unselectedColors)
            {
                _allSelectedColors.AddRange(selectedColors);
                _allUnselectedColors.AddRange(unselectedColors);

                var nextBatch = new List<ColorObject>();

                for (var i = 0; i < _batchSize && _currentIndex < _xyYCoordinates.Count; i++, _currentIndex++)
                {
                    nextBatch.Add(_xyYCoordinates[_currentIndex]);
                }

                var hasMore = _currentIndex < _xyYCoordinates.Count;
                return (nextBatch, hasMore);
            }

            public override int GetTotalBatches()
            {
                return _totalBatches;
            }

            public override ColorMatrixResult GetFinalColors()
            {
                return GetFinalColors(_allSelectedColors, _allUnselectedColors);
            }

            public override ColorMatrixResult GetFinalColors(List<ColorObject> selectedColors, List<ColorObject> unselectedColors)
            {
                // Verify that selected and unselected colors combined match _xyYCoordinates
                /*var allSubmittedColors = new HashSet<Vector3>();
                foreach (var color in selectedColors.Concat(unselectedColors))
                {
                    allSubmittedColors.Add(color.ToColorFormat(ColorFormat.XYY).Vector);
                }

                var allGeneratedColors = new HashSet<Vector3>();
                foreach (var color in _xyYCoordinates)
                {
                    allGeneratedColors.Add(color.Vector);
                }

                // Sort both sets for easier comparison in debug view
                var sortedSubmitted = allSubmittedColors.OrderBy(v => v.x).ThenBy(v => v.y).ThenBy(v => v.z).ToHashSet();
                var sortedGenerated = allGeneratedColors.OrderBy(v => v.x).ThenBy(v => v.y).ThenBy(v => v.z).ToHashSet();
                allSubmittedColors = sortedSubmitted;
                allGeneratedColors = sortedGenerated;

                Debug.Assert(allSubmittedColors.SetEquals(allGeneratedColors), 
                    $"Selected and unselected colors combined do not match the originally generated colors: {allSubmittedColors.Count} vs. {allGeneratedColors.Count}");*/

                var directions = new List<List<(Vector3, bool)>>()
                {
                    new List<(Vector3, bool)>(), // direction0
                    new List<(Vector3, bool)>(), // direction1
                    new List<(Vector3, bool)>(), // direction2
                    new List<(Vector3, bool)>(), // direction3
                    new List<(Vector3, bool)>(), // direction4
                    new List<(Vector3, bool)>(), // direction5
                    new List<(Vector3, bool)>(), // direction6
                    new List<(Vector3, bool)>(), // direction7
                };
                var baseColorVector = _baseColor.ToColorFormat(ColorFormat.XYY).Vector;

                void AddToDirectionList(List<ColorObject> colors, bool isSelected)
                {
                    foreach (var colorObject in colors)
                    {
                        var directionIndex = GetDirectionIndex(baseColorVector, colorObject.ToColorFormat(ColorFormat.XYY).Vector);
                        directions[directionIndex].Add((colorObject.Vector, isSelected));
                    }
                }

                AddToDirectionList(selectedColors, true);
                AddToDirectionList(unselectedColors, false);

                var finalColors = new List<ColorObject>();
                var axis = new List<Vector3>();

                for (int i = 0; i < directions.Count; i++)
                {
                    var finalPoint = CalculateFinalPointForDirection(directions[i], baseColorVector);
                    var directionVector = finalPoint - baseColorVector;
                    var maxDistance = CalculateMaxDistanceForDirection(i, baseColorVector);
                    var normalizedDistance = directionVector.magnitude / maxDistance;
                    axis.Add(directionVector.normalized * normalizedDistance);
                    finalColors.Add(new ColorObject(finalPoint, ColorFormat.XYY));
                }

                return new ColorMatrixResult(axis, finalColors);
            }

            private int GetDirectionIndex(Vector3 baseColor, Vector3 currentColor)
            {
                var directionVector = currentColor - baseColor;
                var angle = Mathf.Atan2(directionVector.y, directionVector.x) * Mathf.Rad2Deg;
                if (angle < 0) angle += 360;

                return Mathf.FloorToInt(angle / 45) % _nDirections;
            }

            private Vector3 CalculateFinalPointForDirection(List<(Vector3, bool)> direction, Vector3 baseColor)
            {
                var ordered = direction.OrderBy(item => Vector3.Distance(item.Item1, baseColor)).ToList();

                if (!ordered.Any())
                {
                    Debug.LogWarning("Direction list is empty");
                    return baseColor;
                }

                Vector3 startPoint = Vector3.zero;
                Vector3 endPoint = Vector3.zero;

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
                var maxExpansion = _nCircles * _circleExpansion + _startExpansion;

                var x = Mathf.Cos((2 * Mathf.PI / _nDirections) * directionIndex) * maxExpansion + baseColor.x;
                var y = Mathf.Sin((2 * Mathf.PI / _nDirections) * directionIndex) * maxExpansion + baseColor.y;
                var maxPoint = new Vector3(x, y, baseColor.z);

                return Vector3.Distance(baseColor, maxPoint);
            }

            private List<ColorObject> CreateCoordinates(ColorObject centerColor)
            {
                var xyYCoordinates = new List<ColorObject>();
                var centerCoordinatexyY = centerColor.ToColorFormat(ColorFormat.XYY);

                for (var direction = 0; direction < _nDirections; direction++)
                {
                    for (var circle = 0; circle < _nCircles; circle++)
                    {
                        var x = Mathf.Cos((2 * Mathf.PI / _nDirections) * direction) * (circle * _circleExpansion + _startExpansion) + centerCoordinatexyY.Vector.x;
                        var y = Mathf.Sin((2 * Mathf.PI / _nDirections) * direction) * (circle * _circleExpansion + _startExpansion) + centerCoordinatexyY.Vector.y;
                        var currentCoordinate = new Vector3(x, y, centerCoordinatexyY.Vector.z);

                        xyYCoordinates.Add(new ColorObject(currentCoordinate, ColorFormat.XYY));
                    }
                }

                return xyYCoordinates;
            }
        }

        public class ColorMatrixResult
        {
            public List<Vector3> AxisEncodings { get; }
            public List<ColorObject> FinalColors { get; }

            public ColorMatrixResult(List<Vector3> axisEncodings, List<ColorObject> finalColors)
            {
                AxisEncodings = axisEncodings;
                FinalColors = finalColors;
            }
        }
    }
}