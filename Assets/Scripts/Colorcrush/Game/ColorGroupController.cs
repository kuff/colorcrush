// Copyright (C) 2024 Peter Guld Leth

#region

using TMPro;
using UnityEngine;

#endregion

namespace Colorcrush.Game
{
    public class ColorGroupController : MonoBehaviour
    {
        public Material material;
        public ColorGroupingData colorGroupingData;
        public Sprite targetSprite;
        public TextMeshProUGUI progressTextBox;
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

            // Set initial visibility based on current target color index
            UpdateVisibilityBasedOnTargetColor();
            SetPercentComplete(); // Update percent complete on start
        }

        public void ShowNextColorGroup()
        {
            UpdateVisibilityBasedOnTargetColor();
            UpdateShaderProperties();
            SetPercentComplete(); // Update percent complete after showing next color group

            Debug.Log($"Showing {_currentGroupCount} color groups");
        }

        public int GetPercentComplete()
        {
            var totalColors = ColorArray.SRGBTargetColors.Length;
            var currentColorIndex = ColorController.GetCurrentTargetColorIndex();
            return Mathf.RoundToInt((float)currentColorIndex / totalColors * 100);
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

        private void UpdateVisibilityBasedOnTargetColor()
        {
            var targetColorIndex = ColorController.GetCurrentTargetColorIndex();
            _currentGroupCount = targetColorIndex;

            // Reset visibility texture
            var pixels = new Color[_visibilityTexture.width * _visibilityTexture.height];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.black;
            }

            _visibilityTexture.SetPixels(pixels);

            // Update visibility for all groups up to but not including the current target color
            for (var i = 0; i < _currentGroupCount; i++)
            {
                var colorGroup = colorGroupingData.colorGroups[i];
                foreach (var pixel in colorGroup.pixels)
                {
                    _visibilityTexture.SetPixel((int)pixel.x, (int)pixel.y, Color.white);
                }
            }

            _visibilityTexture.Apply();
        }

        private void UpdateShaderProperties()
        {
            material.SetInt("_NumAllowedColors", _currentGroupCount);
        }
    }
}