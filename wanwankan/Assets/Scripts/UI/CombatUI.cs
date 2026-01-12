using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WanWanKan.Combat;

namespace WanWanKan.UI
{
    /// <summary>
    /// 战斗UI管理器 - 管理整个战斗界面
    /// </summary>
    public class CombatUI : MonoBehaviour
    {
        [Header("气力条面板")]
        [SerializeField] private Transform playerGaugeContainer;
        [SerializeField] private Transform enemyGaugeContainer;
        [SerializeField] private GameObject actionGaugePrefab;

        [Header("行动顺序预览")]
        [SerializeField] private Transform actionOrderContainer;
        [SerializeField] private GameObject actionOrderIconPrefab;
        [SerializeField] private int actionOrderPreviewCount = 6;

        [Header("行动菜单")]
        [SerializeField] private GameObject actionMenuPanel;
        [SerializeField] private Button attackButton;
        [SerializeField] private Button defendButton;
        [SerializeField] private Button skillButton;
        [SerializeField] private Button itemButton;

        [Header("目标选择")]
        [SerializeField] private GameObject targetSelectionPanel;
        [SerializeField] private Transform targetButtonContainer;
        [SerializeField] private GameObject targetButtonPrefab;

        [Header("战斗日志")]
        [SerializeField] private TextMeshProUGUI combatLogText;
        [SerializeField] private ScrollRect combatLogScroll;
        [SerializeField] private int maxLogLines = 50;

        [Header("状态显示")]
        [SerializeField] private TextMeshProUGUI battleStateText;
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private GameObject defeatPanel;

        // 气力条UI实例
        private Dictionary<CombatUnit, ActionGaugeUI> gaugeUIDict = new Dictionary<CombatUnit, ActionGaugeUI>();
        private List<Image> actionOrderIcons = new List<Image>();
        private List<string> combatLogs = new List<string>();

        private CombatManager combatManager;
        private CombatUnit currentPlayerUnit;
        
        // 目标选择状态
        private bool isSelectingTarget = false;

        private void Awake()
        {
            // 设置按钮事件
            SetupButtons();
            
            // 初始隐藏面板
            HideAllPanels();
        }

        private void Start()
        {
            // 延迟一帧订阅，确保 CombatManager 已创建
            StartCoroutine(LateSubscribe());
        }

        private System.Collections.IEnumerator LateSubscribe()
        {
            // 等待一帧，确保所有 Start() 都执行完
            yield return null;
            
            combatManager = CombatManager.Instance;
            
            if (combatManager != null)
            {
                // 订阅战斗事件
                combatManager.OnCombatStart += OnCombatStart;
                combatManager.OnCombatStateChanged += OnCombatStateChanged;
                combatManager.OnUnitTurnStart += OnUnitTurnStart;
                combatManager.OnUnitTurnEnd += OnUnitTurnEnd;
                combatManager.OnUnitDefeated += OnUnitDefeated;
                combatManager.OnCombatEnd += OnCombatEnd;
                combatManager.OnWaitingForPlayerInput += OnWaitingForPlayerInput;
                
                // 如果战斗已经开始，主动初始化UI
                if (combatManager.CurrentState != CombatState.NotInCombat)
                {
                    OnCombatStart();
                }
            }
        }

        private void Update()
        {
            // 更新行动顺序预览
            if (combatManager != null && combatManager.CurrentState == CombatState.Running)
            {
                UpdateActionOrderPreview();
            }
        }

        private void SetupButtons()
        {
            if (attackButton != null)
                attackButton.onClick.AddListener(OnAttackButtonClicked);
            
            if (defendButton != null)
                defendButton.onClick.AddListener(OnDefendButtonClicked);
            
            if (skillButton != null)
                skillButton.onClick.AddListener(OnSkillButtonClicked);
            
            if (itemButton != null)
                itemButton.onClick.AddListener(OnItemButtonClicked);
        }

        #region 战斗事件处理

        private void OnCombatStart()
        {
            ClearUI();
            CreateGaugeUIs();
            InitializeActionOrderIcons();
            
            AddCombatLog("=== 战斗开始 ===");
        }

        private void OnCombatStateChanged(CombatState state)
        {
            if (battleStateText != null)
            {
                battleStateText.text = state switch
                {
                    CombatState.Running => "战斗进行中...",
                    CombatState.WaitingInput => "选择行动",
                    CombatState.ExecutingAction => "执行中...",
                    CombatState.Victory => "胜利！",
                    CombatState.Defeat => "失败...",
                    _ => ""
                };
            }
        }

        private void OnUnitTurnStart(CombatUnit unit)
        {
            AddCombatLog($"► {unit.UnitName} 的回合");
            HighlightUnit(unit);
        }

