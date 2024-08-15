// Copyright (C) 2024 Peter Guld Leth

#region

using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

#endregion

namespace Editor
{
    public class GenerateMaterials : EditorWindow
    {
        private string _fileNamePrefix = "";
        private const string MaterialPath = "Assets/Resources/GeneratedMaterials";

        private void OnGUI()
        {
            GUILayout.Label("Generate Materials for Images", EditorStyles.boldLabel);
            _fileNamePrefix = EditorGUILayout.TextField("File Name Prefix", _fileNamePrefix);

            if (GUILayout.Button("Generate Materials"))
            {
                GenerateMaterialsForImages();
            }
        }

        [MenuItem("Colorcrush/Generate Transpose Materials for Images In Selection", false, 10)]
        private static void ShowWindow()
        {
            GetWindow<GenerateMaterials>("Generate Materials");
        }

        private void GenerateMaterialsForImages()
        {
            var selectedObjects = Selection.gameObjects;

            foreach (var obj in selectedObjects)
            {
                var images = obj.GetComponentsInChildren<Image>(true);

                foreach (var image in images)
                {
                    CreateAndAssignMaterial(image);
                }
            }
        }

        private void CreateAndAssignMaterial(Image image)
        {
            var uniqueIdentifier = Guid.NewGuid().ToString().Substring(0, 8);
            var materialName = $"{_fileNamePrefix}{image.gameObject.name}_{uniqueIdentifier}_Material";

            // Create the directory if it doesn't exist
            if (!Directory.Exists(MaterialPath))
            {
                Directory.CreateDirectory(MaterialPath);
            }

            var fullPath = $"{MaterialPath}/{materialName}.mat";

            // Create a new material
            var material = new Material(Shader.Find("Colorcrush/ColorTransposeShader"));
            AssetDatabase.CreateAsset(material, fullPath);

            // Assign the material to the Image component
            image.material = material;

            Debug.Log($"Created and assigned material: {fullPath}");
        }
    }
}