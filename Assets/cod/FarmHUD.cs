using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-900)]
public sealed class FarmHUD : MonoBehaviour
{
    public static FarmHUD Instance { get; private set; }

    [Header("Scene UI references")]
    [SerializeField] private Text cropCountText;
    [SerializeField] private Text balanceText;
    [SerializeField] private GameObject progressPanel;
    [SerializeField] private Text operationText;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private Text promptText;
    [SerializeField] private Text messageText;

    private FarmInventory inventory;
    private float messageUntil;
    private bool hasMessage;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { Instance = null; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { enabled = false; return; }
        Instance = this;
        if (!cropCountText && !balanceText && !progressPanel) BuildFallbackUI();
        HideProgress();
        SetPrompt(string.Empty);
        if (messageText) messageText.text = string.Empty;
    }

    private void OnEnable() { BindInventory(); }

    private void OnDisable()
    {
        if (inventory) inventory.Changed -= RefreshCounts;
        inventory = null;
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }

    private void Update()
    {
        if (inventory != FarmInventory.Instance) BindInventory();
        if (hasMessage && Time.unscaledTime >= messageUntil)
        {
            hasMessage = false;
            if (messageText) messageText.text = string.Empty;
        }
    }

    public void Configure(Text crops, Text money, GameObject panel, Text operation,
        Slider slider, Text prompt, Text message)
    {
        cropCountText = crops;
        balanceText = money;
        progressPanel = panel;
        operationText = operation;
        progressSlider = slider;
        promptText = prompt;
        messageText = message;
        if (Application.isPlaying) { HideProgress(); RefreshCounts(); }
    }

    private void BindInventory()
    {
        if (inventory) inventory.Changed -= RefreshCounts;
        inventory = FarmInventory.Instance;
        if (inventory) inventory.Changed += RefreshCounts;
        RefreshCounts();
    }

    private void RefreshCounts()
    {
        if (cropCountText) cropCountText.text = ArabicText.ForDisplay("المحاصيل: " + (inventory ? inventory.CropCount : 0));
        if (balanceText) balanceText.text = ArabicText.ForDisplay("الرصيد: " + (inventory ? inventory.Balance : 0));
    }

    public void SetProgress(string label, float normalized)
    {
        if (progressPanel) progressPanel.SetActive(true);
        if (operationText) operationText.text = ArabicText.ForDisplay(label);
        if (progressSlider) progressSlider.normalizedValue = Mathf.Clamp01(normalized);
    }

    public void HideProgress()
    {
        if (progressPanel) progressPanel.SetActive(false);
        if (progressSlider) progressSlider.normalizedValue = 0;
    }

    public void ShowMessage(string message, float duration = 3f)
    {
        if (messageText) messageText.text = ArabicText.ForDisplay(message);
        messageUntil = Time.unscaledTime + Mathf.Max(0, duration);
        hasMessage = true;
    }

    public void SetPrompt(string prompt)
    {
        if (promptText) promptText.text = ArabicText.ForDisplay(prompt);
    }

    // Inspector-wired UI is preferred. This also keeps the component usable if
    // it is added to another test scene without running the editor setup.
    private void BuildFallbackUI()
    {
        var canvasObject = new GameObject("FarmCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.GetComponent<Canvas>().sortingOrder = 100;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        Font font = Font.CreateDynamicFontFromOSFont(new[] { "Tahoma", "Arial", "Segoe UI" }, 30);
        cropCountText = CreateText(canvasObject.transform, "CropCount", font, new Vector2(1, 1), new Vector2(-230, -55), new Vector2(420, 50));
        balanceText = CreateText(canvasObject.transform, "Balance", font, new Vector2(1, 1), new Vector2(-230, -105), new Vector2(420, 50));
        promptText = CreateText(canvasObject.transform, "Prompt", font, new Vector2(0.5f, 0), new Vector2(0, 200), new Vector2(1100, 60));
        messageText = CreateText(canvasObject.transform, "Message", font, new Vector2(0.5f, 1), new Vector2(0, -90), new Vector2(1150, 60));
        progressPanel = new GameObject("ActionProgress", typeof(RectTransform));
        progressPanel.transform.SetParent(canvasObject.transform, false);
        SetRect(progressPanel.GetComponent<RectTransform>(), new Vector2(0.5f, 0), new Vector2(0, 110), new Vector2(620, 100));
        operationText = CreateText(progressPanel.transform, "Operation", font, new Vector2(0.5f, 0.5f), new Vector2(0, 25), new Vector2(620, 50));
        var sliderObject = new GameObject("Progress", typeof(RectTransform), typeof(Image), typeof(Slider));
        sliderObject.transform.SetParent(progressPanel.transform, false);
        SetRect(sliderObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0, -25), new Vector2(600, 24));
        sliderObject.GetComponent<Image>().color = new Color(0.04f, 0.08f, 0.06f, 0.9f);
        sliderObject.GetComponent<Image>().raycastTarget = false;
        var fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillObject.transform.SetParent(sliderObject.transform, false);
        var fill = fillObject.GetComponent<RectTransform>();
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;
        fillObject.GetComponent<Image>().color = new Color(0.35f, 0.8f, 0.4f, 1);
        fillObject.GetComponent<Image>().raycastTarget = false;
        progressSlider = sliderObject.GetComponent<Slider>();
        progressSlider.fillRect = fill;
        progressSlider.interactable = false;
        progressSlider.transition = Selectable.Transition.None;
        progressSlider.minValue = 0;
        progressSlider.maxValue = 1;
    }

    private static Text CreateText(Transform parent, string name, Font font, Vector2 anchor, Vector2 position, Vector2 size)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Shadow));
        obj.transform.SetParent(parent, false);
        SetRect(obj.GetComponent<RectTransform>(), anchor, position, size);
        var text = obj.GetComponent<Text>();
        text.font = font;
        text.fontSize = 30;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        obj.GetComponent<Shadow>().effectColor = Color.black;
        obj.GetComponent<Shadow>().effectDistance = new Vector2(2, -2);
        return text;
    }

    private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