        private void OnUnitTurnEnd(CombatUnit unit)
        {
            UnhighlightUnit(unit);
            HideActionMenu();
            HideTargetSelection();
            ExitTargetSelectionMode();
        }

        private void OnUnitDefeated(CombatUnit unit)
        {
            AddCombatLog($"✗ {unit.UnitName} 被击败！");
            // 死亡动画会由 ActionGaugeUI 自动处理渐隐效果
        }

        private void OnCombatEnd(bool victory)
        {
            HideAllPanels();
            
            if (victory)
            {
                AddCombatLog("=== 胜利！ ===");
                if (victoryPanel != null)
                    victoryPanel.SetActive(true);
            }
            else
            {
                AddCombatLog("=== 失败... ===");
                if (defeatPanel != null)
                    defeatPanel.SetActive(true);
            }
        }

        private void OnWaitingForPlayerInput(CombatUnit unit)
        {
            Debug.Log($"[CombatUI] 等待玩家输入，当前单位: {unit.UnitName}");
            currentPlayerUnit = unit;
            ShowActionMenu();
        }

        #endregion

        #region UI创建与更新

        private void CreateGaugeUIs()
        {
            Debug.Log($"[CombatUI] CreateGaugeUIs 开始创建...");
            Debug.Log($"[CombatUI] combatManager: {(combatManager != null ? "OK" : "NULL")}");
            Debug.Log($"[CombatUI] actionGaugePrefab: {(actionGaugePrefab != null ? "OK" : "NULL")}");
            Debug.Log($"[CombatUI] playerGaugeContainer: {(playerGaugeContainer != null ? "OK" : "NULL")}");
            Debug.Log($"[CombatUI] enemyGaugeContainer: {(enemyGaugeContainer != null ? "OK" : "NULL")}");
            
            if (combatManager == null)
            {
                Debug.LogError("[CombatUI] combatManager 为空！");
                return;
            }
            
            if (actionGaugePrefab == null)
            {
                Debug.LogError("[CombatUI] actionGaugePrefab 预制体未设置！请在 Inspector 中设置。");
                return;
            }

            // 创建玩家气力条
            Debug.Log($"[CombatUI] 创建 {combatManager.PlayerUnits.Count} 个玩家气力条");
            foreach (var player in combatManager.PlayerUnits)
            {
                CreateGaugeUI(player, playerGaugeContainer);
            }

            // 创建敌人气力条
            Debug.Log($"[CombatUI] 创建 {combatManager.EnemyUnits.Count} 个敌人气力条");
            foreach (var enemy in combatManager.EnemyUnits)
            {
                CreateGaugeUI(enemy, enemyGaugeContainer);
            }
            
            Debug.Log($"[CombatUI] 气力条创建完成，共 {gaugeUIDict.Count} 个");
        }

        private void CreateGaugeUI(CombatUnit unit, Transform container)
        {
            if (container == null)
            {
                Debug.LogWarning($"[CombatUI] 容器为空，无法创建 {unit.UnitName} 的气力条");
                return;
            }

            GameObject gaugeGO = Instantiate(actionGaugePrefab, container);
            ActionGaugeUI gaugeUI = gaugeGO.GetComponent<ActionGaugeUI>();
            
            if (gaugeUI != null)
            {
                gaugeUI.BindUnit(unit);
                gaugeUIDict[unit] = gaugeUI;
                
                // 订阅点击事件（用于选择目标）
                gaugeUI.OnUnitClicked += OnGaugeUIClicked;
                
                // 订阅死亡动画完成事件
                gaugeUI.OnDeathAnimationComplete += OnGaugeUIDeathComplete;
                
                Debug.Log($"[CombatUI] 成功创建 {unit.UnitName} 的气力条");
            }
            else
            {
                Debug.LogError($"[CombatUI] 预制体上没有 ActionGaugeUI 组件！");
            }
        }

        /// <summary>
        /// 气力条被点击时的处理
        /// </summary>
        private void OnGaugeUIClicked(CombatUnit clickedUnit)
        {
            if (!isSelectingTarget) return;
            
            // 只能选择敌人
            if (clickedUnit.UnitType == UnitType.Enemy && clickedUnit.IsAlive)
            {
                Debug.Log($"[CombatUI] 选择目标: {clickedUnit.UnitName}");
                ExitTargetSelectionMode();
                combatManager?.PlayerAttackTarget(clickedUnit);
            }
        }

