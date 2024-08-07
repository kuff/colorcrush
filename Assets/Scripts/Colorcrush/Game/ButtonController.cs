// Copyright (C) 2024 Peter Guld Leth

#region

using System.Collections.Generic;
using System.Linq;
using Colorcrush.Files;
using Colorcrush.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = System.Random;

#endregion

namespace Colorcrush.Game
{
    public class ButtonController : MonoBehaviour
    {
        private const float ShrinkFactor = 0.9f;
        private const float ToggledAlpha = 0.5f;
        private const float DefaultAlpha = 1f;
        private const int MiddleButtonIndex = 4; // Assuming a 3x3 grid, the middle button is index 4
        [SerializeField] private TextMeshProUGUI submitButtonText;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private SceneLoader sceneLoader;
        private bool[] _buttonToggledStates;
        private ColorController _colorController;
        private static Queue<Sprite> _emojiQueue;
        private List<Sprite> _emojiSprites;
        private FilteredEmojiTracker _filteredEmojiTracker;
        private Vector3[] _originalButtonScales;
        private GameObject _selectionGrid;
        private Button[] _selectionGridButtons;
        private Image[] _selectionGridImages;

        private void Awake()
        {
            LoadAndShuffleEmojis();
            if (_emojiQueue == null)
            {
                InitializeEmojiQueue();
            }
            InitializeSelectionGridImages();
            InitializeSelectionGridButtons();
            _colorController = FindObjectOfType<ColorController>();
            if (_colorController == null)
            {
                Debug.LogError("ColorController not found in the scene.");
            }

            _filteredEmojiTracker = new FilteredEmojiTracker(ProjectConfig.InstanceConfig.numColorsToFilter);
            if (submitButtonText == null)
            {
                Debug.LogError("Submit button text not assigned in the inspector.");
            }

            if (progressText == null)
            {
                Debug.LogError("Progress text not assigned in the inspector.");
            }

            if (sceneLoader == null)
            {
                Debug.LogError("SceneLoader not assigned in the inspector.");
            }

            UpdateProgressText();

            // Unqueue the next 9 emojis and put them on all buttons, including the middle button
            for (var i = 0; i < 9; i++)
            {
                var emoji = GetNextEmoji();
                _selectionGridImages[i].sprite = emoji;
                
                // Update the material color for all buttons
                if (_colorController != null)
                {
                    var newColor = _colorController.GetNextColor();
                    _selectionGridImages[i].material.SetColor("_TargetColor", newColor);
                }
            }
            
            // Update the middle button color separately
            UpdateMiddleButtonColor();
            OnSubmitButtonClicked();
        }

        private void LoadAndShuffleEmojis()
        {
            // Load all emoji sprites from the Resources folder
            _emojiSprites = Resources.LoadAll<Sprite>("Colorcrush/Emoji").ToList();

            // Create a new System.Random instance with the seed from ProjectConfig
            var random = new Random(ProjectConfig.InstanceConfig.randomSeed);

            // Shuffle the list using the Fisher-Yates algorithm
            var n = _emojiSprites.Count;
            while (n > 1)
            {
                n--;
                var k = random.Next(n + 1);
                (_emojiSprites[k], _emojiSprites[n]) = (_emojiSprites[n], _emojiSprites[k]);
            }

            Debug.Log($"ButtonController: Loaded and shuffled {_emojiSprites.Count} emojis");
        }

        private void InitializeEmojiQueue()
        {
            _emojiQueue = new Queue<Sprite>(_emojiSprites);
            Debug.Log($"ButtonController: Emoji queue initialized with {_emojiQueue.Count} emojis");
        }

        private void InitializeSelectionGridImages()
        {
            _selectionGrid = GameObject.FindGameObjectWithTag("SelectionGrid");
            if (_selectionGrid != null)
            {
                _selectionGridImages = _selectionGrid.GetComponentsInChildren<Image>()
                    .Where(image => image.transform != _selectionGrid.transform)
                    .OrderBy(image => image.name)
                    .ToArray();
                Debug.Log($"ButtonController: Selection grid images initialized. Count: {_selectionGridImages.Length}");
            }
            else
            {
                Debug.LogWarning("No GameObject with tag 'SelectionGrid' found.");
            }
        }

