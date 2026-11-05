using UnityEngine;

public class WeedsClearance : MonoBehaviour
{
    public Camera playerCamera;
    public HoeingArea hoeArea;
    public WeedPatch[] patches;
    [Min(.5f)] public float interactionDistance = 3f;
    public int Remaining
    {
        get
        {
            int count=0;
            if(patches!=null)foreach(var patch in patches)if(patch && !patch.IsCleared)count++;
            return count;
        }
    }
    public bool IsClean => Remaining==0;
    public string Prompt => "اضغط E لإزالة الأعشاب • المتبقي: "+Remaining;
    public void PatchCleared()
    {
        FarmHUD.Instance?.ShowMessage(IsClean?"الأرض نظيفة! اضغط V للحراثة":"تمت إزالة الأعشاب • المتبقي: "+Remaining);
    }
}
