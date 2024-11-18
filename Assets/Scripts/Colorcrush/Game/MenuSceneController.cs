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
using static Colorcrush.Game.ColorManager;
using Animator = Colorcrush.Animation.Animator;

#endregion

namespace Colorcrush.Game
{
    public class MenuSceneController : MonoBehaviour
    {
        private const float DoubleTapTime = 0.3f;

        [Header("Scroll View Settings")] [SerializeField] [Tooltip("The ScrollRect component that will be reset to the beginning position when the scene loads.")]
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

        [Header("Button Grid Settings")] [SerializeField] [Tooltip("The GridLayoutGroup component that contains and arranges the buttons in a grid layout.")]
        private GridLayoutGroup buttonGrid;

        [SerializeField] [Tooltip("The button that submits the player's selection.")]
        private Button submitButton;

        [SerializeField] [Tooltip("The color of the submit button when a new level is selected.")]
        private Color newLevelColor = Color.green;

        [SerializeField] [Tooltip("The accent color of the submit button when a new level is selected.")]
        private Color newLevelAccentColor = Color.white;

        [SerializeField] [Tooltip("The color of the submit button when a completed level is selected.")]
        private Color completedLevelColor = Color.red;

        [Header("Color Analysis Settings")] [SerializeField] [Tooltip("Toggle to enable or disable the color view inspector.")]
        private bool enableColorViewInspector = true;

        [SerializeField] [Tooltip("The Image component that uses the RadarChartShader material for displaying color analysis.")]
        private Image colorAnalysisImage;

        [SerializeField] [Tooltip("The duration of the animation for the color analysis transition.")]
        private float colorAnalysisAnimationDuration = 0.25f;

        [SerializeField] [Tooltip("The delay between staggered animations of different color analysis axes.")]
        private float colorAnalysisStaggerDelay = 0.02f;

        [SerializeField] [Tooltip("The ColorView prefab to be instantiated when the color analysis image is pressed and held.")]
        private GameObject colorViewPrefab;

        [SerializeField] [Tooltip("The Canvas that contains the UI elements.")]
        private Canvas uiCanvas;

        [SerializeField] [Tooltip("Preset amount of units for tick sound. The distance the ColorView inspector must be dragged before a tick sound is played.")]
        private float TickDistanceThreshold = 50f;

        [SerializeField] [Tooltip("Minimum interval between tick sounds in seconds. Set this to prevent spamming of the tick sound.")]
        private float MinTickInterval = 0.065f;

        [SerializeField] [Tooltip("Maximum pitch shift value. How much the pitch of the tick sound is shifted up when dragging quickly.")]
        private float MaxPitchShift = 1.5f;

        [SerializeField] [Tooltip("The drag signifier object.")]
        private GameObject dragSignifier;

        [Header("Button Animation Settings")] [SerializeField] [Tooltip("The scale factor applied to a button when it is selected. A value less than 1 will shrink the button.")]
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

        private readonly float[] _currentAxisValues = new float[8];

        private float _adjustedWidth;
        private Material _colorAnalysisMaterial;
        private Vector2 _colorAnalysisOriginalPosition;
        private float _colorAnalysisRadius;
        private GameObject _colorViewInstance;
        private float[] _currentAnalysisValues;
        private Color _currentFillColor = Color.clear;
        private Color _currentTargetColor;
        private float _distanceSinceLastTick;
        private bool _hasColorAnalysisBeenClicked;
        private bool _isDraggingColorAnalysisImage;
        private Vector2 _lastDragPosition;
        private float _lastTapTime;
        private float _lastTickTime;
        private float _originalWidth;
        private float _scrollableWidth;
        private RectTransform _scrollbarRectTransform;
        private int _selectedLevelIndex = -1;
        private Coroutine _shakeCoroutine;
        private Coroutine _smoothScrollCoroutine;

        private int _tapCount;
        private HashSet<string> _uniqueCompletedColors;

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
                // Assuming the image is circular, the radius is half the width or height
                _colorAnalysisRadius = colorAnalysisImage.rectTransform.rect.width / 2;
            }

