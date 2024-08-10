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
            var info = $"Desired Color Space: {QualitySettings.desiredColorSpace}\n";
            info += $"Actual Color Space: {QualitySettings.activeColorSpace}\n";
            info += $"Quality Level: {QualitySettings.GetQualityLevel()}\n";
            info += $"HDR Enabled: {QualitySettings.vSyncCount > 0}\n";

            if (GraphicsSettings.renderPipelineAsset == null)
            {
                info += "Render Pipeline: Built-in Render Pipeline";
            }
            else
            {
                var renderPipelineAsset = GraphicsSettings.renderPipelineAsset;
                info += $"Render Pipeline: {renderPipelineAsset.name}";
            }

            infoText.text = info;
        }
    }
}