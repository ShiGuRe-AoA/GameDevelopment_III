using DG.Tweening;
using UnityEngine;

public class Tree_Entity : EntityRuntimeBase
{
    private const int maxHealth = 10;
    private const int spawnID = 10103;
    private const int spawnAmount = 15;

    public int curHealth = 10;

    /// <summary>树的视觉 Transform（锚点位于根部），由 TreeInitializer 注入</summary>
    public Transform TreeTransform { get; set; }

    public void Logging()
    {
        curHealth -= 1;

        if (curHealth <= 0)
        {
            FallingDown();
        }
    }

    public override void OnAwake()
    {
        base.OnAwake();
        curHealth = maxHealth;
    }

    /// <summary>
    /// 树倒下：先注销实体数据，播放 DOTween 旋转倒下动画，
    /// 动画结束后生成掉落物并销毁 GameObject
    /// </summary>
    public void FallingDown()
    {
        // 1. 立即注销实体（清除地图占用，阻止后续交互）
        WorldState.Instance.DestroyEntity(EntityId);

        if (TreeTransform == null)
        {
            // 兜底：无 Transform 引用时直接生成掉落物
            WorldState.Instance.SpawnItem(PivotPos, spawnID, spawnAmount, 1.5f);
            return;
        }

        // 2. 播放倒下动画（绕 Z 轴旋转倒下，锚点在根部）
        TreeTransform.DOKill();
        Vector3 targetRotation = TreeTransform.eulerAngles + new Vector3(0f, 0f, 85f);
        SpriteRenderer sr = TreeTransform.GetComponent<SpriteRenderer>();

        TreeTransform.DORotate(targetRotation, 0.5f, RotateMode.Fast)
            .SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                // 3. 倒下瞬间生成掉落物
                WorldState.Instance.SpawnItem(PivotPos, spawnID, spawnAmount, 1.5f);

                // 4. 淡出消失后销毁
                if (sr != null)
                {
                    sr.DOFade(0f, 0.8f)
                        .SetEase(Ease.OutQuad)
                        .OnComplete(() =>
                        {
                            if (TreeTransform != null)
                                Object.Destroy(TreeTransform.gameObject);
                        });
                }
                else if (TreeTransform != null)
                {
                    Object.Destroy(TreeTransform.gameObject);
                }
            });
    }
}
