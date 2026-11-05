using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-250)]
public class FarmShop : MonoBehaviour
{
    public static FarmShop Instance { get; private set; }
    public static bool IsOpen => Instance && Instance.open;
    public static bool BlocksWorldInput => IsOpen || Time.frameCount == lastClosedFrame;
    public static bool BlocksPointerInput => BlocksWorldInput || (Instance && Instance.openButton &&
        Instance.openButton.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(
            Instance.openButton.GetComponent<RectTransform>(), Input.mousePosition, null));
    private static int lastClosedFrame = -1;
    [Header("Cow purchase")]
    [Min(1)] public int cowPrice = 100;
    public GameObject cow;
    public Transform cowSpawnPoint;
    public FirstPersonController player;
    [Header("Store UI")]
    public GameObject shopPanel;
    public Button openButton;
    public Button buyButton;
    public Text buyLabel;
    public Text priceText;
    public Text balanceText;
    public Text statusText;
    public bool CowPurchased { get; private set; }
    private bool open, purchasing, savedMove, savedLook, savedLock;
    private CursorLockMode savedCursorLock;
    private bool savedCursorVisible;
    private FarmInventory inventory;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Instance = null; lastClosedFrame = -1; }
    void Awake()
    {
        if (Instance && Instance != this) { enabled = false; return; }
        Instance = this;
        if (cow) cow.SetActive(false);
        if (shopPanel) shopPanel.SetActive(false);
    }
    void OnEnable() { BindInventory(); }
    void OnDisable()
    {
        Close();
        if (inventory) inventory.Changed -= Refresh;
        inventory = null;
    }
    void OnDestroy() { if (Instance == this) Instance = null; }
    void OnValidate() { cowPrice = Mathf.Max(1, cowPrice); }
    void Update()
    {
        if(FarmMenu.IsVisible)return;
        if (inventory != FarmInventory.Instance) BindInventory();
        if (Input.GetKeyDown(KeyCode.B)) { if (open) Close(); else Open(); }
        else if (open && Input.GetKeyDown(KeyCode.Escape)) Close();
    }
    void BindInventory()
    {
        if (inventory) inventory.Changed -= Refresh;
        inventory = FarmInventory.Instance;
        if (inventory) inventory.Changed += Refresh;
        Refresh();
    }
    public void Open()
    {
        if (open || !isActiveAndEnabled || !shopPanel || !player) return;
        if (FarmActionRunner.IsBusy) { FarmHUD.Instance?.ShowMessage("انتظر انتهاء العملية أولًا"); return; }
        savedMove = player.playerCanMove; savedLook = player.cameraCanMove; savedLock = player.lockCursor;
        savedCursorLock = Cursor.lockState; savedCursorVisible = Cursor.visible;
        open = true;
        player.playerCanMove = false; player.cameraCanMove = false; player.lockCursor = false;
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        shopPanel.SetActive(true);
        if (openButton) openButton.gameObject.SetActive(false);
        SetStatus(CowPurchased ? "البقرة موجودة في الحظيرة" : "");
        Refresh();
    }
    public void Close()
    {
        if (!open) return;
        open = false; lastClosedFrame = Time.frameCount;
        if (shopPanel) shopPanel.SetActive(false);
        if (openButton) openButton.gameObject.SetActive(true);
        if (player)
        {
            player.playerCanMove = savedMove; player.cameraCanMove = savedLook; player.lockCursor = savedLock;
        }
        Cursor.lockState = savedCursorLock; Cursor.visible = savedCursorVisible;
    }
    public void BuyCow() { TryPurchaseCow(); }
    public void RestorePurchase(bool purchased)
    {
        CowPurchased=purchased;
        if(cow)
        {
            if(purchased && cowSpawnPoint)cow.transform.SetPositionAndRotation(cowSpawnPoint.position,cowSpawnPoint.rotation);
            cow.SetActive(purchased);
            var animator=cow.GetComponent<Animator>();
            if(purchased && animator && animator.runtimeAnimatorController){animator.enabled=true;animator.Play("cow",0,0);}
        }
        Refresh();
    }
    public bool TryPurchaseCow()
    {
        if (!open || purchasing || CowPurchased || !cow || !cowSpawnPoint || cowPrice < 1 || !inventory) return false;
        if (inventory.Balance < cowPrice) { SetStatus("رصيدك غير كافٍ لشراء البقرة"); return false; }
        purchasing = true;
        try
        {
            if (!inventory.TrySpend(cowPrice)) { SetStatus("رصيدك غير كافٍ لشراء البقرة"); return false; }
            CowPurchased = true;
            cow.transform.SetPositionAndRotation(cowSpawnPoint.position, cowSpawnPoint.rotation);
            cow.SetActive(true);
            var animator = cow.GetComponent<Animator>();
            if (animator && animator.runtimeAnimatorController) { animator.enabled = true; animator.Play("cow", 0, 0f); }
            SetStatus("تم الشراء! البقرة الآن في الحظيرة");
            FarmHUD.Instance?.ShowMessage("تم شراء البقرة مقابل " + cowPrice, 4f);
            return true;
        }
        finally { purchasing = false; Refresh(); }
    }
    public void Refresh()
    {
        int balance = inventory ? inventory.Balance : 0;
        if (balanceText) balanceText.text = ArabicText.ForDisplay("رصيدك: " + balance);
        if (priceText) priceText.text = ArabicText.ForDisplay("السعر: " + cowPrice);
        if (buyButton) buyButton.interactable = !CowPurchased && !purchasing && cow && cowSpawnPoint && inventory && balance >= cowPrice;
        if (buyLabel) buyLabel.text = ArabicText.ForDisplay(CowPurchased ? "تم الشراء" : "شراء البقرة");
        if (open && !CowPurchased && inventory && balance < cowPrice) SetStatus("رصيدك غير كافٍ لشراء البقرة");
    }
    void SetStatus(string message) { if (statusText) statusText.text = ArabicText.ForDisplay(message); }
}
