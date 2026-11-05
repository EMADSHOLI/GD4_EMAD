using UnityEngine;

public class HoeingArea : MonoBehaviour
{
    [Header("Clean ground before tilling")]
    public WeedsClearance weeds;
    [Header("Hoe Animation")]
    [SerializeField] private Animator handsAnimator;
    [Header("Soil Objects")]
    [SerializeField] private GameObject normalSoil;
    [SerializeField] private GameObject tilledSoil;
    private readonly System.Collections.Generic.HashSet<Collider> players = new System.Collections.Generic.HashSet<Collider>();
    public bool IsTilled { get; private set; }
    public void RestoreState(bool tilled)
    {
        IsTilled=tilled;
        if(normalSoil)normalSoil.SetActive(!tilled);
        if(tilledSoil)tilledSoil.SetActive(tilled);
    }
    private void Start() { if (normalSoil) normalSoil.SetActive(true); if (tilledSoil) tilledSoil.SetActive(false); }
    private void Update() { if (Input.GetKeyDown(KeyCode.V)) TryHoe(); }
    public bool TryHoe()
    {
        if (players.Count == 0 || IsTilled || !normalSoil || !tilledSoil || !FarmActionRunner.Instance) return false;
        if (FarmActionRunner.IsBusy) return false;
        if (weeds && !weeds.IsClean)
        {
            FarmHUD.Instance?.ShowMessage("نظّف الأعشاب أولًا • المتبقي: " + weeds.Remaining);
            return false;
        }
        return FarmActionRunner.Instance.TryBegin(this, "الحراثة", "Hoeing", () =>
        {
            IsTilled = true; tilledSoil.SetActive(true); normalSoil.SetActive(false);
        }, () => !IsTilled && normalSoil && tilledSoil && (!weeds || weeds.IsClean));
    }
    private void OnTriggerEnter(Collider other) { if (other.CompareTag("Player")) players.Add(other); }
    private void OnTriggerStay(Collider other) { if (other.CompareTag("Player")) players.Add(other); }
    private void OnTriggerExit(Collider other) { players.Remove(other); }
    private void OnDisable() { players.Clear(); FarmActionRunner.Instance?.CancelFor(this); }
}
