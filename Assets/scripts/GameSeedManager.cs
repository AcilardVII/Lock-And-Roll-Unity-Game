using UnityEngine;
using System.Collections.Generic;

public class GameSeedManager : MonoBehaviour
{
    public static GameSeedManager Instance { get; private set; }

    [Header("Seed Ayarları")]
    public string seedString = ""; 
    public int currentSeedInt;    

    [Header("Durum")]
    public bool isInitialized = false;

    
    private System.Random rng;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); 

        InitializeSeed();
    }

    public void InitializeSeed()
    {
        if (string.IsNullOrEmpty(seedString))
        {
            seedString = System.DateTime.Now.Ticks.ToString();
        }

        currentSeedInt = seedString.GetHashCode();

        rng = new System.Random(currentSeedInt);

        isInitialized = true;
        Debug.Log($"[GameSeedManager] Seed Başlatıldı: {seedString} ({currentSeedInt})");
    }


    public int GetRandomInt(int min, int max)
    {
        if (rng == null) InitializeSeed();
        return rng.Next(min, max);
    }

   
    public float GetRandomFloat()
    {
        if (rng == null) InitializeSeed();
        return (float)rng.NextDouble();
    }

    public void ShuffleList<T>(List<T> list)
    {
        if (rng == null) InitializeSeed();

        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}