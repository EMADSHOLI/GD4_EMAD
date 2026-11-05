using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-700)]
public class FarmQuestTracker : MonoBehaviour
{
    public HoeingArea[] hoeAreas;
    public FarmPlot[] plots;
    public Text[] questRows;
    public Text summary;
    public GameObject questPanel;
    readonly bool[] complete=new bool[8];
    readonly string[] labels={"احرث قطعة أرض","ازرع المحصول","اسقِ النباتات","سمّد النباتات","احصد المحصول","بع المحاصيل","اشترِ بقرة","نظّف قطعة أرض"};
    bool[] readySeen;
    FarmInventory inventory;
    int previousCrops,previousBalance;
    public bool IsComplete(int index) { return index>=0 && index<complete.Length && complete[index]; }
    public bool[] CaptureState(){return (bool[])complete.Clone();}
    public void RestoreState(bool[] values)
    {
        System.Array.Clear(complete,0,complete.Length);
        if(values!=null)System.Array.Copy(values,complete,System.Math.Min(values.Length,complete.Length));
        Bind();Refresh();
    }
    void Awake(){readySeen=new bool[plots==null?0:plots.Length];Refresh();}
    void OnEnable(){Bind();}
    void OnDisable(){if(inventory)inventory.Changed-=InventoryChanged;inventory=null;}
    void Bind()
    {
        if(inventory)inventory.Changed-=InventoryChanged;
        inventory=FarmInventory.Instance;
        previousCrops=inventory?inventory.CropCount:0;previousBalance=inventory?inventory.Balance:0;
        if(inventory)inventory.Changed+=InventoryChanged;
    }
    void InventoryChanged()
    {
        if(!inventory)return;
        if(inventory.CropCount<previousCrops && inventory.Balance>previousBalance)Mark(5);
        previousCrops=inventory.CropCount;previousBalance=inventory.Balance;
    }
    void Update()
    {
        if(inventory!=FarmInventory.Instance)Bind();
        if(questPanel && questPanel.activeSelf==FarmShop.IsOpen)questPanel.SetActive(!FarmShop.IsOpen);
        if(hoeAreas!=null)foreach(var hoe in hoeAreas)if(hoe && hoe.IsTilled)Mark(0);
        if(hoeAreas!=null)foreach(var hoe in hoeAreas)
            if(hoe && hoe.weeds && hoe.weeds.patches!=null && hoe.weeds.patches.Length>0 && hoe.weeds.IsClean)Mark(7);
        if(plots!=null)for(int i=0;i<plots.Length;i++)
        {
            var plot=plots[i];if(!plot)continue;
            if(plot.Stage!=FarmPlot.CropStage.Empty)Mark(1);
            if(plot.Stage==FarmPlot.CropStage.Growing || plot.Stage==FarmPlot.CropStage.Ready)Mark(2);
            if(plot.IsFertilized)Mark(3);
            if(plot.Stage==FarmPlot.CropStage.Ready)readySeen[i]=true;
            if(readySeen[i] && plot.Stage==FarmPlot.CropStage.Empty)Mark(4);
        }
        if(FarmShop.Instance && FarmShop.Instance.CowPurchased)Mark(6);
    }
    void Mark(int index){if(complete[index])return;complete[index]=true;Refresh();}
    void Refresh()
    {
        int count=0;
        for(int i=0;i<complete.Length;i++)
        {
            if(complete[i])count++;
            if(questRows!=null && i<questRows.Length && questRows[i])
            {
                questRows[i].text=ArabicText.ForDisplay(labels[i]+(complete[i]?"  •  تم":"  •  لم يتم"));
                questRows[i].color=complete[i]?new Color(.61f,.88f,.39f):new Color(.87f,.88f,.81f);
            }
        }
        if(summary)summary.text=ArabicText.ForDisplay("المهام: "+count+" من "+complete.Length);
    }
}
