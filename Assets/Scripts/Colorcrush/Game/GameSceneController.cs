// Copyright (C) 2024 Peter Guld Leth

#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Colorcrush.Animation;
using Colorcrush.Logging;
using Colorcrush.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Colorcrush.Game.ColorManager;
using Animator = Colorcrush.Animation.Animator;

#endregion

namespace Colorcrush.Game
{
    public class GameSceneController : MonoBehaviour
    {
        [Header("General")]
        [Tooltip("Factor to shrink buttons when toggled. A value of 0.9 means the button will shrink to 90% of its original size.")] [SerializeField]
        private float shrinkFactor = 0.9f;

        [Tooltip("Alpha (transparency) value for toggled buttons. 0 is fully transparent, 1 is fully opaque.")] [SerializeField]
        private float toggledAlpha = 0.5f;

        [Tooltip("Default alpha (transparency) value for buttons when not toggled. 0 is fully transparent, 1 is fully opaque.")] [SerializeField]
        private float defaultAlpha = 1f;

        [Tooltip("Scale factor for initial setup animation. A value greater than 1 will make the target emoji appear larger before settling to their normal size.")] [SerializeField]
        private float setupScaleFactor = 1.2f;

        [Tooltip("Duration in seconds of the initial setup animation for buttons.")] [SerializeField]
        private float setupAnimationDuration = 0.5f;

        [Tooltip("Delay in seconds between each button's fade-in animation during the initial setup.")] [SerializeField]
        private float buttonFadeInDelay = 0.025f;

        [Header("Emoji Animation Settings")]
        [Tooltip("Duration in seconds of the fade-out animation for emojis that were selected.")] [SerializeField]
        private float selectedEmojiFadeDuration = 0.5f;

        [Tooltip("Duration in seconds of the movement animation for emojis that were not selected.")] [SerializeField]
        private float nonSelectedEmojiMoveDuration = 0.5f;

        [Tooltip("Vector3 offset for the movement of non-selected emojis. Determines direction and distance of movement.")] [SerializeField]
        private Vector3 nonSelectedEmojiMoveOffset = Vector3.zero;

        [Header("Sound Settings")]
        [Tooltip("Name of the sound effect to play for non-selected emojis.")] [SerializeField]
        private string nonSelectedEmojiSound = "misc_menu";

        [Tooltip("Base pitch for the first non-selected emoji sound. Values above 1 increase pitch, below 1 decrease pitch.")] [SerializeField]
        private float nonSelectedEmojiBasePitch = 3f;

        [Tooltip("Incremental pitch change for each subsequent non-selected emoji sound.")] [SerializeField]
        private float nonSelectedEmojiPitchStep = 5f;

        [Tooltip("Volume adjustment for the non-selected emoji sounds. Higher values increase volume.")] [SerializeField]
        private float nonSelectedEmojiGain = 1f;

        [Header("UI Elements")]
        [Tooltip("TextMeshProUGUI component for displaying text on the submit button.")] [SerializeField]
        private TextMeshProUGUI submitButtonText;

        [Tooltip("Image component representing the progress bar fill.")] [SerializeField]
        private Image progressBar;

        [Tooltip("Button component for the submit action.")] [SerializeField]
        private Button submitButton;

        [Tooltip("Canvas containing all the UI elements for the game scene.")] [SerializeField]
        private Canvas uiCanvas;

        [Tooltip("Image component for displaying the target emoji that players need to match.")] [SerializeField]
        private Image targetEmojiImage;

        [Header("Scene Management")]
        [Tooltip("Name of the scene to load after successfully completing all color submissions.")] [SerializeField]
        private string nextSceneName = "MuralScene";

        private bool _buttonsInteractable = true;
        private bool[] _buttonToggledStates;
        private ColorExperiment _colorExperiment;
        private List<ColorObject> _currentBatch;
        private GameState _currentState = GameState.Setup;
        private bool _hasMoreBatches;
        private float _initialProgressBarWidth;
        private Vector3[] _originalButtonScales;
        private Vector3 _originalTargetScale;
        private GameObject[] _selectionButtons;
        private Button[] _selectionGridButtons;
        private Image[] _selectionGridImages;
        private int _submitCount;
        private Color _targetColor;
        private Animator _targetEmojiAnimator;
        private Image _targetEmojiImage;
        private bool _targetReached;

