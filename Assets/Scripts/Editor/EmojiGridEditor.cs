using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Editor
{
    public class EmojiGridEditor : UnityEditor.Editor
    {
        [MenuItem("Colorcrush/Generate Emoji Materials", false)]
        static void GenerateEmojiMaterials()
        {
            // Get the selected game object
            GameObject selectedObject = Selection.activeGameObject;

            if (selectedObject == null)
            {
                Debug.LogError("No game object selected.");
                return;
            }

            // Check if the selected object has a GridLayoutGroup component
            GridLayoutGroup gridLayoutGroup = selectedObject.GetComponent<GridLayoutGroup>();

            if (gridLayoutGroup == null)
            {
                Debug.LogError("Selected game object does not have a GridLayoutGroup component.");
                return;
            }

            // Create a folder for the materials if it doesn't exist
            string folderPath = "Assets/GeneratedMaterials";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets", "GeneratedMaterials");
            }

            // Iterate through each child object
            int childCount = selectedObject.transform.childCount;

            for (int i = 0; i < childCount; i++)
            {
                GameObject child = selectedObject.transform.GetChild(i).gameObject;
                Image image = child.GetComponent<Image>();

                if (image == null)
                {
                    Debug.LogError("Child game object does not have an Image component.");
                    continue;
                }

                // Create a new material instance
                Material material = new Material(Shader.Find("Custom/ColorTransposeShader"));

                // Set the initial properties
                material.SetTexture("_MainTex", image.sprite.texture);
                material.SetColor("_SkinColor", new Color(0.97f, 0.87f, 0.25f, 1f)); // Default skin color F8DE40
                material.SetColor("_TargetColor", Random.ColorHSV()); // Random target color for testing
                material.SetFloat("_Tolerance", 0.1f);
                material.SetFloat("_WhiteTolerance", 0.1f);

                // Save the material as an asset
                string materialPath = Path.Combine(folderPath, $"EmojiMaterial_{i + 1}.mat");
                AssetDatabase.CreateAsset(material, materialPath);
                AssetDatabase.SaveAssets();

                // Assign the material to the image component
                image.material = material;

                // Mark the scene as dirty to ensure changes are saved
                EditorUtility.SetDirty(image);
            }

            // Save the scene to ensure all changes are persisted
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log("Generated and saved materials for all child objects in the grid.");
        }
    }
}
