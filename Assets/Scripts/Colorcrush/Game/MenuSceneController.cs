// Copyright (C) 2024 Peter Guld Leth

#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Colorcrush.Animation;
using Colorcrush.Util;
using UnityEngine;
using UnityEngine.UI;
using Animator = Colorcrush.Animation.Animator;

#endregion

namespace Colorcrush.Game
{
    public class MenuSceneController : MonoBehaviour
    {
        [Header("Scroll View Settings")]
        [SerializeField] [Tooltip("The ScrollRect component that will be reset to the beginning position when the scene loads.")]
        private ScrollRect scrollViewToReset;

        [SerializeField] [Tooltip("The Image component representing the scrollbar of the scroll view.")]
        private Image scrollbarImage;

        [SerializeField] [Tooltip("The main ScrollRect component that handles the scrolling functionality of the view.")]
        private ScrollRect scrollView;

        [SerializeField] [Range(0.1f, 0.9f)] [Tooltip("The size ratio of the scrollbar handle relative to the scroll view's content size. Value ranges from 0.1 (small) to 0.9 (large).")]
        private float scrollbarSizeRatio = 0.5f;

        [SerializeField] [Range(1, 10)] [Tooltip("The number of visible columns in the scroll view. This determines how many columns of items are displayed at once.")]
        private int visibleColumns = 4;

        [SerializeField] [Range(0.1f, 2f)] [Tooltip("The duration of the smooth scroll animation in seconds. This controls how long it takes for the scroll view to smoothly transition to a new position.")]
        private float scrollDuration = 0.5f;

        [SerializeField] [Tooltip("The easing function to use for smooth scrolling. This curve defines the acceleration and deceleration of the scroll animation, providing a more natural movement.")]
        private AnimationCurve scrollEasingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Button Grid Settings")]
        [SerializeField] [Tooltip("The GridLayoutGroup component that contains and arranges the buttons in a grid layout.")]
        private GridLayoutGroup buttonGrid;

        [SerializeField] [Tooltip("The button that submits the player's selection.")]
        private Button submitButton;

        [SerializeField] [Tooltip("The color of the submit button when a new level is selected.")]
        private Color newLevelColor = Color.green;

        [SerializeField] [Tooltip("The accent color of the submit button when a new level is selected.")]
        private Color newLevelAccentColor = Color.white;

        [SerializeField] [Tooltip("The color of the submit button when a completed level is selected.")]
        private Color completedLevelColor = Color.red;

        [Header("Color Analysis Settings")]
        [SerializeField] [Tooltip("The Image component that uses the RadarChartShader material for displaying color analysis.")]
        private Image colorAnalysisImage;

        [SerializeField] [Tooltip("The duration of the animation for the color analysis transition.")]
        private float colorAnalysisAnimationDuration = 0.25f;

        [SerializeField] [Tooltip("The delay between staggered animations of different color analysis axes.")]
        private float colorAnalysisStaggerDelay = 0.02f;

        [Header("Button Animation Settings")]
        [SerializeField] [Tooltip("The scale factor applied to a button when it is selected. A value less than 1 will shrink the button.")]
        private float selectedButtonScale = 0.8f;

        [SerializeField] [Tooltip("The duration of the shake animation applied to buttons.")]
        private float buttonShakeDuration = 0.75f;

        [SerializeField] [Tooltip("The strength of the shake animation applied to buttons.")]
        private float buttonShakeStrength = 10f;

        [SerializeField] [Tooltip("The duration of the bump animation applied to buttons.")]
        private float buttonBumpDuration = 0.1f;

        [SerializeField] [Tooltip("The scale factor applied during the bump animation of buttons. A value greater than 1 will enlarge the button temporarily.")]
        private float buttonBumpScaleFactor = 1.05f;

        [SerializeField] [Tooltip("The interval in seconds between periodic shake animations for buttons.")]
        private float buttonShakeInterval = 5f;

        [SerializeField] [Tooltip("The duration of the bump animation applied to the submit button when a menu button is clicked.")]
        private float submitButtonBumpDuration = 0.05f;

        [SerializeField] [Tooltip("The scale factor applied during the bump animation of the submit button when a menu button is clicked.")]
        private float submitButtonBumpScaleFactor = 1.05f;

