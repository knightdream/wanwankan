using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WanWanKan.Core;

namespace WanWanKan.Combat
{
    /// <summary>
    /// 战斗状态
    /// </summary>
    public enum CombatState
    {
        NotInCombat,    // 非战斗状态
        Initializing,   // 初始化中
        Running,        // 战斗进行中（气力累积）
        WaitingInput,   // 等待玩家输入
        ExecutingAction,// 执行行动中
        Victory,        // 胜利
        Defeat          // 失败
    }

    /// <summary>
    /// ATB战斗管理器 - 管理整个战斗流程
    /// </summary>
    public class CombatManager : MonoBehaviour
    {
        public static CombatManager Instance { get; private set; }

        [Header("战斗设置")]
        [SerializeField] private float combatSpeedMultiplier = 1.0f;  // 战斗速度倍率
        [SerializeField] private float actionExecuteDelay = 0.5f;     // 行动执行延迟（动画用）

        [Header("战斗状态")]
        [SerializeField] private CombatState currentState = CombatState.NotInCombat;
        
        // 战斗单位列表
        private List<CombatUnit> allUnits = new List<CombatUnit>();
        private List<CombatUnit> playerUnits = new List<CombatUnit>();
        private List<CombatUnit> enemyUnits = new List<CombatUnit>();
        
        // 当前行动单位
        private CombatUnit currentActingUnit;
        
        // 行动队列（气力满的单位按顺序排队）
        private Queue<CombatUnit> actionQueue = new Queue<CombatUnit>();

        // 属性访问器
        public CombatState CurrentState => currentState;
        public CombatUnit CurrentActingUnit => currentActingUnit;
        public IReadOnlyList<CombatUnit> AllUnits => allUnits;
        public IReadOnlyList<CombatUnit> PlayerUnits => playerUnits;
        public IReadOnlyList<CombatUnit> EnemyUnits => enemyUnits;
        public float CombatSpeedMultiplier 
        { 
            get => combatSpeedMultiplier;
            set => combatSpeedMultiplier = Mathf.Max(0.1f, value);
        }

        // 事件
        public event Action OnCombatStart;
        public event Action<CombatState> OnCombatStateChanged;
        public event Action<CombatUnit> OnUnitTurnStart;
        public event Action<CombatUnit> OnUnitTurnEnd;
        public event Action<CombatUnit> OnUnitDefeated;
        public event Action<bool> OnCombatEnd;  // true = 胜利, false = 失败
        public event Action<CombatUnit> OnWaitingForPlayerInput;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (currentState == CombatState.Running)
            {
                UpdateCombat();
            }
        }

        /// <summary>
        /// 开始战斗
        /// </summary>
        /// <param name="players">玩家单位列表</param>
        /// <param name="enemies">敌人单位列表</param>
        public void StartCombat(List<CombatUnit> players, List<CombatUnit> enemies)
        {
            if (currentState != CombatState.NotInCombat)
            {
                Debug.LogWarning("[CombatManager] 战斗已在进行中！");
                return;
            }

            SetState(CombatState.Initializing);
            
            // 清理并设置单位列表
            allUnits.Clear();
            playerUnits.Clear();
            enemyUnits.Clear();
            actionQueue.Clear();

            playerUnits.AddRange(players);
            enemyUnits.AddRange(enemies);
            allUnits.AddRange(players);
            allUnits.AddRange(enemies);

            // 初始化所有单位
            foreach (var unit in allUnits)
            {
                unit.ResetActionGauge();
                unit.OnActionReady += OnUnitActionReady;
                unit.Stats.OnDeath += () => OnUnitDeath(unit);
            }

            Debug.Log($"[CombatManager] 战斗开始！玩家: {playerUnits.Count}, 敌人: {enemyUnits.Count}");
            
            OnCombatStart?.Invoke();
            SetState(CombatState.Running);
        }

        /// <summary>
        /// 更新战斗（气力累积）
        /// </summary>
        private void UpdateCombat()
        {
            float deltaTime = Time.deltaTime * combatSpeedMultiplier;

            // 更新所有存活单位的气力
            foreach (var unit in allUnits)
            {
                if (unit.IsAlive && !unit.IsReadyToAct)
                {
                    unit.UpdateActionGauge(deltaTime);
                }
            }

            // 处理行动队列
            ProcessActionQueue();
        }

        /// <summary>
        /// 单位气力满时的回调
        /// </summary>
        private void OnUnitActionReady(CombatUnit unit)
        {
            if (!actionQueue.Contains(unit))
            {
                actionQueue.Enqueue(unit);
                Debug.Log($"[CombatManager] {unit.UnitName} 气力已满，加入行动队列");
            }
        }

