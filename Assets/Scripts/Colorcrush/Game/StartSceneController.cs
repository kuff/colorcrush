// Copyright (C) 2024 Peter Guld Leth

#region

using System.Collections;
using System.Collections.Generic;
using Colorcrush.Animation;
using Colorcrush.Util;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Animator = Colorcrush.Animation.Animator;

#endregion

namespace Colorcrush.Game
{
    public class StartSceneController : MonoBehaviour
    {
        [Header("Loading Screen")]
        [Tooltip("Text component for displaying the title")] [SerializeField]
        private TextMeshProUGUI titleText;

        [Tooltip("Text component for displaying the version")] [SerializeField]
        private TextMeshProUGUI versionText;

        [Tooltip("Text component for displaying debug colorspace information")] [SerializeField]
        private TextMeshProUGUI debugColorspaceInfoText;

        [Tooltip("Scene to load on fresh startup")] [FormerlySerializedAs("nextSceneName")] [SerializeField]
        private string freshStartupScene = "GameScene";

        [Tooltip("Scene to load on recurring startup")] [FormerlySerializedAs("menuSceneName")] [SerializeField]
        private string recurringStartupScene = "MenuScene";

        [Tooltip("Initial delay before starting animations")] [SerializeField]
        private float initialDelay = 6f;

        [Tooltip("Delay between adding characters to the title text")] [SerializeField]
        private float delayBetweenCharacters = 0.5f;

        [Tooltip("Interval between shake animations")] [SerializeField]
        private float shakeInterval = 10f;

        [Header("Emoji Shuffle Effect")]
        [Tooltip("Total duration of the emoji shuffle animation")] [SerializeField]
        private float totalAnimationDuration = 3f;

        [Tooltip("Duration of the scaling animation")] [SerializeField]
        private float scaleDuration = 1f;

        [Tooltip("Animation curve for controlling shuffle speed")] [SerializeField]
        private AnimationCurve shuffleSpeedCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Tooltip("Total number of emojis to shuffle through")] [SerializeField]
        private int totalEmojis = 20;

        [Tooltip("Duration of the bump animation for each emoji")] [SerializeField]
        private float bumpDuration = 0.01f;

        [Tooltip("Scale factor for the bump animation")] [SerializeField]
        private float bumpScaleFactor = 1.1f;

        [Tooltip("Target scale for the final emoji")] [SerializeField]
        private float targetScale = 0.5f;

        [Header("Reveal Behind Effect")]
        [Tooltip("Material used for the reveal circle effect")] [SerializeField]
        private Material circleMaterial;

        [Tooltip("Speed at which the reveal circle expands")] [SerializeField]
        private float expandSpeed = 1.0f;

        private Animator[] _animators;
        private float _circleSize;
        private bool _isLoading;
        private Vector3 _originalScale;
        private float _shuffleDuration;
        private float _startTime;
        private Image _targetImage;

        private void Awake()
        {
            if (titleText == null || versionText == null || debugColorspaceInfoText == null)
            {
                Debug.LogError("Title text, Version text, or Debug Colorspace Info text not assigned in the inspector.");
                return;
            }

            SetVersionText();
            _animators = FindObjectsOfType<Animator>();
            StartCoroutine(PlayTwitchAnimationPeriodically());
            StartCoroutine(AddSmileyToTitle());
            StartCoroutine(PlaySoundAfterDelay());

            InstantiateTargetImage();
            if (_targetImage != null)
            {
                var animator = _targetImage.gameObject.GetComponent<Animator>() ?? _targetImage.gameObject.AddComponent<Animator>();
                _originalScale = _targetImage.transform.localScale;
                _shuffleDuration = Mathf.Max(0, totalAnimationDuration - scaleDuration);
                StartCoroutine(ShuffleAndScaleCoroutine());
            }
            else
            {
                Debug.LogError("Failed to instantiate target image for ShuffleEmojisEffect.");
            }

            _startTime = Time.time;
        }

        private void Update()
        {
            var elapsedTime = Time.time - _startTime;
            if (elapsedTime >= 3.0f)
            {
                _circleSize += Time.deltaTime * expandSpeed;
                ShaderManager.SetFloat(circleMaterial, "_CircleSize", _circleSize);
            }

            UpdateColorspaceInfo();
        }

        private void SetVersionText()
        {
            var version = Application.version;
            versionText.text += " " + version;
        }

        private void UpdateColorspaceInfo()
        {
            var info = $"Desired Color Space: {QualitySettings.desiredColorSpace}\n";
            info += $"Actual Color Space: {QualitySettings.activeColorSpace}\n";
            info += $"Quality Level: {QualitySettings.GetQualityLevel()}\n";
            info += $"HDR Enabled: {QualitySettings.vSyncCount > 0}\n";

            if (GraphicsSettings.renderPipelineAsset == null)
            {
                info += "Render Pipeline: Built-in Render Pipeline";
            }
            else
            {
                var renderPipelineAsset = GraphicsSettings.renderPipelineAsset;
                info += $"Render Pipeline: {renderPipelineAsset.name}";
            }

            debugColorspaceInfoText.text = info;
        }

