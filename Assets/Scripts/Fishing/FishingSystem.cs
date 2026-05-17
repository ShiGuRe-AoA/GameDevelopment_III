using System.Collections;
using UnityEngine;

public class FishingSession
{
    // 如果未来设不同水域不同鱼
    // public Vector3Int FishingGrid { get; private set; }
    public ItemStack HookedFish { get; private set; }

    public FishingSession(ItemStack hookedFish)
    {
        // FishingGrid = fishingGrid;
        HookedFish = hookedFish;
    }
}

public enum FishingPhase
{
    None,
    Wait,
    Bite,
    Game
}

public class FishingSystem : MonoBehaviour, IMinuteUpdatable, ITickUpdatable
{
    public static FishingSystem Instance { get; private set; }

    [SerializeField] private FishingLootTable_SO defaultLootTable;
    [SerializeField] private FishingGameView fishingGameView;

    [Header("Wait / 游戏分钟")]
    [SerializeField] private int minHookWaitMinutes = 2;
    [SerializeField] private int maxHookWaitMinutes = 5;
    [SerializeField] private float hookChance = 0.8f;

    [Header("Bite / 现实秒")]
    [SerializeField] private float maxBiteTime = 1.5f;

    public bool IsWaitingForPull => isFishing && phase == FishingPhase.Bite;

    private bool isFishing;
    private bool pullRequested;

    private FishingPhase phase = FishingPhase.None;

    private int waitMinuteCounter;
    private int targetWaitMinutes;

    private float biteTimer;

    private PlayerController currentPlayer;
    private FishingSession currentSession;

    private void Awake()
    {
        Instance = this;
    }

    public void BeginFishing(PlayerController player)
    {
        if (isFishing)
        {
            return;
        }

        isFishing = true;
        pullRequested = false;

        currentPlayer = player;
        currentSession = null;

        phase = FishingPhase.Wait;
        waitMinuteCounter = 0;
        targetWaitMinutes = Random.Range(minHookWaitMinutes, maxHookWaitMinutes + 1);

        currentPlayer.StartFishingWait();

        TimeManager.Instance.Register(this);
    }

    public void RequestPullHook()
    {
        if (!IsWaitingForPull)
        {
            return;
        }

        pullRequested = true;
    }

    public void OnMinuteUpdate()
    {
        if (!isFishing || phase != FishingPhase.Wait)
        {
            return;
        }

        waitMinuteCounter++;

        if (waitMinuteCounter < targetWaitMinutes)
        {
            return;
        }

        ResolveHookCheck();
    }

    public void OnTickUpdate(float deltaTime)
    {
        if (!isFishing || phase != FishingPhase.Bite)
        {
            return;
        }

        TickBite(deltaTime);
    }

    private void ResolveHookCheck()
    {
        currentPlayer.FinishFishing(); // 退出 FishingWait

        bool hooked = Random.value <= hookChance;

        if (!hooked)
        {
            EndFishing();
            return;
        }

        EnterBite();
    }

    private void EnterBite()
    {
        phase = FishingPhase.Bite;
        pullRequested = false;
        biteTimer = 0f;

        currentPlayer.StartFishingBite();
    }

    private void TickBite(float deltaTime)
    {
        biteTimer += deltaTime;

        if (pullRequested)
        {
            PullHook();
            return;
        }

        if (biteTimer >= maxBiteTime)
        {
            currentPlayer.FinishFishing(); // 退出 FishingBite
            EndFishing();
        }
    }

    private void PullHook()
    {
        ItemStack hookedFish = defaultLootTable.Roll();

        currentSession = new FishingSession(hookedFish);

        phase = FishingPhase.Game;

        currentPlayer.FinishFishing(); // 退出 FishingBite
        currentPlayer.StartFishingGame(currentSession);

        fishingGameView.Open(currentSession, OnFishingGameFinished);
    }

    private void OnFishingGameFinished(bool success)
    {
        if (success && currentSession != null)
        {
            GiveFishToPlayer(currentSession.HookedFish);
        }

        currentPlayer.FinishFishing(); // 退出 FishingGame
        EndFishing();
    }

    private void GiveFishToPlayer(ItemStack fish)
    {
        Vector3Int playerGrid = WorldState.Instance.WorldToCell(
            WorldState.Instance.PlayerPos()
        );

        WorldState.Instance.SpawnItem(playerGrid, fish);
    }

    private void EndFishing()
    {
        TimeManager.Instance.Unregister(this);

        isFishing = false;
        pullRequested = false;

        phase = FishingPhase.None;

        waitMinuteCounter = 0;
        targetWaitMinutes = 0;
        biteTimer = 0f;

        currentPlayer = null;
        currentSession = null;
    }
}