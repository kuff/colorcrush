// Copyright (C) 2024 Peter Guld Leth

#region

using System.Collections.Generic;
using UnityEngine;

#endregion

namespace Colorcrush.Animation
{
    public class BumpAllAnimation : AnimationManager.Animation
    {
        private const float MovementFactor = 0.0025f;
        private readonly List<Animator> _animators;
        private readonly Vector3 _centerPoint;
        private readonly float _duration;
        private readonly float _targetScaleFactor;

        public BumpAllAnimation(float duration, float targetScaleFactor, List<Animator> animators)
        {
            Duration = duration;
            IsTemporary = true;
            _targetScaleFactor = targetScaleFactor;
            _duration = duration;
            _animators = animators;
            _centerPoint = CalculateCenterPoint(animators);
        }

        public override void Play(Animator animator, float progress)
        {
            // Calculate linear progress
            var linearProgress = progress / _duration;

            foreach (var anim in _animators)
            {
                var originalScale = anim.GetOriginalScale();
                var originalPosition = anim.transform.position;
                var directionFromCenter = originalPosition - _centerPoint;

                // Determine if we're in the shrink or expand phase
                if (progress < _duration / 2)
                {
                    // Shrink phase
                    var targetScale = originalScale * _targetScaleFactor;
                    anim.SetScale(Vector3.Lerp(originalScale, targetScale, linearProgress * 2));

                    // Move slightly towards center
                    anim.transform.position = Vector3.Lerp(originalPosition, originalPosition - directionFromCenter * MovementFactor, linearProgress * 2);
                }
                else
                {
                    // Expand phase
                    var targetScale = originalScale;
                    anim.SetScale(Vector3.Lerp(originalScale * _targetScaleFactor, targetScale, (linearProgress - 0.5f) * 2));

                    // Move back to original position
                    anim.transform.position = Vector3.Lerp(originalPosition - directionFromCenter * MovementFactor, originalPosition, (linearProgress - 0.5f) * 2);
                }
            }
        }

        private Vector3 CalculateCenterPoint(List<Animator> animators)
        {
            var sum = Vector3.zero;
            foreach (var animator in animators)
            {
                sum += animator.transform.position;
            }

            return sum / animators.Count;
        }
    }
}