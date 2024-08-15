// Copyright (C) 2024 Peter Guld Leth

#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Colorcrush.Animation;
using Colorcrush.Colorspace;
using Colorcrush.Logging;
using Colorcrush.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Animator = Colorcrush.Animation.Animator;

#endregion

namespace Colorcrush.Game
{
    public class GameSceneController : MonoBehaviour
    {
        private const float ShrinkFactor = 0.9f;
        private const float ToggledAlpha = 0.5f;
        private const float DefaultAlpha = 1f;
        private const int TargetSubmitCount = 5;
        private const float SetupScaleFactor = 1.2f;
        private const float SetupAnimationDuration = 0.5f;
        private const float ButtonFadeInDelay = 0.025f;

        [Header("UI Elements")]
        [Tooltip("Text component for the submit button")] [SerializeField]
        private TextMeshProUGUI submitButtonText;

        [Tooltip("Image component for the progress bar")] [SerializeField]
        private Image progressBar;

        [Tooltip("Button component for submitting")] [SerializeField]
        private Button submitButton;

        [Tooltip("Canvas containing the UI elements")] [SerializeField]
        private Canvas uiCanvas;

        [Tooltip("Image component for displaying the target emoji")] [SerializeField]
        private Image targetEmojiImage;

        [Header("Scene Management")]
        [Tooltip("Name of the scene to load after completing the game")] [SerializeField]
        private string nextSceneName = "MuralScene";

        private bool _buttonsInteractable = true;
        private bool[] _buttonToggledStates;
        private GameState _currentState = GameState.Setup;
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
                _targetColor = ColorManager.GetCurrentTargetColor();
                Debug.LogWarning($"Target color not found in PlayerPrefs. Using ColorManager's current target color for debugging: {ColorUtility.ToHtmlStringRGBA(_targetColor)}");
#else
                throw new Exception("Target color not found in PlayerPrefs.");
#endif
            }
            else
            {
                if (!ColorUtility.TryParseHtmlString(targetColorHex, out _targetColor))
                {
                    throw new Exception($"Failed to parse target color '{targetColorHex}' from PlayerPrefs.");
                }
            }

            LoggingManager.LogEvent(new GameLevelBeginEvent(_targetColor));

