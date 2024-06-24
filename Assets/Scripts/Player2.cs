using UnityEngine;

public class Player2 : MonoBehaviour
{
    private Vector3 startPosition;
    public float oscillationAmplitude = 1.0f;
    public float oscillationSpeed = 1.0f;
    private float timeOffset; // Time offset to start at the peak

    void Start()
    {
        startPosition = transform.position;
        // Set the initial time offset so that the sine function starts at its peak
        timeOffset = Mathf.PI / (2 * oscillationSpeed) - Time.time;
        ResetPositionToTopOfCycle();
    }

    void Update()
    {
        float newYPosition = startPosition.y + Oscillate(Time.time + timeOffset, oscillationSpeed, oscillationAmplitude);
        transform.position = new Vector3(startPosition.x, newYPosition, startPosition.z);
    }

    float Oscillate(float time, float speed, float amplitude)
    {
        return Mathf.Sin(time * speed) * amplitude;
    }

    public void ResetPositionToTopOfCycle()
    {
        float maxYPosition = startPosition.y + oscillationAmplitude;
        transform.position = new Vector3(startPosition.x, maxYPosition, startPosition.z);
    }
}