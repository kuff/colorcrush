// Copyright (C) 2024 Peter Guld Leth

#region

using System;
using System.IO;
using System.Linq;
using Colorcrush;
using Colorcrush.Util;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using SceneManager = UnityEngine.SceneManagement.SceneManager;

#endregion

namespace Editor
{
    public class GenerateMaterials : EditorWindow
    {
        private string _fileNamePrefix;

        private void OnEnable()
        {
            _fileNamePrefix = ProjectConfig.InstanceConfig.emojiMaterialPrefix;
        }

        private void OnGUI()
        {
            GUILayout.Label("Generate Materials for Images", EditorStyles.boldLabel);
            _fileNamePrefix = EditorGUILayout.TextField("File Name Prefix", _fileNamePrefix);

            if (GUILayout.Button("Generate Materials"))
            {
                GenerateMaterialsForImages();
            }
        }

        [MenuItem("Colorcrush/Generate Transpose Materials for Images In Selection", false, 1)]
        private static void ShowWindow()
        {
            GetWindow<GenerateMaterials>("Generate Materials");
        }

        private void GenerateMaterialsForImages()
        {
            var selectedObjects = Selection.gameObjects;

            if (selectedObjects.Length == 0)
            {
                Debug.LogError("No objects selected. Please select at least one object containing Image components.");
                return;
            }

            var allImages = selectedObjects.SelectMany(obj => obj.GetComponentsInChildren<Image>(true)).ToArray();

            if (allImages.Length == 0)
            {
                Debug.LogError("No Image components found in the selected objects. Please select objects containing Image components.");
                return;
            }

            foreach (var image in allImages)
            {
                CreateAndAssignMaterial(image);
                if (ProjectConfig.InstanceConfig.disableMaskingOnGenerate)
                {
                    image.maskable = false;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(image);
                    Debug.Log($"Disabled masking for image: {image.gameObject.name}");
                }
            }

            // Save the changes to the scene
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
        }

        private void CreateAndAssignMaterial(Image image)
        {
            var uniqueIdentifier = Guid.NewGuid().ToString()[..8];
            var prefix = string.IsNullOrEmpty(_fileNamePrefix) ? ProjectConfig.InstanceConfig.emojiMaterialPrefix : _fileNamePrefix;
            var materialName = $"{prefix}{image.gameObject.name}_{uniqueIdentifier}_Material";

            // Create the directory if it doesn't exist
            var generatedMaterialsPath = ProjectConfig.InstanceConfig.generatedMaterialsPath;
            if (!Directory.Exists(generatedMaterialsPath))
            {
                Directory.CreateDirectory(generatedMaterialsPath);
            }

            var fullPath = $"{generatedMaterialsPath}/{materialName}.mat";

            // Create a new material
            var material = new Material(Shader.Find("Colorcrush/ColorTransposeShader"));
            AssetDatabase.CreateAsset(material, fullPath);

            // Assign the material to the Image component
            image.material = material;

            // Ensure the change is registered in the prefab if this is a prefab instance
            PrefabUtility.RecordPrefabInstancePropertyModifications(image);

            Debug.Log($"Created and assigned material: {fullPath}");
        }
    }
}