        private void Awake()
        {
            // Get the target color from PlayerPrefs
            var targetColorHex = PlayerPrefs.GetString("TargetColor");
            if (string.IsNullOrEmpty(targetColorHex))
            {
#if DEBUG
                _targetColor = TargetColors[0];
                Debug.LogWarning($"Target color not found in PlayerPrefs. Using first color from ColorArray for debugging: {ColorUtility.ToHtmlStringRGBA(_targetColor)}");
#else
                throw new Exception("Target color not found in PlayerPrefs.");
#endif
            }
            else
            {
                if (!ColorUtility.TryParseHtmlString("#" + targetColorHex, out _targetColor))
                {
                    throw new Exception($"Failed to parse target color '{targetColorHex}' from PlayerPrefs.");
                }
            }

            LoggingManager.LogEvent(new GameLevelBeginEvent(_targetColor));

            InitializeComponents();
            InitializeButtons();
            InitializeProgressBar();
            InitializeColorExperiment();
            UpdateUI();
            StartCoroutine(GameLoop());

            LoggingManager.LogEvent(new SkinColorModeEvent(ProjectConfig.InstanceConfig.useSkinColorMode));
        }

        private void InitializeColorExperiment()
        {
            _colorExperiment = BeginColorExperiment(new ColorObject(_targetColor));
            (_currentBatch, _hasMoreBatches) = _colorExperiment.GetNextColorBatch(null, null);
        }

        private IEnumerator GameLoop()
        {
            yield return StartCoroutine(SetupState());
            yield return StartCoroutine(MainState());
            yield return StartCoroutine(TeardownState());
        }

        private IEnumerator SetupState()
        {
            // Set the target color and alpha for the target emoji image
            if (_targetEmojiImage != null && _targetEmojiImage.material != null)
            {
                ShaderManager.SetColor(_targetEmojiImage.gameObject, "_TargetColor", _targetColor);
                ShaderManager.SetColor(_targetEmojiImage.gameObject, "_OriginalColor", _targetColor);
                ShaderManager.SetFloat(_targetEmojiImage.gameObject, "_Alpha", 1f);
                ShaderManager.SetFloat(_targetEmojiImage.gameObject, "_SkinColorMode", ProjectConfig.InstanceConfig.useSkinColorMode ? 1 : 0);
            }
            else
            {
                Debug.LogError("Target emoji image or its material is null. Unable to set target color and alpha.");
            }

            _currentState = GameState.Setup;
            _targetEmojiImage.transform.localScale = _originalTargetScale * setupScaleFactor;

            AudioManager.PlaySound("MENU B_Back");

            // Set grid buttons' opacity to 0 and activate them
            foreach (var button in _selectionGridButtons)
            {
                var image = button.GetComponent<Image>();
                if (image != null && image.material != null)
                {
                    ShaderManager.SetFloat(button.gameObject, "_Alpha", 0f);
                }

                button.gameObject.SetActive(true);
            }

            // Hide submit button
            var submitButtonAnimator = submitButton.GetComponent<Animator>();
            submitButtonAnimator.SetOpacity(0f, null);
            submitButton.interactable = false;

            // Scale down target
            AnimationManager.PlayAnimation(_targetEmojiAnimator, new ScaleAnimation(_originalTargetScale, setupAnimationDuration));

            yield return new WaitForSeconds(setupAnimationDuration / 2); // Start fading in buttons halfway through target scaling

            // Fade in grid buttons with staggered delay
            for (var i = 0; i < _selectionGridButtons.Length; i++)
            {
                var buttonAnimator = _selectionButtons[i].GetComponent<EmojiAnimator>();
                AnimationManager.PlayAnimation(buttonAnimator, new FadeAnimation(0f, defaultAlpha, 0.5f));
                yield return new WaitForSeconds(buttonFadeInDelay);
            }

            // Wait for all grid buttons to finish fading in
            yield return new WaitForSeconds(0.25f);

            // Fade in submit button
            submitButton.gameObject.SetActive(true);
            AnimationManager.PlayAnimation(submitButtonAnimator, new FadeAnimation(0f, defaultAlpha, setupAnimationDuration / 2));

            yield return new WaitForSeconds(setupAnimationDuration / 2);

            // Make submit button interactable after it's fully revealed
            submitButton.interactable = true;
        }

        private IEnumerator MainState()
        {
            _currentState = GameState.Main;
            _buttonsInteractable = true;

            while (!_targetReached)
            {
                yield return null; // Wait for player interactions
            }
        }