        /// <summary>
        /// 气力条死亡动画完成
        /// </summary>
        private void OnGaugeUIDeathComplete(ActionGaugeUI gaugeUI)
        {
            Debug.Log($"[CombatUI] 单位死亡动画完成");
            
            // 从字典中移除
            CombatUnit unitToRemove = null;
            foreach (var kvp in gaugeUIDict)
            {
                if (kvp.Value == gaugeUI)
                {
                    unitToRemove = kvp.Key;
                    break;
                }
            }
            
            if (unitToRemove != null)
            {
                gaugeUIDict.Remove(unitToRemove);
            }
        }

        private void InitializeActionOrderIcons()
        {
            if (actionOrderContainer == null || actionOrderIconPrefab == null) return;

            // 清理旧图标
            foreach (var icon in actionOrderIcons)
            {
                if (icon != null) Destroy(icon.gameObject);
            }
            actionOrderIcons.Clear();

            // 创建预览图标
            for (int i = 0; i < actionOrderPreviewCount; i++)
            {
                GameObject iconGO = Instantiate(actionOrderIconPrefab, actionOrderContainer);
                Image icon = iconGO.GetComponent<Image>();
                if (icon != null)
                {
                    actionOrderIcons.Add(icon);
                }
            }
        }

        private void UpdateActionOrderPreview()
        {
            if (combatManager == null) return;

            var prediction = combatManager.PredictActionOrder(actionOrderPreviewCount);
            
            for (int i = 0; i < actionOrderIcons.Count; i++)
            {
                if (i < prediction.Count)
                {
                    CombatUnit unit = prediction[i];
                    actionOrderIcons[i].gameObject.SetActive(true);
                    
                    // 设置图标颜色
                    actionOrderIcons[i].color = unit.UnitType switch
                    {
                        UnitType.Player => new Color(0.2f, 0.6f, 1f),
                        UnitType.Enemy => new Color(1f, 0.3f, 0.3f),
                        _ => Color.white
                    };

                    // 如果有图标则使用
                    if (unit.UnitIcon != null)
                    {
                        actionOrderIcons[i].sprite = unit.UnitIcon;
                    }
                }
                else
                {
                    actionOrderIcons[i].gameObject.SetActive(false);
                }
            }
        }

        private void HighlightUnit(CombatUnit unit)
        {
            if (gaugeUIDict.TryGetValue(unit, out var gaugeUI))
            {
                // 添加高亮效果
                gaugeUI.transform.localScale = Vector3.one * 1.1f;
            }
        }

        private void UnhighlightUnit(CombatUnit unit)
        {
            if (gaugeUIDict.TryGetValue(unit, out var gaugeUI))
            {
                gaugeUI.transform.localScale = Vector3.one;
            }
        }

        #endregion

        #region 行动菜单

        private void ShowActionMenu()
        {
            Debug.Log($"[CombatUI] ShowActionMenu 调用");
            Debug.Log($"[CombatUI] actionMenuPanel: {(actionMenuPanel != null ? "OK" : "NULL")}");
            Debug.Log($"[CombatUI] attackButton: {(attackButton != null ? "OK" : "NULL")}");
            
            if (actionMenuPanel != null)
            {
                actionMenuPanel.SetActive(true);
                Debug.Log($"[CombatUI] ActionMenu 已显示");
            }
            else
            {
                Debug.LogError("[CombatUI] actionMenuPanel 未绑定！请运行 WanWanKan → 自动绑定CombatUI引用");
            }
        }

        private void HideActionMenu()
        {
            if (actionMenuPanel != null)
                actionMenuPanel.SetActive(false);
        }

        private void OnAttackButtonClicked()
        {
            HideActionMenu();
            EnterTargetSelectionMode();
        }

        private void OnDefendButtonClicked()
        {
            HideActionMenu();
            combatManager?.PlayerSelectDefend();
        }

        private void OnSkillButtonClicked()
        {
            // TODO: 显示技能列表
            AddCombatLog("技能系统开发中...");
        }

        private void OnItemButtonClicked()
        {
            // TODO: 显示物品列表
            AddCombatLog("物品系统开发中...");
        }

        #endregion

        #region 目标选择（直接点击敌人）

        /// <summary>
        /// 进入目标选择模式 - 敌人气力条变为可点击
        /// </summary>
        private void EnterTargetSelectionMode()
        {
            isSelectingTarget = true;
            AddCombatLog("选择攻击目标...");
            
            // 让所有存活敌人的气力条可选择
            foreach (var kvp in gaugeUIDict)
            {
                CombatUnit unit = kvp.Key;
                ActionGaugeUI gaugeUI = kvp.Value;
                
                if (unit.UnitType == UnitType.Enemy && unit.IsAlive)
                {
                    gaugeUI.SetSelectable(true);
                }
            }
            
            // 更新状态文本
            if (battleStateText != null)
            {
                battleStateText.text = "点击敌人选择目标";
            }
        }

