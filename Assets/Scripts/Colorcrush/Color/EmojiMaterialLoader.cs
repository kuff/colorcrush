using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Colorcrush.Color
{
    public class EmojiMaterialLoader : MonoBehaviour
    {
        [SerializeField]
        private string materialsFolder = "GeneratedMaterials"; // Folder name within Resources
        private List<Material> emojiMaterialsList;

        void Start()
        {
            LoadEmojiMaterials();
        }

        void LoadEmojiMaterials()
        {
            // Load all materials from the specified folder
            Material[] allMaterials = Resources.LoadAll<Material>(materialsFolder);
        
            // Filter and sort materials with names starting with "EmojiMaterial_"
            emojiMaterialsList = allMaterials
                .Where(material => material.name.StartsWith("EmojiMaterial_"))
                .OrderBy(material => material.name)
                .ToList();
        
            // Optional: Print the names of loaded materials to the console
            foreach (var material in emojiMaterialsList)
            {
                Debug.Log("Loaded emoji material: " + material.name);
            }
        }

        public ref List<Material> GetEmojiMaterials()
        {
            return ref emojiMaterialsList;
        }
    }
}