using UnityEngine;

// Keeps the view-model projection synchronized with look, zoom and head bob.
[DefaultExecutionOrder(1000)]
public class FirstPersonHandsView : MonoBehaviour
{
    public Camera worldCamera;
    public Camera handsCamera;
    void LateUpdate()
    {
        if (!worldCamera || !handsCamera) return;
        handsCamera.fieldOfView = worldCamera.fieldOfView;
        handsCamera.aspect = worldCamera.aspect;
        handsCamera.rect = worldCamera.rect;
    }
}