        private IEnumerator PlayTwitchAnimationPeriodically()
        {
            while (true)
            {
                yield return new WaitForSeconds(shakeInterval);
                var animatorsList = new List<Animator>(_animators);
                AnimationManager.PlayAnimation(animatorsList, new ShakeAnimation(0.75f));
            }
        }

        private IEnumerator AddSmileyToTitle()
        {
            yield return new WaitForSeconds(initialDelay);

            var originalText = titleText.text;
            titleText.text = originalText + ":";
            AudioManager.PlaySound("MENU_Pick", pitchShift: 1.25f);

            yield return new WaitForSeconds(delayBetweenCharacters);

            titleText.text = originalText + ":)";
            AudioManager.PlaySound("MENU_Pick", pitchShift: 0.5f);
        }

        private IEnumerator PlaySoundAfterDelay()
        {
            yield return new WaitForSeconds(3f);
            AudioManager.PlaySound("MESSAGE-B_Accept", pitchShift: 0.85f, gain: 2f);
        }

        public void OnStartButtonClicked()
        {
            if (_isLoading)
            {
                return;
            }

            _isLoading = true;
            if (ProgressManager.CompletedTargetColors.Count > 0)
            {
                SceneManager.LoadSceneAsync(recurringStartupScene, OnSceneReady);
            }
            else
            {
                SceneManager.LoadSceneAsync(freshStartupScene, OnSceneReady);
            }
        }

        private void OnSceneReady()
        {
            SceneManager.ActivateLoadedScene();
        }

        private void InstantiateTargetImage()
        {
            var prefab = Resources.Load<GameObject>("Colorcrush/Prefabs/TargetImage");
            if (prefab != null)
            {
                var canvas = FindObjectOfType<Canvas>();
                if (canvas != null)
                {
                    var instance = Instantiate(prefab, canvas.transform);
                    var rectTransform = instance.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                        rectTransform.anchoredPosition = Vector2.zero;
                    }

                    _targetImage = instance.GetComponent<Image>();
                }
                else
                {
                    Debug.LogError("Canvas not found in the scene.");
                }
            }
            else
            {
                Debug.LogError("TargetImage prefab not found in Resources folder.");
            }
        }

        private IEnumerator ShuffleAndScaleCoroutine()
        {
            var elapsedTime = 0f;
            var emojiCount = 0;

            while (elapsedTime < _shuffleDuration)
            {
                var normalizedTime = elapsedTime / _shuffleDuration;
                var curveValue = shuffleSpeedCurve.Evaluate(normalizedTime);
                var targetEmojiCount = Mathf.FloorToInt(curveValue * totalEmojis);

                while (emojiCount < targetEmojiCount && emojiCount < totalEmojis)
                {
                    _targetImage.sprite = Random.value > 0.5f ? EmojiManager.GetNextHappyEmoji() : EmojiManager.GetNextSadEmoji();
                    emojiCount++;
                    StartCoroutine(BumpCoroutine());
                    yield return new WaitForSeconds(0.01f);
                }

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            while (emojiCount < totalEmojis)
            {
                _targetImage.sprite = Random.value > 0.5f ? EmojiManager.GetNextHappyEmoji() : EmojiManager.GetNextSadEmoji();
                emojiCount++;
                StartCoroutine(BumpCoroutine());
                yield return new WaitForSeconds(0.01f);
            }

            _targetImage.sprite = EmojiManager.GetDefaultHappyEmoji();

            var scaleElapsedTime = 0f;
            var targetScaleVector = _originalScale * targetScale;
            while (scaleElapsedTime < scaleDuration)
            {
                var t = scaleElapsedTime / scaleDuration;
                _targetImage.transform.localScale = Vector3.Lerp(_originalScale, targetScaleVector, t);
                scaleElapsedTime += Time.deltaTime;
                yield return null;
            }

            _targetImage.transform.localScale = targetScaleVector;
        }

        private IEnumerator BumpCoroutine()
        {
            var bumpScale = _originalScale * bumpScaleFactor;
            var elapsedTime = 0f;
            var startScale = _targetImage.transform.localScale;

            AudioManager.PlaySound("MENU_Pick");

            while (elapsedTime < bumpDuration / 2)
            {
                var t = elapsedTime / (bumpDuration / 2);
                _targetImage.transform.localScale = Vector3.Lerp(startScale, bumpScale, t);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            elapsedTime = 0f;
            while (elapsedTime < bumpDuration / 2)
            {
                var t = elapsedTime / (bumpDuration / 2);
                _targetImage.transform.localScale = Vector3.Lerp(bumpScale, startScale, t);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            _targetImage.transform.localScale = startScale;
        }
    }
}