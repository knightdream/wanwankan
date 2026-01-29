using System;
using UnityEngine;
using WanWanKan.Character;
using WanWanKan.Core;

namespace WanWanKan.Combat
{
    /// <summary>
    /// 战斗单位类型
    /// </summary>
    public enum UnitType
    {
        Player,     // 玩家
        Enemy,      // 敌人
        Ally        // 友军
    }

    /// <summary>
    /// 战斗单位 - 参与战斗的实体
    /// </summary>
    public class CombatUnit : MonoBehaviour
    {
        [Header("单位信息")]
        [SerializeField] private string unitName = "Unknown";
        [SerializeField] private UnitType unitType = UnitType.Enemy;
        [SerializeField] private Sprite unitIcon;

        [Header("属性")]
        [SerializeField] private CharacterStats stats;

        [Header("气力系统")]
        [SerializeField] private float currentActionGauge = 0f;
        [SerializeField] private float maxActionGauge = 100f;

        // 属性访问器
        public string UnitName => unitName;
        public UnitType UnitType => unitType;
        public Sprite UnitIcon => unitIcon;
        public CharacterStats Stats => stats;
        
        /// <summary>
        /// 当前气力值
        /// </summary>
        public float CurrentActionGauge => currentActionGauge;
        
        /// <summary>
        /// 最大气力值
        /// </summary>
        public float MaxActionGauge => maxActionGauge;
        
        /// <summary>
        /// 气力百分比 (0-1)
        /// </summary>
        public float ActionGaugePercentage => maxActionGauge > 0 ? currentActionGauge / maxActionGauge : 0;
        
        /// <summary>
        /// 气力是否已满（可以行动）
        /// </summary>
        public bool IsReadyToAct => currentActionGauge >= maxActionGauge;
        
        /// <summary>
        /// 是否存活
        /// </summary>
        public bool IsAlive => stats != null && stats.IsAlive;

        /// <summary>
        /// 行动速度（气力恢复速度）- 基于敏捷
        /// </summary>
        public int ActionSpeed => stats?.ActionSpeed ?? 10;

        // 事件
        public event Action<CombatUnit> OnActionReady;          // 气力满，准备行动
        public event Action<float, float> OnActionGaugeChanged; // (当前值, 最大值)
        public event Action<CombatUnit> OnTurnStart;            // 回合开始
        public event Action<CombatUnit> OnTurnEnd;              // 回合结束
        public event Action<CombatUnit, int> OnDealDamage;      // 造成伤害
        public event Action<CombatUnit, int> OnReceiveDamage;   // 受到伤害

        private void Awake()
        {
            if (stats == null)
            {
                stats = new CharacterStats();
            }
        }

        /// <summary>
        /// 初始化战斗单位
        /// </summary>
        public void Initialize(string name, CharacterStats characterStats, UnitType type)
        {
            unitName = name;
            stats = characterStats;
            unitType = type;
            ResetActionGauge();
        }

        /// <summary>
        /// 更新气力值（每帧调用）
        /// </summary>
        /// <param name="deltaTime">时间增量</param>
        public void UpdateActionGauge(float deltaTime)
        {
            if (!IsAlive || IsReadyToAct) return;

            float previousGauge = currentActionGauge;
            
            // 气力恢复 = 敏捷速度 * 时间 * 倍率
            // 倍率用于调整战斗节奏，默认1.0
            float gaugeIncrease = ActionSpeed * deltaTime;
            currentActionGauge = Mathf.Min(currentActionGauge + gaugeIncrease, maxActionGauge);

            if (currentActionGauge != previousGauge)
            {
                OnActionGaugeChanged?.Invoke(currentActionGauge, maxActionGauge);
            }

            // 气力满了，触发行动就绪
            if (IsReadyToAct)
            {
                OnActionReady?.Invoke(this);
            }
        }

        /// <summary>
        /// 消耗气力（行动后调用）
        /// </summary>
        /// <param name="amount">消耗量，默认全部消耗</param>
        public void ConsumeActionGauge(float amount = -1)
        {
            if (amount < 0) amount = maxActionGauge;
            
            currentActionGauge = Mathf.Max(0, currentActionGauge - amount);
            OnActionGaugeChanged?.Invoke(currentActionGauge, maxActionGauge);
        }

        /// <summary>
        /// 重置气力值
        /// </summary>
        public void ResetActionGauge()
        {
            currentActionGauge = 0f;
            OnActionGaugeChanged?.Invoke(currentActionGauge, maxActionGauge);
        }

        /// <summary>
        /// 开始回合
        /// </summary>
        public void StartTurn()
        {
            OnTurnStart?.Invoke(this);
            Debug.Log($"[Combat] {unitName} 的回合开始！");
        }

        /// <summary>
        /// 结束回合
        /// </summary>
        public void EndTurn()
        {
            ConsumeActionGauge();
            OnTurnEnd?.Invoke(this);
            Debug.Log($"[Combat] {unitName} 的回合结束");
        }

