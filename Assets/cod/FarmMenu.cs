using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-300)]
public class FarmMenu : MonoBehaviour
{
    enum Entry { Title,New,Continue }
    static Entry pendingEntry;
    public static FarmMenu Instance {get;private set;}
    public static bool IsVisible=>Instance && Instance.visible;
    static int lastClosedFrame=-100;
    public static bool BlocksWorldInput=>IsVisible || Time.frameCount<=lastClosedFrame+1;
    public static string SettingsPrefix="GD4.Menu.";
    public FirstPersonController player;
    public GameObject overlay,mainPanel,settingsPanel,confirmPanel;
    public GameObject[] gameplayCanvases;
    public Button continueButton;
    public Text saveStatus,volumeLabel,sensitivityLabel;
    public Slider volumeSlider,sensitivitySlider;
    public Toggle fullscreenToggle;
    public Dropdown qualityDropdown;
    public bool SessionActive {get;private set;}
    bool visible,frozen,savedMove,savedLook,savedLock,bypass;
    CursorLockMode savedCursor;
    bool savedCursorVisible;
    float nextSave;
    public bool IsTitleScene=>gameObject.scene.name=="MainMenu";
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset(){Instance=null;pendingEntry=Entry.Title;SettingsPrefix="GD4.Menu.";lastClosedFrame=-100;}
    void Awake()
    {
        if(Instance && Instance!=this){enabled=false;return;}Instance=this;
#if UNITY_EDITOR
        foreach(var key in new[]{"GD4_EMAD.FarmPlayChecks.Pending","GD4_EMAD.ShopChecks.Pending","GD4_EMAD.DoorsCowChecks","GD4_EMAD.MapQuestChecks","GD4_EMAD.MapQuestPurchase"})
            if(UnityEditor.SessionState.GetBool(key,false)){bypass=true;FarmSaveService.SuppressAutoSave=true;}
#endif
        ShowRoot();
    }
    IEnumerator Start()
    {
        var entry=pendingEntry;pendingEntry=Entry.Title;
        ApplyStoredSettings();
        yield return null;
        if(IsTitleScene){RefreshContinue();yield break;}
        FarmSaveService.InitializeSession();
        if(bypass){BeginSession(false);yield break;}
        if(entry==Entry.New){BeginSession(true);yield break;}
        if(entry==Entry.Continue)
        {
            if(FarmSaveService.TryRead(out var data))
            {
                FarmSaveService.Apply(data,player);BeginSession(false);yield break;
            }
            SceneManager.LoadScene("MainMenu");yield break;
        }
        // Playing the farm scene directly also starts gameplay, never a second title menu.
        BeginSession(false);
    }
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            if(FarmShop.IsOpen)return;
            if(visible)
            {
                if(settingsPanel.activeSelf)SaveSettings();
                else if(confirmPanel.activeSelf)CancelNewGame();
                else if(SessionActive)ContinueGame();
            }
            else OpenMenu();
        }
        if(SessionActive && !visible && !FarmSaveService.SuppressAutoSave && Time.unscaledTime>=nextSave &&
            !FarmShop.IsOpen && FarmActionRunner.Instance && FarmActionRunner.Instance.CurrentOperation==null)
        {SaveNow();nextSave=Time.unscaledTime+15f;}
    }
    void LateUpdate()
    {
        if(!visible)return;
        Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        if(player){player.playerCanMove=false;player.cameraCanMove=false;player.lockCursor=false;}
    }
    void ShowRoot()
    {
        visible=true;Time.timeScale=0;
        if(!frozen && player)
        {
            savedMove=player.playerCanMove;savedLook=player.cameraCanMove;savedLock=player.lockCursor;
            savedCursor=Cursor.lockState;savedCursorVisible=Cursor.visible;frozen=true;
            player.playerCanMove=false;player.cameraCanMove=false;player.lockCursor=false;
        }
        if(overlay)overlay.SetActive(true);
        ShowPanel(mainPanel);
        if(gameplayCanvases!=null)foreach(var canvas in gameplayCanvases)if(canvas)canvas.SetActive(false);
        Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        RefreshContinue();
    }
    public void OpenMenu()
    {
        if(visible)return;
        if(FarmShop.IsOpen)FarmShop.Instance.Close();
        FarmActionRunner.Instance?.Cancel();
        bool saved=SaveNow();ShowRoot();
        SetStatus(saved?"تم حفظ تقدّمك تلقائيًا":"تعذّر الحفظ. تحقّق من مساحة القرص وحاول مجددًا.");
    }
    public void RefreshContinue()
    {
        bool hasSave=FarmSaveService.TryRead(out _);
        if(continueButton)continueButton.interactable=SessionActive || hasSave;
    }
    public void NewGame()
    {
        if(SessionActive || File.Exists(FarmSaveService.SavePath))
        {ShowPanel(confirmPanel);return;}
        ConfirmNewGame();
    }
    public void CancelNewGame(){ShowRoot();}
    public void ConfirmNewGame()
    {
        pendingEntry=Entry.New;SessionActive=false;SceneManager.LoadScene("SampleScene");
    }
    public void ContinueGame()
    {
        if(SessionActive){BeginSession(false);return;}
        if(!FarmSaveService.TryRead(out _)){RefreshContinue();return;}
        pendingEntry=Entry.Continue;SceneManager.LoadScene("SampleScene");
    }
    void BeginSession(bool saveInitial)
    {
        SessionActive=true;visible=false;
        lastClosedFrame=Time.frameCount;
        if(overlay)overlay.SetActive(false);
        if(gameplayCanvases!=null)foreach(var canvas in gameplayCanvases)if(canvas)canvas.SetActive(true);
        Time.timeScale=1;
        if(frozen && player)
        {player.playerCanMove=savedMove;player.cameraCanMove=savedLook;player.lockCursor=savedLock;frozen=false;}
        Cursor.lockState=player && player.lockCursor?CursorLockMode.Locked:savedCursor;
        Cursor.visible=player && player.lockCursor?false:savedCursorVisible;
        nextSave=Time.unscaledTime+15f;
        if(saveInitial && !FarmSaveService.SuppressAutoSave)SaveNow();
    }
    public bool SaveNow()
    {
        if(!SessionActive || FarmSaveService.SuppressAutoSave)return false;
        bool success=FarmSaveService.Write(player,out var error);
        if(!success)SetStatus("تعذّر الحفظ: "+error);
        return success;
    }
    public void ReturnToTitle()
    {
        if(SessionActive && !SaveNow()){SetStatus("تعذّر حفظ التقدّم؛ بقيت في اللعبة.");return;}
        SessionActive=false;pendingEntry=Entry.Title;SceneManager.LoadScene("MainMenu");
    }
    public void OpenSettings()
    {
        ShowPanel(settingsPanel);
        volumeSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(SettingsPrefix+"Volume",1f));
        sensitivitySlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(SettingsPrefix+"Sensitivity",player?player.mouseSensitivity:2f));
        fullscreenToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt(SettingsPrefix+"Fullscreen",Screen.fullScreen?1:0)==1);
        qualityDropdown.ClearOptions();qualityDropdown.AddOptions(new System.Collections.Generic.List<string>(QualitySettings.names));
        qualityDropdown.SetValueWithoutNotify(Mathf.Clamp(PlayerPrefs.GetInt(SettingsPrefix+"Quality",QualitySettings.GetQualityLevel()),0,QualitySettings.names.Length-1));
        UpdateSettingsLabels();
    }
    public void UpdateSettingsLabels()
    {
        if(volumeLabel)volumeLabel.text="Volume  "+Mathf.RoundToInt(volumeSlider.value*100)+"%";
        if(sensitivityLabel)sensitivityLabel.text="Mouse Sensitivity  "+sensitivitySlider.value.ToString("F1");
    }
    public void SaveSettings()
    {
        PlayerPrefs.SetFloat(SettingsPrefix+"Volume",volumeSlider.value);
        PlayerPrefs.SetFloat(SettingsPrefix+"Sensitivity",sensitivitySlider.value);
        PlayerPrefs.SetInt(SettingsPrefix+"Fullscreen",fullscreenToggle.isOn?1:0);
        PlayerPrefs.SetInt(SettingsPrefix+"Quality",qualityDropdown.value);PlayerPrefs.Save();
        ApplyStoredSettings();ShowRoot();
    }
    void ApplyStoredSettings()
    {
        AudioListener.volume=Mathf.Clamp01(PlayerPrefs.GetFloat(SettingsPrefix+"Volume",1f));
        if(player)player.mouseSensitivity=Mathf.Clamp(PlayerPrefs.GetFloat(SettingsPrefix+"Sensitivity",2f),.1f,10f);
        int quality=Mathf.Clamp(PlayerPrefs.GetInt(SettingsPrefix+"Quality",QualitySettings.GetQualityLevel()),0,QualitySettings.names.Length-1);
        if(QualitySettings.GetQualityLevel()!=quality)QualitySettings.SetQualityLevel(quality,true);
        if(!Application.isEditor)Screen.fullScreen=PlayerPrefs.GetInt(SettingsPrefix+"Fullscreen",Screen.fullScreen?1:0)==1;
    }
    public void ExitGame()
    {
        if(SessionActive && !SaveNow() && !FarmSaveService.SuppressAutoSave){SetStatus("تعذّر الحفظ. حاول مجددًا قبل الخروج.");return;}
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying=false;
#else
        Application.Quit();
#endif
    }
    void OnApplicationPause(bool paused){if(paused && SessionActive && !FarmSaveService.SuppressAutoSave){FarmActionRunner.Instance?.Cancel();SaveNow();}}
    void OnApplicationQuit(){if(SessionActive && !FarmSaveService.SuppressAutoSave){FarmActionRunner.Instance?.Cancel();SaveNow();}}
    void OnDestroy(){if(Instance==this){Instance=null;Time.timeScale=1;}}
    void ShowPanel(GameObject selected)
    {
        foreach(var panel in new[]{mainPanel,settingsPanel,confirmPanel})if(panel)panel.SetActive(panel==selected);
    }
    void SetStatus(string value){if(saveStatus)saveStatus.text=ArabicText.ForDisplay(value);}
}
