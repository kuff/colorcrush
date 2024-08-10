// Copyright (C) 2024 Peter Guld Leth

#region

using System.Collections;
using System.Collections.Generic;
using Colorcrush.Animation;
using Colorcrush.Util;
using TMPro;
using UnityEngine;
using Animator = Colorcrush.Animation.Animator;

#endregion

namespace Colorcrush.Game
{
    public class LoadingScreenTap : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI tapText;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private string nextSceneName = "GameScene";
        [SerializeField] private float initialDelay = 6f;
        [SerializeField] private float delayBetweenCharacters = 0.5f;
        [SerializeField] private float shakeInterval = 10f;
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

            yield return new WaitForSeconds(delayBetweenCharacters);

            titleText.text = originalText + ":)";
        }

        public void OnTapTextClicked()
        {
            if (_isLoading)
            {
                return; // Prevent multiple clicks
            }

            _isLoading = true;
            tapText.text = "LOADING...";
            SceneManager.LoadSceneAsync(nextSceneName, OnSceneReady);
        }

        private void OnSceneReady()
        {
            SceneManager.ActivateLoadedScene();
        }
    }
}