        /// <summary>
        /// 退出目标选择模式
        /// </summary>
        private void ExitTargetSelectionMode()
        {
            isSelectingTarget = false;
            
            // 取消所有气力条的可选择状态
            foreach (var kvp in gaugeUIDict)
            {
                kvp.Value.SetSelectable(false);
            }
        }

        #endregion

        #region 目标选择（按钮面板 - 备用）

        private void ShowTargetSelection()
        {
            Debug.Log($"[CombatUI] ShowTargetSelection 调用");
            Debug.Log($"[CombatUI] targetSelectionPanel: {(targetSelectionPanel != null ? "OK" : "NULL")}");
            Debug.Log($"[CombatUI] targetButtonContainer: {(targetButtonContainer != null ? "OK" : "NULL")}");
            Debug.Log($"[CombatUI] targetButtonPrefab: {(targetButtonPrefab != null ? "OK" : "NULL")}");
            
            if (targetSelectionPanel != null)
            {
                targetSelectionPanel.SetActive(true);
            }
            else
            {
                Debug.LogError("[CombatUI] targetSelectionPanel 未绑定！");
                return;
            }

            if (targetButtonContainer == null)
            {
                Debug.LogError("[CombatUI] targetButtonContainer 未绑定！");
                return;
            }
            
            if (targetButtonPrefab == null)
            {
                Debug.LogError("[CombatUI] targetButtonPrefab 未绑定！");
                return;
            }

            // 清理旧按钮
            foreach (Transform child in targetButtonContainer)
            {
                Destroy(child.gameObject);
            }

            // 创建目标按钮
            var aliveEnemies = combatManager.GetAliveEnemies();
            Debug.Log($"[CombatUI] 存活敌人数量: {aliveEnemies.Count}");
            
            for (int i = 0; i < aliveEnemies.Count; i++)
            {
                CombatUnit enemy = aliveEnemies[i];
                
                GameObject buttonGO = Instantiate(targetButtonPrefab, targetButtonContainer);
                Button button = buttonGO.GetComponent<Button>();
                TextMeshProUGUI buttonText = buttonGO.GetComponentInChildren<TextMeshProUGUI>();
                
                if (buttonText != null)
                {
                    buttonText.text = $"{enemy.UnitName}\nHP: {enemy.Stats.CurrentHP}/{enemy.Stats.MaxHP}";
                }

                if (button != null)
                {
                    button.onClick.AddListener(() => OnTargetSelected(enemy));
                }
                
                Debug.Log($"[CombatUI] 创建目标按钮: {enemy.UnitName}");
            }
        }

        private void HideTargetSelection()
        {
            if (targetSelectionPanel != null)
                targetSelectionPanel.SetActive(false);
        }

        private void OnTargetSelected(CombatUnit target)
        {
            HideTargetSelection();
            combatManager?.PlayerAttackTarget(target);
        }

        #endregion

        #region 战斗日志

        public void AddCombatLog(string message)
        {
            string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
            combatLogs.Add($"[{timestamp}] {message}");

            // 限制日志行数
            while (combatLogs.Count > maxLogLines)
            {
                combatLogs.RemoveAt(0);
            }

            // 更新UI
            if (combatLogText != null)
            {
                combatLogText.text = string.Join("\n", combatLogs);
            }

            // 滚动到底部
            if (combatLogScroll != null)
            {
                Canvas.ForceUpdateCanvases();
                combatLogScroll.verticalNormalizedPosition = 0f;
            }

            Debug.Log($"[CombatLog] {message}");
        }

        #endregion

        #region 工具方法

        private void ClearUI()
        {
            // 清理气力条
            foreach (var kvp in gaugeUIDict)
            {
                if (kvp.Value != null)
                    Destroy(kvp.Value.gameObject);
            }
            gaugeUIDict.Clear();

            // 清理日志
            combatLogs.Clear();
            if (combatLogText != null)
                combatLogText.text = "";
        }

        private void HideAllPanels()
        {
            HideActionMenu();
            HideTargetSelection();
            
            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (defeatPanel != null) defeatPanel.SetActive(false);
        }

        #endregion

        private void OnDestroy()
        {
            if (combatManager != null)
            {
                combatManager.OnCombatStart -= OnCombatStart;
                combatManager.OnCombatStateChanged -= OnCombatStateChanged;
                combatManager.OnUnitTurnStart -= OnUnitTurnStart;
                combatManager.OnUnitTurnEnd -= OnUnitTurnEnd;
                combatManager.OnUnitDefeated -= OnUnitDefeated;
                combatManager.OnCombatEnd -= OnCombatEnd;
                combatManager.OnWaitingForPlayerInput -= OnWaitingForPlayerInput;
            }
        }
    }
}

