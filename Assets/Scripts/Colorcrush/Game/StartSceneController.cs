﻿// Copyright (C) 2024 Peter Guld Leth

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
        [Header("General")]
        [Tooltip("TextMeshProUGUI component for displaying the main title of the game.")] [SerializeField]
        private TextMeshProUGUI titleText;

        [Tooltip("TextMeshProUGUI component for displaying the current version of the game.")] [SerializeField]
        private TextMeshProUGUI versionText;

        [Tooltip("TextMeshProUGUI component for displaying technical information about the color space (used for debugging).")] [SerializeField]
        private TextMeshProUGUI debugColorspaceInfoText;

        [Tooltip("Name of the scene to load when the game is started for the first time.")] [FormerlySerializedAs("nextSceneName")] [SerializeField]
        private string freshStartupScene = "GameScene";

        [Tooltip("Name of the scene to load when the game is started subsequent times.")] [FormerlySerializedAs("menuSceneName")] [SerializeField]
        private string recurringStartupScene = "MenuScene";

        [Tooltip("Time in seconds to wait before starting the shake animations on the start screen.")] [SerializeField]
        private float initialDelay = 6f;

        [Tooltip("Time in seconds to wait between adding each character to the title text animation.")] [SerializeField]
        private float delayBetweenCharacters = 0.5f;

        [Tooltip("Time in seconds between each shake animation of the title.")] [SerializeField]
        private float shakeInterval = 10f;

        [Header("Emoji Shuffle Effect")]
        [Tooltip("Total duration in seconds of the emoji shuffling animation.")] [SerializeField]
        private float totalAnimationDuration = 3f;

        [Tooltip("Duration in seconds of the scaling animation for the final emoji. This will be subtracted from the total animation duration to determine the duration of the shuffling animation.")] [SerializeField]
        private float scaleDuration = 1f;

        [Tooltip("Animation curve that controls the speed of the emoji shuffling over time.")] [SerializeField]
        private AnimationCurve shuffleSpeedCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Tooltip("Total number of different emojis to cycle through during the shuffle animation.")] [SerializeField]
        private int totalEmojis = 20;

        [Tooltip("Duration in seconds of the small 'bump' animation for each emoji during shuffling.")] [SerializeField]
        private float bumpDuration = 0.01f;

        [Tooltip("Scale factor for the 'bump' animation (e.g., 1.1 means the emoji grows 10% larger).")] [SerializeField]
        private float bumpScaleFactor = 1.1f;

        [Tooltip("Final scale of the emoji after the shuffling animation completes.")] [SerializeField]
        private float targetScale = 0.5f;

        [Tooltip("Color to apply to the shuffled emojis.")] [SerializeField]
        private Color emojiColor = new(0.82f, 0.47f, 0.46f); // D27975 in RGB

        [Tooltip("Color of the background behind the shuffling emojis.")] [SerializeField]
        private Color backgroundColor = Color.white;

        [Tooltip("Image component for the background of the start screen.")] [SerializeField]
        private Image backgroundImage;

        [Header("Reveal Behind Effect")]
        [Tooltip("Material used to create the circular reveal effect behind the emoji.")] [SerializeField]
        private Material circleMaterial;

        [Tooltip("Speed at which the reveal circle expands behind the emoji.")] [SerializeField]
        private float expandSpeed = 1.0f;

        [Header("Sound Effects")]
        [Tooltip("Name of the sound effect to play when adding the smiley face to the title.")] [SerializeField]
        private string smileySound = "MENU_Pick";

        [Tooltip("Pitch adjustment for the colon sound in the smiley face (higher values = higher pitch).")] [SerializeField]
        private float smileyColonPitchShift = 1.25f;

        [Tooltip("Pitch adjustment for the parenthesis sound in the smiley face (lower values = lower pitch).")] [SerializeField]
        private float smileyParenthesisPitchShift = 0.5f;

        [Tooltip("Name of the sound effect to play after the initial delay.")] [SerializeField]
        private string initialDelaySound = "MESSAGE-B_Accept";

        [Tooltip("Pitch adjustment for the sound played after the initial delay.")] [SerializeField]
        private float initialDelayPitchShift = 0.85f;

        [Tooltip("Volume adjustment for the sound played after the initial delay.")] [SerializeField]
        private float initialDelayGain = 2f;

        [Tooltip("Name of the sound effect to play during each emoji 'bump' in the shuffle animation.")] [SerializeField]
        private string emojiBumpSound = "MENU_Pick";

        [Tooltip("Pitch adjustment for the emoji bump sound.")] [SerializeField]
        private float emojiBumpPitchShift = 1f;

        [Tooltip("Volume adjustment for the emoji bump sound.")] [SerializeField]
        private float emojiBumpGain = 1f;

        [Header("Shake Animation")]
        [Tooltip("Duration in seconds of the shake animation applied to the title.")] [SerializeField]
        private float shakeDuration = 0.75f;

        [Tooltip("Intensity of the shake animation (higher values = more intense shaking).")] [SerializeField]
        private float shakeStrength = 5f;

        [Tooltip("Number of shakes per second in the shake animation.")] [SerializeField]
        private float shakeVibrato = 15f;

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
                _ = _targetImage.gameObject.GetComponent<Animator>() ?? _targetImage.gameObject.AddComponent<Animator>();
                _originalScale = _targetImage.transform.localScale;
                _shuffleDuration = Mathf.Max(0, totalAnimationDuration - scaleDuration);
                StartCoroutine(ShuffleAndScaleCoroutine());
                ShaderManager.SetColor(_targetImage.material, "_TargetColor", emojiColor);
            }
            else
            {
                Debug.LogError("Failed to instantiate target image for ShuffleEmojisEffect.");
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = backgroundColor;
            }
            else
            {
                Debug.LogWarning("Background Image component not assigned in the inspector.");
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
                AnimationManager.PlayAnimation(animatorsList, new ShakeAnimation(shakeDuration, shakeStrength, shakeVibrato));
            }
        }

        private IEnumerator AddSmileyToTitle()
        {
            yield return new WaitForSeconds(initialDelay);

            var originalText = titleText.text;
            titleText.text = originalText + ":";
            AudioManager.PlaySound(smileySound, pitchShift: smileyColonPitchShift);

            yield return new WaitForSeconds(delayBetweenCharacters);

            titleText.text = originalText + ":)";
            AudioManager.PlaySound(smileySound, pitchShift: smileyParenthesisPitchShift);
        }

        private IEnumerator PlaySoundAfterDelay()
        {
            yield return new WaitForSeconds(3f);
            AudioManager.PlaySound(initialDelaySound, pitchShift: initialDelayPitchShift, gain: initialDelayGain);
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

            AudioManager.PlaySound(emojiBumpSound, pitchShift: emojiBumpPitchShift, gain: emojiBumpGain);

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