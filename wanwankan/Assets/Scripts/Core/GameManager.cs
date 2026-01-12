using System.Collections.Generic;
using UnityEngine;
using WanWanKan.Combat;
using WanWanKan.Character;

namespace WanWanKan.Core
{
    /// <summary>
    /// 游戏管理器 - 管理整体游戏流程
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("测试战斗设置")]
        [SerializeField] private bool autoStartTestBattle = true;
        
        [Header("玩家配置")]
        [SerializeField] private string playerName = "勇者";
        [SerializeField] private int playerSTR = 8;
        [SerializeField] private int playerAGI = 10;  // 敏捷=气力恢复速度
        [SerializeField] private int playerWIS = 6;

        [Header("敌人配置")]
        [SerializeField] private List<EnemyConfig> testEnemies = new List<EnemyConfig>();

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

        private void Start()
        {
            // 确保骰子系统和战斗管理器存在
            EnsureSystemsExist();

            if (autoStartTestBattle)
            {
                StartTestBattle();
            }
        }

        private void EnsureSystemsExist()
        {
            if (DiceSystem.Instance == null)
            {
                GameObject diceSystemGO = new GameObject("DiceSystem");
                diceSystemGO.AddComponent<DiceSystem>();
            }

            if (CombatManager.Instance == null)
            {
                GameObject combatManagerGO = new GameObject("CombatManager");
                combatManagerGO.AddComponent<CombatManager>();
            }
        }

        /// <summary>
        /// 开始测试战斗
        /// </summary>
        [ContextMenu("Start Test Battle")]
        public void StartTestBattle()
        {
            Debug.Log("[GameManager] 开始测试战斗...");

            // 创建玩家
            List<CombatUnit> players = new List<CombatUnit>();
            CombatUnit player = CombatUnit.CreatePlayer(
                gameObject, 
                playerName, 
                playerSTR, 
                playerAGI, 
                playerWIS
            );
            players.Add(player);

            // 创建敌人
            List<CombatUnit> enemies = new List<CombatUnit>();
            
            if (testEnemies.Count == 0)
            {
                // 默认敌人
                enemies.Add(CombatUnit.CreateEnemy(gameObject, "哥布林", 5, 8, 3));
                enemies.Add(CombatUnit.CreateEnemy(gameObject, "史莱姆", 3, 12, 2));
            }
            else
            {
                foreach (var config in testEnemies)
                {
                    enemies.Add(CombatUnit.CreateEnemy(
                        gameObject, 
                        config.enemyName, 
                        config.strength, 
                        config.agility, 
                        config.wisdom
                    ));
                }
            }

            // 输出战斗信息
            Debug.Log("=== 战斗单位信息 ===");
            Debug.Log($"玩家: {player}");
            foreach (var enemy in enemies)
            {
                Debug.Log($"敌人: {enemy}");
            }
            Debug.Log("====================");

            // 开始战斗
            CombatManager.Instance.StartCombat(players, enemies);
        }

        /// <summary>
        /// 测试骰子系统
        /// </summary>
        [ContextMenu("Test Dice Roll")]
        public void TestDiceRoll()
        {
            Debug.Log("=== 骰子测试 ===");
            
            var d6Result = DiceSystem.Instance.Roll(DiceType.D6, 2, 3);
            Debug.Log($"2d6+3: {d6Result}");

            var d20Result = DiceSystem.Instance.Roll(DiceType.D20, 1, 5);
            Debug.Log($"1d20+5: {d20Result}");

            var d100Result = DiceSystem.Instance.Roll(DiceType.D100);
            Debug.Log($"1d100: {d100Result}");

            // 检定测试
            bool success = DiceSystem.Instance.Check(5, 15, out var checkResult);
            Debug.Log($"检定(+5 vs DC15): {checkResult} => {(success ? "成功" : "失败")}");

            // 概率检定
            bool percentSuccess = DiceSystem.Instance.PercentageCheck(30, out var percentResult);
            Debug.Log($"概率检定(30%): {percentResult.FinalResult} => {(percentSuccess ? "成功" : "失败")}");
        }

        /// <summary>
        /// 模拟气力系统
        /// </summary>
        [ContextMenu("Simulate Action Gauge")]
        public void SimulateActionGauge()
        {
            Debug.Log("=== 气力系统模拟 ===");
            
            // 模拟两个单位的气力恢复
            int playerAgi = 40;  // 玩家敏捷
            int enemyAgi = 81;   // 敌人敏捷
            
            float playerGauge = 0;
            float enemyGauge = 0;
            float maxGauge = 100;
            
            int playerActions = 0;
            int enemyActions = 0;
            int tick = 0;
            
            Debug.Log($"玩家敏捷: {playerAgi}, 敌人敏捷: {enemyAgi}");
            Debug.Log("开始模拟 10 次行动...");
            
            while (playerActions + enemyActions < 10)
            {
                tick++;
                playerGauge += playerAgi;
                enemyGauge += enemyAgi;
                
                // 检查谁先行动
                while (playerGauge >= maxGauge || enemyGauge >= maxGauge)
                {
                    if (playerGauge >= maxGauge && (enemyGauge < maxGauge || playerGauge >= enemyGauge))
                    {
                        playerActions++;
                        Debug.Log($"Tick {tick}: 玩家行动 (第{playerActions}次)");
                        playerGauge -= maxGauge;
                    }
                    else if (enemyGauge >= maxGauge)
                    {
                        enemyActions++;
                        Debug.Log($"Tick {tick}: 敌人行动 (第{enemyActions}次)");
                        enemyGauge -= maxGauge;
                    }
                }
            }
            
            Debug.Log($"结果: 玩家行动 {playerActions} 次, 敌人行动 {enemyActions} 次");
            Debug.Log($"比例: 玩家:敌人 = {playerAgi}:{enemyAgi} ≈ 1:{(float)enemyAgi/playerAgi:F2}");
        }

        private void Update()
        {
            // 测试快捷键
            if (Input.GetKeyDown(KeyCode.T))
            {
                TestDiceRoll();
            }

            if (Input.GetKeyDown(KeyCode.B))
            {
                StartTestBattle();
            }

            if (Input.GetKeyDown(KeyCode.G))
            {
                SimulateActionGauge();
            }

            // 战斗中的快捷键
            if (CombatManager.Instance != null && 
                CombatManager.Instance.CurrentState == CombatState.WaitingInput)
            {
                // 数字键选择目标攻击
                for (int i = 0; i < 9; i++)
                {
                    if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                    {
                        CombatManager.Instance.PlayerSelectAttack(i);
                        break;
                    }
                }

                // D键防御
                if (Input.GetKeyDown(KeyCode.D))
                {
                    CombatManager.Instance.PlayerSelectDefend();
                }
            }

            // ESC强制结束战斗
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CombatManager.Instance?.ForceEndCombat();
            }
        }

        private void OnGUI()
        {
            // 简单的调试UI
            GUILayout.BeginArea(new Rect(10, 10, 300, 500));
            
            GUILayout.Label("=== 调试控制 ===");
            GUILayout.Label("T: 测试骰子");
            GUILayout.Label("B: 开始战斗");
            GUILayout.Label("G: 模拟气力系统");
            GUILayout.Label("ESC: 强制结束战斗");
            
            if (CombatManager.Instance != null)
            {
                GUILayout.Space(10);
                GUILayout.Label($"战斗状态: {CombatManager.Instance.CurrentState}");
                
                if (CombatManager.Instance.CurrentState == CombatState.WaitingInput)
                {
                    GUILayout.Label("1-9: 选择目标攻击");
                    GUILayout.Label("D: 防御");
                }

                // 显示单位信息
                GUILayout.Space(10);
                GUILayout.Label("=== 战斗单位 ===");
                
                foreach (var unit in CombatManager.Instance.AllUnits)
                {
                    if (unit.IsAlive)
                    {
                        string status = unit.IsReadyToAct ? " [就绪]" : "";
                        GUILayout.Label($"{unit.UnitName}: HP {unit.Stats.CurrentHP}/{unit.Stats.MaxHP} " +
                                       $"气力 {unit.CurrentActionGauge:F0}/100{status}");
                    }
                    else
                    {
                        GUILayout.Label($"{unit.UnitName}: [已击败]");
                    }
                }
            }
            
            GUILayout.EndArea();
        }
    }

    /// <summary>
    /// 敌人配置（用于Inspector）
    /// </summary>
    [System.Serializable]
    public class EnemyConfig
    {
        public string enemyName = "敌人";
        [Range(1, 20)] public int strength = 5;
        [Range(1, 20)] public int agility = 5;
        [Range(1, 20)] public int wisdom = 5;
    }
}

