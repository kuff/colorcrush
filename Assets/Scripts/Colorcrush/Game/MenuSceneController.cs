// Copyright (C) 2024 Peter Guld Leth

#region

using UnityEngine;
using UnityEngine.UI;

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

        private float _adjustedWidth;
        private float _originalWidth;
        private float _scrollableWidth;
        private RectTransform _scrollbarRectTransform;

        private void Awake()
        {
            ResetScrollViewToBeginning();
            InitializeScrollBarEffect();
        }

        private void OnDestroy()
        {
            if (scrollView != null)
            {
                scrollView.onValueChanged.RemoveListener(OnScrollValueChanged);
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
                Debug.LogWarning("ScrollRect to reset is not assigned in the inspector.");
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
    }
}