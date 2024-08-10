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
        [SerializeField] private string nextSceneName = "GameScene";
        private Animator[] _animators;
        private bool _isLoading;

        private void Awake()
        {
            if (tapText == null)
            {
                Debug.LogError("Tap text not assigned in the inspector.");
                return;
            }

            _animators = FindObjectsOfType<Animator>();
            StartCoroutine(PlayTwitchAnimationPeriodically());
        }

        private IEnumerator PlayTwitchAnimationPeriodically()
        {
            while (true)
            {
                yield return new WaitForSeconds(8f);
                var animatorsList = new List<Animator>(_animators);
                AnimationManager.PlayAnimation(animatorsList, new ShakeAnimation(0.75f));
            }
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