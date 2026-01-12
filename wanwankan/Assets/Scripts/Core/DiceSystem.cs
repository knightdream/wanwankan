using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace WanWanKan.Core
{
    /// <summary>
    /// 骰子类型枚举
    /// </summary>
    public enum DiceType
    {
        D6 = 6,      // 普通骰子 1-6
        D20 = 20,    // 特殊骰子 1-20 (常用于攻击/技能检定)
        D100 = 100   // 特殊骰子 1-100 (常用于概率事件)
    }

    /// <summary>
    /// 骰子投掷结果
    /// </summary>
    public class DiceResult
    {
        public DiceType DiceType { get; private set; }
        public int[] Rolls { get; private set; }
        public int Total { get; private set; }
        public int Modifier { get; private set; }
        public int FinalResult { get; private set; }
        
        // 特殊结果判定
        public bool IsCriticalSuccess { get; private set; }  // 大成功
        public bool IsCriticalFailure { get; private set; }  // 大失败

        public DiceResult(DiceType type, int[] rolls, int modifier = 0)
        {
            DiceType = type;
            Rolls = rolls;
            Modifier = modifier;
            
            Total = 0;
            foreach (int roll in rolls)
            {
                Total += roll;
            }
            
            FinalResult = Total + Modifier;
            
            // 判定大成功/大失败 (仅对D20有效，经典TRPG规则)
            if (type == DiceType.D20 && rolls.Length == 1)
            {
                IsCriticalSuccess = rolls[0] == 20;
                IsCriticalFailure = rolls[0] == 1;
            }
        }

        public override string ToString()
        {
            string rollsStr = string.Join("+", Rolls);
            if (Modifier != 0)
            {
                string sign = Modifier > 0 ? "+" : "";
                return $"{Rolls.Length}d{(int)DiceType}({rollsStr}){sign}{Modifier} = {FinalResult}";
            }
            return $"{Rolls.Length}d{(int)DiceType}({rollsStr}) = {FinalResult}";
        }
    }

    /// <summary>
    /// 骰子系统 - 处理所有骰子相关的逻辑
    /// </summary>
    public class DiceSystem : MonoBehaviour
    {
        public static DiceSystem Instance { get; private set; }

        [Header("骰子动画设置")]
        [SerializeField] private float rollAnimationDuration = 1.0f;
        [SerializeField] private int animationFrames = 10;

        // 事件：骰子开始投掷
        public event Action<DiceType, int> OnDiceRollStarted;
        // 事件：骰子投掷中（动画用）
        public event Action<int> OnDiceRolling;
        // 事件：骰子投掷完成
        public event Action<DiceResult> OnDiceRollCompleted;

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

        /// <summary>
        /// 投掷骰子 - 即时返回结果
        /// </summary>
        /// <param name="diceType">骰子类型</param>
        /// <param name="count">骰子数量</param>
        /// <param name="modifier">修正值</param>
        /// <returns>骰子结果</returns>
        public DiceResult Roll(DiceType diceType, int count = 1, int modifier = 0)
        {
            int[] rolls = new int[count];
            int maxValue = (int)diceType;
            
            for (int i = 0; i < count; i++)
            {
                rolls[i] = Random.Range(1, maxValue + 1);
            }
            
            return new DiceResult(diceType, rolls, modifier);
        }

        /// <summary>
        /// 投掷骰子 - 带动画效果（协程）
        /// </summary>
        public void RollWithAnimation(DiceType diceType, int count = 1, int modifier = 0, Action<DiceResult> onComplete = null)
        {
            StartCoroutine(RollAnimationCoroutine(diceType, count, modifier, onComplete));
        }

        private IEnumerator RollAnimationCoroutine(DiceType diceType, int count, int modifier, Action<DiceResult> onComplete)
        {
            int maxValue = (int)diceType;
            
            OnDiceRollStarted?.Invoke(diceType, count);

            // 动画阶段 - 快速显示随机数字
            float frameDelay = rollAnimationDuration / animationFrames;
            for (int i = 0; i < animationFrames; i++)
            {
                int randomPreview = Random.Range(1, maxValue + 1);
                OnDiceRolling?.Invoke(randomPreview);
                yield return new WaitForSeconds(frameDelay);
            }

            // 最终结果
            DiceResult result = Roll(diceType, count, modifier);
            
            OnDiceRollCompleted?.Invoke(result);
            onComplete?.Invoke(result);
        }

        /// <summary>
        /// 进行检定（投d20 vs 难度值）
        /// </summary>
        /// <param name="modifier">属性修正</param>
        /// <param name="difficultyClass">难度等级</param>
        /// <returns>是否成功</returns>
        public bool Check(int modifier, int difficultyClass, out DiceResult result)
        {
            result = Roll(DiceType.D20, 1, modifier);
            
            // 大成功必定成功，大失败必定失败
            if (result.IsCriticalSuccess) return true;
            if (result.IsCriticalFailure) return false;
            
            return result.FinalResult >= difficultyClass;
        }

        /// <summary>
        /// 概率检定（投d100，判断是否小于等于成功率）
        /// </summary>
        /// <param name="successRate">成功率 (1-100)</param>
        /// <returns>是否成功</returns>
        public bool PercentageCheck(int successRate, out DiceResult result)
        {
            result = Roll(DiceType.D100);
            return result.FinalResult <= successRate;
        }

        /// <summary>
        /// 投掷伤害骰（通常用d6）
        /// </summary>
        /// <param name="diceCount">骰子数量</param>
        /// <param name="modifier">伤害加成</param>
        /// <returns>总伤害</returns>
        public int RollDamage(int diceCount, int modifier = 0)
        {
            DiceResult result = Roll(DiceType.D6, diceCount, modifier);
            return result.FinalResult;
        }

        #region 便捷方法

        /// <summary>投 1d6</summary>
        public int Roll1D6() => Roll(DiceType.D6).FinalResult;

        /// <summary>投 2d6</summary>
        public int Roll2D6() => Roll(DiceType.D6, 2).FinalResult;

        /// <summary>投 1d20</summary>
        public int Roll1D20() => Roll(DiceType.D20).FinalResult;

        /// <summary>投 1d100</summary>
        public int Roll1D100() => Roll(DiceType.D100).FinalResult;

        #endregion
    }
}

