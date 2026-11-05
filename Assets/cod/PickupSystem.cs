using UnityEngine;

public class PickupSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Animator handsAnimator;
    [Header("Pickup Settings")]
    [SerializeField] private float pickupDistance = 3f;
    [SerializeField] private float animationDuration = 1f;
    private void Update()
    {
        if (!playerCamera) return;
        string prompt = "V حراثة • P زراعة • F سماد • E تفاعل";
        SellBox box = null; FarmPlot plot = null; GameObject pickup = null;
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out RaycastHit hit, pickupDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            box = hit.collider.GetComponentInParent<SellBox>();
            plot = hit.collider.GetComponentInParent<FarmPlot>();
            if (!box && !plot && hit.collider.CompareTag("Pickup")) pickup = hit.collider.gameObject;
        }
        DoorInteraction door = DoorInteraction.NearestInRange();
        WeedPatch weed = WeedPatch.LookedAt(playerCamera, pickupDistance);
        if (weed) prompt = weed.clearance.Prompt;
        else if (box) prompt = SellBox.Prompt;
        else if (plot && plot.InRange()) prompt = plot.InteractionPrompt();
        else if (pickup) prompt = "اضغط E للالتقاط";
        else if (door) prompt = door.IsOpening ? "الباب يتحرك" : DoorInteraction.Prompt;
        else { var nearby = FarmPlot.NearestInRange(); if (nearby) prompt = nearby.InteractionPrompt(); }
        FarmHUD.Instance?.SetPrompt(FarmActionRunner.IsBusy ? "" : prompt);
        if (!Input.GetKeyDown(KeyCode.E) || FarmActionRunner.IsBusy) return;
        if (weed) { weed.TryRemove(); return; }
        if (box) { box.TrySell(); return; }
        if (plot) { plot.TryHarvest(); return; }
        if (!pickup && door) { door.TryOpen(); return; }
        if (!pickup || !FarmActionRunner.Instance) return;
        GameObject target = pickup;
        FarmActionRunner.Instance.TryBegin(this, "الالتقاط", "Pickup", () => { if (target) Destroy(target); },
            () => target && target.activeInHierarchy, Mathf.Max(.1f, animationDuration));
    }
}
