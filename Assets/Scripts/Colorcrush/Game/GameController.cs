// Copyright (C) 2024 Peter Guld Leth

#region

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Colorcrush.Animation;
using Colorcrush.Logging;
using Colorcrush.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#endregion

namespace Colorcrush.Game
{
    public class GameController : MonoBehaviour
    {
        private const float ShrinkFactor = 0.9f;
        private const float ToggledAlpha = 0.5f;
        private const float DefaultAlpha = 1f;
        private const int TargetSubmitCount = 5;

        [SerializeField] private TextMeshProUGUI submitButtonText;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private string nextSceneName = "MuralScene";
        [SerializeField] private Button submitButton;
        [SerializeField] private Canvas uiCanvas;
        private bool _buttonsInteractable = true;
        private bool[] _buttonToggledStates;

        private ColorController _colorController;
        private EmojiController _emojiController;
        private Vector3[] _originalButtonScales;

        private GameObject[] _selectionButtons;
        private Button[] _selectionGridButtons;
        private Image[] _selectionGridImages;
        private int _submitCount;

        private Image _targetEmojiImage;
        private Material _targetMaterial;
        private bool _targetReached;

        private void Awake()
        {
            InitializeComponents();
            InitializeButtons();
            UpdateUI();
        }

        private void InitializeComponents()
        {
            _colorController = FindObjectOfType<ColorController>();
            if (_colorController == null)
            {
                Debug.LogError("ColorController not found in the scene.");
            }

            _emojiController = FindObjectOfType<EmojiController>();
            if (_emojiController == null)
            {
                Debug.LogError("EmojiController not found in the scene.");
            }

            if (submitButtonText == null)
            {
                Debug.LogError("Submit button text not assigned in the inspector.");
            }

            if (progressText == null)
            {
                Debug.LogError("Progress text not assigned in the inspector.");
            }

            if (submitButton == null)
            {
                Debug.LogError("Submit button not assigned in the inspector.");
            }
            else
            {
                var buttonAnimator = submitButton.gameObject.GetComponent<ButtonAnimator>();
                submitButton.onClick.AddListener(() => AnimationManager.PlayAnimation(buttonAnimator, new TapAnimation(0.1f, 0.9f)));
            }

            if (uiCanvas == null)
            {
                Debug.LogError("UI Canvas not assigned in the inspector.");
            }
        }

        private void InitializeButtons()
        {
            _selectionButtons = GameObject.FindGameObjectsWithTag("SelectionButton");
            if (_selectionButtons.Length == 0)
            {
                Debug.LogWarning("No GameObjects with tag 'SelectionButton' found.");
                return;
            }

            _selectionGridImages = GetSortedComponentsFromButtons<Image>(_selectionButtons);
            _selectionGridButtons = GetSortedComponentsFromButtons<Button>(_selectionButtons);
            _buttonToggledStates = new bool[_selectionGridButtons.Length];
            _originalButtonScales = _selectionGridButtons.Select(b => b.transform.localScale).ToArray();

            _targetEmojiImage = GameObject.FindGameObjectWithTag("SelectionTarget")?.GetComponent<Image>();
            if (_targetEmojiImage == null)
            {
                Debug.LogError("Target emoji object not found or missing Image component.");
            }
            else
            {
                _targetEmojiImage.sprite = _emojiController.GetDefaultEmoji();
            }

            _targetMaterial = _targetEmojiImage?.material;
        }

        private T[] GetSortedComponentsFromButtons<T>(GameObject[] buttons) where T : Component
        {
            return buttons
                .Select(b => b.GetComponent<T>())
                .Where(c => c != null)
                .OrderBy(c => int.Parse(c.name.Replace("ColorPickButton", "")))
                .ToArray();
        }

        private void UpdateUI()
        {
            for (var i = 0; i < _selectionGridImages.Length; i++)
            {
                UpdateButton(i);
            }

            UpdateTargetButtonColor();
            UpdateProgressText();
        }

