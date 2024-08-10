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
using Animator = Colorcrush.Animation.Animator;

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
        [SerializeField] private Image progressBar;
        [SerializeField] private string nextSceneName = "MuralScene";
        [SerializeField] private Button submitButton;
        [SerializeField] private Canvas uiCanvas;
        private bool _buttonsInteractable = true;
        private bool[] _buttonToggledStates;

        private ColorController _colorController;
        private EmojiController _emojiController;
        private float _initialProgressBarWidth;
        private Vector3[] _originalButtonScales;
        private GameObject[] _selectionButtons;
        private Button[] _selectionGridButtons;
        private Image[] _selectionGridImages;
        private int _submitCount;
        private Animator _targetEmojiAnimator;
        private Image _targetEmojiImage;
        private bool _targetReached;

        private void Awake()
        {
            InitializeComponents();
            InitializeButtons();
            InitializeProgressBar();
            UpdateUI();
        }

        private void InitializeComponents()
        {
            _colorController = FindObjectOfType<ColorController>();
            _emojiController = FindObjectOfType<EmojiController>();
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
                    _targetEmojiImage.sprite = _emojiController.GetDefaultEmoji();
                }

                var targetButton = targetEmojiObject.GetComponent<Button>();
                if (targetButton != null)
                {
                    targetButton.onClick.AddListener(OnTargetEmojiClicked);
                }
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
                return;
            }

            _buttonToggledStates[index] = !_buttonToggledStates[index];
            var alpha = _buttonToggledStates[index] ? ToggledAlpha : DefaultAlpha;
            _selectionGridImages[index].material.SetFloat("_Alpha", alpha);

            var targetScale = _buttonToggledStates[index] ? _originalButtonScales[index] * ShrinkFactor : _originalButtonScales[index];
            _selectionGridButtons[index].transform.localScale = targetScale;

            // Add debug info
            Debug.Log($"Button {index} {(_buttonToggledStates[index] ? "selected" : "deselected")}. New alpha: {alpha}, New scale: {targetScale}");

            LoggingManager.LogEvent(_buttonToggledStates[index]
                ? new ColorSelectedEvent(index, _selectionGridImages[index].sprite.name)
                : new ColorDeselectedEvent(index));
        }

        public void OnSubmitButtonClicked()
        {
            Debug.Log("Submit button clicked");

            if (SceneManager.IsLoading || !_buttonsInteractable)
            {
                AnimationManager.PlayAnimation(submitButton.GetComponent<Animator>(), new ShakeAnimation(0.1f, 9f));
                return;
            }

            AnimationManager.PlayAnimation(submitButton.GetComponent<Animator>(), new BumpAnimation(0.1f, 0.9f));

            if (_targetReached)
            {
                _colorController.AdvanceToNextTargetColor();
                submitButtonText.text = "...";
                SceneManager.LoadSceneAsync(nextSceneName);
                return;
            }

            _submitCount++;
            LoggingManager.LogEvent(new ColorsSubmittedEvent());

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

                submitButtonText.text = "CONTINUE";
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
            var prefab = Resources.Load<GameObject>("Colorcrush/Misc/ColorPickButton");
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
                image.material.SetFloat("_Alpha", 0.0f);
                button.interactable = false;
            }
        }

        private IEnumerator AnimateEmojis(List<EmojiAnimator> emojiAnimators)
        {
            for (var i = 0; i < emojiAnimators.Count; i++)
            {
                if (_buttonToggledStates[i])
                {
                    AnimateSelectedEmoji(emojiAnimators[i]);
                }
                else
                {
                    yield return AnimateNonSelectedEmoji(emojiAnimators[i]);
                }
            }

            yield return new WaitForSeconds(1f);
        }

        private void AnimateSelectedEmoji(EmojiAnimator animator)
        {
            var image = animator.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = _emojiController.GetNextSadEmoji();
            }

            AnimationManager.PlayAnimation(animator, new FadeAnimation(ToggledAlpha, 0f, 0.5f));
        }

        private IEnumerator AnimateNonSelectedEmoji(EmojiAnimator animator)
        {
            var image = animator.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = _emojiController.GetNextHappyEmoji();
            }

            var targetPosition = progressBar.transform.position;
            AnimationManager.PlayAnimation(animator, new MoveToAnimation(targetPosition, 0.5f, Vector3.zero));
            yield return new WaitForSeconds(0.05f);
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
            _targetEmojiImage.sprite = _emojiController.GetNextHappyEmoji();
            yield return new WaitForSeconds(1f);
            _targetEmojiImage.sprite = _emojiController.GetDefaultEmoji();
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
            if (_colorController != null && _targetEmojiImage != null && _targetEmojiImage.material != null)
            {
                var targetColor = ColorController.GetCurrentTargetColor();
                _targetEmojiImage.material.SetColor("_TargetColor", targetColor);
                LoggingManager.LogEvent(new NewTargetColorEvent(targetColor));
            }
        }
    }
}