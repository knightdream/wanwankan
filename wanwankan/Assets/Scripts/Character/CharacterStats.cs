using System;
using UnityEngine;

namespace WanWanKan.Character
{
    /// <summary>
    /// 角色基础属性
    /// </summary>
    [Serializable]
    public class CharacterStats
    {
        [Header("基础属性")]
        [Range(1, 20)]
        [SerializeField] private int strength = 1;      // 力量 - 物理伤害、负重
        
        [Range(1, 20)]
        [SerializeField] private int agility = 1;       // 敏捷 - 气力恢复速度、闪避
        
        [Range(1, 20)]
        [SerializeField] private int wisdom = 1;        // 智慧 - 魔法伤害、感知

        [Header("战斗属性")]
        [SerializeField] private int maxHP = 20;
        [SerializeField] private int currentHP = 20;

        // 属性访问器
        public int Strength 
        { 
            get => strength;
            set => strength = Mathf.Clamp(value, 1, 20);
        }
        
        public int Agility 
        { 
            get => agility;
            set => agility = Mathf.Clamp(value, 1, 20);
        }
        
        public int Wisdom 
        { 
            get => wisdom;
            set => wisdom = Mathf.Clamp(value, 1, 20);
        }

        public int MaxHP 
        { 
            get => maxHP;
            set => maxHP = Mathf.Max(1, value);
        }
        
        public int CurrentHP 
        { 
            get => currentHP;
            set => currentHP = Mathf.Clamp(value, 0, maxHP);
        }

        /// <summary>
        /// 气力恢复速度（基于敏捷）
        /// 公式：基础速度10 + 敏捷值 * 3
        /// 范围：13 (AGI=1) 到 70 (AGI=20)
        /// </summary>
        public int ActionSpeed => 10 + agility * 3;

        /// <summary>
        /// 是否存活
        /// </summary>
        public bool IsAlive => currentHP > 0;

        /// <summary>
        /// HP百分比 (0-1)
        /// </summary>
        public float HPPercentage => maxHP > 0 ? (float)currentHP / maxHP : 0;

        // 事件
        public event Action<int, int> OnHPChanged;          // (当前HP, 最大HP)
        public event Action OnDeath;
        public event Action<int> OnDamageTaken;             // 受到的伤害值
        public event Action<int> OnHealed;                  // 治疗量

        /// <summary>
        /// 创建默认属性
        /// </summary>
        public CharacterStats()
        {
            strength = 1;
            agility = 1;
            wisdom = 1;
            CalculateMaxHP();
            currentHP = maxHP;
        }

        /// <summary>
        /// 创建指定属性
        /// </summary>
        public CharacterStats(int str, int agi, int wis)
        {
            strength = Mathf.Clamp(str, 1, 20);
            agility = Mathf.Clamp(agi, 1, 20);
            wisdom = Mathf.Clamp(wis, 1, 20);
            CalculateMaxHP();
            currentHP = maxHP;
        }

        /// <summary>
        /// 计算最大HP（基于力量）
        /// 公式：10 + 力量 * 2
        /// </summary>
        public void CalculateMaxHP()
        {
            int oldMax = maxHP;
            maxHP = 10 + strength * 2;
            
            // 如果最大HP增加，当前HP也相应增加
            if (maxHP > oldMax)
            {
                currentHP += (maxHP - oldMax);
            }
            // 如果最大HP减少，确保当前HP不超过最大值
            else if (currentHP > maxHP)
            {
                currentHP = maxHP;
            }
        }

        /// <summary>
        /// 受到伤害
        /// </summary>
        /// <param name="damage">伤害值</param>
        /// <returns>实际受到的伤害</returns>
        public int TakeDamage(int damage)
        {
            if (damage <= 0 || !IsAlive) return 0;
            
            int actualDamage = Mathf.Min(damage, currentHP);
            currentHP -= actualDamage;
            
            OnDamageTaken?.Invoke(actualDamage);
            OnHPChanged?.Invoke(currentHP, maxHP);
            
            if (!IsAlive)
            {
                OnDeath?.Invoke();
            }
            
            return actualDamage;
        }

        /// <summary>
        /// 治疗
        /// </summary>
        /// <param name="amount">治疗量</param>
        /// <returns>实际治疗量</returns>
        public int Heal(int amount)
        {
            if (amount <= 0 || !IsAlive) return 0;
            
            int actualHeal = Mathf.Min(amount, maxHP - currentHP);
            currentHP += actualHeal;
            
            OnHealed?.Invoke(actualHeal);
            OnHPChanged?.Invoke(currentHP, maxHP);
            
            return actualHeal;
        }

        /// <summary>
        /// 完全恢复HP
        /// </summary>
        public void FullHeal()
        {
            if (currentHP < maxHP)
            {
                int healAmount = maxHP - currentHP;
                currentHP = maxHP;
                OnHealed?.Invoke(healAmount);
                OnHPChanged?.Invoke(currentHP, maxHP);
            }
        }

        /// <summary>
        /// 获取属性修正值（用于骰子检定）
        /// TRPG风格：(属性值 - 10) / 2，但我们简化为 属性值 - 5
        /// </summary>
        public int GetStrengthModifier() => strength - 5;
        public int GetAgilityModifier() => agility - 5;
        public int GetWisdomModifier() => wisdom - 5;

        /// <summary>
        /// 复制属性
        /// </summary>
        public CharacterStats Clone()
        {
            return new CharacterStats(strength, agility, wisdom)
            {
                maxHP = this.maxHP,
                currentHP = this.currentHP
            };
        }

        public override string ToString()
        {
            return $"STR:{strength} AGI:{agility} WIS:{wisdom} HP:{currentHP}/{maxHP} SPD:{ActionSpeed}";
        }
    }
}

