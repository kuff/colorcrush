// Copyright (C) 2024 Peter Guld Leth

#region

using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#endregion

namespace Colorcrush.Files
{
    public class SceneLoader : MonoBehaviour
    {
        // Reference to the UI button
        public Button loadButton;
        public Text buttonText;
        public TextMeshProUGUI buttonTextTMP;

        // This function can be called to load a scene by its name
        public void LoadScene(string sceneName)
        {
            // Check if the scene is already loaded to avoid reloading it
            if (SceneManager.GetActiveScene().name != sceneName)
            {
                // If the Text reference exists, update its properties
                if (buttonText != null)
                {
                    buttonText.text = "LOADING...";
                }

                // If the TextMeshPro reference exists, update its properties
                if (buttonTextTMP != null)
                {
                    buttonTextTMP.text = "LOADING...";
                }

                // If the button reference exists, update its properties
                if (loadButton != null)
                {
                    loadButton.interactable = false;
                }

                // Load the scene by its name
                SceneManager.LoadScene(sceneName);
            }
            else
            {
                Debug.Log("Scene " + sceneName + " is already loaded.");
            }
        }
    }
}