        private void InitializeSelectionGridButtons()
        {
            if (_selectionGrid != null)
            {
                _selectionGridButtons = _selectionGrid.GetComponentsInChildren<Button>()
                    .OrderBy(button => button.name)
                    .ToArray();

                _buttonToggledStates = new bool[_selectionGridButtons.Length];
                _originalButtonScales = new Vector3[_selectionGridButtons.Length];

                for (var i = 0; i < _selectionGridButtons.Length; i++)
                {
                    _buttonToggledStates[i] = false;
                    _originalButtonScales[i] = _selectionGridButtons[i].transform.localScale;
                    if (i == MiddleButtonIndex)
                    {
                        _selectionGridButtons[i].interactable = false;
                    }
                }

                Debug.Log($"ButtonController: Selection grid buttons initialized. Count: {_selectionGridButtons.Length}");
            }
            else
            {
                Debug.LogWarning("No GameObject with tag 'SelectionGrid' found.");
            }
        }

        public void OnButtonClicked(int index)
        {
            Debug.Log($"ButtonController: Button clicked. Index: {index}");
            if (index >= 0 && index < _selectionGridButtons.Length && index != MiddleButtonIndex)
            {
                _buttonToggledStates[index] = !_buttonToggledStates[index];

                // Toggle opacity in shader
                var alpha = _buttonToggledStates[index] ? ToggledAlpha : DefaultAlpha;
                _selectionGridImages[index].material.SetFloat("_Alpha", alpha);

                // Toggle scale
                var targetScale = _buttonToggledStates[index]
                    ? _originalButtonScales[index] * ShrinkFactor
                    : _originalButtonScales[index];
                _selectionGridButtons[index].transform.localScale = targetScale;

                Debug.Log($"ButtonController: Button {index} toggled. New state: {_buttonToggledStates[index]}");
            }
            else
            {
                Debug.LogWarning("Invalid button index or middle button clicked.");
            }
        }

        public void OnSubmitButtonClicked()
        {
            Debug.Log("ButtonController: Submit button clicked");
            if (_filteredEmojiTracker.TargetReached)
            {
                _colorController.AdvanceToNextTargetColor();
                submitButtonText.text = "LOADING...";
                sceneLoader.LoadScene("MuralScene");
                return;
            }

            List<(int buttonIndex, Material buttonMaterial)> filteredEmojis = new();
            var updatedButtonsCount = 0;
            string firstUpdatedObjectName = null;
            for (var i = 0; i < _selectionGridButtons.Length; i++)
            {
                if (i == MiddleButtonIndex)
                {
                    UpdateMiddleButtonColor();
                    continue;
                }

                if (_buttonToggledStates[i])
                {
                    _buttonToggledStates[i] = false;
                    _selectionGridImages[i].sprite = GetNextEmoji();

                    if (firstUpdatedObjectName == null)
                    {
                        firstUpdatedObjectName = _selectionGridImages[i].name;
                    }

                    // Reset button appearance
                    _selectionGridImages[i].material.SetFloat("_Alpha", DefaultAlpha);
                    _selectionGridButtons[i].transform.localScale = _originalButtonScales[i];

                    // Update the material color
                    if (_colorController != null)
                    {
                        var newColor = _colorController.GetNextColor();
                        _selectionGridImages[i].material.SetColor("_TargetColor", newColor);
                    }

                    filteredEmojis.Add((i, _selectionGridImages[i].material));
                    updatedButtonsCount++;
                }
            }

            _filteredEmojiTracker.TrackFilteredEmojis(filteredEmojis);
            Debug.Log($"ButtonController: Updated {updatedButtonsCount} button(s)" + (updatedButtonsCount > 0 ? $", including {firstUpdatedObjectName}" : ""));

            UpdateProgressText();

            if (_filteredEmojiTracker.TargetReached)
            {
                Debug.Log("Target number of filtered emojis reached!");
                _selectionGrid.SetActive(false);
                submitButtonText.text = "CONTINUE";
            }
        }

        private void UpdateProgressText()
        {
            var progress = Mathf.Min((float)_filteredEmojiTracker.FilteredEmojiCount / _filteredEmojiTracker.TargetFilteredCount * 100f, 100f);
            progressText.text = $"{progress:F0}%";
        }

        public Sprite GetNextEmoji()
        {
            if (_emojiQueue.Count == 0)
            {
                Debug.Log("ButtonController: Emoji queue empty, reinitializing");
                InitializeEmojiQueue(); // Reinitialize if queue is empty
            }

            var nextEmoji = _emojiQueue.Dequeue();
            _emojiQueue.Enqueue(nextEmoji); // Add back to the end for wrapping
            Debug.Log($"ButtonController: Next emoji retrieved. Remaining in queue: {_emojiQueue.Count}");
            return nextEmoji;
        }

        private void UpdateMiddleButtonColor()
        {
            if (_colorController != null)
            {
                Color targetColor = ColorController.GetCurrentTargetColor();
                _selectionGridImages[MiddleButtonIndex].material.SetColor("_TargetColor", targetColor);
            }
        }
    }
}