        private void UpdateButton(int index, bool ignoreAlpha = false)
        {
            _selectionGridImages[index].sprite = _emojiController.GetDefaultEmoji();
            var nextColor = _colorController.GetNextColor();
            _selectionGridImages[index].material.SetColor("_TargetColor", nextColor);
            if (!ignoreAlpha)
            {
                _selectionGridImages[index].material.SetFloat("_Alpha", DefaultAlpha);
            }

            LoggingManager.LogEvent(new ColorGeneratedEvent(index, nextColor));
        }

        public void OnButtonClicked(int index)
        {
            if (index < 0 || index >= _selectionGridButtons.Length)
            {
                Debug.LogWarning("Invalid button index clicked.");
                return;
            }

            _buttonToggledStates[index] = !_buttonToggledStates[index];
            var alpha = _buttonToggledStates[index] ? ToggledAlpha : DefaultAlpha;
            _selectionGridImages[index].material.SetFloat("_Alpha", alpha);

            var targetScale = _buttonToggledStates[index] ? _originalButtonScales[index] * ShrinkFactor : _originalButtonScales[index];
            _selectionGridButtons[index].transform.localScale = targetScale;

            // We no longer update the emoji sprite here

            if (_buttonToggledStates[index])
            {
                LoggingManager.LogEvent(new ColorSelectedEvent(index, _selectionGridImages[index].sprite.name));
            }
            else
            {
                LoggingManager.LogEvent(new ColorDeselectedEvent(index));
            }

            Debug.Log($"GameController: Button {index} toggled. New state: {_buttonToggledStates[index]}");
        }

        public void OnSubmitButtonClicked()
        {
            if (SceneManager.IsLoading || !_buttonsInteractable)
            {
                return; // Prevent button spamming or clicking before buttons are ready
            }

            if (_targetReached)
            {
                _colorController.AdvanceToNextTargetColor();
                submitButtonText.text = "LOADING...";
                SceneManager.LoadSceneAsync(nextSceneName);
                return;
            }

            _submitCount++;
            LoggingManager.LogEvent(new ColorsSubmittedEvent());

            StartCoroutine(AnimateEmojisAndResetButtons());

            UpdateProgressText();
            UpdateTargetButtonColor();
            StartCoroutine(ShowHappyEmojiCoroutine());

            if (_submitCount >= TargetSubmitCount)
            {
                _targetReached = true;
                foreach (var button in _selectionButtons)
                {
                    button.SetActive(false);
                }

                submitButtonText.text = "CONTINUE";
            }
        }

