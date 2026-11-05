using System;
using UnityEngine;

/// <summary>One owner and one completion callback for all hand actions.</summary>
[DefaultExecutionOrder(-100)]
public class FarmActionRunner : MonoBehaviour
{
    public const float FarmDuration = 6f;
    public static FarmActionRunner Instance { get; private set; }
    public static bool IsBusy => FarmMenu.BlocksWorldInput || FarmShop.BlocksWorldInput || (Instance != null && (Instance.busy || Instance.lastFinishedFrame == Time.frameCount));
    public Animator handsAnimator;
    public float Progress => busy ? Mathf.Clamp01(elapsed / duration) : 0f;
    public string CurrentOperation { get; private set; }
    private bool busy;
    private int lastFinishedFrame = -1;
    private float elapsed, duration, savedSpeed;
    private MonoBehaviour owner;
    private Action completion;
    private Func<bool> stillValid;
    private static readonly int BusyHash = Animator.StringToHash("FarmActionBusy");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { Instance = null; }
    private void Awake() { if (Instance && Instance != this) { enabled = false; return; } Instance = this; }

    public bool TryBegin(MonoBehaviour requester, string label, string stateName, Action onComplete,
        Func<bool> validate = null, float seconds = FarmDuration)
    {
        if (IsBusy || !requester || !requester.isActiveAndEnabled || !handsAnimator ||
            !handsAnimator.isActiveAndEnabled || !handsAnimator.runtimeAnimatorController || seconds <= 0f) return false;
        if (validate != null && !validate()) return false;
        int state = Animator.StringToHash("Base Layer." + stateName);
        if (!handsAnimator.HasState(0, state)) { Debug.LogError("Missing farming animation state: " + stateName, this); return false; }
        bool hasLock = false;
        foreach (var parameter in handsAnimator.parameters)
        {
            if (parameter.nameHash == BusyHash && parameter.type == AnimatorControllerParameterType.Bool) hasLock = true;
            if (parameter.type == AnimatorControllerParameterType.Trigger) handsAnimator.ResetTrigger(parameter.nameHash);
        }
        if (!hasLock) { Debug.LogError("Run Tools/Farm/Setup to configure the hands Animator.", this); return false; }
        busy = true; owner = requester; completion = onComplete; stillValid = validate;
        duration = seconds; elapsed = 0f; CurrentOperation = label; savedSpeed = handsAnimator.speed;
        handsAnimator.SetBool(BusyHash, true);
        handsAnimator.Play(state, 0, 0f);
        handsAnimator.Update(0f);
        var clips = handsAnimator.GetCurrentAnimatorClipInfo(0);
        if (clips.Length == 0) { Cancel(); return false; }
        handsAnimator.speed = clips[0].clip.length / duration;
        FarmHUD.Instance?.SetProgress(label, 0f);
        return true;
    }

    private void Update()
    {
        if (!busy) return;
        if (!owner || !owner.isActiveAndEnabled || !handsAnimator || !handsAnimator.isActiveAndEnabled ||
            (stillValid != null && !stillValid())) { Cancel(); return; }
        elapsed += Time.deltaTime;
        FarmHUD.Instance?.SetProgress(CurrentOperation, Mathf.Clamp01(elapsed / duration));
        if (elapsed < duration) return;
        // Clear the callback before invoking it. Repeated keys cannot award twice.
        var result = completion;
        completion = null;
        try { result?.Invoke(); }
        finally { Finish(); }
    }

    public void CancelFor(MonoBehaviour requester) { if (busy && owner == requester) Cancel(); }
    public void Cancel() { completion = null; Finish(); }
    private void Finish()
    {
        if (!busy) return;
        if (handsAnimator)
        {
            foreach (var parameter in handsAnimator.parameters)
                if (parameter.type == AnimatorControllerParameterType.Trigger) handsAnimator.ResetTrigger(parameter.nameHash);
            handsAnimator.Play("Base Layer.Idle", 0, 0f);
            handsAnimator.SetBool(BusyHash, false);
            handsAnimator.speed = savedSpeed;
        }
        busy = false; owner = null; stillValid = null; completion = null;
        lastFinishedFrame = Time.frameCount; CurrentOperation = null;
        FarmHUD.Instance?.HideProgress();
    }
    private void OnDisable() { Cancel(); }
    private void OnDestroy() { if (Instance == this) Instance = null; }
}
