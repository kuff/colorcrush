// Copyright (C) 2024 Peter Guld Leth

#region

using UnityEngine;

#endregion

namespace Colorcrush.Animation
{
    public class MoveFadeAnimation : AnimationManager.Animation
    {
        private readonly Vector3 _direction;
        private readonly float _duration;
        private readonly float _endAlpha;
        private readonly float _moveDistance;
        private readonly float _startAlpha;

        public MoveFadeAnimation(float startAlpha, float endAlpha, float duration, Vector3 direction, float moveDistance)
        {
            _startAlpha = startAlpha;
            _endAlpha = endAlpha;
            _duration = duration;
            _direction = direction.normalized;
            _moveDistance = moveDistance;
        }

        public override void Play(Animator animator, float progress)
        {
            var easedProgress = EaseInOutCubic(progress);
            var currentAlpha = Mathf.Lerp(_startAlpha, _endAlpha, easedProgress);
            var currentPosition = animator.transform.localPosition + _direction * (_moveDistance * easedProgress);

            animator.SetOpacity(currentAlpha);
            animator.transform.localPosition = currentPosition;

            // Log animation state
            Debug.Log($"MoveFadeAnimation - Progress: {progress:F2}, Alpha: {currentAlpha:F2}, Position: {currentPosition}");
        }

        public float GetDuration()
        {
            return _duration;
        }

        private float EaseInOutCubic(float t)
        {
            return t < 0.5 ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2;
        }
    }
}