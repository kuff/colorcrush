using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Colorcrush
{
    public class RectangleOutline : MonoBehaviour
    {
        public RectTransform targetUIObject; // The UI object to outline
        public GameObject pointPrefab; // Prefab for the points
        public int numberOfPoints = 50; // Number of points around the rectangle
        public float radius = 10f; // Radius of each point from the rectangle edge
        public float animationSpeed = 1f; // Speed of the animation
        public float noiseStrength = 5f; // Strength of the noise for the bubbly effect

        private List<GameObject> points;
        private float[] pointOffsets;

        void Start()
        {
            points = new List<GameObject>();
            pointOffsets = new float[numberOfPoints];

            // Create points
            for (int i = 0; i < numberOfPoints; i++)
            {
                GameObject point = Instantiate(pointPrefab, transform);
                points.Add(point);
                pointOffsets[i] = Random.Range(0f, Mathf.PI * 2f); // Randomize initial offset for each point
            }

            // Start the animation
            StartCoroutine(AnimateOutline());
        }

        IEnumerator AnimateOutline()
        {
            while (true)
            {
                for (int i = 0; i < numberOfPoints; i++)
                {
                    float t = ((float)i / numberOfPoints + Time.time * animationSpeed + pointOffsets[i]) % 1;
                    Vector2 position = GetPointOnRectangle(t) + Random.insideUnitCircle * noiseStrength;

                    // Position the points around the target UI object
                    points[i].GetComponent<RectTransform>().anchoredPosition = targetUIObject.anchoredPosition + position;
                }

                yield return null;
            }
        }

        Vector2 GetPointOnRectangle(float t)
        {
            Rect rect = targetUIObject.rect;
            float perimeter = 2 * (rect.width + rect.height);
            float distance = t * perimeter;

            if (distance < rect.width)
            {
                return new Vector2(distance - rect.width / 2, rect.height / 2 + radius);
            }
            else if (distance < rect.width + rect.height)
            {
                return new Vector2(rect.width / 2 + radius, rect.height / 2 - (distance - rect.width));
            }
            else if (distance < 2 * rect.width + rect.height)
            {
                return new Vector2(rect.width / 2 - (distance - rect.width - rect.height), -rect.height / 2 - radius);
            }
            else
            {
                return new Vector2(-rect.width / 2 - radius, -rect.height / 2 + (distance - 2 * rect.width - rect.height));
            }
        }
    }
}
