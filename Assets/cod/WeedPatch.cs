using UnityEngine;

public class WeedPatch : MonoBehaviour
{
    public WeedsClearance clearance;
    public Collider interactionCollider;
    public bool IsCleared { get; private set; }
    public void RestoreState(bool cleared){IsCleared=cleared;gameObject.SetActive(!cleared);}
    public bool InRange()
    {
        return clearance && clearance.playerCamera && interactionCollider &&
            Vector3.Distance(clearance.playerCamera.transform.position,interactionCollider.ClosestPoint(clearance.playerCamera.transform.position))<=clearance.interactionDistance;
    }
    public static WeedPatch LookedAt(Camera camera,float distance)
    {
        if(!camera)return null;
        var ray=new Ray(camera.transform.position,camera.transform.forward);
        // Other interaction triggers do not obscure weeds, but solid walls do.
        if(Physics.Raycast(ray,out var solid,distance,~0,QueryTriggerInteraction.Ignore))distance=solid.distance+.01f;
        WeedPatch nearest=null;float closest=distance;
        foreach(var hit in Physics.RaycastAll(ray,distance,~0,QueryTriggerInteraction.Collide))
        {
            var patch=hit.collider.GetComponent<WeedPatch>();
            if(patch && patch.isActiveAndEnabled && !patch.IsCleared && patch.InRange() && hit.distance<=closest)
            {nearest=patch;closest=hit.distance;}
        }
        return nearest;
    }
    public bool TryRemove()
    {
        if(!isActiveAndEnabled || IsCleared || !clearance || !clearance.hoeArea || clearance.hoeArea.IsTilled ||
            !InRange() || LookedAt(clearance.playerCamera,clearance.interactionDistance)!=this || !FarmActionRunner.Instance)return false;
        return FarmActionRunner.Instance.TryBegin(this,"إزالة الأعشاب","Pickup",()=>
        {
            IsCleared=true;
            gameObject.SetActive(false);
            clearance.PatchCleared();
        },()=>!IsCleared && clearance && clearance.hoeArea && !clearance.hoeArea.IsTilled && InRange());
    }
    void OnDisable(){if(!IsCleared)FarmActionRunner.Instance?.CancelFor(this);}
}
