// Copyright (C) 2024 Peter Guld Leth

#region

using UnityEngine;
using UnityEngine.UI;

#endregion

namespace Colorcrush.Game
{
    public class ColorGroupController : MonoBehaviour
    {
        public Material material;
        public ColorGroupingData colorGroupingData;
        public Sprite targetSprite;
        public Text progressTextBox;
        private int _currentGroupCount;

        private Texture2D _visibilityTexture;

        private void Start()
        {
            // Initialize the visibility texture
            InitializeVisibilityTexture();
            UpdateShaderProperties();

            // Print the size of each color group
            foreach (var colorGroup in colorGroupingData.colorGroups)
            {
                Debug.Log($"Color: {colorGroup.color}, Pixels: {colorGroup.pixels.Count}");
            }
        }

        public void ShowNextColorGroup()
        {
            if (_currentGroupCount < colorGroupingData.colorGroups.Count)
            {
                _currentGroupCount++;
                UpdateVisibilityTexture();
                UpdateShaderProperties();
            }

            Debug.Log($"Showing {_currentGroupCount} color groups");
        }

        public int GetPercentComplete()
        {
            return Mathf.RoundToInt((float)_currentGroupCount / colorGroupingData.colorGroups.Count * 100);
        }

        public void SetPercentComplete()
        {
            var percent = GetPercentComplete();

            if (progressTextBox != null)
            {
                progressTextBox.text = $"YOUR VISION:\n{percent}% COMPLETE";
            }
            else
            {
                Debug.LogWarning("Progress text box not set");
            }
        }

        private void InitializeVisibilityTexture()
        {
            var spriteTexture = targetSprite.texture;
            _visibilityTexture = new Texture2D(spriteTexture.width, spriteTexture.height, TextureFormat.RFloat, false);

            var pixels = new Color[spriteTexture.width * spriteTexture.height];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.black; // Initialize all pixels as invisible
            }

            _visibilityTexture.SetPixels(pixels);
            _visibilityTexture.Apply();

            material.SetTexture("_VisiblePixels", _visibilityTexture);
        }

        private void UpdateVisibilityTexture()
        {
            foreach (var colorGroup in colorGroupingData.colorGroups.GetRange(0, _currentGroupCount))
            foreach (var pixel in colorGroup.pixels)
            {
                _visibilityTexture.SetPixel((int)pixel.x, (int)pixel.y, Color.white); // Mark as visible
            }

            _visibilityTexture.Apply();
        }

        private void UpdateShaderProperties()
        {
            material.SetInt("_NumAllowedColors", _currentGroupCount); // Optional, if needed for some logic in shader
        }
    }
}