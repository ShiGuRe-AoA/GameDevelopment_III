using TMPro;
using UnityEngine;

/// <summary>
/// 单例 Debug HUD：在 Build 中无法看 Console 时，将调试信息输出到场景中的 TMP_Text。
/// 用法：DebugHUD.Log("任意消息"); 或 DebugHUD.Instance.Append("消息");
/// </summary>
public class DebugHUD : MonoBehaviour
{
    public static DebugHUD Instance { get; private set; }

    [SerializeField] private TMP_Text textDisplay;
    [SerializeField] private int maxLines = 20;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (textDisplay != null)
            textDisplay.text = string.Empty;
    }

    // ================================================================================
    // Public API
    // ================================================================================

    /// <summary>追加一行文本（自动换行，超出上限移除最旧行）</summary>
    public void Append(string msg)
    {
        if (textDisplay == null) return;

        textDisplay.text += msg + "\n";
        TrimLines();
    }

    /// <summary>覆盖全部文本</summary>
    public void Set(string msg)
    {
        if (textDisplay == null) return;
        textDisplay.text = msg;
    }

    /// <summary>清空</summary>
    public void Clear()
    {
        if (textDisplay == null) return;
        textDisplay.text = string.Empty;
    }

    // ================================================================================
    // 静态快捷方式
    // ================================================================================

    /// <summary>静态快捷追加（实例不存在时静默忽略）</summary>
    public static void Log(string msg)
    {
        Instance?.Append(msg);
    }

    /// <summary>静态快捷覆盖</summary>
    public static void SetLog(string msg)
    {
        Instance?.Set(msg);
    }

    /// <summary>静态快捷清空</summary>
    public static void ClearLog()
    {
        Instance?.Clear();
    }

    // ================================================================================
    // Helpers
    // ================================================================================
    private void TrimLines()
    {
        if (string.IsNullOrEmpty(textDisplay.text)) return;

        string[] lines = textDisplay.text.Split('\n');
        if (lines.Length <= maxLines) return;

        // 保留最后 maxLines 行（去掉末尾空行）
        int start = lines.Length - maxLines;
        textDisplay.text = string.Join("\n", lines, start, maxLines);
    }
}