            SetDragSignifierActive(true);
        }

        private void Update()
        {
            // Handle color view inspector
            if (enableColorViewInspector)
            {
                HandleColorViewInspector();
            }

            // Handle triple tap to toggle useSkinColorMode
            if (Input.GetMouseButtonDown(0) && ProjectConfig.InstanceConfig.enableTripleTapToggleSkinColorMode)
            {
                if (Time.time - _lastTapTime < DoubleTapTime)
                {
                    _tapCount++;
                }
                else
                {
                    _tapCount = 1;
                }

                _lastTapTime = Time.time;

                if (_tapCount == 3)
                {
                    _tapCount = 0;
                    ProjectConfig.InstanceConfig.useSkinColorMode = !ProjectConfig.InstanceConfig.useSkinColorMode;

                    AudioManager.PlaySound("MENU B_Select");

                    // Update the shader on all buttons to reflect the change in useSkinColorMode
                    var buttons = FindObjectsOfType<Button>();
                    foreach (var button in buttons)
                    {
                        var image = button.GetComponent<Image>();
                        if (image != null)
                        {
                            var material = image.material;
                            if (material != null && material.shader.name == "Colorcrush/ColorTransposeShader")
                            {
                                ShaderManager.SetFloat(material, "_SkinColorMode", ProjectConfig.InstanceConfig.useSkinColorMode ? 1.0f : 0.0f);
                            }
                        }
                    }
                }
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

        private void HandleColorViewInspector()
        {
            if (colorAnalysisImage == null || colorViewPrefab == null || uiCanvas == null)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                // Check if the selected button is the most recent one (yet to be completed)
                if (_selectedLevelIndex == _uniqueCompletedColors.Count && !ProjectConfig.InstanceConfig.unlockAllLevelsFromStart)
                {
                    var bumpAnimation = new BumpAnimation(submitButtonBumpDuration, submitButtonBumpScaleFactor);
                    var submitButtonAnimator = submitButton.GetComponent<Animator>();
                    if (submitButtonAnimator != null)
                    {
                        AnimationManager.PlayAnimation(submitButtonAnimator, bumpAnimation);
                    }
                    else
                    {
                        Debug.LogWarning("Submit button is missing Animator component.");
                    }

                    return;
                }

                if (RectTransformUtility.RectangleContainsScreenPoint(colorAnalysisImage.rectTransform, Input.mousePosition, uiCanvas.worldCamera))
                {
                    _isDraggingColorAnalysisImage = true;
                    colorAnalysisImage.rectTransform.anchoredPosition = _colorAnalysisOriginalPosition + new Vector2(0, -800);

                    if (_colorViewInstance == null)
                    {
                        _colorViewInstance = Instantiate(colorViewPrefab, uiCanvas.transform);
                    }

                    // Hide other UI elements
                    SetUIElementsActive(false);

                    AudioManager.PlaySound("MENU B_Select");
                    _lastDragPosition = Input.mousePosition;
                    _distanceSinceLastTick = 0f;
                    _lastTickTime = Time.time;

                    if (ColorUtility.ToHtmlStringRGB(_currentTargetColor) == ProgressManager.MostRecentCompletedTargetColor)
                    {
                        _hasColorAnalysisBeenClicked = true;
                        SetDragSignifierActive(false);
                    }
                }
            }

            if (_isDraggingColorAnalysisImage && Input.GetMouseButtonUp(0))
            {
                _isDraggingColorAnalysisImage = false;
                if (_colorViewInstance != null)
                {
                    Destroy(_colorViewInstance);
                    _colorViewInstance = null;
                }

                colorAnalysisImage.rectTransform.anchoredPosition = _colorAnalysisOriginalPosition;

                // Show other UI elements
                SetUIElementsActive(true);

                AudioManager.PlaySound("MENU B_Back");
            }

            if (_isDraggingColorAnalysisImage && _colorViewInstance != null)
            {
                Vector2 localPoint;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(uiCanvas.transform as RectTransform, Input.mousePosition, uiCanvas.worldCamera, out localPoint))
                {
                    // Calculate the vector from the original center of the analysis image to the mouse position
                    var dragCenter = _colorAnalysisOriginalPosition + new Vector2(0, 1389);
                    var dragVector = localPoint - dragCenter;

                    // Clamp the magnitude of the drag vector to the radius of the analysis image
                    if (dragVector.magnitude > _colorAnalysisRadius)
                    {
                        dragVector = dragVector.normalized * _colorAnalysisRadius;
                    }

                    // Set the position of the ColorView instance relative to the drag center
                    _colorViewInstance.transform.localPosition = dragCenter + dragVector;

                    // Calculate the color for Circle 1 based on the drag position
                    UpdateCircle1Color(dragVector);
                    // Calculate the color for Circle 2 based on the drag position
                    UpdateCircle2Color(dragVector);

                    // Calculate the distance moved since the last tick
                    var currentDragPosition = Input.mousePosition;
                    var distanceMoved = Vector2.Distance(currentDragPosition, _lastDragPosition);
                    _distanceSinceLastTick += distanceMoved;

                    // Check if the distance threshold is met and the minimum interval has passed
                    if (_distanceSinceLastTick >= TickDistanceThreshold && Time.time - _lastTickTime >= MinTickInterval)
                    {
                        // Calculate pitch shift based on speed (logarithmic scaling)
                        var speed = distanceMoved / Time.deltaTime;
                        var pitchShift = Mathf.Lerp(1.0f, MaxPitchShift, Mathf.Log10(speed + 1) / Mathf.Log10(1000 + 1));

                        // Play tick sound with pitch shift
                        AudioManager.PlaySound("click_2", 0.5f, pitchShift);

                        // Reset distance and update last tick time
                        _distanceSinceLastTick = 0f;
                        _lastTickTime = Time.time;
                    }

                    // Update last drag position
                    _lastDragPosition = currentDragPosition;
                }
            }
        }

        private void UpdateCircle1Color(Vector2 dragVector)
        {
            // Get the index from completed colors list that matches current target color
            var targetColorHex = ColorUtility.ToHtmlStringRGB(_currentTargetColor);
            var completedColorIndex = ProgressManager.CompletedTargetColors.IndexOf(targetColorHex);
            var finalColorsResult = ProgressManager.FinalColors[completedColorIndex];

            // Use the knowledge of the center color and the 8 result colors, as well as the magnitude of the vector, to determine the color at the edges of each axis
            var edges = new Color[8];

            for (var i = 0; i < 8; i++)
            {
                var axisEncoding = finalColorsResult.AxisEncodings[i];
                var axisColor = finalColorsResult.FinalColors[i].ToColor();

                var magnitude = axisEncoding.magnitude;

                edges[i] = Color.Lerp(_currentTargetColor, axisColor, 1 / magnitude);
            }

            var angle = Mathf.Atan2(dragVector.y, dragVector.x);
            if (angle < 0)
            {
                angle += 2 * Mathf.PI;
            }

            var normalizedAngle = angle / (2 * Mathf.PI);
            var lowerIndex = Mathf.FloorToInt(normalizedAngle * 8) % 8;
            var upperIndex = (lowerIndex + 1) % 8;

            var lowerWeight = 1 - (normalizedAngle * 8 - lowerIndex);
            var upperWeight = 1 - lowerWeight;

            var lowerColor = edges[lowerIndex];
            var upperColor = edges[upperIndex];

            var interpolatedColor = Color.Lerp(lowerColor, upperColor, upperWeight);
            var finalColor = Color.Lerp(_currentTargetColor, interpolatedColor, dragVector.magnitude / _colorAnalysisRadius);

            var compareView = _colorViewInstance.transform.Find("CompareView");
            if (compareView != null)
            {
                var material = compareView.GetComponent<Image>().material;
                ShaderManager.SetColor(material, "_Circle1Color", finalColor);
            }
        }

        private void UpdateCircle2Color(Vector2 dragVector)
        {
            var finalColor = _currentTargetColor;

            var compareView = _colorViewInstance.transform.Find("CompareView");
            if (compareView != null)
            {
                var material = compareView.GetComponent<Image>().material;
                ShaderManager.SetColor(material, "_Circle2Color", finalColor);
            }
        }

        private void SetUIElementsActive(bool isActive)
        {
            foreach (Transform child in uiCanvas.transform)
            {
                if (child.gameObject != colorAnalysisImage.gameObject && child.gameObject != _colorViewInstance)
                {
                    child.gameObject.SetActive(isActive);
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

                var targetColor = SRGBTargetColors[i];
                var targetColorHex = ColorUtility.ToHtmlStringRGB(targetColor);

                // Check if all levels should be unlocked
                if (ProjectConfig.InstanceConfig.unlockAllLevelsFromStart || i <= nextColorIndex)
                {
                    // Enable button and set color
                    buttons[i].interactable = true;
                    ShaderManager.SetColor(buttonImage.material, "_TargetColor", targetColor);
                    ShaderManager.SetColor(buttonImage.material, "_OriginalColor", targetColor);
                    ShaderManager.SetFloat(buttonImage.material, "_Alpha", 1f);
                    var index = i;
                    buttons[i].onClick.AddListener(() => OnButtonClicked(index));

                    // Set the emoji based on completion status and unlockAllLevelsFromStart setting
                    if (ProjectConfig.InstanceConfig.unlockAllLevelsFromStart)
                    {
                        buttonImage.sprite = EmojiManager.GetDefaultHappyEmoji();
                    }
                    else if (_uniqueCompletedColors.Contains(targetColorHex))
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

                    // Set up animation for the next level button if not unlocking all levels
                    if (!ProjectConfig.InstanceConfig.unlockAllLevelsFromStart && i == nextColorIndex)
                    {
                        StartCoroutine(AnimateNextLevelButton(buttons[i]));
                    }
                }
                else
                {
                    // Disable button and set to black with 20% transparency
                    buttons[i].interactable = false;
                    ShaderManager.SetColor(buttonImage.material, "_TargetColor", Color.black);
                    ShaderManager.SetColor(buttonImage.material, "_OriginalColor", Color.black);
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
                var mostRecentIndex = Array.FindIndex(SRGBTargetColors, c => ColorUtility.ToHtmlStringRGB(c) == mostRecentCompletedColor);
                if (mostRecentIndex != -1 && mostRecentIndex < buttons.Length)
                {
                    OnButtonClicked(mostRecentIndex);
                }
            }
            else
            {
                // Select the first level by default
                OnButtonClicked(0);
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

                    //yield return new WaitForSeconds(buttonShakeDuration);
                }
            }
        }

        private void OnButtonClicked(int index)
        {
            Debug.Log($"Button clicked at index: {index}");

            // Don't do anything if the button clicked is the same button that is already selected
            if (index == _selectedLevelIndex)
            {
                return;
            }

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

            // Update the current target color immediately
            _currentTargetColor = SRGBTargetColors[_selectedLevelIndex];

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

            // Update the submit button immediately after updating the target color
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

            AudioManager.PlaySound("MENU_Pick");
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
                // Determine if the selected level is new or completed
                var isNewLevel = !ProgressManager.CompletedTargetColors.Contains(ColorUtility.ToHtmlStringRGB(_currentTargetColor));

                var buttonImage = submitButton.GetComponent<Image>();
                if (buttonImage != null && buttonImage.material != null)
                {
                    if (isNewLevel)
                    {
                        // Set colors for a new level
                        ShaderManager.SetColor(buttonImage.material, "_BackgroundColor", newLevelColor);
                        ShaderManager.SetColor(buttonImage.material, "_AccentColor", newLevelAccentColor);
                        ShaderManager.SetFloat(buttonImage.material, "_EffectToggle", 1f);
                        UpdateSubmitIcon("Colorcrush/Icons/icons8-advance-90");
                    }
                    else
                    {
                        // Set colors for a completed level
                        ShaderManager.SetColor(buttonImage.material, "_BackgroundColor", completedLevelColor);
                        ShaderManager.SetFloat(buttonImage.material, "_EffectToggle", 0f);
                        UpdateSubmitIcon("Colorcrush/Icons/icons8-undo-90");
                    }
                }
                else
                {
                    Debug.LogError("Submit button is missing Image component or material.");
                }

                // Enable or disable the submit button based on the selected level index
                submitButton.interactable = (_selectedLevelIndex != -1 && _selectedLevelIndex <= _uniqueCompletedColors.Count) || ProjectConfig.InstanceConfig.unlockAllLevelsFromStart;
            }
        }

        private void UpdateSubmitIcon(string iconPath)
        {
            // Find the GameObject with the "SubmitIcon" tag
            var submitIconObject = GameObject.FindGameObjectWithTag("SubmitIcon");
            if (submitIconObject != null)
            {
                var submitIconImage = submitIconObject.GetComponent<Image>();
                if (submitIconImage != null)
                {
                    // Change the sprite to the specified icon
                    submitIconImage.sprite = Resources.Load<Sprite>(iconPath);
                }
                else
                {
                    Debug.LogWarning("SubmitIcon object does not have an Image component.");
                }
            }
            else
            {
                Debug.LogWarning("GameObject with tag 'SubmitIcon' not found.");
            }
        }

        private void OnSubmitButtonClicked()
        {
            var targetColor = SRGBTargetColors[_selectedLevelIndex];
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

            AudioManager.PlaySound("misc_menu", pitchShift: 1.15f);
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

            _currentTargetColor = SRGBTargetColors[_selectedLevelIndex];

            if (_selectedLevelIndex == _uniqueCompletedColors.Count)
            {
                // This is a new, uncompleted level
                _currentAnalysisValues = new float[8];
                StartCoroutine(AnimateAxisValuesAndColor(_currentAnalysisValues, _currentTargetColor));
                SetDragSignifierActive(false);
            }
            else
            {
                // Get the index from completed colors list that matches current target color
                var targetColorHex = ColorUtility.ToHtmlStringRGB(_currentTargetColor);

                // Search the list of completed colors for the target color in reverse order, so that only the most recent completion is used
                var completedColorIndex = ProgressManager.CompletedTargetColors.FindLastIndex(c => c == targetColorHex);

                // Use that index to get the corresponding final colors result
                if (completedColorIndex >= 0 && completedColorIndex < ProgressManager.FinalColors.Count)
                {
                    var finalColorsResult = ProgressManager.FinalColors[completedColorIndex];
                    _currentAnalysisValues = finalColorsResult.AxisEncodings.Select(v => v.magnitude).ToArray();
                }
                else
                {
                    _currentAnalysisValues = new float[8];
                }

                StartCoroutine(AnimateAxisValuesAndColor(_currentAnalysisValues, _currentTargetColor));

                if (ColorUtility.ToHtmlStringRGB(_currentTargetColor) != ProgressManager.MostRecentCompletedTargetColor)
                {
                    SetDragSignifierActive(false);
                }
                else if (!_hasColorAnalysisBeenClicked)
                {
                    SetDragSignifierActive(true);
                }
            }
        }

        private void SetDragSignifierActive(bool isActive)
        {
            ShaderManager.SetFloat(_colorAnalysisMaterial, "_PulseEffect", isActive ? 1 : 0);
            var dragSignifierAnimator = dragSignifier.GetComponent<UnityEngine.Animator>();
            dragSignifierAnimator.enabled = isActive;
            //dragSignifier.SetActive(isActive);
            var dragSignifierImage = dragSignifier.GetComponent<Image>();
            var color = dragSignifierImage.color;
            color.a = isActive ? 1f : 0f;
            dragSignifierImage.color = color;
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