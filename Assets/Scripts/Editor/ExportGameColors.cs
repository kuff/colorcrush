// Copyright (C) 2025 Peter Guld Leth

#region

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using Colorcrush;
using Colorcrush.Game;
using UnityEditor;
using Debug = UnityEngine.Debug;

#endregion

namespace Editor
{
    public class ExportGameColors : EditorWindow
    {
        [MenuItem("Colorcrush/Export experiment colors")]
        public static void ExportColorExperimentData()
        {
            // Get the experiment name from the config
            var experimentName = ProjectConfig.InstanceConfig.colorExperimentName;
            if (string.IsNullOrEmpty(experimentName))
            {
                EditorUtility.DisplayDialog("Error", "No color experiment is configured. Please set a colorExperimentName in the project configuration.", "OK");
                return;
            }

            // Ask user for file path
            var filePath = EditorUtility.SaveFilePanel(
                "Save experiment colors",
                EditorPrefs.GetString("LastExportPath", Environment.GetFolderPath(Environment.SpecialFolder.Desktop)),
                $"ColorExperiment_{experimentName}_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                "csv");

            if (string.IsNullOrEmpty(filePath))
            {
                // User cancelled the dialog
                return;
            }

            EditorPrefs.SetString("LastExportPath", Path.GetDirectoryName(filePath));

            try
            {
                ExportColorData(filePath, experimentName);
                EditorUtility.DisplayDialog("Export Complete", $"Experiment colors successfully exported to:\n{filePath}", "OK");

                // Open file in default application
                if (EditorUtility.DisplayDialog("Open File?", "Would you like to open the exported file?", "Yes", "No"))
                {
                    Process.Start(filePath);
                }
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Export Error", $"An error occurred during export:\n{ex.Message}", "OK");
                Debug.LogError($"Error exporting experiment colors: {ex}");
            }
        }

        private static void ExportColorData(string filePath, string experimentName)
        {
            var csvContent = new StringBuilder();

            // Use invariant culture to ensure consistent decimal formatting with periods
            var invariantCulture = CultureInfo.InvariantCulture;

            // Add CSV header with RGB255 indicator
            csvContent.AppendLine("BaseColorIndex,BaseColor_DisplayP3_R255,BaseColor_DisplayP3_G255,BaseColor_DisplayP3_B255," +
                                  "BaseColor_xyY_x,BaseColor_xyY_y,BaseColor_xyY_Y," +
                                  "BatchNumber,PositionInBatch," +
                                  "Color_DisplayP3_R255,Color_DisplayP3_G255,Color_DisplayP3_B255," +
                                  "Color_xyY_x,Color_xyY_y,Color_xyY_Y," +
                                  "DirectionIndex");

            // Load all target colors
            var targetColors = ColorManager.TargetColors;

            // For each target color
            for (var colorIndex = 0; colorIndex < targetColors.Length; colorIndex++)
            {
                // Create color object for the base color
                var baseColor = new ColorManager.ColorObject(targetColors[colorIndex]);
                var baseColorDisplayP3 = baseColor.ToColorFormat(ColorManager.ColorFormat.DisplayP3ZeroToOne);
                var baseColorXyY = baseColor.ToColorFormat(ColorManager.ColorFormat.Xyy);

                // Begin the color experiment
                var experiment = ColorManager.BeginColorExperiment(baseColor);
                var totalBatches = experiment.GetTotalBatches();

                // For each batch
                for (var batchNumber = 0; batchNumber < totalBatches; batchNumber++)
                {
                    // Get the next batch of colors
                    var (colorBatch, hasMore) = experiment.GetNextColorBatch(null, null);

                    // For each color in the batch
                    for (var position = 0; position < colorBatch.Count; position++)
                    {
                        var color = colorBatch[position];
                        var colorDisplayP3 = color.ToColorFormat(ColorManager.ColorFormat.DisplayP3ZeroToOne);
                        var colorXyY = color.ToColorFormat(ColorManager.ColorFormat.Xyy);

                        // Convert 0-1 RGB values to 0-255 as integers
                        var baseColorRGB255 = baseColorDisplayP3.Vector * 255f;
                        var colorRGB255 = colorDisplayP3.Vector * 255f;

                        // Append CSV row with proper formatting using invariant culture
                        csvContent.AppendLine(
                            string.Format(invariantCulture,
                                "{0}," +
                                "{1:0},{2:0},{3:0}," +
                                "{4:0.000000},{5:0.000000},{6:0.000000}," +
                                "{7},{8}," +
                                "{9:0},{10:0},{11:0}," +
                                "{12:0.000000},{13:0.000000},{14:0.000000}," +
                                "{15}",
                                colorIndex,
                                (int)baseColorRGB255.x, (int)baseColorRGB255.y, (int)baseColorRGB255.z,
                                baseColorXyY.Vector.x, baseColorXyY.Vector.y, baseColorXyY.Vector.z,
                                batchNumber + 1, position + 1,
                                (int)colorRGB255.x, (int)colorRGB255.y, (int)colorRGB255.z,
                                colorXyY.Vector.x, colorXyY.Vector.y, colorXyY.Vector.z,
                                color.DirectionIndex));
                    }

                    if (!hasMore)
                    {
                        break;
                    }
                }
            }

            // Write content to file
            File.WriteAllText(filePath, csvContent.ToString());

            Debug.Log($"Experiment colors successfully exported to {filePath}");
        }
    }
}