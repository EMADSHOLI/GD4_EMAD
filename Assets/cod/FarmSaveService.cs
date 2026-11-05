using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public static class FarmSaveService
{
    [Serializable] public class Snapshot
    {
        public int version=1,crops,money;
        public string scene,utc;
        public Vector3 playerPosition;
        public Quaternion playerRotation,cameraRotation;
        public bool cowPurchased;
        public bool[] quests;
        public List<PlotState> plots=new List<PlotState>();
        public List<FlagState> soils=new List<FlagState>(),weeds=new List<FlagState>(),pickups=new List<FlagState>();
        public List<DoorState> doors=new List<DoorState>();
    }
    [Serializable] public class FlagState { public string id;public bool value; }
    [Serializable] public class PlotState { public string id;public int stage;public float growth;public bool fertilized; }
    [Serializable] public class DoorState { public string id;public bool open;public float time; }
    public static string OverridePath;
    public static bool SuppressAutoSave;
    static string[] pickupIds=new string[0];
    public static string SavePath => string.IsNullOrEmpty(OverridePath)?Path.Combine(Application.persistentDataPath,"farm-save-v1.json"):OverridePath;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset(){OverridePath=null;SuppressAutoSave=false;pickupIds=new string[0];}
    static FarmSaveId[] Objects()=>UnityEngine.Object.FindObjectsOfType<FarmSaveId>(true).Where(o=>o.gameObject.scene.IsValid() && o.gameObject.scene.isLoaded).ToArray();
    public static void InitializeSession(){pickupIds=Objects().Where(o=>o.CompareTag("Pickup")).Select(o=>o.Id).ToArray();}
    public static bool TryRead(out Snapshot data)
    {
        data=null;
        try
        {
            if(!File.Exists(SavePath))return false;
            var value=JsonUtility.FromJson<Snapshot>(File.ReadAllText(SavePath));
            if(value==null || value.version!=1 || value.scene!="SampleScene" || value.plots==null || value.soils==null || value.weeds==null || value.doors==null || value.pickups==null || value.quests==null)return false;
            if(float.IsNaN(value.playerPosition.x)||float.IsInfinity(value.playerPosition.x)||float.IsNaN(value.playerPosition.y)||float.IsInfinity(value.playerPosition.y)||float.IsNaN(value.playerPosition.z)||float.IsInfinity(value.playerPosition.z))return false;
            data=value;return true;
        }
        catch(Exception){return false;}
    }
    public static Snapshot Capture(FirstPersonController player)
    {
        var inventory=FarmInventory.Instance;
        var data=new Snapshot{scene="SampleScene",utc=DateTime.UtcNow.ToString("O"),crops=inventory.CropCount,money=inventory.Balance,
            playerPosition=player.transform.position,playerRotation=player.transform.rotation,cameraRotation=player.playerCamera.transform.localRotation,
            cowPurchased=FarmShop.Instance && FarmShop.Instance.CowPurchased};
        var tracker=UnityEngine.Object.FindObjectOfType<FarmQuestTracker>(true);data.quests=tracker?tracker.CaptureState():new bool[8];
        var objects=Objects();var byId=objects.ToDictionary(o=>o.Id);
        foreach(var obj in objects)
        {
            var hoe=obj.GetComponent<HoeingArea>();if(hoe)data.soils.Add(new FlagState{id=obj.Id,value=hoe.IsTilled});
            var weed=obj.GetComponent<WeedPatch>();if(weed)data.weeds.Add(new FlagState{id=obj.Id,value=weed.IsCleared});
            var plot=obj.GetComponent<FarmPlot>();if(plot)data.plots.Add(new PlotState{id=obj.Id,stage=(int)plot.Stage,growth=plot.GrowthProgress,fertilized=plot.IsFertilized});
            var door=obj.GetComponent<DoorInteraction>();if(door && door.doorAnimator)
            {var state=door.doorAnimator.GetCurrentAnimatorStateInfo(0);data.doors.Add(new DoorState{id=obj.Id,open=door.IsOpening || state.IsName("Open"),time=state.IsName("Open")?Mathf.Clamp01(state.normalizedTime):0});}
        }
        foreach(var id in pickupIds)data.pickups.Add(new FlagState{id=id,value=byId.TryGetValue(id,out var obj) && obj.gameObject.activeSelf});
        return data;
    }
    public static bool Write(FirstPersonController player,out string error)
    {
        error=null;
        try
        {
            var data=Capture(player);var directory=Path.GetDirectoryName(SavePath);Directory.CreateDirectory(directory);
            string temp=SavePath+".tmp";File.WriteAllText(temp,JsonUtility.ToJson(data,true));
            if(File.Exists(SavePath))File.Replace(temp,SavePath,SavePath+".bak");else File.Move(temp,SavePath);
            return true;
        }
        catch(Exception e){error=e.Message;return false;}
    }
    public static void Apply(Snapshot data,FirstPersonController player)
    {
        var objects=Objects().ToDictionary(o=>o.Id);
        foreach(var state in data.weeds)if(objects.TryGetValue(state.id,out var obj)){var weed=obj.GetComponent<WeedPatch>();if(weed)weed.RestoreState(state.value);}
        foreach(var state in data.soils)if(objects.TryGetValue(state.id,out var obj)){var hoe=obj.GetComponent<HoeingArea>();if(hoe)hoe.RestoreState(state.value);}
        foreach(var state in data.plots)if(objects.TryGetValue(state.id,out var obj)){var plot=obj.GetComponent<FarmPlot>();if(plot)plot.RestoreState(state.stage,state.growth,state.fertilized);}
        FarmInventory.Instance.RestoreState(data.crops,data.money);
        FarmShop.Instance?.RestorePurchase(data.cowPurchased);
        foreach(var state in data.doors)if(objects.TryGetValue(state.id,out var obj)){var door=obj.GetComponent<DoorInteraction>();if(door)door.RestorePose(state.open,state.time);}
        foreach(var state in data.pickups)if(objects.TryGetValue(state.id,out var obj))obj.gameObject.SetActive(state.value);
        var tracker=UnityEngine.Object.FindObjectOfType<FarmQuestTracker>(true);if(tracker)tracker.RestoreState(data.quests);
        player.RestoreFarmView(data.playerPosition,data.playerRotation,data.cameraRotation);Physics.SyncTransforms();
    }
}