        [SerializeField] [Tooltip("The duration of the bump animation applied to the submit button when clicked.")]
        private float submitButtonClickBumpDuration = 0.1f;

        [SerializeField] [Tooltip("The scale factor applied during the bump animation of the submit button when clicked.")]
        private float submitButtonClickBumpScaleFactor = 0.9f;

        [SerializeField] [Tooltip("The ColorView prefab to be instantiated when the color analysis image is pressed and held.")]
        private GameObject colorViewPrefab;

        [SerializeField] [Tooltip("The Canvas that contains the UI elements.")]
        private Canvas uiCanvas;

        private readonly float[] _currentAxisValues = new float[8];

        private float _adjustedWidth;
        private Material _colorAnalysisMaterial;
        private Color _currentFillColor = Color.clear;
        private float _originalWidth;
        private float _scrollableWidth;
        private RectTransform _scrollbarRectTransform;
        private int _selectedLevelIndex = -1;
        private Coroutine _shakeCoroutine;
        private Coroutine _smoothScrollCoroutine;
        private HashSet<string> _uniqueCompletedColors;
        private GameObject _colorViewInstance;
        private bool _isDraggingColorAnalysisImage;
        private Vector2 _colorAnalysisOriginalPosition;

        private void Awake()
        {
            _uniqueCompletedColors = new HashSet<string>(ProgressManager.CompletedTargetColors);
            ScrollToButtonIndex(0);
            InitializeScrollBarEffect();
            SetupButtons();
            UpdateSubmitButton();
            InitializeColorAnalysis();
            
            if (colorAnalysisImage != null)
            {
                _colorAnalysisOriginalPosition = colorAnalysisImage.rectTransform.anchoredPosition;
            }
        }

        private void OnDestroy()
        {
            if (scrollView != null)
            {
                scrollView.onValueChanged.RemoveListener(OnScrollValueChanged);
            }

            if (_shakeCoroutine != null)
            {
                StopCoroutine(_shakeCoroutine);
            }

            if (_smoothScrollCoroutine != null)
            {
                StopCoroutine(_smoothScrollCoroutine);
            }
        }

        private void Update()
        {
            HandleColorAnalysisImageDrag();
        }

        private void HandleColorAnalysisImageDrag()
        {
            if (colorAnalysisImage == null || colorViewPrefab == null || uiCanvas == null)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(colorAnalysisImage.rectTransform, Input.mousePosition, uiCanvas.worldCamera))
                {
                    _isDraggingColorAnalysisImage = true;
                    colorAnalysisImage.rectTransform.anchoredPosition = _colorAnalysisOriginalPosition + new Vector2(0, -800);

                    if (_colorViewInstance == null)
                    {
                        _colorViewInstance = Instantiate(colorViewPrefab, uiCanvas.transform);
                    }
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                _isDraggingColorAnalysisImage = false;
                if (_colorViewInstance != null)
                {
                    Destroy(_colorViewInstance);
                    _colorViewInstance = null;
                }
                colorAnalysisImage.rectTransform.anchoredPosition = _colorAnalysisOriginalPosition;
            }

