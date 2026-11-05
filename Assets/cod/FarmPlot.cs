using System.Collections.Generic;
using UnityEngine;

public class FarmPlot : MonoBehaviour
{
    public enum CropStage { Empty, Planted, Growing, Ready }
    public Transform player;
    public Animator handsAnimator;
    public Collider areaCollider;
    public GameObject[] plants;
    [Min(0.1f)] public float interactionDistance = 3f;
    [Min(1f)] public float growthDuration = 30f;
    [Range(0.1f, 1f)] public float fertilizerGrowthMultiplier = .5f;
    public CropStage Stage { get; private set; }
    public bool IsFertilized { get; private set; }
    public float GrowthProgress { get; private set; }
    public bool IsFertilizing => FarmActionRunner.IsBusy && FarmActionRunner.Instance.CurrentOperation == "التسميد";
    private Vector3[] fullScale;
    private static readonly List<FarmPlot> activePlots = new List<FarmPlot>();

    private void Awake()
    {
        if(fullScale!=null)return;
        if (plants == null) plants = new GameObject[0];
        fullScale = new Vector3[plants.Length];
        for (int i = 0; i < plants.Length; i++) if (plants[i]) { fullScale[i] = plants[i].transform.localScale; plants[i].SetActive(false); }
    }
    private void OnEnable() { if (!activePlots.Contains(this)) activePlots.Add(this); }
    public void RestoreState(int stage,float growth,bool fertilized)
    {
        Awake();
        Stage=(CropStage)Mathf.Clamp(stage,0,3);GrowthProgress=Mathf.Clamp01(growth);IsFertilized=fertilized;
        if(Stage==CropStage.Ready)GrowthProgress=1;
        ApplyPlantScale(Stage==CropStage.Empty || Stage==CropStage.Ready?1:Mathf.Lerp(.18f,1,GrowthProgress));
        foreach(var plant in plants)if(plant)plant.SetActive(Stage!=CropStage.Empty);
    }
    private void OnDisable() { activePlots.Remove(this); FarmActionRunner.Instance?.CancelFor(this); }
    public bool InRange() => player && areaCollider && Vector3.Distance(player.position, areaCollider.ClosestPoint(player.position)) <= interactionDistance;
    public static FarmPlot NearestInRange()
    {
        FarmPlot nearest = null; float distance = float.PositiveInfinity;
        foreach (var plot in activePlots)
            if (plot && plot.InRange())
            {
                float d = (plot.player.position - plot.areaCollider.bounds.center).sqrMagnitude;
                if (d < distance) { nearest = plot; distance = d; }
            }
        return nearest;
    }
    private void Update()
    {
        if (Stage == CropStage.Growing)
        {
            float seconds = Mathf.Max(1f, growthDuration) * (IsFertilized ? fertilizerGrowthMultiplier : 1f);
            GrowthProgress = Mathf.Min(1f, GrowthProgress + Time.deltaTime / Mathf.Max(.1f, seconds));
            ApplyPlantScale(Mathf.Lerp(.18f, 1f, GrowthProgress));
            if (GrowthProgress >= 1f) { Stage = CropStage.Ready; FarmHUD.Instance?.ShowMessage("المحصول جاهز للحصاد"); }
        }
        if (Input.GetKeyDown(KeyCode.P) && NearestInRange() == this) TryPlant();
    }
    private bool Begin(string label, string state, System.Action done, System.Func<bool> validate)
    {
        return InRange() && FarmActionRunner.Instance && FarmActionRunner.Instance.TryBegin(this, label, state, done, validate);
    }
    public bool TryPlant()
    {
        return Stage == CropStage.Empty && Begin("الزراعة", "Pickup", () =>
        {
            Stage = CropStage.Planted; GrowthProgress = 0f; IsFertilized = false;
            ApplyPlantScale(.18f); foreach (var plant in plants) if (plant) plant.SetActive(true);
        }, () => Stage == CropStage.Empty);
    }
    public bool TryWater()
    {
        return Stage == CropStage.Planted && Begin("السقي", "Watering", () =>
        {
            Stage = CropStage.Growing; GrowthProgress = 0f;
            FarmHUD.Instance?.ShowMessage("بدأ نمو النباتات");
        }, () => Stage == CropStage.Planted);
    }
    public bool TryFertilize()
    {
        if (IsFertilized || Stage == CropStage.Empty || Stage == CropStage.Ready) return false;
        return Begin("التسميد", "Fertilize", () => IsFertilized = true,
            () => !IsFertilized && Stage != CropStage.Empty);
    }
    public bool TryHarvest()
    {
        if (Stage != CropStage.Ready || !FarmInventory.Instance) return false;
        return Begin("الحصاد", "Pickup", () =>
        {
            int quantity = 0;
            foreach (var plant in plants) if (plant && plant.activeSelf) { quantity++; plant.SetActive(false); }
            Stage = CropStage.Empty; IsFertilized = false; GrowthProgress = 0f;
            ApplyPlantScale(1f);
            FarmInventory.Instance.AddCrops(quantity);
            FarmHUD.Instance?.ShowMessage("تم حصاد " + quantity + " محاصيل");
        }, () => Stage == CropStage.Ready && FarmInventory.Instance);
    }
    public string InteractionPrompt()
    {
        switch (Stage)
        {
            case CropStage.Empty: return "اضغط P للزراعة";
            case CropStage.Planted: return "زر الفأرة الأيسر للسقي • F للتسميد";
            case CropStage.Growing: return IsFertilized ? "النباتات تنمو " + Mathf.RoundToInt(GrowthProgress * 100) + "%" : "النباتات تنمو • F للتسميد";
            default: return "اضغط E لحصاد المحاصيل";
        }
    }
    private void ApplyPlantScale(float factor) { for (int i = 0; i < plants.Length; i++) if (plants[i]) plants[i].transform.localScale = fullScale[i] * factor; }
}
