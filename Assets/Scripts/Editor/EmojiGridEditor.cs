// Copyright (C) 2024 Peter Guld Leth

#region

using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#endregion

namespace Editor
{
    public class EmojiGridEditor : UnityEditor.Editor
    {
        [MenuItem("Colorcrush/Generate Emoji Materials", false)]
        private static void GenerateEmojiMaterials()
        {
            // Get the selected game object
            var selectedObject = Selection.activeGameObject;

            if (selectedObject == null)
            {
                Debug.LogError("No game object selected.");
                return;
            }

            // Check if the selected object has a GridLayoutGroup component
            var gridLayoutGroup = selectedObject.GetComponent<GridLayoutGroup>();

            if (gridLayoutGroup == null)
            {
                Debug.LogError("Selected game object does not have a GridLayoutGroup component.");
                return;
            }

            // Create a folder for the materials if it doesn't exist
            var folderPath = "Assets/Resources/GeneratedMaterials";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "GeneratedMaterials");
            }

            // Iterate through each child object
            var childCount = selectedObject.transform.childCount;

            for (var i = 0; i < childCount; i++)
            {
                var child = selectedObject.transform.GetChild(i).gameObject;
                var image = child.GetComponent<Image>();

                if (image == null)
                {
                    Debug.LogError("Child game object does not have an Image component.");
                    continue;
                }

                // Create a new material instance
                var material = new Material(Shader.Find("Custom/ColorTransposeShader"));

                // Set the initial properties
                material.SetTexture("_MainTex", image.sprite.texture);
                material.SetColor("_SkinColor", new Color(0.97f, 0.87f, 0.25f, 1f)); // Default skin color F8DE40
                material.SetColor("_TargetColor", Random.ColorHSV()); // Random target color for testing
                material.SetFloat("_Tolerance", 0.1f);
                material.SetFloat("_WhiteTolerance", 0.1f);

                // Save the material as an asset
                var materialPath = Path.Combine(folderPath, $"EmojiMaterial_{i + 1}.mat");
                AssetDatabase.CreateAsset(material, materialPath);
                AssetDatabase.SaveAssets();

                // Assign the material to the image component
                image.material = material;

                // Mark the scene as dirty to ensure changes are saved
                EditorUtility.SetDirty(image);
            }

            // Save the scene to ensure all changes are persisted
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            Debug.Log("Generated and saved materials for all child objects in the grid.");
        }
    }
}