        private IEnumerator AnimateEmojisAndResetButtons()
        {
            _buttonsInteractable = false;
            var instantiatedObjects = new List<GameObject>();
            var emojiAnimators = new List<EmojiAnimator>();

            // Instantiate all emojis at once
            for (var i = 0; i < _selectionGridButtons.Length; i++)
            {
                var button = _selectionGridButtons[i].gameObject;
                var prefab = Resources.Load<GameObject>("Colorcrush/Misc/ColorPickButton");
                var instance = Instantiate(prefab, button.transform.position, button.transform.rotation, uiCanvas.transform);
                instance.transform.SetAsLastSibling(); // Ensure the instance is rendered on top
                instantiatedObjects.Add(instance);

                // Copy the original sprite, material, scale, and opacity
                var buttonImage = button.GetComponent<Image>();
                var instanceImage = instance.GetComponent<Image>();
                if (buttonImage != null && instanceImage != null)
                {
                    instanceImage.sprite = buttonImage.sprite;
                    instanceImage.material = new Material(buttonImage.material);
                    instance.transform.localScale = button.transform.localScale;
                    var color = buttonImage.color;
                    instanceImage.color = new Color(color.r, color.g, color.b, _buttonToggledStates[i] ? ToggledAlpha : DefaultAlpha);
                }

                var emojiAnimator = instance.AddComponent<EmojiAnimator>();
                emojiAnimators.Add(emojiAnimator);
            }

            // Set original buttons to 0 opacity and make them uninteractable
            foreach (var button in _selectionGridButtons)
            {
                var image = button.GetComponent<Image>();
                image.material.SetFloat("_Alpha", 0.0f);
                button.interactable = false;
            }

            // Animate selected emojis immediately and without delay
            for (var i = 0; i < emojiAnimators.Count; i++)
            {
                if (_buttonToggledStates[i])
                {
                    // Change to sad emoji for selected emojis
                    var instanceImage = emojiAnimators[i].GetComponent<Image>();
                    if (instanceImage != null)
                    {
                        instanceImage.sprite = _emojiController.GetNextSadEmoji();
                    }

                    // Fade out selected emojis
                    AnimationManager.PlayAnimation(emojiAnimators[i], new FadeAnimation(ToggledAlpha, 0f, 0.5f));
                }
            }

            // Animate non-selected emojis with delay
            for (var i = 0; i < emojiAnimators.Count; i++)
            {
                if (!_buttonToggledStates[i])
                {
                    // Change to happy emoji and move for non-selected emojis
                    var instanceImage = emojiAnimators[i].GetComponent<Image>();
                    if (instanceImage != null)
                    {
                        instanceImage.sprite = _emojiController.GetNextHappyEmoji();
                    }

                    var targetPosition = progressText.transform.position;

                    // Combine MoveToAnimation with ScaleAnimation
                    AnimationManager.PlayAnimation(emojiAnimators[i], new MoveToAnimation(targetPosition, 1f));
                    AnimationManager.PlayAnimation(emojiAnimators[i], new ScaleAnimation(Vector3.zero, 1f));

                    yield return new WaitForSeconds(0.05f);
                }
            }

            // Wait for all animations to complete
            yield return new WaitForSeconds(1f);

            // Clean up instantiated objects
            foreach (var obj in instantiatedObjects)
            {
                Destroy(obj);
            }

            // Reset buttons without showing them yet
            for (var i = 0; i < _selectionGridButtons.Length; i++)
            {
                UpdateButton(i, true);
                if (_buttonToggledStates[i])
                {
                    _buttonToggledStates[i] = false;
                    _selectionGridButtons[i].transform.localScale = _originalButtonScales[i];
                }
            }

            // Fade in buttons at the start of the next cycle
            yield return StartCoroutine(FadeInButtons());
        }

        private IEnumerator FadeInButtons()
        {
            // Ensure all buttons are not interactable before starting the fade-in
            foreach (var button in _selectionGridButtons)
            {
                button.interactable = false;
            }

            // Show the buttons and fade them in
            for (var i = 0; i < _selectionGridButtons.Length; i++)
            {
                var buttonAnimator = _selectionButtons[i].GetComponent<EmojiAnimator>();
                var fadeAnimation = new FadeAnimation(0f, DefaultAlpha, 0.5f);
                AnimationManager.PlayAnimation(buttonAnimator, fadeAnimation);

                // Add a small delay between each button's fade-in
                yield return new WaitForSeconds(0.025f);
            }

            // Wait for the fade-in animation to complete
            yield return new WaitForSeconds(0.5f);

            // Make all buttons interactable after the fade-in is complete
            foreach (var button in _selectionGridButtons)
            {
                button.interactable = true;
            }

            _buttonsInteractable = true;
        }

        private IEnumerator ShowHappyEmojiCoroutine()
        {
            _targetEmojiImage.sprite = _emojiController.GetNextHappyEmoji();
            yield return new WaitForSeconds(1f);
            _targetEmojiImage.sprite = _emojiController.GetDefaultEmoji();
        }

        private void UpdateProgressText()
        {
            var progress = Mathf.Min((float)_submitCount / TargetSubmitCount * 100f, 100f);
            progressText.text = $"{progress:F0}%";
        }

        private void UpdateTargetButtonColor()
        {
            if (_colorController != null && _targetEmojiImage != null && _targetMaterial != null)
            {
                var targetColor = ColorController.GetCurrentTargetColor();
                _targetMaterial.SetColor("_TargetColor", targetColor);
                LoggingManager.LogEvent(new NewTargetColorEvent(targetColor));
            }
        }
    }
}