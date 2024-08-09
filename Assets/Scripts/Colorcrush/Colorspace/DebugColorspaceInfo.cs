// Copyright (C) 2024 Peter Guld Leth

#region

using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

#endregion

namespace Colorcrush.Colorspace
{
    public class DebugColorspaceInfo : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI infoText;

        private void Update()
        {
            if (infoText != null)
            {
                UpdateColorspaceInfo();
            }
        }

        private void UpdateColorspaceInfo()
        {
            var info = $"Color Space: {QualitySettings.activeColorSpace}\n";
            info += $"Desired Color Space: {QualitySettings.desiredColorSpace}\n";
            info += $"Quality Level: {QualitySettings.GetQualityLevel()}\n";
            info += $"HDR Enabled: {QualitySettings.vSyncCount > 0}\n";
            info += $"Render Pipeline: {GetRenderPipelineInfo()}\n";

            infoText.text = info;
        }

        private string GetRenderPipelineInfo()
        {
            if (GraphicsSettings.renderPipelineAsset == null)
            {
                return "Built-in Render Pipeline";
            }

            return GraphicsSettings.renderPipelineAsset.GetType().Name;
        }
    }
}