using UnityEngine;

namespace Colorcrush.Color
{
    public class ColorGroupController : MonoBehaviour
    {
        public Material material;
        public ColorGroupingData colorGroupingData;
        public Sprite targetSprite;

        private Texture2D visibilityTexture;
        private int currentGroupCount = 0;

        void Start()
        {
            // Initialize the visibility texture
            InitializeVisibilityTexture();
            UpdateShaderProperties();
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