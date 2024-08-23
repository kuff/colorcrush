// Copyright (C) 2024 Peter Guld Leth

#region

using UnityEngine;
using UnityEngine.UI;
using Colorcrush.Util;
using System.Linq;
using TMPro;
using System.Collections.Generic;

#endregion

namespace Colorcrush.Game
{
    public class MenuSceneController : MonoBehaviour
    {
        [SerializeField] [Tooltip("The ScrollRect to reset to the beginning")]
        private ScrollRect scrollViewToReset;

        [SerializeField] [Tooltip("The Image component of the scrollbar")]
        private Image scrollbarImage;

        [SerializeField] [Tooltip("The ScrollRect component of the main scroll view")]
        private ScrollRect scrollView;

        [SerializeField] [Range(0.1f, 0.9f)] [Tooltip("The size ratio of the scrollbar (0.1 to 0.9)")]
        private float scrollbarSizeRatio = 0.5f;

        [SerializeField] [Tooltip("GridLayoutGroup containing the buttons")]
        private GridLayoutGroup buttonGrid;

        [SerializeField] [Tooltip("The submit button")]
        private Button submitButton;

        private float _adjustedWidth;
        private float _originalWidth;
        private float _scrollableWidth;
        private RectTransform _scrollbarRectTransform;
        private int _selectedLevelIndex = -1;

        private void Awake()
        {
            ResetScrollViewToBeginning();
            InitializeScrollBarEffect();
            SetSelectedLevel();
            SetupButtons();
            UpdateSubmitButton();
        }

        private void OnDestroy()
        {
            if (scrollView != null)
            {
                scrollView.onValueChanged.RemoveListener(OnScrollValueChanged);
            }
        }

        private void SetSelectedLevel()
        {
            var completedColors = ProgressManager.CompletedTargetColors;
            if (SceneManager.GetPreviousSceneName() == "GameScene" && PlayerPrefs.HasKey("TargetColor"))
            {
                string targetColor = PlayerPrefs.GetString("TargetColor");
                _selectedLevelIndex = ColorArray.SRGBTargetColors.ToList().FindIndex(c => ColorUtility.ToHtmlStringRGB(c) == targetColor);
            }
            else
            {
                _selectedLevelIndex = completedColors.Count;
            }
        }

        private void ResetScrollViewToBeginning()
        {
            if (scrollViewToReset != null)
            {
                scrollViewToReset.horizontalNormalizedPosition = 0f;
                Canvas.ForceUpdateCanvases();
                scrollViewToReset.velocity = Vector2.zero;
            }
            else
            {
                Debug.LogError("ScrollRect to reset is not assigned in the inspector.");
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

            Button[] buttons = buttonGrid.GetComponentsInChildren<Button>();
            var completedColors = ProgressManager.CompletedTargetColors;
            var rewardedEmojis = ProgressManager.RewardedEmojis;
            var uniqueCompletedColors = new HashSet<string>(completedColors);
            int nextColorIndex = uniqueCompletedColors.Count;

            for (int i = 0; i < buttons.Length; i++)
            {
                Image buttonImage = buttons[i].GetComponent<Image>();
                if (buttonImage == null || buttonImage.material == null)
                {
                    Debug.LogError($"Button {i} is missing Image component or material.");
                    continue;
                }

                Color targetColor = ColorArray.SRGBTargetColors[i];
                string targetColorHex = ColorUtility.ToHtmlStringRGB(targetColor);

                if (i <= nextColorIndex)
                {
                    // Enable button and set color
                    buttons[i].interactable = true;
                    ShaderManager.SetColor(buttonImage.material, "_TargetColor", targetColor);
                    ShaderManager.SetFloat(buttonImage.material, "_Alpha", 1f);
                    int index = i;
                    buttons[i].onClick.AddListener(() => OnButtonClicked(index));

                    // Set the rewarded emoji if available
                    if (uniqueCompletedColors.Contains(targetColorHex))
                    {
                        int colorIndex = completedColors.IndexOf(targetColorHex);
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
                }
                else
                {
                    // Disable button and set to black with 20% transparency
                    buttons[i].interactable = false;
                    ShaderManager.SetColor(buttonImage.material, "_TargetColor", Color.black);
                    ShaderManager.SetFloat(buttonImage.material, "_Alpha", 0.2f);
                    buttons[i].transform.localScale = Vector3.one * 0.8f;
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
            _selectedLevelIndex = index;
            UpdateSubmitButton();
        }

        private void UpdateSubmitButton()
        {
            if (submitButton != null)
            {
                var uniqueCompletedColors = new HashSet<string>(ProgressManager.CompletedTargetColors);
                bool isNewLevel = _selectedLevelIndex == uniqueCompletedColors.Count;
                
                Image buttonImage = submitButton.GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = isNewLevel ? Color.green : Color.red;
                }
                else
                {
                    Debug.LogError("Submit button is missing Image component.");
                }

                submitButton.interactable = _selectedLevelIndex != -1 && _selectedLevelIndex <= uniqueCompletedColors.Count;
            }
        }

        private void OnSubmitButtonClicked()
        {
            var uniqueCompletedColors = new HashSet<string>(ProgressManager.CompletedTargetColors);
            if (_selectedLevelIndex != -1 && _selectedLevelIndex <= uniqueCompletedColors.Count && _selectedLevelIndex < ColorArray.SRGBTargetColors.Length)
            {
                Color targetColor = ColorArray.SRGBTargetColors[_selectedLevelIndex];
                PlayerPrefs.SetString("TargetColor", ColorUtility.ToHtmlStringRGB(targetColor));
                PlayerPrefs.Save();
                SceneManager.LoadSceneAsync("GameScene");
            }
        }
    }
}