            InitializeComponents();
            InitializeButtons();
            InitializeProgressBar();
            UpdateUI();
            StartCoroutine(GameLoop());
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
                ShaderManager.SetColor(_targetEmojiImage.material, "_TargetColor", _targetColor);
                ShaderManager.SetFloat(_targetEmojiImage.material, "_Alpha", 1f);
            }
            else
            {
                Debug.LogError("Target emoji image or its material is null. Unable to set target color and alpha.");
            }

            _currentState = GameState.Setup;
            _targetEmojiImage.transform.localScale = _originalTargetScale * SetupScaleFactor;

            AudioManager.PlaySound("MENU B_Back");

            // Set grid buttons' opacity to 0 and activate them
            foreach (var button in _selectionGridButtons)
            {
                var image = button.GetComponent<Image>();
                if (image != null && image.material != null)
                {
                    ShaderManager.SetFloat(image.material, "_Alpha", 0f);
                }

                button.gameObject.SetActive(true);
            }

            // Hide submit button
            var submitButtonAnimator = submitButton.GetComponent<Animator>();
            submitButtonAnimator.SetOpacity(0f);
            submitButton.interactable = false;

            // Scale down target
            AnimationManager.PlayAnimation(_targetEmojiAnimator, new ScaleAnimation(_originalTargetScale, SetupAnimationDuration));

            yield return new WaitForSeconds(SetupAnimationDuration / 2); // Start fading in buttons halfway through target scaling

            // Fade in grid buttons with staggered delay
            for (var i = 0; i < _selectionGridButtons.Length; i++)
            {
                var buttonAnimator = _selectionButtons[i].GetComponent<EmojiAnimator>();
                AnimationManager.PlayAnimation(buttonAnimator, new FadeAnimation(0f, DefaultAlpha, 0.5f));
                yield return new WaitForSeconds(ButtonFadeInDelay);
            }

            // Wait for all grid buttons to finish fading in
            yield return new WaitForSeconds(0.25f);

            // Fade in submit button
            submitButton.gameObject.SetActive(true);
            AnimationManager.PlayAnimation(submitButtonAnimator, new FadeAnimation(0f, DefaultAlpha, SetupAnimationDuration / 2));

            yield return new WaitForSeconds(SetupAnimationDuration / 2);

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
            LoggingManager.LogEvent(new GameLevelEndEvent());

            _currentState = GameState.Teardown;
            _buttonsInteractable = false;

            // Fade out grid and submit button
            foreach (var button in _selectionGridButtons)
            {
                var buttonAnimator = button.GetComponent<Animator>();
                AnimationManager.PlayAnimation(buttonAnimator, new FadeAnimation(DefaultAlpha, 0f, SetupAnimationDuration / 2));
            }

            yield return new WaitForSeconds(1f);

            var sceneIsLoaded = false;
            SceneManager.LoadSceneAsync(nextSceneName, () => sceneIsLoaded = true);

            var submitAnimator = submitButton.GetComponent<Animator>();
            AnimationManager.PlayAnimation(submitAnimator, new FadeAnimation(DefaultAlpha, 0f, SetupAnimationDuration / 2));

            // Wait for an additional second before scaling up the target
            yield return new WaitForSeconds(SetupAnimationDuration / 2);

            // Scale up target
            AnimationManager.PlayAnimation(_targetEmojiAnimator, new ScaleAnimation(_originalTargetScale * SetupScaleFactor, SetupAnimationDuration));
            AudioManager.PlaySound("MENU B_Select");

            yield return new WaitForSeconds(SetupAnimationDuration);

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
            var nextColor = ColorManager.GetNextColor();
            ShaderManager.SetColor(_selectionGridImages[index].material, "_TargetColor", nextColor);
            if (!ignoreAlpha)
            {
                ShaderManager.SetFloat(_selectionGridImages[index].material, "_Alpha", DefaultAlpha);
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
            var alpha = _buttonToggledStates[index] ? ToggledAlpha : DefaultAlpha;
            ShaderManager.SetFloat(_selectionGridImages[index].material, "_Alpha", alpha);

            var targetScale = _buttonToggledStates[index] ? _originalButtonScales[index] * ShrinkFactor : _originalButtonScales[index];
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

            Debug.Log("Submit button clicked");

            AnimationManager.PlayAnimation(submitButton.GetComponent<Animator>(), new BumpAnimation(0.1f, 0.9f));

            _submitCount++;
            StartCoroutine(AnimateEmojisAndResetButtons());
            UpdateProgressBar();
            UpdateTargetButtonColor();
            StartCoroutine(ShowHappyEmojiCoroutine());

            if (_submitCount >= TargetSubmitCount)
            {
                _targetReached = true;
                foreach (var button in _selectionButtons)
                {
                    button.SetActive(false);
                }

                // Set the target emoji to the default happy emoji
                _targetEmojiImage.sprite = EmojiManager.GetDefaultHappyEmoji();
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
                instanceImage.color = new Color(color.r, color.g, color.b, _buttonToggledStates[index] ? ToggledAlpha : DefaultAlpha);
            }

            return instance;
        }

        private void SetOriginalButtonsInactive()
        {
            foreach (var button in _selectionGridButtons)
            {
                var image = button.GetComponent<Image>();
                ShaderManager.SetFloat(image.material, "_Alpha", 0f);
                button.interactable = false;
            }
        }

        private IEnumerator AnimateEmojis(List<EmojiAnimator> emojiAnimators)
        {
            var nonSelectedCount = emojiAnimators.Count(i => !_buttonToggledStates[emojiAnimators.IndexOf(i)]);
            var pitchStep = nonSelectedCount > 1 ? 5f / (nonSelectedCount - 1) : 0f;
            var currentPitch = 3f;

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

            AnimationManager.PlayAnimation(animator, new FadeAnimation(ToggledAlpha, 0f, 0.5f));
        }

        private IEnumerator AnimateNonSelectedEmoji(EmojiAnimator animator, float pitch)
        {
            var image = animator.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = EmojiManager.GetNextHappyEmoji();
            }

            var targetPosition = progressBar.transform.position;
            AnimationManager.PlayAnimation(animator, new MoveToAnimation(targetPosition, 0.5f, Vector3.zero));
            yield return new WaitForSeconds(0.05f);
            AudioManager.PlaySound("misc_menu", pitchShift: pitch);
        }

        private void ResetButtons()
        {
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
                AnimationManager.PlayAnimation(buttonAnimator, new FadeAnimation(0f, DefaultAlpha, 0.5f));
                yield return new WaitForSeconds(0.025f);
            }

            yield return new WaitForSeconds(0.5f);

            foreach (var button in _selectionGridButtons)
            {
                button.interactable = true;
            }

            _buttonsInteractable = true;
        }

        private IEnumerator ShowHappyEmojiCoroutine()
        {
            _targetEmojiImage.sprite = EmojiManager.GetNextHappyEmoji();
            LoggingManager.LogEvent(new ColorsSubmittedEvent(_targetEmojiImage.sprite.name));
            yield return new WaitForSeconds(1f);
            if (!_targetReached)
            {
                _targetEmojiImage.sprite = EmojiManager.GetDefaultEmoji();
            }
        }

        private void UpdateProgressBar()
        {
            if (progressBar != null)
            {
                var progress = Mathf.Min((float)_submitCount / TargetSubmitCount, 1f);
                var newWidth = _initialProgressBarWidth * progress;
                progressBar.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);
            }
        }

        private void UpdateTargetButtonColor()
        {
            if (_targetEmojiImage != null && _targetEmojiImage.material != null)
            {
                ShaderManager.SetColor(_targetEmojiImage.material, "_TargetColor", _targetColor);
            }
        }

        private enum GameState
        {
            Setup,
            Main,
            Teardown,
        }
    }
}