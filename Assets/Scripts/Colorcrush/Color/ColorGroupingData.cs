using System.Collections.Generic;
using UnityEngine;

namespace Colorcrush.Color
{
    [CreateAssetMenu(fileName = "ColorGroupingData", menuName = "Colorcrush/ColorGroupingData", order = 1)]
    public class ColorGroupingData : ScriptableObject
    {
        public List<ColorGroup> colorGroups = new List<ColorGroup>();

        [System.Serializable]
        public class ColorGroup
        {
            public UnityEngine.Color color;
            public List<Vector2> pixels;
        }
    }
}