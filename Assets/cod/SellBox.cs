using UnityEngine;

[DisallowMultipleComponent]
public sealed class SellBox : MonoBehaviour
{
    public const string Prompt = "اضغط E لبيع المحاصيل";

    [SerializeField, Min(1)] private int cropPrice = 10;
    public int CropPrice => cropPrice;

    private void OnValidate() { cropPrice = Mathf.Max(1, cropPrice); }

    public bool IsLookedAt(Camera playerCamera, float distance)
    {
        if (!isActiveAndEnabled || !playerCamera || distance <= 0) return false;
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        return Physics.Raycast(ray, out RaycastHit hit, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            && hit.collider.GetComponentInParent<SellBox>() == this;
    }

    // Called by the existing shared E interaction router; this component never
    // reads input itself, so one key press cannot also start a pickup.
    public bool TrySell()
    {
        if (!isActiveAndEnabled || FarmActionRunner.IsBusy) return false;
        FarmInventory inventory = FarmInventory.Instance;
        if (!inventory) return false;
        if (inventory.CropCount == 0)
        {
            if (FarmHUD.Instance) FarmHUD.Instance.ShowMessage("لا توجد محاصيل للبيع");
            return false;
        }

        int cropCount = inventory.CropCount;
        int proceeds = inventory.SellAll(cropPrice);
        if (proceeds <= 0)
        {
            if (FarmHUD.Instance) FarmHUD.Instance.ShowMessage("تعذر إتمام البيع");
            return false;
        }
        if (FarmHUD.Instance) FarmHUD.Instance.ShowMessage("تم بيع " + cropCount + " محاصيل مقابل " + proceeds, 4f);
        return true;
    }
}