        /// <summary>
        /// 处理行动队列
        /// </summary>
        private void ProcessActionQueue()
        {
            // 如果当前没有单位在行动，且队列中有单位
            if (currentActingUnit == null && actionQueue.Count > 0)
            {
                CombatUnit nextUnit = actionQueue.Dequeue();
                
                // 确保单位还活着
                if (nextUnit.IsAlive)
                {
                    StartUnitTurn(nextUnit);
                }
            }
        }

        /// <summary>
        /// 开始单位回合
        /// </summary>
        private void StartUnitTurn(CombatUnit unit)
        {
            currentActingUnit = unit;
            unit.StartTurn();
            OnUnitTurnStart?.Invoke(unit);

            if (unit.UnitType == UnitType.Player)
            {
                // 玩家回合 - 等待输入
                SetState(CombatState.WaitingInput);
                OnWaitingForPlayerInput?.Invoke(unit);
                Debug.Log($"[CombatManager] 等待玩家 {unit.UnitName} 输入指令...");
            }
            else
            {
                // AI回合 - 自动执行
                SetState(CombatState.ExecutingAction);
                StartCoroutine(ExecuteAIAction(unit));
            }
        }

        /// <summary>
        /// 执行AI行动
        /// </summary>
        private IEnumerator ExecuteAIAction(CombatUnit aiUnit)
        {
            yield return new WaitForSeconds(actionExecuteDelay);

            // 简单AI：攻击随机一个存活的玩家
            var aliveTargets = playerUnits.Where(p => p.IsAlive).ToList();
            if (aliveTargets.Count > 0)
            {
                CombatUnit target = aliveTargets[UnityEngine.Random.Range(0, aliveTargets.Count)];
                aiUnit.Attack(target);
            }

            EndUnitTurn(aiUnit);
        }

        /// <summary>
        /// 玩家选择攻击目标
        /// </summary>
        /// <param name="targetIndex">目标在敌人列表中的索引</param>
        public void PlayerSelectAttack(int targetIndex)
        {
            if (currentState != CombatState.WaitingInput || currentActingUnit == null)
            {
                Debug.LogWarning("[CombatManager] 当前不是玩家输入阶段！");
                return;
            }

            if (targetIndex < 0 || targetIndex >= enemyUnits.Count)
            {
                Debug.LogWarning("[CombatManager] 无效的目标索引！");
                return;
            }

            CombatUnit target = enemyUnits[targetIndex];
            if (!target.IsAlive)
            {
                Debug.LogWarning("[CombatManager] 目标已被击败！");
                return;
            }

            SetState(CombatState.ExecutingAction);
            StartCoroutine(ExecutePlayerAttack(target));
        }

        /// <summary>
        /// 玩家攻击指定目标
        /// </summary>
        public void PlayerAttackTarget(CombatUnit target)
        {
            if (currentState != CombatState.WaitingInput || currentActingUnit == null)
            {
                Debug.LogWarning("[CombatManager] 当前不是玩家输入阶段！");
                return;
            }

            if (target == null || !target.IsAlive)
            {
                Debug.LogWarning("[CombatManager] 无效的目标！");
                return;
            }

            SetState(CombatState.ExecutingAction);
            StartCoroutine(ExecutePlayerAttack(target));
        }

        /// <summary>
        /// 执行玩家攻击
        /// </summary>
        private IEnumerator ExecutePlayerAttack(CombatUnit target)
        {
            yield return new WaitForSeconds(actionExecuteDelay);
            
            currentActingUnit.Attack(target);
            EndUnitTurn(currentActingUnit);
        }

        /// <summary>
        /// 玩家选择防御
        /// </summary>
        public void PlayerSelectDefend()
        {
            if (currentState != CombatState.WaitingInput || currentActingUnit == null) return;

            SetState(CombatState.ExecutingAction);
            StartCoroutine(ExecutePlayerDefend());
        }

        /// <summary>
        /// 执行玩家防御
        /// </summary>
        private IEnumerator ExecutePlayerDefend()
        {
            yield return new WaitForSeconds(actionExecuteDelay);
            
            // 防御效果：下次受到伤害减半（简化版本）
            Debug.Log($"[CombatManager] {currentActingUnit.UnitName} 进入防御姿态！");
            // TODO: 实现防御状态
            
            EndUnitTurn(currentActingUnit);
        }

