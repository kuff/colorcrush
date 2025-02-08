// Copyright (C) 2025 Peter Guld Leth

#region

using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Colorcrush;
using Colorcrush.Game;
using UnityEditor;
using UnityEngine;

#endregion

namespace Editor
{
    [CustomEditor(typeof(ProjectConfigurationObject))]
    public class ProjectConfigurationObjectEditor : UnityEditor.Editor
    {
        // EditorPrefs keys for persisting fold states
        private const string GAME_CONFIG_KEY = "Colorcrush.ProjectConfig.GameConfig.Expanded";
        private const string EDITOR_CONFIG_KEY = "Colorcrush.ProjectConfig.EditorConfig.Expanded";
        private const string EMOJI_CONFIG_KEY = "Colorcrush.ProjectConfig.EmojiConfig.Expanded";
        private const string LOGGING_CONFIG_KEY = "Colorcrush.ProjectConfig.LoggingConfig.Expanded";
        private const string AUDIO_CONFIG_KEY = "Colorcrush.ProjectConfig.AudioConfig.Expanded";
        private bool showAudioConfig;
        private bool showEditorConfig;
        private bool showEmojiConfig;

        // Fold states with default values
        private bool showGameConfig;
        private bool showLoggingConfig;

        private void OnEnable()
        {
            // Load saved states or use defaults
            showGameConfig = EditorPrefs.GetBool(GAME_CONFIG_KEY, true); // Game config expanded by default
            showEditorConfig = EditorPrefs.GetBool(EDITOR_CONFIG_KEY, false);
            showEmojiConfig = EditorPrefs.GetBool(EMOJI_CONFIG_KEY, false);
            showLoggingConfig = EditorPrefs.GetBool(LOGGING_CONFIG_KEY, false);
            showAudioConfig = EditorPrefs.GetBool(AUDIO_CONFIG_KEY, false);
        }

        public override void OnInspectorGUI()
        {
            if (serializedObject == null)
            {
                return;
            }

            serializedObject.Update();

            // Script field
            var scriptProperty = serializedObject.FindProperty("m_Script");
            if (scriptProperty != null)
            {
                GUI.enabled = false;
                EditorGUILayout.PropertyField(scriptProperty);
                GUI.enabled = true;
            }

            EditorGUILayout.Space(5);

            // Game Configuration Section
            EditorGUI.BeginChangeCheck();
            showGameConfig = EditorGUILayout.Foldout(showGameConfig, "Game Configuration", true);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool(GAME_CONFIG_KEY, showGameConfig);
            }

            if (showGameConfig)
            {
                EditorGUI.indentLevel++;
                DrawGameConfiguration();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);

            // Editor Configuration Section
            EditorGUI.BeginChangeCheck();
            showEditorConfig = EditorGUILayout.Foldout(showEditorConfig, "Editor Configuration", true);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool(EDITOR_CONFIG_KEY, showEditorConfig);
            }

            if (showEditorConfig)
            {
                EditorGUI.indentLevel++;
                DrawEditorConfiguration();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);

