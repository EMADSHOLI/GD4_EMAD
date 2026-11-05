using UnityEngine;

public class WateringInput : MonoBehaviour
{
    [SerializeField] private Animator handsAnimator;
    private void Update()
    {
        if (!Input.GetMouseButtonDown(0) || FarmActionRunner.IsBusy || FarmShop.BlocksPointerInput) return;
        var plot = FarmPlot.NearestInRange();
        if (plot) { plot.TryWater(); return; }
        // Preserve free watering animation away from a crop bed.
        FarmActionRunner.Instance?.TryBegin(this, "السقي", "Watering", null);
    }
}
