// Copyright (C) 2024 Peter Guld Leth

#region

using System.Collections.Generic;
using System.Linq;
using Colorcrush.Util;
using UnityEngine;

#endregion

namespace Colorcrush.Game
{
    public class EmojiMaterialLoader : MonoBehaviour
    {
        private List<Material> _emojiMaterialsList;

        private void Start()
        {
            LoadEmojiMaterials();
        }

        private void LoadEmojiMaterials()
        {
            // Load all materials from the specified folder
            var allMaterials = Resources.LoadAll<Material>(ProjectConfig.InstanceConfig.emojiMaterialsFolder);

            // Filter and sort materials with names starting with "EmojiMaterial_"
            _emojiMaterialsList = allMaterials
                .Where(material => material.name.StartsWith("EmojiMaterial_"))
                .OrderBy(material => material.name)
                .ToList();

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