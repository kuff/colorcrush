// Copyright (C) 2024 Peter Guld Leth

#region

using Colorcrush.Util;
using UnityEngine;

#endregion

namespace Colorcrush.Game
{
    public class RevealBehindEffect : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Material used for the reveal circle effect")]
        private Material circleMaterial;

        [SerializeField]
        [Tooltip("Speed at which the reveal circle expands")]
        private float expandSpeed = 1.0f;

        private float _circleSize;
        private float _startTime;

        private void Awake()
        {
            _startTime = Time.time;
        }

        private void Update()
        {
            var elapsedTime = Time.time - _startTime;
            if (elapsedTime >= 3.0f)
            {
                _circleSize += Time.deltaTime * expandSpeed;
                ShaderManager.SetFloat(circleMaterial, "_CircleSize", _circleSize);
            }
        }
    }
}