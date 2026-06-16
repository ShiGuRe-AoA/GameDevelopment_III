using TMPro;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class TextOrderSetter : MonoBehaviour
{
    [SerializeField] private TMP_Text tmpText;
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int sortingOrder = 1000;

    private void Reset()
    {
        tmpText = GetComponent<TMP_Text>();
        ApplySorting();
    }

    private void OnValidate()
    {
        ApplySorting();
    }

    private void Awake()
    {
        ApplySorting();
    }

    private void ApplySorting()
    {
        if (tmpText == null)
            tmpText = GetComponent<TMP_Text>();

        if (tmpText == null)
            return;

        Renderer r = tmpText.GetComponent<Renderer>();

        if (r == null)
            return;

        r.sortingLayerName = sortingLayerName;
        r.sortingOrder = sortingOrder;

#if UNITY_EDITOR
        if (!Application.isPlaying)
            EditorUtility.SetDirty(r);
#endif
    }
}