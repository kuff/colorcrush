using System.Collections.Generic;
using Colorcrush.Color;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class ColorGroupingEditor : MonoBehaviour
    {
        [MenuItem("Assets/Create Color Grouping Data", false, 10)]
        private static void CreateColorGroupingData()
        {
            if (Selection.activeObject is Sprite sprite)
            {
                SpriteColorAnalyzer analyzer = new GameObject().AddComponent<SpriteColorAnalyzer>();
                Dictionary<Color, List<Vector2>> colorGroups = analyzer.AnalyzeSpriteColors(sprite);
                Object.DestroyImmediate(analyzer.gameObject);

                ColorGroupingData colorGroupingData = ScriptableObject.CreateInstance<ColorGroupingData>();
                foreach (var kvp in colorGroups)
                {
                    ColorGroupingData.ColorGroup colorGroup = new ColorGroupingData.ColorGroup
                    {
                        color = kvp.Key,
                        pixels = kvp.Value
                    };
                    colorGroupingData.colorGroups.Add(colorGroup);
                }

                string path = AssetDatabase.GetAssetPath(sprite);
                string assetPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path), $"{sprite.name}_ColorGroupingData.asset");
                AssetDatabase.CreateAsset(colorGroupingData, assetPath);
                AssetDatabase.SaveAssets();

                EditorUtility.FocusProjectWindow();
                Selection.activeObject = colorGroupingData;
            }
            else
            {
                Debug.LogWarning("Please select a sprite to create color grouping data.");
            }
        }
    }
}