using System;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public sealed class FarmInventory : MonoBehaviour
{
    public static FarmInventory Instance { get; private set; }

    [SerializeField, Min(0)] private int cropCount;
    [SerializeField, Min(0)] private int balance;

    public int CropCount => cropCount;
    public int Balance => balance;
    public event Action Changed;
    public void RestoreState(int crops, int money)
    {
        cropCount=Math.Max(0,crops); balance=Math.Max(0,money); Changed?.Invoke();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { Instance = null; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            enabled = false;
            return;
        }
        Instance = this;
        cropCount = Mathf.Max(0, cropCount);
        balance = Mathf.Max(0, balance);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void AddCrops(int amount)
    {
        if (amount <= 0) return;
        cropCount = (int)Math.Min(int.MaxValue, (long)cropCount + amount);
        Changed?.Invoke();
    }

    public bool TrySpend(int amount)
    {
        if (amount <= 0 || balance < amount) return false;
        balance -= amount;
        Changed?.Invoke();
        return true;
    }

    // Both values change before observers run, so another E press cannot sell twice.
    // Refuse an overflowing transaction without consuming the player's crops.
    public int SellAll(int unitPrice)
    {
        if (unitPrice <= 0 || cropCount <= 0) return 0;
        long proceeds = (long)cropCount * unitPrice;
        if (proceeds > int.MaxValue - (long)balance) return 0;

        cropCount = 0;
        balance += (int)proceeds;
        Changed?.Invoke();
        return (int)proceeds;
    }
}