        private IEnumerator TeardownState()
        {
            // Save the results
            var results = _colorExperiment.GetResultingColors();
            LoggingManager.LogEvent(new FinalColorsEvent(results));

            LoggingManager.LogEvent(new GameLevelEndEvent());

            _currentState = GameState.Teardown;
            _buttonsInteractable = false;

            // Fade out grid and submit button
            foreach (var button in _selectionGridButtons)
            {
                var buttonAnimator = button.GetComponent<Animator>();
                AnimationManager.PlayAnimation(buttonAnimator, new FadeAnimation(defaultAlpha, 0f, setupAnimationDuration / 2));
            }

            yield return new WaitForSeconds(1f);

            var sceneIsLoaded = false;
            SceneManager.LoadSceneAsync(nextSceneName, () => sceneIsLoaded = true);

            var submitAnimator = submitButton.GetComponent<Animator>();
            AnimationManager.PlayAnimation(submitAnimator, new FadeAnimation(defaultAlpha, 0f, setupAnimationDuration / 2));

            // Wait for an additional second before scaling up the target
            yield return new WaitForSeconds(setupAnimationDuration / 2);

            // Scale up target
            AnimationManager.PlayAnimation(_targetEmojiAnimator, new ScaleAnimation(_originalTargetScale * setupScaleFactor, setupAnimationDuration));
            AudioManager.PlaySound("MENU B_Select");

            yield return new WaitForSeconds(setupAnimationDuration);

            // Allow scene to load
            while (!sceneIsLoaded)
            {
                yield return null;
            }

            SceneManager.ActivateLoadedScene();
        }

        private void InitializeComponents()
        {
            FindObjectOfType<EmojiManager>();
        }

        private void InitializeButtons()
        {
            _selectionButtons = GameObject.FindGameObjectsWithTag("SelectionButton");
            if (_selectionButtons.Length == 0)
            {
                return;
            }

            _selectionGridImages = GetSortedComponentsFromButtons<Image>(_selectionButtons);
            _selectionGridButtons = GetSortedComponentsFromButtons<Button>(_selectionButtons);
            _buttonToggledStates = new bool[_selectionGridButtons.Length];
            _originalButtonScales = _selectionGridButtons.Select(b => b.transform.localScale).ToArray();

            var targetEmojiObject = GameObject.FindGameObjectWithTag("SelectionTarget");
            if (targetEmojiObject != null)
            {
                _targetEmojiImage = targetEmojiObject.GetComponent<Image>();
                _targetEmojiAnimator = targetEmojiObject.GetComponent<Animator>();
                if (_targetEmojiImage != null)
                {
                    _targetEmojiImage.sprite = EmojiManager.GetDefaultEmoji();
                }

                var targetButton = targetEmojiObject.GetComponent<Button>();
                if (targetButton != null)
                {
                    targetButton.onClick.AddListener(OnTargetEmojiClicked);
                }

                _originalTargetScale = targetEmojiObject.transform.localScale;
            }
        }

        private void OnTargetEmojiClicked()
        {
            if (_targetEmojiAnimator != null)
            {
                AnimationManager.PlayAnimation(_targetEmojiAnimator, new ShakeAnimation(0.5f, 5f, 15f));
            }
        }

        private void InitializeProgressBar()
        {
            if (progressBar != null)
            {
                _initialProgressBarWidth = progressBar.rectTransform.rect.width;
                progressBar.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 0);
            }
            else
            {
                Debug.LogError("Progress bar image not assigned in the inspector.");
            }
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
            UpdateProgressBar();
        }

        private void UpdateButton(int index, bool ignoreAlpha = false)
        {
            _selectionGridImages[index].sprite = EmojiManager.GetDefaultEmoji();
            var nextColor = GetNextColor(index);
            ShaderManager.SetColor(_selectionGridImages[index].gameObject, "_TargetColor", nextColor);
            ShaderManager.SetColor(_selectionGridImages[index].gameObject, "_OriginalColor", _targetColor);
            ShaderManager.SetFloat(_selectionGridImages[index].gameObject, "_SkinColorMode", ProjectConfig.InstanceConfig.useSkinColorMode ? 1 : 0);
            if (!ignoreAlpha)
            {
                ShaderManager.SetFloat(_selectionGridImages[index].gameObject, "_Alpha", defaultAlpha);
            }

            LoggingManager.LogEvent(new ColorGeneratedEvent(index, nextColor));
        }

