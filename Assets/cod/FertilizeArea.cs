using UnityEngine;

public class FertilizeArea : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Animator handsAnimator;
    [SerializeField] private Collider areaCollider;
    [SerializeField] private GameObject[] plantsToReveal;
    [SerializeField] private float interactionDistance = 3f;
    private FarmPlot plot;
    private bool legacyFertilized;
    public bool IsFertilizing => plot ? plot.IsFertilizing : FarmActionRunner.IsBusy && FarmActionRunner.Instance.CurrentOperation == "التسميد";
    public bool IsFertilized => plot ? plot.IsFertilized : legacyFertilized;
    public static bool HandsBusy(Animator animator) => FarmActionRunner.IsBusy;
    private void Awake()
    {
        plot = GetComponent<FarmPlot>();
        if (!plot && plantsToReveal != null) foreach (var plant in plantsToReveal) if (plant) plant.SetActive(false);
    }
    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.F)) return;
        var nearest = FarmPlot.NearestInRange();
        if (plot ? nearest == plot : nearest == null) TryFertilize();
    }
    public bool TryFertilize()
    {
        if (plot) return plot.TryFertilize();
        if (legacyFertilized || !player || !handsAnimator || !areaCollider || !FarmActionRunner.Instance) return false;
        if (Vector3.Distance(player.position, areaCollider.ClosestPoint(player.position)) > interactionDistance) return false;
        return FarmActionRunner.Instance.TryBegin(this, "التسميد", "Fertilize", () =>
        {
            legacyFertilized = true;
            if (plantsToReveal != null) foreach (var plant in plantsToReveal) if (plant) plant.SetActive(true);
        }, () => !legacyFertilized);
    }
    private void OnDisable() { FarmActionRunner.Instance?.CancelFor(this); }
}
