// Copyright (C) 2024 Peter Guld Leth

#region

using System.IO;
using Colorcrush.Game;
using UnityEditor;
using UnityEngine;

#endregion

namespace Editor
{
    public class ColorGroupingEditor : MonoBehaviour
    {
        [MenuItem("Assets/Create Color Grouping Data From Sprite", false, 10)]
        private static void CreateColorGroupingData()
        {
            if (Selection.activeObject is Sprite sprite)
            {
                // Create SpriteColorAnalyzer instance without instantiating a GameObject
                var analyzer = new SpriteColorAnalyzer();
                var colorGroups = analyzer.AnalyzeSpriteColors(sprite);

                var colorGroupingData = ScriptableObject.CreateInstance<ColorGroupingData>();
                colorGroupingData.colorGroups.Capacity = colorGroups.Count; // Pre-allocate capacity
                foreach (var kvp in colorGroups)
                {
                    colorGroupingData.colorGroups.Add(new ColorGroupingData.ColorGroup
                    {
                        color = kvp.Key,
                        pixels = kvp.Value,
                    });
                }

                var path = AssetDatabase.GetAssetPath(sprite);
                var directory = Path.GetDirectoryName(path);
                var assetPath = Path.Combine(directory, $"{sprite.name}_ColorGroupingData.asset");

                // Use AssetDatabase.CreateAsset directly, which will overwrite if the asset exists
                AssetDatabase.CreateAsset(colorGroupingData, assetPath);
                AssetDatabase.SaveAssets();

                Selection.activeObject = colorGroupingData;
                EditorUtility.FocusProjectWindow();
            }
            else
            {
                Debug.LogWarning("Please select a sprite to create color grouping data.");
            }
        }
    }
}