        public void OnButtonClicked(int index)
        {
            if (_currentState != GameState.Main || !_buttonsInteractable || index < 0 || index >= _selectionGridButtons.Length)
            {
                return;
            }

            _buttonToggledStates[index] = !_buttonToggledStates[index];
            var alpha = _buttonToggledStates[index] ? toggledAlpha : defaultAlpha;
            ShaderManager.SetFloat(_selectionGridImages[index].gameObject, "_Alpha", alpha);

            var targetScale = _buttonToggledStates[index] ? _originalButtonScales[index] * shrinkFactor : _originalButtonScales[index];
            _selectionGridButtons[index].transform.localScale = targetScale;

            // Log the button selected or deselected event
            if (_buttonToggledStates[index])
            {
                LoggingManager.LogEvent(new ColorSelectedEvent(index));
                AudioManager.PlaySound("MENU_Pick");
            }
            else
            {
                LoggingManager.LogEvent(new ColorDeselectedEvent(index));
                AudioManager.PlaySound("MENU_Pick", pitchShift: 0.85f);
            }

            // Add debug info
            Debug.Log($"Button {index} {(_buttonToggledStates[index] ? "selected" : "deselected")}. New alpha: {alpha}, New scale: {targetScale}");
        }

        public void OnSubmitButtonClicked()
        {
            if (_currentState != GameState.Main || SceneManager.IsLoading || !_buttonsInteractable)
            {
                AnimationManager.PlayAnimation(submitButton.GetComponent<Animator>(), new ShakeAnimation(0.1f, 9f));
                AudioManager.PlaySound("misc_menu", pitchShift: 0.85f);
                return;
            }

            AudioManager.PlaySound("misc_menu", pitchShift: 1.15f);

            AnimationManager.PlayAnimation(submitButton.GetComponent<Animator>(), new BumpAnimation(0.1f, 0.9f));

            // Save selected and non-selected colors
            var selectedBatchColors = new List<ColorObject>();
            var nonSelectedBatchColors = new List<ColorObject>();

            for (var i = 0; i < _selectionGridButtons.Length; i++)
            {
                var color = _currentBatch[i];
                if (_buttonToggledStates[i])
                {
                    selectedBatchColors.Add(color);
                }
                else
                {
                    nonSelectedBatchColors.Add(color);
                }
            }

            (_currentBatch, _hasMoreBatches) = _colorExperiment.GetNextColorBatch(selectedBatchColors, nonSelectedBatchColors);
            _submitCount++;

            StartCoroutine(AnimateEmojisAndResetButtons());
            UpdateProgressBar();
            UpdateTargetButtonColor();

            if (!_hasMoreBatches)
            {
                _targetReached = true;
                foreach (var button in _selectionButtons)
                {
                    button.SetActive(false);
                }

                // Set the target emoji to the reward emoji
                var emoji = EmojiManager.GetRewardEmojiForColor(_targetColor);
                _targetEmojiImage.sprite = emoji;
                LoggingManager.LogEvent(new EmojiRewardedEvent(emoji.name));
            }
            else
            {
                StartCoroutine(ShowHappyEmojiCoroutine());
            }
        }

        private IEnumerator AnimateEmojisAndResetButtons()
        {
            _buttonsInteractable = false;
            var instantiatedObjects = new List<GameObject>();
            var emojiAnimators = new List<EmojiAnimator>();

            for (var i = 0; i < _selectionGridButtons.Length; i++)
            {
                var instance = CreateEmojiInstance(i);
                instantiatedObjects.Add(instance);
                emojiAnimators.Add(instance.AddComponent<EmojiAnimator>());
            }

            SetOriginalButtonsInactive();
            yield return AnimateEmojis(emojiAnimators);

            foreach (var obj in instantiatedObjects)
            {
                Destroy(obj);
            }

            ResetButtons();
            yield return StartCoroutine(FadeInButtons());
        }

        private GameObject CreateEmojiInstance(int index)
        {
            var button = _selectionGridButtons[index].gameObject;
            var prefab = Resources.Load<GameObject>("Colorcrush/Prefabs/ColorPickButton");
            var instance = Instantiate(prefab, button.transform.position, button.transform.rotation, uiCanvas.transform);
            instance.transform.SetAsLastSibling();

            var buttonImage = button.GetComponent<Image>();
            var instanceImage = instance.GetComponent<Image>();
            if (buttonImage != null && instanceImage != null)
            {
                instanceImage.sprite = buttonImage.sprite;
                instanceImage.material = new Material(buttonImage.material);
                instance.transform.localScale = button.transform.localScale;
                var color = buttonImage.color;
                instanceImage.color = new Color(color.r, color.g, color.b, _buttonToggledStates[index] ? toggledAlpha : defaultAlpha);
                ShaderManager.SetFloat(instanceImage.gameObject, "_SkinColorMode", 0);
            }

            return instance;
        }

