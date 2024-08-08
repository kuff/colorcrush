// Copyright (C) 2024 Peter Guld Leth

#region

using System.Collections.Generic;
using UnityEngine;

#endregion

namespace Colorcrush.Animation
{
    public class TapAllAnimation : AnimationManager.Animation
    {
        private const float PositionAdjustmentFactor = 0.05f; // Subtle movement factor
        private readonly Vector3 _center;
        private readonly float _minScale;

        public TapAllAnimation(List<Animator> animators, float duration = 0.1f, float minScale = 0.9f)
        {
            Duration = duration;
            _minScale = minScale;
            IsTemporary = true;

            // Calculate the common center
            _center = CalculateCenter(animators);
        }

        public override void Play(Animator animator, float progress)
        {
            var scale = Mathf.Lerp(_minScale, 1f, progress);
            var originalPosition = animator.transform.position;

            // Calculate the scaled position relative to the common center with subtle movement
            var direction = (originalPosition - _center).normalized;
            var scaledPosition = originalPosition + direction * ((1f - scale) * PositionAdjustmentFactor);

            animator.transform.position = scaledPosition;
            animator.transform.localScale = Vector3.one * scale;
        }

        private Vector3 CalculateCenter(List<Animator> animators)
        {
            if (animators.Count == 0)
            {
                return Vector3.zero;
            }

            var sum = Vector3.zero;
            foreach (var animator in animators)
            {
                sum += animator.transform.position;
            }

            return sum / animators.Count;
        }
    }
}