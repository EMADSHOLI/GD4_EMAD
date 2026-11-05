using System.Collections.Generic;
using UnityEngine;

public class DoorInteraction : MonoBehaviour
{
    public const string Prompt = "اضغط E لفتح الباب";
    public Animator doorAnimator;
    public Transform player;
    public Collider[] proximityColliders;
    [Min(.1f)] public float interactionDistance = 3f;
    public bool IsOpening { get; private set; }
    private static readonly List<DoorInteraction> doors = new List<DoorInteraction>();
    private int startedFrame;
    public void RestorePose(bool opened,float normalized)
    {
        if(!doorAnimator)return;
        doorAnimator.ResetTrigger("OpenDoor");
        doorAnimator.Play(opened?"Open":"Closed",0,Mathf.Clamp01(normalized));doorAnimator.Update(0);
        IsOpening=opened;startedFrame=Time.frameCount;
    }
    private void OnEnable() { if (!doors.Contains(this)) doors.Add(this); }
    private void OnDisable() { doors.Remove(this); IsOpening = false; }
    public float DistanceToPlayer()
    {
        if (!player) return float.PositiveInfinity;
        float distance = float.PositiveInfinity;
        if (proximityColliders != null) foreach (var collider in proximityColliders)
            if (collider && collider.enabled && collider.gameObject.activeInHierarchy)
                distance = Mathf.Min(distance, Vector3.Distance(player.position, collider.ClosestPoint(player.position)));
        return distance;
    }
    public static DoorInteraction NearestInRange()
    {
        DoorInteraction closest = null;
        float best = float.PositiveInfinity;
        foreach (var door in doors)
        {
            if (!door || !door.isActiveAndEnabled) continue;
            float distance = door.DistanceToPlayer();
            if (distance <= door.interactionDistance && distance < best) { closest = door; best = distance; }
        }
        return closest;
    }
    public bool TryOpen()
    {
        if (!isActiveAndEnabled || IsOpening || FarmActionRunner.IsBusy || !doorAnimator ||
            !doorAnimator.isActiveAndEnabled || DistanceToPlayer() > interactionDistance) return false;
        if (!doorAnimator.HasState(0, Animator.StringToHash("Base Layer.Open"))) return false;
        IsOpening = true;
        startedFrame = Time.frameCount;
        doorAnimator.ResetTrigger("OpenDoor");
        doorAnimator.SetTrigger("OpenDoor");
        return true;
    }
    private void Update()
    {
        if (!IsOpening || !doorAnimator || Time.frameCount <= startedFrame + 1) return;
        var state = doorAnimator.GetCurrentAnimatorStateInfo(0);
        if (state.IsName("Closed") && !doorAnimator.IsInTransition(0)) IsOpening = false;
    }
}
