using UnityEngine;

public class OutlineTarget : MonoBehaviour, IHoverTarget
{
    private Renderer[] renderers;

    private bool isOutlined;

    private void Reset()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    private void Awake()
    {
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    public void OnHoverEnter()
    {
        SetOutlined(true);
    }

    public void OnHoverExit()
    {
        SetOutlined(false);
    }

    private void OnDisable()
    {
        SetOutlined(false);
    }

    public void SetOutlined(bool value)
    {
        if (isOutlined == value)
            return;

        isOutlined = value;

        foreach (var r in renderers)
        {
            if (r == null)
                continue;

            if (value)
                OutlineRegistry.Add(r);
            else
                OutlineRegistry.Remove(r);
        }
    }
}