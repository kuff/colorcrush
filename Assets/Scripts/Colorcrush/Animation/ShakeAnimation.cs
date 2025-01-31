// Copyright (C) 2025 Peter Guld Leth

#region

using UnityEngine;

#endregion

namespace Colorcrush.Animation
{
    public class ShakeAnimation : AnimationManager.Animation
    {
        private readonly float _duration;
        private readonly float _frequency;
        private readonly float _maxRotation;

        public ShakeAnimation(float duration, float maxRotation = 2f, float frequency = 10f)
        {
            Duration = duration;
            IsTemporary = false;
            _duration = duration;
            _maxRotation = maxRotation;
            _frequency = frequency;
        }

        public override void Play(Animator animator, float progress)
        {
            var originalRotation = animator.GetOriginalRotation();
            var time = progress * _duration;

            // Calculate the rotation offset using a sine wave
            var rotationOffset = Mathf.Sin(time * _frequency) * _maxRotation;

            // Apply the rotation offset
            var newRotation = originalRotation * Quaternion.Euler(0, 0, rotationOffset);
            animator.SetRotation(newRotation, this);

            // Gradually reduce the twitching intensity as the animation progresses
            var fadeOutFactor = 1 - progress * progress; // Quadratic ease-out
            animator.SetRotation(Quaternion.Slerp(originalRotation, newRotation, fadeOutFactor), this);
        }
    }
}