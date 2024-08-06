// Copyright (C) 2024 Peter Guld Leth

#region

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#endregion

namespace Colorcrush.Game
{
    public class EmojiMaterialLoader : MonoBehaviour
    {
        [SerializeField] private string materialsFolder = "GeneratedMaterials"; // Folder name within Resources

        private List<Material> _emojiMaterialsList;

        private void Start()
        {
            LoadEmojiMaterials();
        }

        private void LoadEmojiMaterials()
        {
            // Load all materials from the specified folder
            var allMaterials = Resources.LoadAll<Material>(materialsFolder);

            // Filter and sort materials with names starting with "EmojiMaterial_"
            _emojiMaterialsList = allMaterials
                .Where(material => material.name.StartsWith("EmojiMaterial_"))
                .OrderBy(material => material.name)
                .ToList();

            // Optional: Print the names of loaded materials to the console
            foreach (var material in _emojiMaterialsList)
            {
                Debug.Log("Loaded emoji material: " + material.name);
            }
        }

        public ref List<Material> GetEmojiMaterials()
        {
            return ref _emojiMaterialsList;
        }
    }
}