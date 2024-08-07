// Copyright (C) 2024 Peter Guld Leth

#region

using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
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

        private void UpdateButton(int index)
        {
            _selectionGridImages[index].sprite = _emojiController.GetDefaultEmoji();
            _selectionGridImages[index].material.SetColor("_TargetColor", _colorController.GetNextColor());
            _selectionGridImages[index].material.SetFloat("_Alpha", DefaultAlpha);
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

            // Update emoji sprite based on toggle state
            _selectionGridImages[index].sprite = _buttonToggledStates[index]
                ? _emojiController.GetNextSadEmoji()
                : _emojiController.GetDefaultEmoji();

            Debug.Log($"GameController: Button {index} toggled. New state: {_buttonToggledStates[index]}");
        }

        public void OnSubmitButtonClicked()
        {
            if (_targetReached)
            {
                _colorController.AdvanceToNextTargetColor();
                submitButtonText.text = "LOADING...";
                SceneManager.LoadScene(nextSceneName);
                return;
            }

            _submitCount++;

            for (var i = 0; i < _selectionGridButtons.Length; i++)
            {
                if (_buttonToggledStates[i])
                {
                    UpdateButton(i);
                    _buttonToggledStates[i] = false;
                    _selectionGridButtons[i].transform.localScale = _originalButtonScales[i];
                }
            }

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
                _targetMaterial.SetColor("_TargetColor", ColorController.GetCurrentTargetColor());
            }
        }
    }
}