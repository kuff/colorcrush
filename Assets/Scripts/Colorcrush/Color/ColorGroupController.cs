using UnityEngine;
using UnityEngine.UI;

namespace Colorcrush.Color
{
    public class ColorGroupController : MonoBehaviour
    {
        public Material material;
        public ColorGroupingData colorGroupingData;
        public Sprite targetSprite;
        public Text progressTextBox;
        
        private Texture2D visibilityTexture;
        private int currentGroupCount = 0;

        void Start()
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
            if (currentGroupCount < colorGroupingData.colorGroups.Count)
            {
                currentGroupCount++;
                UpdateVisibilityTexture();
                UpdateShaderProperties();
            }
            
            Debug.Log($"Showing {currentGroupCount} color groups");
        }
        
        public int GetPercentComplete()
        {
            return Mathf.RoundToInt((float)currentGroupCount / colorGroupingData.colorGroups.Count * 100);
        }
        
        public void SetPercentComplete()
        {
            var percent = GetPercentComplete();
            
            if (progressTextBox != null)
            {
                progressTextBox.text = $"Your vision:\n{percent}% complete";
            }
            else
            {
                Debug.LogWarning("Progress text box not set");
            }
        }

        void InitializeVisibilityTexture()
        {
            Texture2D spriteTexture = targetSprite.texture;
            visibilityTexture = new Texture2D(spriteTexture.width, spriteTexture.height, TextureFormat.RFloat, false);

            UnityEngine.Color[] pixels = new UnityEngine.Color[spriteTexture.width * spriteTexture.height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = UnityEngine.Color.black; // Initialize all pixels as invisible
            }

            visibilityTexture.SetPixels(pixels);
            visibilityTexture.Apply();

            material.SetTexture("_VisiblePixels", visibilityTexture);
        }

        void UpdateVisibilityTexture()
        {
            foreach (var colorGroup in colorGroupingData.colorGroups.GetRange(0, currentGroupCount))
            {
                foreach (var pixel in colorGroup.pixels)
                {
                    visibilityTexture.SetPixel((int)pixel.x, (int)pixel.y, UnityEngine.Color.white); // Mark as visible
                }
            }

            visibilityTexture.Apply();
        }

        void UpdateShaderProperties()
        {
            material.SetInt("_NumAllowedColors", currentGroupCount); // Optional, if needed for some logic in shader
        }
    }
}