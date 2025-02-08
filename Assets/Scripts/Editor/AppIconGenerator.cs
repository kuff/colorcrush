// Copyright (C) 2025 Peter Guld Leth

#region

using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

#endregion

namespace Editor
{
    public class AppIconGenerator : EditorWindow
    {
        private bool isProcessing;
        private MessageType messageType = MessageType.Info;
        private string outputMessage = "";
        private Vector2 scrollPosition;
        private Object selectedEmojiFile;
        private string selectedEmojiPath = "";

        private void OnEnable()
        {
            // Set default emoji
            const string defaultEmojiPath = "Assets/Resources/Colorcrush/Emoji/Happy/reshot-icon-happy-laugh-72WQS35RC4.png";
            selectedEmojiFile = AssetDatabase.LoadAssetAtPath<Object>(defaultEmojiPath);
            if (selectedEmojiFile != null)
            {
                selectedEmojiPath = defaultEmojiPath;
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("App Icon Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            const string helpText = "Setup Instructions:\n\n" +
                                    "1. Install Python from python.org if not installed\n" +
                                    "2. Open terminal/command prompt in project root\n" +
                                    "3. Run: pip install -r requirements.txt\n\n" +
                                    "To generate icons:\n" +
                                    "1. Select an emoji file from Resources/Colorcrush/Emoji folder\n" +
                                    "2. Click Generate App Icons";

            EditorGUILayout.HelpBox(helpText, MessageType.Info);
            EditorGUILayout.Space(10);

            // Emoji file selection
            EditorGUI.BeginChangeCheck();
            selectedEmojiFile = EditorGUILayout.ObjectField("Emoji File", selectedEmojiFile, typeof(Object), false);
            if (EditorGUI.EndChangeCheck() && selectedEmojiFile != null)
            {
                selectedEmojiPath = AssetDatabase.GetAssetPath(selectedEmojiFile);
            }

            EditorGUILayout.Space(10);

            GUI.enabled = !string.IsNullOrEmpty(selectedEmojiPath) && !isProcessing;
            if (GUILayout.Button("Generate App Icons"))
            {
                GenerateAppIcons();
            }

            GUI.enabled = true;

            // Output message area
            if (!string.IsNullOrEmpty(outputMessage))
            {
                EditorGUILayout.Space(10);
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(100));
                EditorGUILayout.HelpBox(outputMessage, messageType);
                EditorGUILayout.EndScrollView();
            }
        }

        [MenuItem("Colorcrush/App Icon Generator")]
        public static void ShowWindow()
        {
            GetWindow<AppIconGenerator>("App Icon Generator");
        }

        private void GenerateAppIcons()
        {
            if (string.IsNullOrEmpty(selectedEmojiPath))
            {
                ShowError("Please select an emoji file first.");
                return;
            }

            isProcessing = true;
            ShowInfo("Generating app icons...");

            // Get the project's root directory (one level up from Assets)
            var projectRootPath = Path.GetDirectoryName(Application.dataPath);
            var pythonScriptPath = Path.Combine(projectRootPath!, "generate_app_icons.py");

            if (!File.Exists(pythonScriptPath))
            {
                ShowError($"Could not find generate_app_icons.py script in the project root directory ({projectRootPath}).\nPlease ensure the script is placed in the project's root folder.");
                isProcessing = false;
                return;
            }

            var fileName = Path.GetFileNameWithoutExtension(selectedEmojiPath);

            var startInfo = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"\"{pythonScriptPath}\" \"{fileName}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = projectRootPath, // Set working directory to project root
            };

            try
            {
                using var process = Process.Start(startInfo);
                process!.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    // Focus the generated square icon in the Project window
                    const string squareIconPath = "Assets/AppIcon_Square.png";
                    Object squareIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(squareIconPath);

                    AssetDatabase.Refresh();

                    if (squareIcon != null)
                    {
                        EditorUtility.FocusProjectWindow();
                        Selection.activeObject = squareIcon;
                        ShowSuccess("App icons generated successfully!");
                        Close(); // Close the window after successful generation
                    }
                    else
                    {
                        ShowError("Icons were generated but could not be found in the Assets folder.");
                    }
                }
                else
                {
                    ShowError($"Failed to generate app icons:\n{error}");
                }
            }
            catch (Exception e)
            {
                ShowError($"Failed to execute Python script:\n{e.Message}");
            }
            finally
            {
                isProcessing = false;
                Repaint();
            }
        }

        private void ShowError(string message)
        {
            outputMessage = message;
            messageType = MessageType.Error;
        }

        private void ShowSuccess(string message)
        {
            outputMessage = message;
            messageType = MessageType.Info;
        }

        private void ShowInfo(string message)
        {
            outputMessage = message;
            messageType = MessageType.Info;
        }
    }
}