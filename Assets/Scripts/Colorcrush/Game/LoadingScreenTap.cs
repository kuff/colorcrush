// Copyright (C) 2024 Peter Guld Leth

#region

using System.Collections;
using System.Collections.Generic;
using Colorcrush.Animation;
using Colorcrush.Util;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using Animator = Colorcrush.Animation.Animator;

#endregion

namespace Colorcrush.Game
{
    public class LoadingScreenTap : MonoBehaviour
    {
        [Tooltip("Text to display for tap prompt")] [SerializeField]
        private TextMeshProUGUI tapText;

        [Tooltip("Text to display for the title")] [SerializeField]
        private TextMeshProUGUI titleText;

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

        private Animator[] _animators;
        private bool _isLoading;

        private void Awake()
        {
            if (tapText == null)
            {
                Debug.LogError("Tap text not assigned in the inspector.");
                return;
            }

            if (titleText == null)
            {
                Debug.LogError("Title text not assigned in the inspector.");
                return;
            }

            _animators = FindObjectsOfType<Animator>();
            StartCoroutine(PlayTwitchAnimationPeriodically());
            StartCoroutine(AddSmileyToTitle());
            StartCoroutine(PlaySoundAfterDelay());
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

        public void OnTapTextClicked()
        {
            if (_isLoading)
            {
                return; // Prevent multiple clicks
            }

            //AudioManager.PlaySound("click_2");

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
    }
}