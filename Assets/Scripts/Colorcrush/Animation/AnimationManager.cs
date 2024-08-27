// Copyright (C) 2024 Peter Guld Leth

#region

using System.Collections.Generic;
using UnityEngine;

#endregion

namespace Colorcrush.Animation
{
    public class AnimationManager : MonoBehaviour
    {
        private static AnimationManager _instance;

        private readonly Dictionary<Animation, List<AnimationState>> _activeAnimations = new();
        private readonly Dictionary<Animator, List<Animation>> _animatorAnimations = new();

        public static AnimationManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<AnimationManager>();
                    if (_instance == null)
                    {
                        var obj = new GameObject
                        {
                            name = nameof(AnimationManager),
                        };
                        _instance = obj.AddComponent<AnimationManager>();
                    }
                }

                return _instance;
            }
        }

        private void Update()
        {
            var completedAnimations = new List<Animation>();

            foreach (var (animation, states) in _activeAnimations)
            {
                var completedStates = new List<AnimationState>();

                foreach (var state in states)
                {
                    state.ElapsedTime += Time.deltaTime;
                    var progress = Mathf.Clamp01(state.ElapsedTime / animation.Duration);

                    if (state.IsReversing)
                    {
                        progress = 1 - progress;
                    }

                    animation.Play(state.Animator, progress);

                    if (progress >= 1)
                    {
                        if (animation.IsTemporary && !state.IsReversing)
                        {
                            state.IsReversing = true;
                            state.ElapsedTime = 0;
                        }
                        else
                        {
                            completedStates.Add(state);
                        }
                    }
                }

                foreach (var state in completedStates)
                {
                    states.Remove(state);
                    RemoveAnimatorAnimation(state.Animator, animation);
                }

                if (states.Count == 0)
                {
                    completedAnimations.Add(animation);
                }
            }

            foreach (var animation in completedAnimations)
            {
                _activeAnimations.Remove(animation);
            }
        }

        public static void PlayAnimation(Animator animator, Animation animation)
        {
            PlayAnimation(new[] { animator, }, animation);
        }

        public static void PlayAnimation(IEnumerable<Animator> animators, Animation animation)
        {
            if (!Instance._activeAnimations.TryGetValue(animation, out var states))
            {
                states = new List<AnimationState>();
                Instance._activeAnimations[animation] = states;
                Debug.Log($"AnimationManager: Created new animation state for {animation.GetType().Name}");
            }
            else
            {
                Debug.Log($"AnimationManager: Using existing animation state for {animation.GetType().Name}");
            }

            var count = 0;
            foreach (var animator in animators)
            {
                if (animator != null)
                {
                    // Remove any existing animations for this animator
                    RemoveExistingAnimations(animator);
                    // Add a new state for this animator
                    states.Add(new AnimationState { Animator = animator, ElapsedTime = 0, IsReversing = false, });
                    Instance.AddAnimatorAnimation(animator, animation);
                    count++;
                }
                else
                {
                    Debug.LogWarning("AnimationManager: Attempted to add a null animator to the animation state");
                }
            }

            if (count > 0)
            {
                Debug.Log($"AnimationManager: Added {count} animator(s) to {animation.GetType().Name}");
            }
            else
            {
                Debug.LogWarning($"AnimationManager: No valid animators were added to {animation.GetType().Name}");
            }
        }

        public static void RemoveExistingAnimations(Animator animator)
        {
            if (Instance._animatorAnimations.TryGetValue(animator, out var animations))
            {
                foreach (var anim in animations)
                {
                    if (Instance._activeAnimations.TryGetValue(anim, out var states))
                    {
                        states.RemoveAll(s => s.Animator == animator);
                    }
                }

                Instance._animatorAnimations.Remove(animator);
            }
        }

        private void AddAnimatorAnimation(Animator animator, Animation animation)
        {
            if (!_animatorAnimations.TryGetValue(animator, out var animations))
            {
                animations = new List<Animation>();
                _animatorAnimations[animator] = animations;
            }

            animations.Add(animation);
        }

        private void RemoveAnimatorAnimation(Animator animator, Animation animation)
        {
            if (_animatorAnimations.TryGetValue(animator, out var animations))
            {
                animations.Remove(animation);
                if (animations.Count == 0)
                {
                    _animatorAnimations.Remove(animator);
                }
            }
        }

        public abstract class Animation
        {
            public bool IsTemporary { get; protected set; }
            public float Duration { get; protected set; }
            public abstract void Play(Animator animator, float progress);
        }

        private class AnimationState
        {
            public Animator Animator;
            public float ElapsedTime;
            public bool IsReversing;
        }
    }
}