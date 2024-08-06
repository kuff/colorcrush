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
                var analyzer = new GameObject().AddComponent<SpriteColorAnalyzer>();
                var colorGroups = analyzer.AnalyzeSpriteColors(sprite);
                DestroyImmediate(analyzer.gameObject);

                var colorGroupingData = ScriptableObject.CreateInstance<ColorGroupingData>();
                foreach (var kvp in colorGroups)
                {
                    var colorGroup = new ColorGroupingData.ColorGroup
                    {
                        color = kvp.Key,
                        pixels = kvp.Value,
                    };
                    colorGroupingData.colorGroups.Add(colorGroup);
                }

                var path = AssetDatabase.GetAssetPath(sprite);
                var directory = Path.GetDirectoryName(path);
                var assetPath = Path.Combine(directory, $"{sprite.name}_ColorGroupingData.asset");

                // Check if the asset already exists and delete it if it does
                var existingAsset = AssetDatabase.LoadAssetAtPath<ColorGroupingData>(assetPath);
                if (existingAsset != null)
                {
                    AssetDatabase.DeleteAsset(assetPath);
                }

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