        private void SetOriginalButtonsInactive()
        {
            foreach (var button in _selectionGridButtons)
            {
                var image = button.GetComponent<Image>();
                ShaderManager.SetFloat(image.gameObject, "_Alpha", 0f);
                button.interactable = false;
            }
        }

        private IEnumerator AnimateEmojis(List<EmojiAnimator> emojiAnimators)
        {
            var nonSelectedCount = emojiAnimators.Count(i => !_buttonToggledStates[emojiAnimators.IndexOf(i)]);
            var pitchStep = nonSelectedCount > 1 ? nonSelectedEmojiPitchStep / (nonSelectedCount - 1) : 0f;
            var currentPitch = nonSelectedEmojiBasePitch;

            for (var i = 0; i < emojiAnimators.Count; i++)
            {
                if (_buttonToggledStates[i])
                {
                    AnimateSelectedEmoji(emojiAnimators[i]);
                }
                else
                {
                    yield return AnimateNonSelectedEmoji(emojiAnimators[i], currentPitch);
                    currentPitch += pitchStep;
                }
            }

            yield return new WaitForSeconds(1f);
        }

        private void AnimateSelectedEmoji(EmojiAnimator animator)
        {
            var image = animator.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = EmojiManager.GetNextSadEmoji();
            }

            AnimationManager.PlayAnimation(animator, new FadeAnimation(toggledAlpha, 0f, selectedEmojiFadeDuration));
        }

        private IEnumerator AnimateNonSelectedEmoji(EmojiAnimator animator, float pitch)
        {
            var image = animator.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = EmojiManager.GetNextHappyEmoji();
            }

            var targetPosition = progressBar.transform.position + nonSelectedEmojiMoveOffset;
            AnimationManager.PlayAnimation(animator, new MoveToAnimation(targetPosition, nonSelectedEmojiMoveDuration, Vector3.zero));
            yield return new WaitForSeconds(0.05f);
            AudioManager.PlaySound(nonSelectedEmojiSound, pitchShift: pitch, gain: nonSelectedEmojiGain);
        }

        private void ResetButtons()
        {
            if (!_hasMoreBatches)
            {
                return;
            }

            for (var i = 0; i < _selectionGridButtons.Length; i++)
            {
                UpdateButton(i, true);
                if (_buttonToggledStates[i])
                {
                    _buttonToggledStates[i] = false;
                    _selectionGridButtons[i].transform.localScale = _originalButtonScales[i];
                }
            }
        }

        private IEnumerator FadeInButtons()
        {
            foreach (var button in _selectionGridButtons)
            {
                button.interactable = false;
            }

            for (var i = 0; i < _selectionGridButtons.Length; i++)
            {
                var buttonAnimator = _selectionButtons[i].GetComponent<EmojiAnimator>();
                AnimationManager.PlayAnimation(buttonAnimator, new FadeAnimation(0f, defaultAlpha, setupAnimationDuration));
                yield return new WaitForSeconds(buttonFadeInDelay);
            }

            yield return new WaitForSeconds(setupAnimationDuration);

            foreach (var button in _selectionGridButtons)
            {
                button.interactable = true;
            }

            _buttonsInteractable = true;
        }

        private IEnumerator ShowHappyEmojiCoroutine()
        {
            ShaderManager.SetFloat(_targetEmojiImage.gameObject, "_SkinColorMode", 0);
            _targetEmojiImage.sprite = EmojiManager.GetNextHappyEmoji();
            LoggingManager.LogEvent(new ColorsSubmittedEvent(EmojiManager.GetNextHappyEmoji().name));
            yield return new WaitForSeconds(1f);
            ShaderManager.SetFloat(_targetEmojiImage.gameObject, "_SkinColorMode", ProjectConfig.InstanceConfig.useSkinColorMode ? 1 : 0);
            _targetEmojiImage.sprite = EmojiManager.GetDefaultEmoji();
        }

        private void UpdateProgressBar()
        {
            if (progressBar != null)
            {
                var progress = Mathf.Min((float)_submitCount / _colorExperiment.GetTotalBatches(), 1f);
                var newWidth = _initialProgressBarWidth * progress;
                progressBar.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);
            }
        }

        private void UpdateTargetButtonColor()
        {
            if (_targetEmojiImage != null && _targetEmojiImage.material != null)
            {
                ShaderManager.SetColor(_targetEmojiImage.gameObject, "_TargetColor", _targetColor);
            }
        }

        private Color GetNextColor(int buttonIndex)
        {
            return _currentBatch[buttonIndex].ToDisplayColor();
        }

        private enum GameState
        {
            Setup,
            Main,
            Teardown,
        }
    }
}