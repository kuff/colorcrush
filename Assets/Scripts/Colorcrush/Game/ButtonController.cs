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
        private const int TargetSubmitCount = 5;
        private static Queue<Sprite> _emojiQueue;
        [SerializeField] private TextMeshProUGUI submitButtonText;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private SceneLoader sceneLoader;
        private bool[] _buttonToggledStates;
        private ColorController _colorController;
        private List<Sprite> _emojiSprites;
        private int _submitCount;
        private Vector3[] _originalButtonScales;
        private GameObject[] _selectionButtons;
        private Button[] _selectionGridButtons;
        private Image[] _selectionGridImages;
        private GameObject _targetEmojiObject;
        private Image _targetEmojiImage;
        private Material _targetMaterial;
        private bool _targetReached;

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

            _submitCount = 0;
            _targetReached = false;
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

            // Unqueue the next emojis and put them on all buttons, including the target button
            for (var i = 0; i < _selectionGridImages.Length; i++)
            {
                var emoji = GetNextEmoji();
                _selectionGridImages[i].sprite = emoji;

                // Update the material color for all buttons
                if (_colorController != null)
                {
                    var newColor = _colorController.GetNextColor();
                    _selectionGridImages[i].material.SetColor("_TargetColor", newColor);
                }

                // Reset the alpha value for all buttons
                _selectionGridImages[i].material.SetFloat("_Alpha", DefaultAlpha);
            }

            // Update the target button color separately
            UpdateTargetButtonColor();
            //OnSubmitButtonClicked();
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
            _selectionButtons = GameObject.FindGameObjectsWithTag("SelectionButton");
            if (_selectionButtons != null && _selectionButtons.Length > 0)
            {
                _selectionGridImages = GetSortedComponentsFromButtons<Image>(_selectionButtons);
                Debug.Log($"ButtonController: Selection grid images initialized. Count: {_selectionGridImages.Length}");

                // Log the names of the buttons in the order they are used for indexing
                Debug.Log("Button order for indexing:");
                for (int i = 0; i < _selectionGridImages.Length; i++)
                {
                    Debug.Log($"Index {i}: {_selectionGridImages[i].name}");
                }

                // Find the last material in alphabetical order
                _targetMaterial = _selectionGridImages
                    .Select(image => image.material)
                    .OrderBy(material => material.name)
                    .Last();
            }
            else
            {
                Debug.LogWarning("No GameObjects with tag 'SelectionButton' found.");
            }

            _targetEmojiObject = GameObject.FindGameObjectWithTag("SelectionTarget");
            if (_targetEmojiObject != null)
            {
                _targetEmojiImage = _targetEmojiObject.GetComponent<Image>();
                if (_targetEmojiImage == null)
                {
                    Debug.LogError("Target emoji object does not have an Image component.");
                }
                else
                {
                    // The material is already assigned in the editor, so we don't need to assign it here
                    _targetMaterial = _targetEmojiImage.material;
                }
            }
            else
            {
                Debug.LogError("No GameObject with tag 'SelectionTarget' found.");
            }
        }

        private void InitializeSelectionGridButtons()
        {
            if (_selectionButtons != null && _selectionButtons.Length > 0)
            {
                _selectionGridButtons = GetSortedComponentsFromButtons<Button>(_selectionButtons);

                _buttonToggledStates = new bool[_selectionGridButtons.Length];
                _originalButtonScales = new Vector3[_selectionGridButtons.Length];

                for (var i = 0; i < _selectionGridButtons.Length; i++)
                {
                    _buttonToggledStates[i] = false;
                    _originalButtonScales[i] = _selectionGridButtons[i].transform.localScale;
                }

                Debug.Log($"ButtonController: Selection grid buttons initialized. Count: {_selectionGridButtons.Length}");
            }
            else
            {
                Debug.LogWarning("No GameObjects with tag 'SelectionButton' found.");
            }
        }

        private T[] GetSortedComponentsFromButtons<T>(GameObject[] buttons) where T : Component
        {
            return buttons
                .Select(button => button.GetComponent<T>())
                .Where(component => component != null)
                .OrderBy(component => int.Parse(component.name.Replace("ColorPickButton", "")))
                .ToArray();
        }

        public void OnButtonClicked(int index)
        {
            Debug.Log($"ButtonController: Button clicked. Index: {index}");
            if (index >= 0 && index < _selectionGridButtons.Length)
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
                Debug.LogWarning("Invalid button index clicked.");
            }
        }

        public void OnSubmitButtonClicked()
        {
            Debug.Log("ButtonController: Submit button clicked");

            if (_targetReached)
            {
                _colorController.AdvanceToNextTargetColor();
                submitButtonText.text = "LOADING...";
                sceneLoader.LoadScene("MuralScene");
                return;
            }

            _submitCount++;

            List<(int buttonIndex, Material buttonMaterial)> filteredEmojis = new();
            var updatedButtonsCount = 0;
            string firstUpdatedObjectName = null;
            for (var i = 0; i < _selectionGridButtons.Length; i++)
            {
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

            Debug.Log($"ButtonController: Updated {updatedButtonsCount} button(s)" + (updatedButtonsCount > 0 ? $", including {firstUpdatedObjectName}" : ""));

            UpdateProgressText();

            if (_submitCount >= TargetSubmitCount)
            {
                _targetReached = true;
                Debug.Log("Target number of submissions reached!");
                foreach (var button in _selectionButtons)
                {
                    button.SetActive(false);
                }
                submitButtonText.text = "CONTINUE";
            }

            // Update the target button color
            UpdateTargetButtonColor();
        }

        private void UpdateProgressText()
        {
            var progress = Mathf.Min((float)_submitCount / TargetSubmitCount * 100f, 100f);
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

        private void UpdateTargetButtonColor()
        {
            if (_colorController != null && _targetEmojiImage != null && _targetMaterial != null)
            {
                var targetColor = ColorController.GetCurrentTargetColor();
                _targetMaterial.SetColor("_TargetColor", targetColor);
            }
        }
    }
}