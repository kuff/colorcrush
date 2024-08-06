// Copyright (C) 2024 Peter Guld Leth

#region

using System;
using System.Collections.Generic;
using UnityEngine;

#endregion

namespace Colorcrush.Game
{
    [CreateAssetMenu(fileName = "ColorGroupingData", menuName = "Colorcrush/ColorGroupingData", order = 1)]
    public class ColorGroupingData : ScriptableObject
    {
        public List<ColorGroup> colorGroups = new();

        [Serializable]
        public class ColorGroup
        {
            public Color color;
            public List<Vector2> pixels;
        }
    }
}