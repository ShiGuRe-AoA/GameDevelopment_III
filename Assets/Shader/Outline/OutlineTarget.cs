using UnityEngine;

public class OutlineTarget : MonoBehaviour
{
    [SerializeField] private SpriteRenderer[] spriteRenderers;

    public SpriteRenderer[] SpriteRenderers => spriteRenderers;

    private void Reset()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
    }
}