            if (_isDraggingColorAnalysisImage && _colorViewInstance != null)
            {
                Vector2 localPoint;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(uiCanvas.transform as RectTransform, Input.mousePosition, uiCanvas.worldCamera, out localPoint))
                {
                    _colorViewInstance.transform.localPosition = localPoint + new Vector2(0, 0);
                }
            }
        }

        private void ScrollToButtonIndex(int index)
        {
            if (scrollView == null || buttonGrid == null)
            {
                Debug.LogError("ScrollView or ButtonGrid not assigned.");
                return;
            }

            if (index < 0 || index >= buttonGrid.transform.childCount)
            {
                Debug.LogError($"Invalid button index: {index}");
                return;
            }

            var cellsPerRow = buttonGrid.constraintCount;
            var cellSize = buttonGrid.cellSize.x;
            var cellSpacing = buttonGrid.spacing.x;

            var totalCells = buttonGrid.transform.childCount;
            var totalColumns = Mathf.CeilToInt((float)totalCells / cellsPerRow);

            var viewportWidth = scrollView.viewport.rect.width;
            var contentWidth = totalColumns * (cellSize + cellSpacing) - cellSpacing;
            var visibleWidth = visibleColumns * (cellSize + cellSpacing) - cellSpacing;

            var clickedColumn = index / cellsPerRow;
            var currentLeftmostColumn = Mathf.FloorToInt(scrollView.horizontalNormalizedPosition * (contentWidth - viewportWidth) / (cellSize + cellSpacing));

            int targetColumn;
            if (clickedColumn == currentLeftmostColumn) // Clicked leftmost visible column
            {
                targetColumn = Mathf.Max(0, currentLeftmostColumn - 2);
            }
            else if (clickedColumn == currentLeftmostColumn + visibleColumns - 1) // Clicked rightmost visible column
            {
                targetColumn = Mathf.Min(totalColumns - visibleColumns, currentLeftmostColumn + 2);
            }
            else // Clicked a middle column or outside visible area
            {
                targetColumn = Mathf.Clamp(clickedColumn - 1, 0, totalColumns - visibleColumns);
            }

            var normalizedPosition = targetColumn * (cellSize + cellSpacing) / (contentWidth - visibleWidth);
            normalizedPosition = Mathf.Clamp01(normalizedPosition);

            StartCoroutine(SmoothScrollTo(normalizedPosition));

            Debug.Log($"Scrolling to column {targetColumn}, normalized position: {normalizedPosition}");
        }

        private IEnumerator SmoothScrollTo(float targetNormalizedPosition)
        {
            var startPosition = scrollView.horizontalNormalizedPosition;
            var elapsedTime = 0f;
            var initialInteractionEnded = false;

            // Wait for the initial click/touch to end
            yield return new WaitUntil(() => !Input.GetMouseButton(0) && Input.touchCount == 0);

            while (elapsedTime < scrollDuration)
            {
                if (!initialInteractionEnded)
                {
                    // Check if the initial interaction has truly ended
                    if (Input.GetMouseButtonDown(0) || Input.touchCount > 0)
                    {
                        initialInteractionEnded = true;
                    }
                }
                else if (Input.GetMouseButton(0) || Input.touchCount > 0)
                {
                    // Subsequent user interaction detected, stop smooth scrolling
                    yield break;
                }

                elapsedTime += Time.deltaTime;
                var t = elapsedTime / scrollDuration;

                // Apply custom easing curve
                t = scrollEasingCurve.Evaluate(t);

                scrollView.horizontalNormalizedPosition = Mathf.Lerp(startPosition, targetNormalizedPosition, t);
                yield return null;
            }

            // Ensure we end exactly at the target position
            scrollView.horizontalNormalizedPosition = targetNormalizedPosition;

            // Force the scroll view to update and stop any residual velocity
            Canvas.ForceUpdateCanvases();
            scrollView.velocity = Vector2.zero;
        }

        private void InitializeScrollBarEffect()
        {
            if (scrollbarImage == null || scrollView == null)
            {
                Debug.LogError("Scrollbar Image or ScrollView not assigned in ScrollBarEffect.");
                return;
            }

            _scrollbarRectTransform = scrollbarImage.rectTransform;
            _originalWidth = _scrollbarRectTransform.rect.width;

            // Calculate the scrollable width
            _scrollableWidth = scrollView.content.rect.width - scrollView.viewport.rect.width;
            if (_scrollableWidth <= 0)
            {
                // Content fits within the viewport, no need for scrolling
                return;
            }

            _adjustedWidth = _originalWidth * scrollbarSizeRatio;
            _scrollbarRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _adjustedWidth);

            // Add listener for scroll value changes
            scrollView.onValueChanged.AddListener(OnScrollValueChanged);

            // Initial position update
            UpdateScrollbarPosition(scrollView.normalizedPosition);
        }

        private void OnScrollValueChanged(Vector2 scrollPosition)
        {
            UpdateScrollbarPosition(scrollPosition);
            if (_smoothScrollCoroutine != null)
            {
                StopCoroutine(_smoothScrollCoroutine);
                _smoothScrollCoroutine = null;
            }
        }

        private void UpdateScrollbarPosition(Vector2 scrollPosition)
        {
            // Calculate the new position of the scrollbar
            var scrollPercentage = scrollPosition.x;
            var maxScrollDistance = _originalWidth - _adjustedWidth;

            // Adjust the position to account for the reduced size of the scrollbar
            var leftmostPosition = -maxScrollDistance / 2;
            var rightmostPosition = maxScrollDistance / 2;

            var newXPosition = Mathf.Lerp(leftmostPosition, rightmostPosition, scrollPercentage);

            // Update the scrollbar position
            var localPosition = _scrollbarRectTransform.localPosition;
            localPosition.x = newXPosition;
            _scrollbarRectTransform.localPosition = localPosition;
        }

        private void SetupButtons()
        {
            if (buttonGrid == null)
            {
                Debug.LogError("Button grid is not assigned.");
                return;
            }

            var buttons = buttonGrid.GetComponentsInChildren<Button>();
            var completedColors = ProgressManager.CompletedTargetColors;
            var rewardedEmojis = ProgressManager.RewardedEmojis;
            var mostRecentCompletedColor = ProgressManager.MostRecentCompletedTargetColor;
            var nextColorIndex = _uniqueCompletedColors.Count;

            for (var i = 0; i < buttons.Length; i++)
            {
                var buttonImage = buttons[i].GetComponent<Image>();
                if (buttonImage == null || buttonImage.material == null)
                {
                    Debug.LogError($"Button {i} is missing Image component or material.");
                    continue;
                }

                var targetColor = ColorArray.SRGBTargetColors[i];
                var targetColorHex = ColorUtility.ToHtmlStringRGB(targetColor);

                if (i <= nextColorIndex)
                {
                    // Enable button and set color
                    buttons[i].interactable = true;
                    ShaderManager.SetColor(buttonImage.material, "_TargetColor", targetColor);
                    ShaderManager.SetFloat(buttonImage.material, "_Alpha", 1f);
                    var index = i;
                    buttons[i].onClick.AddListener(() => OnButtonClicked(index));

                    // Set the rewarded emoji if available
                    if (_uniqueCompletedColors.Contains(targetColorHex))
                    {
                        var colorIndex = completedColors.IndexOf(targetColorHex);
                        if (colorIndex < rewardedEmojis.Count)
                        {
                            buttonImage.sprite = EmojiManager.GetEmojiByName(rewardedEmojis[colorIndex]);
                        }
                        else
                        {
                            buttonImage.sprite = EmojiManager.GetDefaultEmoji();
                            Debug.LogError($"No rewarded emoji found for color index {colorIndex}. Using default emoji.");
                        }
                    }
                    else
                    {
                        buttonImage.sprite = EmojiManager.GetDefaultEmoji();
                    }

                    buttons[i].transform.localScale = Vector3.one;

                    // Set up animation for the next level button
                    if (i == nextColorIndex)
                    {
                        StartCoroutine(AnimateNextLevelButton(buttons[i]));
                    }
                }
                else
                {
                    // Disable button and set to black with 20% transparency
                    buttons[i].interactable = false;
                    ShaderManager.SetColor(buttonImage.material, "_TargetColor", Color.black);
                    ShaderManager.SetFloat(buttonImage.material, "_Alpha", 0.2f);
                    buttons[i].transform.localScale = Vector3.one * selectedButtonScale;
                }
            }

            if (submitButton != null)
            {
                submitButton.onClick.AddListener(OnSubmitButtonClicked);
            }

            // Select the most recently played level at startup
            if (!string.IsNullOrEmpty(mostRecentCompletedColor))
            {
                var mostRecentIndex = Array.FindIndex(ColorArray.SRGBTargetColors, c => ColorUtility.ToHtmlStringRGB(c) == mostRecentCompletedColor);
                if (mostRecentIndex != -1 && mostRecentIndex < buttons.Length)
                {
                    OnButtonClicked(mostRecentIndex);
                }
            }
        }

        private IEnumerator AnimateNextLevelButton(Button button)
        {
            var animator = button.GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogError("Next level button is missing Animator component.");
                yield break;
            }

            while (true)
            {
                yield return new WaitForSeconds(buttonShakeInterval);

                if (_selectedLevelIndex != _uniqueCompletedColors.Count)
                {
                    var bumpAnimation = new BumpAnimation(buttonBumpDuration, buttonBumpScaleFactor);
                    AnimationManager.PlayAnimation(animator, bumpAnimation);

                    yield return new WaitForSeconds(buttonBumpDuration);

                    var shakeAnimation = new ShakeAnimation(buttonShakeDuration, buttonShakeStrength);
                    AnimationManager.PlayAnimation(animator, shakeAnimation);

                    yield return new WaitForSeconds(buttonShakeDuration);
                }
            }
        }

        private void OnButtonClicked(int index)
        {
            Debug.Log($"Button clicked at index: {index}");

            // Stop the shake coroutine if it's running
            if (_shakeCoroutine != null)
            {
                StopCoroutine(_shakeCoroutine);
                _shakeCoroutine = null;
            }

            // Scale back the previously selected button, if any
            if (_selectedLevelIndex != -1 && _selectedLevelIndex < buttonGrid.transform.childCount)
            {
                var previousButtonTransform = buttonGrid.transform.GetChild(_selectedLevelIndex);
                ScaleButton(previousButtonTransform, 1f);
            }

            _selectedLevelIndex = index;

            // Scale down the newly selected button
            if (index < buttonGrid.transform.childCount)
            {
                var buttonTransform = buttonGrid.transform.GetChild(index);
                var animator = buttonTransform.GetComponent<Animator>();
                if (animator != null)
                {
                    ScaleButton(buttonTransform, selectedButtonScale);
                    ScrollToButtonIndex(index);
                }
                else
                {
                    Debug.LogError("Button is missing Animator component.");
                }
            }

            UpdateSubmitButton();
            UpdateColorAnalysis();

            // Animate the submit button with a bump animation
            if (submitButton != null)
            {
                var submitButtonAnimator = submitButton.GetComponent<Animator>();
                if (submitButtonAnimator != null)
                {
                    var bumpAnimation = new BumpAnimation(submitButtonBumpDuration, submitButtonBumpScaleFactor);
                    AnimationManager.PlayAnimation(submitButtonAnimator, bumpAnimation);
                }
                else
                {
                    Debug.LogError("Submit button is missing Animator component.");
                }
            }
        }

        private void ScaleButton(Transform buttonTransform, float targetScale)
        {
            var animator = buttonTransform.GetComponent<Animator>();
            if (animator != null)
            {
                var scaleAnimation = new FillScaleAnimation(targetScale, buttonBumpDuration);
                AnimationManager.PlayAnimation(animator, scaleAnimation);
            }
            else
            {
                Debug.LogError("Button is missing Animator component.");
            }
        }

        private void UpdateSubmitButton()
        {
            if (submitButton != null)
            {
                var isNewLevel = _selectedLevelIndex == _uniqueCompletedColors.Count;

                var buttonImage = submitButton.GetComponent<Image>();
                if (buttonImage != null && buttonImage.material != null)
                {
                    if (isNewLevel)
                    {
                        ShaderManager.SetColor(buttonImage.material, "_BackgroundColor", newLevelColor);
                        ShaderManager.SetColor(buttonImage.material, "_AccentColor", newLevelAccentColor);
                        ShaderManager.SetFloat(buttonImage.material, "_EffectToggle", 1f);
                    }
                    else
                    {
                        ShaderManager.SetColor(buttonImage.material, "_BackgroundColor", completedLevelColor);
                        ShaderManager.SetFloat(buttonImage.material, "_EffectToggle", 0f);
                    }
                }
                else
                {
                    Debug.LogError("Submit button is missing Image component or material.");
                }

                submitButton.interactable = _selectedLevelIndex != -1 && _selectedLevelIndex <= _uniqueCompletedColors.Count;
            }
        }

        private void OnSubmitButtonClicked()
        {
            if (_selectedLevelIndex != -1 && _selectedLevelIndex <= _uniqueCompletedColors.Count && _selectedLevelIndex < ColorArray.SRGBTargetColors.Length)
            {
                var targetColor = ColorArray.SRGBTargetColors[_selectedLevelIndex];
                PlayerPrefs.SetString("TargetColor", ColorUtility.ToHtmlStringRGB(targetColor));
                PlayerPrefs.Save();

                // Play bump animation
                var animator = submitButton.GetComponent<Animator>();
                if (animator != null)
                {
                    var bumpAnimation = new BumpAnimation(submitButtonClickBumpDuration, submitButtonClickBumpScaleFactor);
                    AnimationManager.PlayAnimation(animator, bumpAnimation);
                }
                else
                {
                    Debug.LogError("Submit button is missing Animator component.");
                }

                // Wait for 1 second before loading the scene
                StartCoroutine(LoadSceneAfterDelay(0.1f));
            }
        }

        private IEnumerator LoadSceneAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            SceneManager.LoadSceneAsync("GameScene");
        }

        private void InitializeColorAnalysis()
        {
            if (colorAnalysisImage == null)
            {
                Debug.LogError("Color analysis image is not assigned.");
                return;
            }

            _colorAnalysisMaterial = colorAnalysisImage.material;
            if (_colorAnalysisMaterial == null)
            {
                Debug.LogError("Color analysis material is not assigned to the image.");
                return;
            }

            UpdateColorAnalysis();
        }

        private void UpdateColorAnalysis()
        {
            if (_selectedLevelIndex == -1 || _colorAnalysisMaterial == null)
            {
                return;
            }

            var targetColor = ColorArray.SRGBTargetColors[_selectedLevelIndex];

            if (_selectedLevelIndex == _uniqueCompletedColors.Count)
            {
                // This is a new, uncompleted level
                var zeroValues = new float[8];
                StartCoroutine(AnimateAxisValuesAndColor(zeroValues, targetColor));
            }
            else
            {
                var selectedColors = new List<Color>();

                if (_selectedLevelIndex < ProgressManager.SelectedColors.Count)
                {
                    selectedColors = ProgressManager.SelectedColors[_selectedLevelIndex]
                        .Select(colorHex => ColorUtility.TryParseHtmlString(colorHex, out var color) ? color : Color.black)
                        .ToList();
                }

                var analysisValues = ColorManager.GenerateColorAnalysis(targetColor, selectedColors);

                StartCoroutine(AnimateAxisValuesAndColor(analysisValues, targetColor));
            }
        }

        private IEnumerator AnimateAxisValuesAndColor(float[] targetValues, Color targetColor)
        {
            var elapsedTime = 0f;

            var startValues = new float[8];
            for (var i = 0; i < 8; i++)
            {
                startValues[i] = _currentAxisValues[i];
            }

            var startColor = _currentFillColor;

            while (elapsedTime < colorAnalysisAnimationDuration + 7 * colorAnalysisStaggerDelay)
            {
                elapsedTime += Time.deltaTime;

                for (var i = 0; i < 8; i++)
                {
                    var axisElapsedTime = elapsedTime - i * colorAnalysisStaggerDelay;
                    if (axisElapsedTime > 0)
                    {
                        var t = Mathf.Clamp01(axisElapsedTime / colorAnalysisAnimationDuration);
                        var easedT = EaseInOutCubic(t);
                        _currentAxisValues[i] = Mathf.Lerp(startValues[i], targetValues[i], easedT);
                        ShaderManager.SetFloat(_colorAnalysisMaterial, $"_Axis{i + 1}", _currentAxisValues[i]);
                    }
                }

                var colorT = Mathf.Clamp01(elapsedTime / colorAnalysisAnimationDuration);
                var easedColorT = EaseInOutCubic(colorT);
                _currentFillColor = Color.Lerp(startColor, new Color(targetColor.r, targetColor.g, targetColor.b, 0.5f), easedColorT);
                ShaderManager.SetColor(_colorAnalysisMaterial, "_FillColor", _currentFillColor);

                yield return null;
            }

            // Ensure final values are set
            for (var i = 0; i < 8; i++)
            {
                _currentAxisValues[i] = targetValues[i];
                ShaderManager.SetFloat(_colorAnalysisMaterial, $"_Axis{i + 1}", _currentAxisValues[i]);
            }

            _currentFillColor = new Color(targetColor.r, targetColor.g, targetColor.b, 0.5f);
            ShaderManager.SetColor(_colorAnalysisMaterial, "_FillColor", _currentFillColor);
        }

        private float EaseInOutCubic(float t)
        {
            return t < 0.5 ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2;
        }
    }
}