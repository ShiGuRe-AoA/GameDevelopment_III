using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 地图管理器（单例）。
/// 负责：地图间传送、Cinemachine 虚拟摄像机切换、重生点管理。
/// 挂载在场景中的 MapManager GameObject 上。
/// </summary>
public class MapManager : MonoBehaviour
{
    public static MapManager Instance { get; private set; }

    // ================================================================================
    // 事件
    // ================================================================================
    /// <summary>地图即将切换（fromMap, toMap），可在此时做淡入淡出等效果</summary>
    public static event Action<MapIdentity, MapIdentity> OnMapAboutToChange;

    /// <summary>地图切换完成（fromMap, toMap）</summary>
    public static event Action<MapIdentity, MapIdentity> OnMapChanged;

    // ================================================================================
    // Inspector
    // ================================================================================
    [Header("玩家")]
    [SerializeField] private Transform playerTransform;

    [Header("淡入淡出")]
    [SerializeField] private CanvasGroup fadeOverlay;
    [SerializeField] private float fadeInDuration = 0.4f;
    [SerializeField] private float fadeOutDuration = 0.4f;
    [SerializeField] private float blackHoldDuration = 1f;

    [Header("地图摄像机绑定")]
    [SerializeField] private List<MapCameraBinding> cameraBindings = new();

    [Header("初始地图")]
    [SerializeField] private MapIdentity startMap = MapIdentity.Farm;

    [Header("传送冷却")]
    [SerializeField] private float transitionCooldown = 0.5f;

    // ================================================================================
    // 运行时
    // ================================================================================
    private Dictionary<MapIdentity, MapCameraBinding> bindingDict;
    public MapIdentity CurrentMap { get; private set; }
    private bool isTransitioning;

    // ================================================================================
    // 序列化子类型
    // ================================================================================
    [System.Serializable]
    public class SpawnPoint
    {
        public string name;
        public Transform point;
    }

    [System.Serializable]
    public class MapCameraBinding
    {
        public MapIdentity mapId;
        [Tooltip("该地图对应的 Cinemachine Virtual Camera")]
        public CinemachineVirtualCamera virtualCamera;
        [Tooltip("当未指定具体出生点时使用的默认位置")]
        public Transform defaultSpawnPoint;
        [Tooltip("该地图内命名的出生点列表")]
        public List<SpawnPoint> namedSpawnPoints = new();

        /// <summary>根据名称获取出生点，找不到则返回默认出生点</summary>
        public Transform GetSpawnPoint(string name)
        {
            if (!string.IsNullOrEmpty(name) && namedSpawnPoints != null)
            {
                foreach (var sp in namedSpawnPoints)
                {
                    if (sp != null && sp.name == name && sp.point != null)
                        return sp.point;
                }
            }
            return defaultSpawnPoint;
        }
    }

    // ================================================================================
    // 生命周期
    // ================================================================================
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        BuildDictionary();
    }

    private void Start()
    {
        // 尝试从 WorldState 自动获取玩家 Transform
        if (playerTransform == null && WorldState.Instance != null)
            playerTransform = WorldState.Instance.PlayerTransform;

        // 确保遮罩初始透明
        if (fadeOverlay != null)
            fadeOverlay.alpha = 0f;

        ActivateMap(startMap);
    }

    private void BuildDictionary()
    {
        bindingDict = new Dictionary<MapIdentity, MapCameraBinding>();
        foreach (var binding in cameraBindings)
        {
            if (binding == null || binding.mapId == MapIdentity.None) continue;
            bindingDict[binding.mapId] = binding;
        }
    }

    // ================================================================================
    // 公开 API
    // ================================================================================
    /// <summary>
    /// 传送到目标地图。
    /// </summary>
    /// <param name="targetMap">目标地图</param>
    /// <param name="spawnPointName">出生点名称，留空使用默认出生点</param>
    public void TransitionTo(MapIdentity targetMap, string spawnPointName = "")
    {
        if (isTransitioning) return;
        if (targetMap == CurrentMap) return;
        if (!bindingDict.TryGetValue(targetMap, out var targetBinding))
        {
            Debug.LogError($"[MapManager] 未找到地图 {targetMap} 的摄像机绑定");
            return;
        }

        StartCoroutine(TransitionRoutine(targetMap, targetBinding, spawnPointName));
    }

    /// <summary>直接激活指定地图（不经过传送流程，用于初始加载）</summary>
    public void ActivateMap(MapIdentity mapId)
    {
        if (!bindingDict.TryGetValue(mapId, out var binding))
        {
            Debug.LogError($"[MapManager] 未找到地图 {mapId} 的摄像机绑定");
            return;
        }

        // 关掉所有摄像机
        foreach (var kvp in bindingDict)
        {
            if (kvp.Value.virtualCamera != null)
                kvp.Value.virtualCamera.gameObject.SetActive(false);
        }

        // 激活目标摄像机
        if (binding.virtualCamera != null)
            binding.virtualCamera.gameObject.SetActive(true);

        CurrentMap = mapId;
    }

    // ================================================================================
    // 内部
    // ================================================================================
    private IEnumerator TransitionRoutine(MapIdentity targetMap, MapCameraBinding targetBinding, string spawnPointName)
    {
        isTransitioning = true;
        MapIdentity fromMap = CurrentMap;

        // 1. 黑屏淡入
        if (fadeOverlay != null)
        {
            fadeOverlay.blocksRaycasts = true;
            yield return fadeOverlay.DOFade(1f, fadeInDuration).SetEase(Ease.InQuad).WaitForCompletion();
        }

        OnMapAboutToChange?.Invoke(fromMap, targetMap);

        // 2. 切换摄像机
        SwitchCamera(targetMap, targetBinding);

        // 3. 移动玩家到出生点
        Transform spawnPoint = targetBinding.GetSpawnPoint(spawnPointName);
        if (playerTransform != null && spawnPoint != null)
            playerTransform.position = spawnPoint.position;

        CurrentMap = targetMap;
        OnMapChanged?.Invoke(fromMap, targetMap);

        // 4. 黑屏保持
        yield return new WaitForSeconds(blackHoldDuration);

        // 5. 黑屏淡出
        if (fadeOverlay != null)
        {
            yield return fadeOverlay.DOFade(0f, fadeOutDuration).SetEase(Ease.OutQuad).WaitForCompletion();
            fadeOverlay.blocksRaycasts = false;
        }
        else
        {
            yield return new WaitForSeconds(transitionCooldown);
        }

        isTransitioning = false;
    }

    private void SwitchCamera(MapIdentity targetMap, MapCameraBinding targetBinding)
    {
        // 关闭当前摄像机
        if (bindingDict.TryGetValue(CurrentMap, out var currentBinding) && currentBinding.virtualCamera != null)
            currentBinding.virtualCamera.gameObject.SetActive(false);

        // 激活目标摄像机
        if (targetBinding.virtualCamera != null)
            targetBinding.virtualCamera.gameObject.SetActive(true);
    }
}
