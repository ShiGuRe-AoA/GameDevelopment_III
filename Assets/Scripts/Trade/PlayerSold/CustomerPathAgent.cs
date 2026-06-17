using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class CustomerPathAgent : MonoBehaviour
{
    private static readonly HashSet<CustomerPathAgent> ActiveAgents = new();

    #region Inspector

    [Header("移动")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float arriveDistance = 0.06f;
    [SerializeField] private float pathNodeArriveDistance = 0.1f;

    [Header("寻路")]
    [SerializeField] private int maxPathSearchCount = 500;

    [Tooltip("如果目标格被占用，是否允许把目标格作为终点。QueueSlot 贴近摊位时建议开启。")]
    [SerializeField] private bool allowOccupiedTargetCell = true;

    [Tooltip("目标世界坐标变化超过该距离时，才重新计算路径。")]
    [SerializeField] private float repathTargetMoveDistance = 0.02f;

    [Header("四向移动稳定")]
    [SerializeField] private float axisSwitchTolerance = 0.04f;

    [Header("顾客动态避障")]
    [SerializeField] private bool avoidOtherCustomers = true;
    [SerializeField] private int otherCustomerAvoidPenalty = 20;
    [SerializeField] private bool waitWhenNextCellOccupied = true;
    [SerializeField] private float blockedRepathDelay = 0.35f;

    [Header("顾客防重叠")]
    [SerializeField] private bool useCustomerSeparation = true;
    [SerializeField] private float customerSeparationRadius = 0.55f;
    [SerializeField] private float customerSeparationStrength = 1.2f;
    [SerializeField] private float maxSeparationStep = 0.035f;

    [Header("徘徊")]
    [SerializeField] private float wanderRadius = 4f;
    [SerializeField] private float wanderWaitTime = 2f;
    [SerializeField] private int maxWanderPickTryCount = 12;

    #endregion

    #region Runtime

    private Rigidbody2D rb;

    private readonly List<Vector3Int> pathCells = new();
    private int pathIndex;

    private bool hasMoveTarget;
    private Vector3Int currentTargetCell;
    private Vector2 lastMoveTargetPos;

    private bool faceUpOnArrive;
    private bool suppressIdleSeparation;

    private Vector2 moveDirExp;
    private Vector2 lastFourDirInput;
    private Direction facingDir = Direction.Down;

    private float blockedTimer;

    private bool isWandering;
    private Vector3Int wanderCenterCell;
    private float wanderTimer;

    #endregion

    #region Properties

    public Direction FacingDir => facingDir;
    public bool IsMoving => moveDirExp.sqrMagnitude > 0.0001f;
    public bool HasMoveTarget => hasMoveTarget;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        ActiveAgents.Add(this);
    }

    private void OnDisable()
    {
        ActiveAgents.Remove(this);
    }

    private void FixedUpdate()
    {
        UpdatePathMove();
    }

    #endregion

    #region Public API

    public void MoveTo(Vector2 targetPos, bool faceUpWhenArrive = false)
    {
        WorldState world = WorldState.Instance;
        if (world == null)
            return;

        faceUpOnArrive = faceUpWhenArrive;
        suppressIdleSeparation = false;

        Vector2 curPos = rb != null ? rb.position : (Vector2)transform.position;

        if ((curPos - targetPos).sqrMagnitude <= arriveDistance * arriveDistance)
        {
            StopMove(faceUpWhenArrive);

            if (faceUpWhenArrive)
                FaceUp();

            return;
        }

        Vector3Int targetCell = world.WorldToCell(targetPos);

        bool sameTargetCell =
            hasMoveTarget &&
            targetCell == currentTargetCell;

        bool sameTargetWorld =
            hasMoveTarget &&
            (targetPos - lastMoveTargetPos).sqrMagnitude
            <= repathTargetMoveDistance * repathTargetMoveDistance;

        if (sameTargetCell && sameTargetWorld)
            return;

        currentTargetCell = targetCell;
        lastMoveTargetPos = targetPos;

        hasMoveTarget = true;
        blockedTimer = 0f;

        RebuildPath(targetCell);
    }

    public void StopMove(bool suppressSeparation = false)
    {
        hasMoveTarget = false;
        suppressIdleSeparation = suppressSeparation;

        pathCells.Clear();
        pathIndex = 0;
        blockedTimer = 0f;

        SetMoveInput(Vector2.zero);
    }

    public void FaceUp()
    {
        FaceDirection(Direction.Up);
    }

    public bool HasArrived(Vector2 targetPos)
    {
        Vector2 curPos = rb != null ? rb.position : (Vector2)transform.position;
        return (curPos - targetPos).sqrMagnitude <= arriveDistance * arriveDistance;
    }

    public void BeginWander()
    {
        WorldState world = WorldState.Instance;
        if (world == null)
            return;

        StopMove(false);

        isWandering = true;
        wanderCenterCell = world.WorldToCell(transform.position);
        wanderTimer = 0f;
    }

    public void StopWander()
    {
        isWandering = false;
    }

    public void UpdateWander()
    {
        if (!isWandering)
            return;

        if (hasMoveTarget)
            return;

        wanderTimer -= Time.deltaTime;
        if (wanderTimer > 0f)
            return;

        if (TryPickWanderTarget(out Vector3Int targetCell))
        {
            Vector2 targetWorld = WorldState.Instance.CellToWorld(targetCell);
            MoveTo(targetWorld, false);
        }

        wanderTimer = wanderWaitTime;
    }

    #endregion

    #region Wander

    private bool TryPickWanderTarget(out Vector3Int targetCell)
    {
        targetCell = wanderCenterCell;

        WorldState world = WorldState.Instance;
        if (world == null)
            return false;

        int radius = Mathf.Max(1, Mathf.RoundToInt(wanderRadius));

        for (int i = 0; i < maxWanderPickTryCount; i++)
        {
            int x = Random.Range(-radius, radius + 1);
            int y = Random.Range(-radius, radius + 1);

            Vector3Int candidate = wanderCenterCell + new Vector3Int(x, y, 0);

            if (!IsStaticWalkable(candidate))
                continue;

            if (IsCellOccupiedByOtherCustomer(candidate))
                continue;

            targetCell = candidate;
            return true;
        }

        return false;
    }

    #endregion

    #region Pathfinding

    private static readonly Vector3Int[] FourDirections =
    {
        new Vector3Int(0, 1, 0),
        new Vector3Int(1, 0, 0),
        new Vector3Int(0, -1, 0),
        new Vector3Int(-1, 0, 0)
    };

    private void RebuildPath(Vector3Int targetCell)
    {
        pathCells.Clear();
        pathIndex = 0;

        WorldState world = WorldState.Instance;
        if (world == null)
        {
            StopMove();
            return;
        }

        Vector3Int startCell = world.WorldToCell(transform.position);

        List<Vector3Int> result = FindPath4Dir(startCell, targetCell);

        if (result == null || result.Count == 0)
        {
            StopMove();
            return;
        }

        pathCells.AddRange(result);
    }

    private List<Vector3Int> FindPath4Dir(Vector3Int start, Vector3Int target)
    {
        if (start == target)
            return new List<Vector3Int> { target };

        List<Vector3Int> openList = new();
        HashSet<Vector3Int> closedSet = new();

        Dictionary<Vector3Int, Vector3Int> cameFrom = new();
        Dictionary<Vector3Int, int> gScore = new();

        openList.Add(start);
        gScore[start] = 0;

        int searchCount = 0;

        while (openList.Count > 0)
        {
            searchCount++;
            if (searchCount > maxPathSearchCount)
                break;

            Vector3Int current = GetLowestFCell(openList, gScore, target);

            if (current == target)
                return ReconstructPath(cameFrom, current);

            openList.Remove(current);
            closedSet.Add(current);

            foreach (Vector3Int dir in FourDirections)
            {
                Vector3Int next = current + dir;

                if (closedSet.Contains(next))
                    continue;

                if (!IsWalkable(next, target))
                    continue;

                int tentativeG =
                    gScore[current] +
                    1 +
                    GetOtherCustomerAvoidPenalty(next, target);

                if (!gScore.ContainsKey(next) || tentativeG < gScore[next])
                {
                    cameFrom[next] = current;
                    gScore[next] = tentativeG;

                    if (!openList.Contains(next))
                        openList.Add(next);
                }
            }
        }

        return null;
    }

    private bool IsWalkable(Vector3Int cell, Vector3Int targetCell)
    {
        if (allowOccupiedTargetCell && cell == targetCell)
            return true;

        return IsStaticWalkable(cell);
    }

    private bool IsStaticWalkable(Vector3Int cell)
    {
        WorldState world = WorldState.Instance;
        if (world == null)
            return false;

        return world.CheckEmpty(cell);
    }

    private bool IsCellOccupiedByOtherCustomer(Vector3Int cell)
    {
        if (!avoidOtherCustomers)
            return false;

        WorldState world = WorldState.Instance;
        if (world == null)
            return false;

        foreach (CustomerPathAgent other in ActiveAgents)
        {
            if (other == null || other == this)
                continue;

            if (!other.isActiveAndEnabled)
                continue;

            Vector3Int otherCell = world.WorldToCell(other.transform.position);

            if (otherCell == cell)
                return true;
        }

        return false;
    }

    private int GetOtherCustomerAvoidPenalty(Vector3Int cell, Vector3Int targetCell)
    {
        if (!avoidOtherCustomers)
            return 0;

        if (cell == targetCell)
            return 0;

        return IsCellOccupiedByOtherCustomer(cell)
            ? otherCustomerAvoidPenalty
            : 0;
    }

    private Vector3Int GetLowestFCell(
        List<Vector3Int> openList,
        Dictionary<Vector3Int, int> gScore,
        Vector3Int target)
    {
        Vector3Int best = openList[0];
        int bestF = GetFScore(best, gScore, target);

        for (int i = 1; i < openList.Count; i++)
        {
            Vector3Int cell = openList[i];
            int f = GetFScore(cell, gScore, target);

            if (f < bestF)
            {
                best = cell;
                bestF = f;
            }
        }

        return best;
    }

    private int GetFScore(
        Vector3Int cell,
        Dictionary<Vector3Int, int> gScore,
        Vector3Int target)
    {
        int g = gScore.TryGetValue(cell, out int value)
            ? value
            : int.MaxValue / 2;

        int h = ManhattanDistance(cell, target);

        return g + h;
    }

    private int ManhattanDistance(Vector3Int a, Vector3Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private List<Vector3Int> ReconstructPath(
        Dictionary<Vector3Int, Vector3Int> cameFrom,
        Vector3Int current)
    {
        List<Vector3Int> path = new() { current };

        while (cameFrom.TryGetValue(current, out Vector3Int previous))
        {
            current = previous;
            path.Add(current);
        }

        path.Reverse();

        if (path.Count > 1)
            path.RemoveAt(0);

        return path;
    }

    #endregion

    #region Movement

    private void UpdatePathMove()
    {
        if (!hasMoveTarget || pathCells.Count == 0)
        {
            SetMoveInput(Vector2.zero);
            ApplySeparationOnly();
            return;
        }

        Vector2 targetWorld = GetCurrentPathWorldTarget();
        Vector2 delta = targetWorld - rb.position;

        while (delta.magnitude <= GetCurrentArriveDistance())
        {
            pathIndex++;

            if (pathIndex >= pathCells.Count)
            {
                StopMove(faceUpOnArrive);

                if (faceUpOnArrive)
                    FaceUp();

                return;
            }

            targetWorld = GetCurrentPathWorldTarget();
            delta = targetWorld - rb.position;
        }

        if (waitWhenNextCellOccupied && IsNextPathCellBlockedByCustomer())
        {
            HandleBlockedByCustomer();
            ApplySeparationOnly();
            return;
        }

        blockedTimer = 0f;

        Vector2 moveInput = ToFourDirStable(delta);
        SetMoveInput(moveInput);

        float step = moveSpeed * Time.fixedDeltaTime;
        Vector2 pathStepDelta = GetClampedFourDirStep(delta, moveInput, step);

        Vector2 separationDelta = GetCustomerSeparationDelta();

        Vector2 finalDelta = pathStepDelta + separationDelta;
        finalDelta = ClampDeltaByStaticWalkable(finalDelta);

        if (finalDelta.sqrMagnitude <= 0.0001f)
            return;

        rb.MovePosition(rb.position + finalDelta);
    }

    private Vector2 GetCurrentPathWorldTarget()
    {
        if (pathCells.Count == 0)
            return rb.position;

        pathIndex = Mathf.Clamp(pathIndex, 0, pathCells.Count - 1);

        if (pathIndex >= pathCells.Count - 1)
            return lastMoveTargetPos;

        return WorldState.Instance.CellToWorld(pathCells[pathIndex]);
    }

    private float GetCurrentArriveDistance()
    {
        if (pathIndex >= pathCells.Count - 1)
            return arriveDistance;

        return Mathf.Max(arriveDistance, pathNodeArriveDistance);
    }

    private void SetMoveInput(Vector2 input)
    {
        moveDirExp = input;

        if (moveDirExp.sqrMagnitude > 0.0001f)
        {
            lastFourDirInput = moveDirExp;
            facingDir = GetFacingDirection(moveDirExp);
        }
    }

    private Vector2 ToFourDirStable(Vector2 delta)
    {
        if (delta.sqrMagnitude <= 0.0001f)
            return Vector2.zero;

        float absX = Mathf.Abs(delta.x);
        float absY = Mathf.Abs(delta.y);

        if (absX <= arriveDistance * 0.5f && absY > arriveDistance * 0.5f)
            return new Vector2(0f, Mathf.Sign(delta.y));

        if (absY <= arriveDistance * 0.5f && absX > arriveDistance * 0.5f)
            return new Vector2(Mathf.Sign(delta.x), 0f);

        bool wasMovingX = Mathf.Abs(lastFourDirInput.x) > 0.5f;
        bool wasMovingY = Mathf.Abs(lastFourDirInput.y) > 0.5f;

        if (wasMovingX && absX > arriveDistance * 0.5f && absX + axisSwitchTolerance >= absY)
            return new Vector2(Mathf.Sign(delta.x), 0f);

        if (wasMovingY && absY > arriveDistance * 0.5f && absY + axisSwitchTolerance >= absX)
            return new Vector2(0f, Mathf.Sign(delta.y));

        if (absX >= absY)
            return new Vector2(Mathf.Sign(delta.x), 0f);

        return new Vector2(0f, Mathf.Sign(delta.y));
    }

    private Vector2 GetClampedFourDirStep(Vector2 delta, Vector2 moveInput, float maxStep)
    {
        if (moveInput.sqrMagnitude <= 0.0001f)
            return Vector2.zero;

        if (Mathf.Abs(moveInput.x) > 0.0001f)
        {
            float stepX = Mathf.Sign(delta.x) * Mathf.Min(Mathf.Abs(delta.x), maxStep);
            return new Vector2(stepX, 0f);
        }

        if (Mathf.Abs(moveInput.y) > 0.0001f)
        {
            float stepY = Mathf.Sign(delta.y) * Mathf.Min(Mathf.Abs(delta.y), maxStep);
            return new Vector2(0f, stepY);
        }

        return Vector2.zero;
    }

    private Direction GetFacingDirection(Vector2 moveDir)
    {
        if (Mathf.Abs(moveDir.x) >= Mathf.Abs(moveDir.y))
            return moveDir.x >= 0f ? Direction.Right : Direction.Left;

        return moveDir.y >= 0f ? Direction.Up : Direction.Down;
    }

    private void FaceDirection(Direction dir)
    {
        facingDir = dir;
        SetMoveInput(Vector2.zero);
    }

    #endregion

    #region Dynamic Avoidance / Separation

    private bool IsNextPathCellBlockedByCustomer()
    {
        if (!avoidOtherCustomers)
            return false;

        if (pathCells.Count == 0)
            return false;

        int index = Mathf.Clamp(pathIndex, 0, pathCells.Count - 1);
        Vector3Int nextCell = pathCells[index];

        if (nextCell == currentTargetCell)
            return false;

        return IsCellOccupiedByOtherCustomer(nextCell);
    }

    private void HandleBlockedByCustomer()
    {
        SetMoveInput(Vector2.zero);

        blockedTimer += Time.fixedDeltaTime;

        if (blockedTimer < blockedRepathDelay)
            return;

        blockedTimer = 0f;

        if (hasMoveTarget)
            RebuildPath(currentTargetCell);
    }

    private void ApplySeparationOnly()
    {
        if (suppressIdleSeparation)
            return;

        Vector2 separationDelta = GetCustomerSeparationDelta();
        separationDelta = ClampDeltaByStaticWalkable(separationDelta);

        if (separationDelta.sqrMagnitude > 0.0001f)
            rb.MovePosition(rb.position + separationDelta);
    }

    private Vector2 GetCustomerSeparationDelta()
    {
        if (!useCustomerSeparation)
            return Vector2.zero;

        Vector2 selfPos = rb != null ? rb.position : (Vector2)transform.position;
        Vector2 separation = Vector2.zero;

        foreach (CustomerPathAgent other in ActiveAgents)
        {
            if (other == null || other == this)
                continue;

            if (!other.isActiveAndEnabled)
                continue;

            Vector2 otherPos = other.rb != null
                ? other.rb.position
                : (Vector2)other.transform.position;

            Vector2 diff = selfPos - otherPos;
            float dist = diff.magnitude;

            if (dist >= customerSeparationRadius)
                continue;

            if (dist <= 0.0001f)
            {
                float angle = (GetInstanceID() % 360) * Mathf.Deg2Rad;
                diff = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                dist = 0.0001f;
            }

            float pushWeight = (customerSeparationRadius - dist) / customerSeparationRadius;
            separation += diff.normalized * pushWeight;
        }

        if (separation.sqrMagnitude <= 0.0001f)
            return Vector2.zero;

        Vector2 delta =
            separation.normalized *
            customerSeparationStrength *
            Time.fixedDeltaTime;

        return Vector2.ClampMagnitude(delta, maxSeparationStep);
    }

    private Vector2 ClampDeltaByStaticWalkable(Vector2 delta)
    {
        if (delta.sqrMagnitude <= 0.0001f)
            return Vector2.zero;

        Vector2 curPos = rb != null ? rb.position : (Vector2)transform.position;

        if (CanOccupyWorldPos(curPos + delta))
            return delta;

        Vector2 xOnly = new Vector2(delta.x, 0f);
        if (xOnly.sqrMagnitude > 0.0001f && CanOccupyWorldPos(curPos + xOnly))
            return xOnly;

        Vector2 yOnly = new Vector2(0f, delta.y);
        if (yOnly.sqrMagnitude > 0.0001f && CanOccupyWorldPos(curPos + yOnly))
            return yOnly;

        return Vector2.zero;
    }

    private bool CanOccupyWorldPos(Vector2 worldPos)
    {
        WorldState world = WorldState.Instance;
        if (world == null)
            return false;

        Vector3Int cell = world.WorldToCell(worldPos);

        if (allowOccupiedTargetCell && hasMoveTarget && cell == currentTargetCell)
            return true;

        return world.CheckEmpty(cell);
    }

    #endregion

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (pathCells == null || pathCells.Count == 0)
            return;

        if (WorldState.Instance == null)
            return;

        Gizmos.color = Color.cyan;

        for (int i = 0; i < pathCells.Count; i++)
        {
            Vector3 world = WorldState.Instance.CellToWorld(pathCells[i]);
            Gizmos.DrawWireSphere(world, 0.12f);

            if (i + 1 < pathCells.Count)
            {
                Vector3 next = WorldState.Instance.CellToWorld(pathCells[i + 1]);
                Gizmos.DrawLine(world, next);
            }
        }

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(lastMoveTargetPos, 0.16f);
    }
#endif
}