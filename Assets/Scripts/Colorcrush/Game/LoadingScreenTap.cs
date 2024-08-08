// Copyright (C) 2024 Peter Guld Leth

#region

using System.Collections;
using System.Collections.Generic;
using Colorcrush.Animation;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Animator = Colorcrush.Animation.Animator;

#endregion

namespace Colorcrush.Game
{
    public class LoadingScreenTap : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI tapText;
        [SerializeField] private string nextSceneName = "MuralScene";
        private Animator[] _animators;

        private void Awake()
        {
            if (tapText == null)
            {
                Debug.LogError("Tap text not assigned in the inspector.");
                return;
            }

            _animators = FindObjectsOfType<Animator>();
        }

        public void OnTapTextClicked()
        {
            tapText.text = "LOADING...";
            AnimationManager.PlayAnimation(new List<Animator>(_animators), new TapAllAnimation(new List<Animator>(_animators)));
            StartCoroutine(LoadNextSceneAfterAnimation());
        }

        private IEnumerator LoadNextSceneAfterAnimation()
        {
            yield return new WaitForSeconds(0.1f); // Wait for the animation duration
            SceneManager.LoadScene(nextSceneName);
        }
    }
}