            // Emoji Configuration Section
            EditorGUI.BeginChangeCheck();
            showEmojiConfig = EditorGUILayout.Foldout(showEmojiConfig, "Emoji Configuration", true);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool(EMOJI_CONFIG_KEY, showEmojiConfig);
            }

            if (showEmojiConfig)
            {
                EditorGUI.indentLevel++;
                DrawEmojiConfiguration();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);

            // Logging Configuration Section
            EditorGUI.BeginChangeCheck();
            showLoggingConfig = EditorGUILayout.Foldout(showLoggingConfig, "Logging Configuration", true);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool(LOGGING_CONFIG_KEY, showLoggingConfig);
            }

            if (showLoggingConfig)
            {
                EditorGUI.indentLevel++;
                DrawLoggingConfiguration();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);

            // Audio Configuration Section
            EditorGUI.BeginChangeCheck();
            showAudioConfig = EditorGUILayout.Foldout(showAudioConfig, "Audio Configuration", true);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool(AUDIO_CONFIG_KEY, showAudioConfig);
            }

            if (showAudioConfig)
            {
                EditorGUI.indentLevel++;
                DrawAudioConfiguration();
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPropertySafely(string propertyName)
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                EditorGUILayout.PropertyField(property, true);
            }
        }

        private string ConvertToResourcePath(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
            {
                return "";
            }

            var resourcePath = fullPath;
            if (resourcePath.Contains("Resources/"))
            {
                resourcePath = resourcePath[(resourcePath.IndexOf("Resources/", StringComparison.Ordinal) + 10)..];
            }

            if (resourcePath.EndsWith(".txt"))
            {
                resourcePath = resourcePath[..^4];
            }

            return resourcePath;
        }

        private void DrawGameConfiguration()
        {
            // Draw colorExperimentName with validation first
            var colorExperimentNameProp = serializedObject.FindProperty("colorExperimentName");
            if (colorExperimentNameProp != null)
            {
                // Get all concrete (non-abstract) nested types in ColorManager that inherit from ColorExperiment
                var experimentTypes = typeof(ColorManager)
                    .GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(t => t.IsClass &&
                                !t.IsAbstract &&
                                typeof(ColorManager.ColorExperiment).IsAssignableFrom(t) &&
                                t != typeof(ColorManager.ColorExperiment))
                    .ToList();

                var options = experimentTypes.Select(t => t.Name).ToList();
                var currentIndex = options.IndexOf(colorExperimentNameProp.stringValue);

                var rect = EditorGUILayout.GetControlRect();
                EditorGUI.BeginProperty(rect, new GUIContent(colorExperimentNameProp.displayName, colorExperimentNameProp.tooltip), colorExperimentNameProp);

                var newIndex = EditorGUI.Popup(
                    rect,
                    colorExperimentNameProp.displayName,
                    currentIndex != -1 ? currentIndex : 0,
                    options.ToArray()
                );

                if (newIndex != currentIndex || currentIndex == -1)
                {
                    colorExperimentNameProp.stringValue = options[newIndex];
                }

                EditorGUI.EndProperty();

                if (options.Count == 0)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel(" ");
                    EditorGUILayout.HelpBox("No concrete color experiments found in ColorManager", MessageType.Warning);
                    EditorGUILayout.EndHorizontal();
                }
            }

            // Draw colorDataFilePath with validation
            var colorDataFilePathProp = serializedObject.FindProperty("colorDataFilePath");
            if (colorDataFilePathProp != null)
            {
                EditorGUILayout.PropertyField(colorDataFilePathProp);

                var resourcePath = ConvertToResourcePath(colorDataFilePathProp.stringValue);
                var textAsset = !string.IsNullOrEmpty(resourcePath) ? Resources.Load<TextAsset>(resourcePath) : null;

                if (textAsset == null && !string.IsNullOrEmpty(colorDataFilePathProp.stringValue))
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel(" ");
                    EditorGUILayout.HelpBox("Color data file not found in Resources folder", MessageType.Error);
                    EditorGUILayout.EndHorizontal();
                }
            }

            // Draw colorSplitRegex with preview
            var colorSplitRegexProp = serializedObject.FindProperty("colorSplitRegex");
            if (colorSplitRegexProp != null)
            {
                EditorGUILayout.PropertyField(colorSplitRegexProp);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(" ");

                var resourcePath = ConvertToResourcePath(colorDataFilePathProp?.stringValue ?? "");
                var textAsset = !string.IsNullOrEmpty(resourcePath) ? Resources.Load<TextAsset>(resourcePath) : null;

                if (textAsset != null)
                {
                    try
                    {
                        var firstLine = textAsset.text.Split('\n')[0].Trim();
                        var regex = new Regex(colorSplitRegexProp.stringValue);
                        var match = regex.Match(firstLine);

                        if (match.Success && match.Groups.Count >= 4)
                        {
                            EditorGUILayout.HelpBox(
                                $"Preview of first line split:\nR: {match.Groups[1].Value}\nG: {match.Groups[2].Value}\nB: {match.Groups[3].Value}",
                                MessageType.Info);
                        }
                        else
                        {
                            EditorGUILayout.HelpBox(
                                "Regex does not match expected pattern (should capture 3 color components)",
                                MessageType.Error);
                        }
                    }
                    catch (Exception)
                    {
                        EditorGUILayout.HelpBox("Invalid regex pattern", MessageType.Error);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("No preview available - color data file not loaded", MessageType.None);
                }

                EditorGUILayout.EndHorizontal();
            }

            // Draw remaining properties
            var remainingProperties = new[]
            {
                "colorDataFormat",
                "useSkinColorMode",
                "unlockAllLevelsFromStart",
                "enableTripleTapToggleSkinColorMode",
                "enableResetButton",
                "useDisplayP3ColorSpace",
            };

            foreach (var prop in remainingProperties)
            {
                DrawPropertySafely(prop);
            }
        }

        private void DrawEditorConfiguration()
        {
            var properties = new[]
            {
                "initiatingScenePath",
                "useInitiatingScene",
                "mainResourcesPath",
                "mainScenesPath",
                "mainScriptsPath",
                "deleteAllLogFilesOnEditorStartup",
            };

            foreach (var prop in properties)
            {
                DrawPropertySafely(prop);
            }
        }

        private void DrawEmojiConfiguration()
        {
            var properties = new[]
            {
                "defaultEmojiName",
                "defaultHappyEmojiName",
                "happyEmojiFolder",
                "sadEmojiFolder",
            };

            foreach (var prop in properties)
            {
                DrawPropertySafely(prop);
            }
        }

        private void DrawLoggingConfiguration()
        {
            var properties = new[]
            {
                "logFilePrefix",
                "useAdditiveTimestamps",
                "logFileExtension",
                "logSaveInterval",
                "minimumLogSeverity",
                "alwaysCreateNewLogFileOnStartup",
                "logFileBufferSize",
                "suppressConsoleLoggingInEditor",
                "logFileEncoding",
            };

            foreach (var prop in properties)
            {
                DrawPropertySafely(prop);
            }
        }

        private void DrawAudioConfiguration()
        {
            var properties = new[]
            {
                "globalGain",
                "maxAudioSources",
                "targetRms",
                "audioPath",
                "mixerGainDB",
                "minVolumeAdjustment",
                "maxVolumeAdjustment",
            };

            foreach (var prop in properties)
            {
                DrawPropertySafely(prop);
            }
        }
    }
}