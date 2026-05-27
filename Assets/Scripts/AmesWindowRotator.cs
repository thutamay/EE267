using UnityEngine;

public class AmesWindowRotator : MonoBehaviour
{
    public float rotationSpeedDegreesPerSecond = 720f;

    private void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeedDegreesPerSecond * Time.deltaTime, Space.World);
    }
}
