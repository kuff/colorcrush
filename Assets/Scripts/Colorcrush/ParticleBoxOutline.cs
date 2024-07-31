using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleBoxOutline : MonoBehaviour
{
    public RectTransform targetBox;
    private ParticleSystem particleSystem;
    private ParticleSystem.ShapeModule shapeModule;

    void Start()
    {
        particleSystem = GetComponent<ParticleSystem>();
        shapeModule = particleSystem.shape;
        UpdateShape();
    }

    void Update()
    {
        UpdateShape();
    }

    void UpdateShape()
    {
        if (targetBox != null)
        {
            // Convert the size of the RectTransform to world units
            Vector3 worldSize = new Vector3(targetBox.rect.width, targetBox.rect.height, 0);
            Vector3 scaleFactor = targetBox.lossyScale;
            worldSize = Vector3.Scale(worldSize, scaleFactor);

            shapeModule.scale = worldSize;
            shapeModule.position = targetBox.position;
        }
    }
}