        /// <summary>
        /// 执行攻击
        /// </summary>
        /// <param name="target">目标</param>
        /// <param name="damageBonus">额外伤害加成</param>
        /// <returns>造成的伤害</returns>
        public int Attack(CombatUnit target, int damageBonus = 0)
        {
            if (target == null || !target.IsAlive) return 0;

            // 攻击检定：1d20 + 力量修正 vs 目标防御
            int attackRoll = DiceSystem.Instance.Roll1D20() + stats.GetStrengthModifier();
            int targetDefense = 10 + target.Stats.GetAgilityModifier(); // 基础防御10 + 敏捷修正

            // 检查是否暴击（投出20）
            bool isCritical = attackRoll >= 20;

            Debug.Log($"[Combat] {unitName} 攻击 {target.UnitName}: {attackRoll} vs {targetDefense} {(isCritical ? "(暴击!)" : "")}");

            // 播放攻击动画
            if (CombatVisualEffects.Instance != null)
            {
                CombatVisualEffects.Instance.PlayAttackAnimation(this, target);
            }

            if (attackRoll >= targetDefense)
            {
                // 命中！计算伤害：1d6 + 力量修正 + 额外加成
                int damage = DiceSystem.Instance.RollDamage(1, stats.GetStrengthModifier() + damageBonus);
                if (isCritical)
                {
                    damage = Mathf.RoundToInt(damage * 1.5f); // 暴击1.5倍伤害
                }
                damage = Mathf.Max(1, damage); // 至少造成1点伤害
                
                int actualDamage = target.TakeDamage(damage, this);
                OnDealDamage?.Invoke(target, actualDamage);
                
                // 显示伤害数字和受击动画
                if (CombatVisualEffects.Instance != null)
                {
                    CombatVisualEffects.Instance.ShowDamageNumber(target, actualDamage, isCritical, false);
                    CombatVisualEffects.Instance.PlayHitAnimation(target, isCritical);
                }
                
                Debug.Log($"[Combat] 命中！{unitName} 对 {target.UnitName} 造成 {actualDamage} 点伤害 {(isCritical ? "(暴击!)" : "")}");
                return actualDamage;
            }
            else
            {
                // 未命中，显示"Miss"
                if (CombatVisualEffects.Instance != null)
                {
                    CombatVisualEffects.Instance.ShowDamageNumber(target, 0, false, false);
                }
                Debug.Log($"[Combat] 未命中！");
                return 0;
            }
        }

        /// <summary>
        /// 受到伤害
        /// </summary>
        /// <param name="damage">伤害值</param>
        /// <param name="attacker">攻击者</param>
        /// <returns>实际受到的伤害</returns>
        public int TakeDamage(int damage, CombatUnit attacker = null)
        {
            int actualDamage = stats.TakeDamage(damage);
            OnReceiveDamage?.Invoke(attacker, actualDamage);
            
            if (!IsAlive)
            {
                Debug.Log($"[Combat] {unitName} 被击败了！");
                // 播放死亡动画
                if (CombatVisualEffects.Instance != null)
                {
                    CombatVisualEffects.Instance.PlayDeathAnimation(this);
                }
            }
            
            return actualDamage;
        }

        /// <summary>
        /// 预测到达满气力需要的时间
        /// </summary>
        /// <returns>预计时间（秒）</returns>
        public float GetTimeToReady()
        {
            if (IsReadyToAct) return 0;
            float remaining = maxActionGauge - currentActionGauge;
            return ActionSpeed > 0 ? remaining / ActionSpeed : float.MaxValue;
        }

        /// <summary>
        /// 创建玩家单位
        /// </summary>
        public static CombatUnit CreatePlayer(GameObject parent, string name, int str, int agi, int wis)
        {
            GameObject go = new GameObject($"Player_{name}");
            if (parent != null) go.transform.SetParent(parent.transform);
            
            CombatUnit unit = go.AddComponent<CombatUnit>();
            unit.Initialize(name, new CharacterStats(str, agi, wis), UnitType.Player);
            return unit;
        }

        /// <summary>
        /// 创建敌人单位
        /// </summary>
        public static CombatUnit CreateEnemy(GameObject parent, string name, int str, int agi, int wis)
        {
            GameObject go = new GameObject($"Enemy_{name}");
            if (parent != null) go.transform.SetParent(parent.transform);
            
            CombatUnit unit = go.AddComponent<CombatUnit>();
            unit.Initialize(name, new CharacterStats(str, agi, wis), UnitType.Enemy);
            return unit;
        }

        public override string ToString()
        {
            return $"{unitName} [{unitType}] HP:{stats.CurrentHP}/{stats.MaxHP} AG:{currentActionGauge:F0}/{maxActionGauge} SPD:{ActionSpeed}";
        }
    }
}

