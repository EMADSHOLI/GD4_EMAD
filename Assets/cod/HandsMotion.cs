using UnityEngine;

public class HandsMotion : MonoBehaviour
{
    public float swayAmount = 2f;
    public float smoothness = 10f;
    public float bobSpeed = 10f;
    public float bobAmount = 0.02f;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private float timer;

    void Start()
    {
        startPosition = transform.localPosition;
        startRotation = transform.localRotation;
    }

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        Quaternion targetRotation = startRotation * Quaternion.Euler(
            -mouseY * swayAmount,
            mouseX * swayAmount,
            mouseX * swayAmount * 0.5f
        );

        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            targetRotation,
            smoothness * Time.deltaTime
        );

        float movement = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        ).magnitude;

        Vector3 targetPosition = startPosition;

        if (movement > 0.1f)
        {
            timer += Time.deltaTime * bobSpeed;

            targetPosition += new Vector3(
                Mathf.Cos(timer * 0.5f) * bobAmount,
                Mathf.Sin(timer) * bobAmount,
                0
            );
        }
        else
        {
            timer = 0;
        }

        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            targetPosition,
            smoothness * Time.deltaTime
        );
    }
}