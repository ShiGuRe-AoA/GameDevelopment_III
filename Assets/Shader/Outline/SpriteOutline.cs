using System.Collections.Generic;
using UnityEngine;

public class SpriteOutline : MonoBehaviour
{
    private readonly List<SpriteRenderer> outlineRenderers = new();

    private static readonly Vector2[] Directions =
    {
        Vector2.up,
        Vector2.down,
        Vector2.left,
        Vector2.right,
        new Vector2(1, 1).normalized,
        new Vector2(1, -1).normalized,
        new Vector2(-1, 1).normalized,
        new Vector2(-1, -1).normalized,
    };

    public void Build(
        OutlineTarget target,
        Material outlineMaterial,
        Color outlineColor,
        float pixelOffset,
        float pixelsPerUnit = 100f)
    {
        Clear();

        float worldOffset = pixelOffset / pixelsPerUnit;

        foreach (var source in target.SpriteRenderers)
        {
            if (source == null || source.sprite == null)
                continue;

            foreach (var dir in Directions)
            {
                GameObject child = new GameObject("OutlineRenderer");
                child.transform.SetParent(transform);

                child.transform.position = source.transform.position + (Vector3)(dir * worldOffset);
                child.transform.rotation = source.transform.rotation;
                child.transform.localScale = source.transform.lossyScale;

                var sr = child.AddComponent<SpriteRenderer>();

                sr.sprite = source.sprite;
                sr.color = outlineColor;
                sr.material = outlineMaterial;

                sr.flipX = source.flipX;
                sr.flipY = source.flipY;
                sr.drawMode = source.drawMode;
                sr.size = source.size;
                sr.maskInteraction = source.maskInteraction;

                sr.sortingLayerID = source.sortingLayerID;
                sr.sortingOrder = source.sortingOrder - 1;

                outlineRenderers.Add(sr);
            }
        }
    }

    public void Clear()
    {
        for (int i = outlineRenderers.Count - 1; i >= 0; i--)
        {
            if (outlineRenderers[i] != null)
                Destroy(outlineRenderers[i].gameObject);
        }

        outlineRenderers.Clear();
    }
}