        /// <summary>
        /// 结束单位回合
        /// </summary>
        private void EndUnitTurn(CombatUnit unit)
        {
            unit.EndTurn();
            OnUnitTurnEnd?.Invoke(unit);
            currentActingUnit = null;

            // 检查战斗是否结束
            if (CheckCombatEnd())
            {
                return;
            }

            // 继续战斗
            SetState(CombatState.Running);
        }

        /// <summary>
        /// 单位死亡回调
        /// </summary>
        private void OnUnitDeath(CombatUnit unit)
        {
            OnUnitDefeated?.Invoke(unit);
            Debug.Log($"[CombatManager] {unit.UnitName} 被击败！");
        }

        /// <summary>
        /// 检查战斗是否结束
        /// </summary>
        private bool CheckCombatEnd()
        {
            bool allPlayersDefeated = playerUnits.All(p => !p.IsAlive);
            bool allEnemiesDefeated = enemyUnits.All(e => !e.IsAlive);

            if (allPlayersDefeated)
            {
                EndCombat(false);
                return true;
            }

            if (allEnemiesDefeated)
            {
                EndCombat(true);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 结束战斗
        /// </summary>
        /// <param name="victory">是否胜利</param>
        private void EndCombat(bool victory)
        {
            SetState(victory ? CombatState.Victory : CombatState.Defeat);
            
            // 清理事件订阅
            foreach (var unit in allUnits)
            {
                unit.OnActionReady -= OnUnitActionReady;
            }

            Debug.Log($"[CombatManager] 战斗结束！{(victory ? "胜利！" : "失败...")}");
            OnCombatEnd?.Invoke(victory);

            // 重置状态
            currentActingUnit = null;
            actionQueue.Clear();
            SetState(CombatState.NotInCombat);
        }

        /// <summary>
        /// 强制结束战斗
        /// </summary>
        public void ForceEndCombat()
        {
            if (currentState == CombatState.NotInCombat) return;
            
            StopAllCoroutines();
            EndCombat(false);
        }

        /// <summary>
        /// 设置战斗状态
        /// </summary>
        private void SetState(CombatState newState)
        {
            if (currentState != newState)
            {
                currentState = newState;
                OnCombatStateChanged?.Invoke(newState);
                Debug.Log($"[CombatManager] 状态变更: {newState}");
            }
        }

        /// <summary>
        /// 获取所有存活的敌人
        /// </summary>
        public List<CombatUnit> GetAliveEnemies()
        {
            return enemyUnits.Where(e => e.IsAlive).ToList();
        }

        /// <summary>
        /// 获取所有存活的玩家
        /// </summary>
        public List<CombatUnit> GetAlivePlayers()
        {
            return playerUnits.Where(p => p.IsAlive).ToList();
        }

        /// <summary>
        /// 获取按气力排序的单位列表（用于UI显示）
        /// </summary>
        public List<CombatUnit> GetUnitsSortedByActionGauge()
        {
            return allUnits
                .Where(u => u.IsAlive)
                .OrderByDescending(u => u.CurrentActionGauge)
                .ToList();
        }

        /// <summary>
        /// 预测接下来的行动顺序
        /// </summary>
        /// <param name="count">预测数量</param>
        public List<CombatUnit> PredictActionOrder(int count = 5)
        {
            List<CombatUnit> prediction = new List<CombatUnit>();
            
            // 复制当前气力状态
            Dictionary<CombatUnit, float> simulatedGauge = new Dictionary<CombatUnit, float>();
            foreach (var unit in allUnits.Where(u => u.IsAlive))
            {
                simulatedGauge[unit] = unit.CurrentActionGauge;
            }

            // 模拟未来的行动顺序
            while (prediction.Count < count && simulatedGauge.Count > 0)
            {
                // 找到最快达到满气力的单位
                CombatUnit nextUnit = null;
                float minTime = float.MaxValue;

                foreach (var kvp in simulatedGauge)
                {
                    float timeToReady = (100f - kvp.Value) / kvp.Key.ActionSpeed;
                    if (timeToReady < minTime)
                    {
                        minTime = timeToReady;
                        nextUnit = kvp.Key;
                    }
                }

                if (nextUnit != null)
                {
                    // 更新所有单位的模拟气力
                    var keys = simulatedGauge.Keys.ToList();
                    foreach (var unit in keys)
                    {
                        simulatedGauge[unit] += unit.ActionSpeed * minTime;
                    }

                    // 该单位行动，气力重置
                    simulatedGauge[nextUnit] = 0;
                    prediction.Add(nextUnit);
                }
            }

            return prediction;
        }
    }
}

