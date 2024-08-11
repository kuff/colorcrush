// Copyright (C) 2024 Peter Guld Leth

#region

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Animator = Colorcrush.Animation.Animator;

#endregion

namespace Colorcrush.Game
{
    public class ShuffleEmojisEffect : MonoBehaviour
    {
        [SerializeField] private float totalAnimationDuration = 3f;
        [SerializeField] private float scaleDuration = 1f;
        [SerializeField] private AnimationCurve shuffleSpeedCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private int totalEmojis = 20;
        [SerializeField] private float bumpDuration = 0.01f;
        [SerializeField] private float bumpScaleFactor = 1.1f;
        [SerializeField] private float targetScale = 0.5f;
        private Animator animator;
        private Vector3 originalScale;
        private float shuffleDuration;

        private Image targetImage;

        private void Start()
        {
            InstantiateTargetImage();

            if (targetImage != null)
            {
                animator = targetImage.gameObject.GetComponent<Animator>();
                if (animator == null)
                {
                    animator = targetImage.gameObject.AddComponent<Animator>();
                }

                originalScale = targetImage.transform.localScale;
                shuffleDuration = Mathf.Max(0, totalAnimationDuration - scaleDuration);
                StartCoroutine(ShuffleAndScaleCoroutine());
            }
            else
            {
                Debug.LogError("Failed to instantiate target image for ShuffleEmojisEffect.");
            }
        }

        private void InstantiateTargetImage()
        {
            var prefab = Resources.Load<GameObject>("Colorcrush/Misc/TargetImage");
            if (prefab != null)
            {
                var canvas = FindObjectOfType<Canvas>();
                if (canvas != null)
                {
                    var instance = Instantiate(prefab, canvas.transform);

                    // Center the image on the screen
                    var rectTransform = instance.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                        rectTransform.anchoredPosition = Vector2.zero;
                    }

                    targetImage = instance.GetComponent<Image>();
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

            while (elapsedTime < shuffleDuration)
            {
                var normalizedTime = elapsedTime / shuffleDuration;
                var curveValue = shuffleSpeedCurve.Evaluate(normalizedTime);
                var targetEmojiCount = Mathf.FloorToInt(curveValue * totalEmojis);

                while (emojiCount < targetEmojiCount && emojiCount < totalEmojis)
                {
                    targetImage.sprite = Random.value > 0.5f ? EmojiManager.GetNextHappyEmoji() : EmojiManager.GetNextSadEmoji();
                    emojiCount++;

                    // Start bump animation without waiting
                    StartCoroutine(BumpCoroutine());

                    yield return new WaitForSeconds(0.01f); // Small delay between emoji changes
                }

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // Ensure all emojis have been shown
            while (emojiCount < totalEmojis)
            {
                targetImage.sprite = Random.value > 0.5f ? EmojiManager.GetNextHappyEmoji() : EmojiManager.GetNextSadEmoji();
                emojiCount++;

                // Start bump animation without waiting
                StartCoroutine(BumpCoroutine());

                yield return new WaitForSeconds(0.01f);
            }

            // Set final emoji
            targetImage.sprite = EmojiManager.GetDefaultHappyEmoji();

            // Manual scale down
            var scaleElapsedTime = 0f;
            var targetScaleVector = originalScale * targetScale;
            while (scaleElapsedTime < scaleDuration)
            {
                var t = scaleElapsedTime / scaleDuration;
                targetImage.transform.localScale = Vector3.Lerp(originalScale, targetScaleVector, t);
                scaleElapsedTime += Time.deltaTime;
                yield return null;
            }

            targetImage.transform.localScale = targetScaleVector;
        }

        private IEnumerator BumpCoroutine()
        {
            var bumpScale = originalScale * bumpScaleFactor;
            var elapsedTime = 0f;
            var startScale = targetImage.transform.localScale;

            //AudioManager.PlaySound("click_2");

            // Scale up
            while (elapsedTime < bumpDuration / 2)
            {
                var t = elapsedTime / (bumpDuration / 2);
                targetImage.transform.localScale = Vector3.Lerp(startScale, bumpScale, t);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // Scale back down
            elapsedTime = 0f;
            while (elapsedTime < bumpDuration / 2)
            {
                var t = elapsedTime / (bumpDuration / 2);
                targetImage.transform.localScale = Vector3.Lerp(bumpScale, startScale, t);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            targetImage.transform.localScale = startScale;
        }
    }
}