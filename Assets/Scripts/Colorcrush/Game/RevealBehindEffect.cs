// Copyright (C) 2024 Peter Guld Leth

#region

using UnityEngine;

#endregion

namespace Colorcrush.Game
{
    public class RevealBehindEffect : MonoBehaviour
    {
        public Material circleMaterial;
        public float expandSpeed = 1.0f;

        private float circleSize;
        private float startTime;

        private void Start()
        {
            startTime = Time.time;
            circleMaterial.SetFloat("_CircleSize", 0f);
        }

        private void Update()
        {
            var elapsedTime = Time.time - startTime;
            if (elapsedTime >= 3.0f)
            {
                circleSize += Time.deltaTime * expandSpeed;
                circleMaterial.SetFloat("_CircleSize", circleSize);
            }
        }
    }
}