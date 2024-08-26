// Copyright (C) 2024 Peter Guld Leth

#region

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

        private float _adjustedWidth;
        private Material _colorAnalysisMaterial;
        private readonly float[] _currentAxisValues = new float[8];
        private Color _currentFillColor = Color.clear;
        private float _originalWidth;
        private float _scrollableWidth;
        private RectTransform _scrollbarRectTransform;
        private int _selectedLevelIndex = -1;
        private Coroutine _shakeCoroutine;

        private void Awake()
        {
            ScrollToButtonIndex(0);
            InitializeScrollBarEffect();
            SetSelectedLevel();
            SetupButtons();
            UpdateSubmitButton();
            InitializeColorAnalysis();
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

            int cellsPerRow = buttonGrid.constraintCount;
            float cellSize = buttonGrid.cellSize.x;
            float cellSpacing = buttonGrid.spacing.x;

            int totalCells = buttonGrid.transform.childCount;
            int totalColumns = Mathf.CeilToInt((float)totalCells / cellsPerRow);

            float viewportWidth = scrollView.viewport.rect.width;
            float contentWidth = totalColumns * (cellSize + cellSpacing) - cellSpacing;
            float visibleWidth = visibleColumns * (cellSize + cellSpacing) - cellSpacing;

            int clickedColumn = index / cellsPerRow;
            int currentLeftmostColumn = Mathf.FloorToInt(scrollView.horizontalNormalizedPosition * (contentWidth - viewportWidth) / (cellSize + cellSpacing));

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

            float normalizedPosition = (targetColumn * (cellSize + cellSpacing)) / (contentWidth - visibleWidth);
            normalizedPosition = Mathf.Clamp01(normalizedPosition);

            StartCoroutine(SmoothScrollTo(normalizedPosition));

            Debug.Log($"Scrolling to column {targetColumn}, normalized position: {normalizedPosition}");
        }

        private IEnumerator SmoothScrollTo(float targetNormalizedPosition)
        {
            float startPosition = scrollView.horizontalNormalizedPosition;
            float elapsedTime = 0f;

            while (elapsedTime < scrollDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / scrollDuration;
                
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
        }

        private void SetSelectedLevel()
        {
            var completedColors = ProgressManager.CompletedTargetColors;
            var uniqueCompletedColors = new HashSet<string>(completedColors);
            _selectedLevelIndex = uniqueCompletedColors.Count;
            UpdateColorAnalysis();

            // Scale the initially selected button
            if (_selectedLevelIndex < buttonGrid.transform.childCount)
            {
                var buttonTransform = buttonGrid.transform.GetChild(_selectedLevelIndex);
                ScaleButton(buttonTransform, Vector3.one * selectedButtonScale);
                ScrollToButtonIndex(_selectedLevelIndex);
            }
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
            var uniqueCompletedColors = new HashSet<string>(completedColors);
            var nextColorIndex = uniqueCompletedColors.Count;

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
                    if (uniqueCompletedColors.Contains(targetColorHex))
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

                    // Start shake animation for the button representing the color level that is yet to be completed
                    if (i == nextColorIndex && _shakeCoroutine == null)
                    {
                        _shakeCoroutine = StartCoroutine(ShakeButtonPeriodically(buttons[i], i));
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
        }

        private void OnButtonClicked(int index)
        {
            Debug.Log($"Button clicked at index: {index}");

            // Scale back the previously selected button, if any
            if (_selectedLevelIndex != -1 && _selectedLevelIndex < buttonGrid.transform.childCount)
            {
                ScaleButton(buttonGrid.transform.GetChild(_selectedLevelIndex), Vector3.one);
            }

            _selectedLevelIndex = index;

            // Scale down the newly selected button
            if (index < buttonGrid.transform.childCount)
            {
                var buttonTransform = buttonGrid.transform.GetChild(index);
                var animator = buttonTransform.GetComponent<Animator>();
                if (animator != null)
                {
                    // Remove existing animations before applying new ones
                    AnimationManager.RemoveExistingAnimations(animator);
                    ScaleButton(buttonTransform, Vector3.one * selectedButtonScale);
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

        private void ScaleButton(Transform buttonTransform, Vector3 targetScale)
        {
            var animator = buttonTransform.GetComponent<Animator>();
            if (animator != null)
            {
                var scaleAnimation = new ScaleAnimation(targetScale, buttonBumpDuration);
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
                var uniqueCompletedColors = new HashSet<string>(ProgressManager.CompletedTargetColors);
                var isNewLevel = _selectedLevelIndex == uniqueCompletedColors.Count;

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

                submitButton.interactable = _selectedLevelIndex != -1 && _selectedLevelIndex <= uniqueCompletedColors.Count;
            }
        }

        private void OnSubmitButtonClicked()
        {
            var uniqueCompletedColors = new HashSet<string>(ProgressManager.CompletedTargetColors);
            if (_selectedLevelIndex != -1 && _selectedLevelIndex <= uniqueCompletedColors.Count && _selectedLevelIndex < ColorArray.SRGBTargetColors.Length)
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
                StartCoroutine(LoadSceneAfterDelay(1f));
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
            var uniqueCompletedColors = new HashSet<string>(ProgressManager.CompletedTargetColors);

            if (_selectedLevelIndex == uniqueCompletedColors.Count)
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

        private IEnumerator ShakeButtonPeriodically(Button button, int buttonIndex)
        {
            while (true)
            {
                yield return new WaitForSeconds(buttonShakeInterval);
                if (button != null && buttonIndex != _selectedLevelIndex)
                {
                    var animator = button.GetComponent<Animator>();
                    if (animator != null)
                    {
                        var shakeAnimation = new ShakeAnimation(buttonShakeDuration, buttonShakeStrength);
                        var bumpAnimation = new BumpAnimation(buttonBumpDuration, buttonBumpScaleFactor);
                        AnimationManager.PlayAnimation(animator, shakeAnimation);
                        AnimationManager.PlayAnimation(animator, bumpAnimation);
                    }
                }
            }
        }
    }
}