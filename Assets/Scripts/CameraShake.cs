using UnityEngine;

public class CameraShake : MonoBehaviour
{
    private Vector3 originalPos;
    private float shakeDuration = 0f;
    private float shakeMagnitude = 0.2f;

    void Start()
    {
        originalPos = transform.localPosition;
    }

    void LateUpdate()
    {
        if (shakeDuration > 0)
        {
            transform.localPosition = originalPos + Random.insideUnitSphere * shakeMagnitude;
            shakeDuration -= Time.deltaTime;
        }
        else
        {
            transform.localPosition = originalPos;
        }
    }

    public void TriggerShake(float duration, float magnitude)
    {
        shakeDuration = duration;
        shakeMagnitude = magnitude;
    }
}
