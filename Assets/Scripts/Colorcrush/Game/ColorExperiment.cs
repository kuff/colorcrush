// Copyright (C) 2024 Peter Guld Leth

#region

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#endregion

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
            private const float _startExpansion = 0.0005f; // 0.0005f
            private const float _circleExpansion = 0.0013f; // 0.0013f
            private const int _batchSize = 12;
            private const int _nCircles = 6;
            private const int _nDirections = 8;
            private readonly List<ColorObject> _allSelectedColors = new();
            private readonly List<ColorObject> _allUnselectedColors = new();
            private readonly int _totalBatches;
            private readonly List<ColorObject> _xyYCoordinates;

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
                var sortedXyYCoordinates = _xyYCoordinates.OrderBy(c => c.ToColorFormat(ColorFormat.XYY).Vector.magnitude).ToList();
                var combinedColors = selectedColors.Concat(unselectedColors)
                    .OrderBy(c => c.ToColorFormat(ColorFormat.XYY).Vector.magnitude)
                    .ToList();

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
                var baseColorVector = _baseColor.ToColorFormat(ColorFormat.XYY).Vector;

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
                    finalColors.Add(new ColorObject(finalPoint, ColorFormat.XYY));
                }

                return new ColorMatrixResult(axis, finalColors);

                void AddToDirectionList(List<ColorObject> colors, bool wasSelected)
                {
                    foreach (var colorObject in colors)
                    {
                        //var directionIndex = GetDirectionIndex(baseColorVector, colorObject.ToColorFormat(ColorFormat.XYY).Vector);
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
                    Debug.LogWarning("Direction list is empty");
                    return baseColor;
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
                const float maxExpansion = _nCircles * _circleExpansion + _startExpansion;

                var x = Mathf.Cos(2 * Mathf.PI / _nDirections * directionIndex) * maxExpansion + baseColor.x;
                var y = Mathf.Sin(2 * Mathf.PI / _nDirections * directionIndex) * maxExpansion + baseColor.y;
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
                        var x = Mathf.Cos(2 * Mathf.PI / _nDirections * direction) * (circle * _circleExpansion + _startExpansion) + centerCoordinatexyY.Vector.x;
                        var y = Mathf.Sin(2 * Mathf.PI / _nDirections * direction) * (circle * _circleExpansion + _startExpansion) + centerCoordinatexyY.Vector.y;
                        var currentCoordinate = new Vector3(x, y, centerCoordinatexyY.Vector.z);

                        xyYCoordinates.Add(new ColorObject(currentCoordinate, ColorFormat.XYY, direction));
                    }
                }

                return xyYCoordinates;
            }
        }

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
    }
}