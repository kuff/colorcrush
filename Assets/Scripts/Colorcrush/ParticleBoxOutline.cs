// Copyright (C) 2024 Peter Guld Leth

#region

using UnityEngine;

#endregion

[RequireComponent(typeof(ParticleSystem))]
public class ParticleBoxOutline : MonoBehaviour
{
    public RectTransform targetBox;
    private ParticleSystem particleSystem;
    private ParticleSystem.ShapeModule shapeModule;

    private void Start()
    {
        particleSystem = GetComponent<ParticleSystem>();
        shapeModule = particleSystem.shape;
        UpdateShape();
    }

    private void Update()
    {
        UpdateShape();
    }

    private void UpdateShape()
    {
        if (targetBox != null)
        {
            // Convert the size of the RectTransform to world units
            var worldSize = new Vector3(targetBox.rect.width, targetBox.rect.height, 0);
            var scaleFactor = targetBox.lossyScale;
            worldSize = Vector3.Scale(worldSize, scaleFactor);

            shapeModule.scale = worldSize;
            shapeModule.position = targetBox.position;
        }
    }
}