#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace WanWanKan.Editor
{
    /// <summary>
    /// 战斗UI创建工具 - 在编辑器中一键生成UI预制体
    /// </summary>
    public class CombatUICreator : EditorWindow
    {
        // 中文字体路径
        private const string CHINESE_FONT_PATH = "Assets/Resources/Font/SourceHanSansSC-Normal SDF.asset";
        
        // 缓存字体资源
        private static TMP_FontAsset _chineseFont;
        
        /// <summary>
        /// 获取中文字体
        /// </summary>
        private static TMP_FontAsset GetChineseFont()
        {
            if (_chineseFont == null)
            {
                _chineseFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CHINESE_FONT_PATH);
                if (_chineseFont == null)
                {
                    Debug.LogWarning($"未找到中文字体: {CHINESE_FONT_PATH}，将使用默认字体");
                }
            }
            return _chineseFont;
        }
        
        /// <summary>
        /// 设置 TMP 文本使用中文字体
        /// </summary>
        private static void SetChineseFont(TextMeshProUGUI tmp)
        {
            if (tmp != null)
            {
                var font = GetChineseFont();
                if (font != null)
                {
                    tmp.font = font;
                }
            }
        }

        [MenuItem("WanWanKan/创建战斗UI")]
        public static void ShowWindow()
        {
            GetWindow<CombatUICreator>("战斗UI创建器");
        }

        private void OnGUI()
        {
            GUILayout.Label("战斗UI创建工具", EditorStyles.boldLabel);
            GUILayout.Space(10);

            if (GUILayout.Button("创建完整战斗UI", GUILayout.Height(40)))
            {
                CreateCompleteCombatUI();
            }

            GUILayout.Space(10);
            GUILayout.Label("单独创建组件:", EditorStyles.boldLabel);

            if (GUILayout.Button("创建气力条预制体"))
            {
                CreateActionGaugePrefab();
            }

            if (GUILayout.Button("创建战斗菜单"))
            {
                CreateActionMenu();
            }

            if (GUILayout.Button("创建目标选择面板"))
            {
                CreateTargetSelectionPanel();
            }

            if (GUILayout.Button("创建骰子显示UI"))
            {
                CreateDiceUI();
            }

            if (GUILayout.Button("创建战斗日志"))
            {
                CreateCombatLog();
            }

            if (GUILayout.Button("创建结果面板(胜利/失败)"))
            {
                CreateResultPanels();
            }
        }

        /// <summary>
        /// 创建完整的战斗UI系统
        /// </summary>
        [MenuItem("WanWanKan/一键创建完整战斗UI")]
        public static void CreateCompleteCombatUI()
        {
            // 查找或创建Canvas
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGO = new GameObject("CombatCanvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasGO.AddComponent<GraphicRaycaster>();
            }

            GameObject combatUI = new GameObject("CombatUI");
            combatUI.transform.SetParent(canvas.transform, false);
            RectTransform combatRect = combatUI.AddComponent<RectTransform>();
            combatRect.anchorMin = Vector2.zero;
            combatRect.anchorMax = Vector2.one;
            combatRect.offsetMin = Vector2.zero;
            combatRect.offsetMax = Vector2.zero;

            // 添加CombatUI组件
            var combatUIScript = combatUI.AddComponent<UI.CombatUI>();

            // 创建各个UI部分
            GameObject playerGauges = CreateGaugeContainer(combatUI.transform, "PlayerGauges", new Vector2(0, 1), new Vector2(0.3f, 1), new Vector2(10, -10), new Vector2(-10, -100));
            GameObject enemyGauges = CreateGaugeContainer(combatUI.transform, "EnemyGauges", new Vector2(0.7f, 1), new Vector2(1, 1), new Vector2(10, -10), new Vector2(-10, -100));
            
            GameObject actionMenu = CreateActionMenuInternal(combatUI.transform);
            GameObject targetPanel = CreateTargetSelectionInternal(combatUI.transform);
            GameObject diceUI = CreateDiceUIInternal(combatUI.transform);
            GameObject combatLog = CreateCombatLogInternal(combatUI.transform);
            GameObject actionOrder = CreateActionOrderPreview(combatUI.transform);
            
            CreateResultPanelsInternal(combatUI.transform, out GameObject victoryPanel, out GameObject defeatPanel);
            GameObject battleState = CreateBattleStateText(combatUI.transform);

            // 创建气力条预制体
            GameObject gaugePrefab = CreateActionGaugePrefabInternal();
            
            // 设置CombatUI的引用（通过SerializedObject）
            SerializedObject so = new SerializedObject(combatUIScript);
            so.FindProperty("playerGaugeContainer").objectReferenceValue = playerGauges.transform;
            so.FindProperty("enemyGaugeContainer").objectReferenceValue = enemyGauges.transform;
            so.FindProperty("actionGaugePrefab").objectReferenceValue = gaugePrefab;
            so.FindProperty("actionMenuPanel").objectReferenceValue = actionMenu;
            so.FindProperty("attackButton").objectReferenceValue = actionMenu.transform.Find("AttackButton")?.GetComponent<Button>();
            so.FindProperty("defendButton").objectReferenceValue = actionMenu.transform.Find("DefendButton")?.GetComponent<Button>();
            so.FindProperty("skillButton").objectReferenceValue = actionMenu.transform.Find("SkillButton")?.GetComponent<Button>();
            so.FindProperty("itemButton").objectReferenceValue = actionMenu.transform.Find("ItemButton")?.GetComponent<Button>();
            so.FindProperty("targetSelectionPanel").objectReferenceValue = targetPanel;
            so.FindProperty("targetButtonContainer").objectReferenceValue = targetPanel.transform.Find("TargetButtonContainer");
            so.FindProperty("combatLogText").objectReferenceValue = combatLog.GetComponentInChildren<TextMeshProUGUI>();
            so.FindProperty("combatLogScroll").objectReferenceValue = combatLog.GetComponent<ScrollRect>();
            so.FindProperty("battleStateText").objectReferenceValue = battleState.GetComponent<TextMeshProUGUI>();
            so.FindProperty("victoryPanel").objectReferenceValue = victoryPanel;
            so.FindProperty("defeatPanel").objectReferenceValue = defeatPanel;
            so.FindProperty("actionOrderContainer").objectReferenceValue = actionOrder.transform;
            so.ApplyModifiedProperties();

            // 保存预制体
            SavePrefab(combatUI, "Assets/Prefabs/UI/CombatUI.prefab");
            SavePrefab(gaugePrefab, "Assets/Prefabs/UI/ActionGauge.prefab");

            Debug.Log("✅ 战斗UI创建完成！预制体已保存到 Assets/Prefabs/UI/");
            Selection.activeGameObject = combatUI;
        }

        private static GameObject CreateGaugeContainer(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject container = new GameObject(name);
            container.transform.SetParent(parent, false);
            
            RectTransform rect = container.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            VerticalLayoutGroup layout = container.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 5;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.padding = new RectOffset(5, 5, 5, 5);

            // 添加背景
            Image bg = container.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.5f);

            return container;
        }

        public static void CreateActionGaugePrefab()
        {
            GameObject prefab = CreateActionGaugePrefabInternal();
            SavePrefab(prefab, "Assets/Prefabs/UI/ActionGauge.prefab");
            Debug.Log("✅ 气力条预制体已创建！");
        }

        private static GameObject CreateActionGaugePrefabInternal()
        {
            GameObject gauge = new GameObject("ActionGauge");
            RectTransform gaugeRect = gauge.AddComponent<RectTransform>();
            gaugeRect.sizeDelta = new Vector2(200, 40);

            HorizontalLayoutGroup hLayout = gauge.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 5;
            hLayout.childForceExpandWidth = false;
            hLayout.childForceExpandHeight = true;
            hLayout.childControlWidth = false;
            hLayout.childControlHeight = true;
            hLayout.padding = new RectOffset(5, 5, 2, 2);

            // 背景
            Image bgImage = gauge.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            // 单位图标
            GameObject icon = new GameObject("UnitIcon");
            icon.transform.SetParent(gauge.transform, false);
            RectTransform iconRect = icon.AddComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(36, 36);
            Image iconImage = icon.AddComponent<Image>();
            iconImage.color = Color.white;
            LayoutElement iconLayout = icon.AddComponent<LayoutElement>();
            iconLayout.minWidth = 36;
            iconLayout.preferredWidth = 36;

            // 信息容器
            GameObject infoContainer = new GameObject("InfoContainer");
            infoContainer.transform.SetParent(gauge.transform, false);
            VerticalLayoutGroup vLayout = infoContainer.AddComponent<VerticalLayoutGroup>();
            vLayout.spacing = 2;
            vLayout.childForceExpandWidth = true;
            vLayout.childForceExpandHeight = false;
            LayoutElement infoLayout = infoContainer.AddComponent<LayoutElement>();
            infoLayout.flexibleWidth = 1;

            // 名字
            GameObject nameObj = new GameObject("UnitName");
            nameObj.transform.SetParent(infoContainer.transform, false);
            TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
            SetChineseFont(nameText);
            nameText.text = "单位名称";
            nameText.fontSize = 14;
            nameText.color = Color.white;
            nameText.alignment = TextAlignmentOptions.Left;
            RectTransform nameRect = nameObj.GetComponent<RectTransform>();
            nameRect.sizeDelta = new Vector2(0, 16);

            // ========== HP条 ==========
            GameObject hpBar = new GameObject("HPBar");
            hpBar.transform.SetParent(infoContainer.transform, false);
            RectTransform hpBarRect = hpBar.AddComponent<RectTransform>();
            hpBarRect.sizeDelta = new Vector2(0, 8);
            Image hpBarBg = hpBar.AddComponent<Image>();
            hpBarBg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            // HP伤害延迟条（白色，在绿色条下面）
            GameObject hpDamage = new GameObject("HPDamage");
            hpDamage.transform.SetParent(hpBar.transform, false);
            RectTransform hpDamageRect = hpDamage.AddComponent<RectTransform>();
            hpDamageRect.anchorMin = Vector2.zero;
            hpDamageRect.anchorMax = Vector2.one;
            hpDamageRect.offsetMin = Vector2.zero;
            hpDamageRect.offsetMax = Vector2.zero;
            Image hpDamageImage = hpDamage.AddComponent<Image>();
            hpDamageImage.color = new Color(1f, 1f, 1f, 0.8f);
            hpDamageImage.type = Image.Type.Filled;
            hpDamageImage.fillMethod = Image.FillMethod.Horizontal;
            hpDamageImage.fillAmount = 1f;

            // HP填充条（绿色）
            GameObject hpFill = new GameObject("HPFill");
            hpFill.transform.SetParent(hpBar.transform, false);
            RectTransform hpFillRect = hpFill.AddComponent<RectTransform>();
            hpFillRect.anchorMin = Vector2.zero;
            hpFillRect.anchorMax = Vector2.one;
            hpFillRect.offsetMin = Vector2.zero;
            hpFillRect.offsetMax = Vector2.zero;
            Image hpFillImage = hpFill.AddComponent<Image>();
            hpFillImage.color = new Color(0.2f, 0.8f, 0.2f, 1f);
            hpFillImage.type = Image.Type.Filled;
            hpFillImage.fillMethod = Image.FillMethod.Horizontal;
            hpFillImage.fillAmount = 1f;

            // HP数值文本
            GameObject hpValueObj = new GameObject("HPValue");
            hpValueObj.transform.SetParent(hpBar.transform, false);
            RectTransform hpValueRect = hpValueObj.AddComponent<RectTransform>();
            hpValueRect.anchorMin = Vector2.zero;
            hpValueRect.anchorMax = Vector2.one;
            hpValueRect.offsetMin = Vector2.zero;
            hpValueRect.offsetMax = Vector2.zero;
            TextMeshProUGUI hpValueText = hpValueObj.AddComponent<TextMeshProUGUI>();
            SetChineseFont(hpValueText);
            hpValueText.text = "20/20";
            hpValueText.fontSize = 8;
            hpValueText.color = Color.white;
            hpValueText.alignment = TextAlignmentOptions.Center;
            hpValueText.fontStyle = FontStyles.Bold;

            // ========== 气力条 ==========
            // 气力条背景
            GameObject gaugeBar = new GameObject("GaugeBackground");
            gaugeBar.transform.SetParent(infoContainer.transform, false);
            RectTransform barRect = gaugeBar.AddComponent<RectTransform>();
            barRect.sizeDelta = new Vector2(0, 10);
            Image barBg = gaugeBar.AddComponent<Image>();
            barBg.color = new Color(0.1f, 0.1f, 0.1f, 1f);

            // 气力条填充
            GameObject gaugeFill = new GameObject("GaugeFill");
            gaugeFill.transform.SetParent(gaugeBar.transform, false);
            RectTransform fillRect = gaugeFill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fillImage = gaugeFill.AddComponent<Image>();
            fillImage.color = new Color(0.2f, 0.6f, 1f, 1f);
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillAmount = 0.7f;

            // 就绪指示器
            GameObject readyIndicator = new GameObject("ReadyIndicator");
            readyIndicator.transform.SetParent(gaugeBar.transform, false);
            RectTransform readyRect = readyIndicator.AddComponent<RectTransform>();
            readyRect.anchorMin = Vector2.zero;
            readyRect.anchorMax = Vector2.one;
            readyRect.offsetMin = Vector2.zero;
            readyRect.offsetMax = Vector2.zero;
            Image readyImage = readyIndicator.AddComponent<Image>();
            readyImage.color = new Color(1f, 0.9f, 0.2f, 0.5f);
            readyIndicator.SetActive(false);

            // 气力值文本
            GameObject valueObj = new GameObject("GaugeValue");
            valueObj.transform.SetParent(gaugeBar.transform, false);
            RectTransform valueRect = valueObj.AddComponent<RectTransform>();
            valueRect.anchorMin = Vector2.zero;
            valueRect.anchorMax = Vector2.one;
            valueRect.offsetMin = Vector2.zero;
            valueRect.offsetMax = Vector2.zero;
            TextMeshProUGUI valueText = valueObj.AddComponent<TextMeshProUGUI>();
            SetChineseFont(valueText);
            valueText.text = "70/100";
            valueText.fontSize = 8;
            valueText.color = Color.white;
            valueText.alignment = TextAlignmentOptions.Center;

            // 添加ActionGaugeUI组件并设置引用
            var gaugeUI = gauge.AddComponent<UI.ActionGaugeUI>();
            SerializedObject so = new SerializedObject(gaugeUI);
            so.FindProperty("gaugeBackground").objectReferenceValue = barBg;
            so.FindProperty("gaugeFill").objectReferenceValue = fillImage;
            so.FindProperty("unitIcon").objectReferenceValue = iconImage;
            so.FindProperty("unitNameText").objectReferenceValue = nameText;
            so.FindProperty("gaugeValueText").objectReferenceValue = valueText;
            so.FindProperty("readyIndicator").objectReferenceValue = readyImage;
            // HP相关引用
            so.FindProperty("hpBarBackground").objectReferenceValue = hpBarBg;
            so.FindProperty("hpBarFill").objectReferenceValue = hpFillImage;
            so.FindProperty("hpBarDamage").objectReferenceValue = hpDamageImage;
            so.FindProperty("hpValueText").objectReferenceValue = hpValueText;
            so.ApplyModifiedProperties();

            return gauge;
        }

        public static void CreateActionMenu()
        {
            Canvas canvas = FindOrCreateCanvas();
            GameObject menu = CreateActionMenuInternal(canvas.transform);
            SavePrefab(menu, "Assets/Prefabs/UI/ActionMenu.prefab");
            Debug.Log("✅ 战斗菜单已创建！");
        }

        private static GameObject CreateActionMenuInternal(Transform parent)
        {
            GameObject menu = new GameObject("ActionMenu");
            menu.transform.SetParent(parent, false);
            
            RectTransform rect = menu.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0);
            rect.anchorMax = new Vector2(0.5f, 0);
            rect.pivot = new Vector2(0.5f, 0);
            rect.anchoredPosition = new Vector2(0, 20);
            rect.sizeDelta = new Vector2(400, 60);

            Image bg = menu.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);

            HorizontalLayoutGroup layout = menu.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.childAlignment = TextAnchor.MiddleCenter;

            // 创建按钮
            CreateMenuButton(menu.transform, "AttackButton", "攻击", new Color(0.8f, 0.3f, 0.3f));
            CreateMenuButton(menu.transform, "DefendButton", "防御", new Color(0.3f, 0.5f, 0.8f));
            CreateMenuButton(menu.transform, "SkillButton", "技能", new Color(0.6f, 0.3f, 0.8f));
            CreateMenuButton(menu.transform, "ItemButton", "物品", new Color(0.3f, 0.7f, 0.4f));

            menu.SetActive(false);
            return menu;
        }

        private static void CreateMenuButton(Transform parent, string name, string text, Color color)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            Image btnImage = btnObj.AddComponent<Image>();
            btnImage.color = color;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = btnImage;

            ColorBlock colors = btn.colors;
            colors.highlightedColor = color * 1.2f;
            colors.pressedColor = color * 0.8f;
            btn.colors = colors;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
            SetChineseFont(tmpText);
            tmpText.text = text;
            tmpText.fontSize = 18;
            tmpText.color = Color.white;
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.fontStyle = FontStyles.Bold;
        }

        public static void CreateTargetSelectionPanel()
        {
            Canvas canvas = FindOrCreateCanvas();
            GameObject panel = CreateTargetSelectionInternal(canvas.transform);
            SavePrefab(panel, "Assets/Prefabs/UI/TargetSelection.prefab");
            Debug.Log("✅ 目标选择面板已创建！");
        }

        private static GameObject CreateTargetSelectionInternal(Transform parent)
        {
            GameObject panel = new GameObject("TargetSelectionPanel");
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(300, 200);

            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

            VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 5;
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            // 标题
            GameObject title = new GameObject("Title");
            title.transform.SetParent(panel.transform, false);
            TextMeshProUGUI titleText = title.AddComponent<TextMeshProUGUI>();
            SetChineseFont(titleText);
            titleText.text = "选择目标";
            titleText.fontSize = 20;
            titleText.color = Color.white;
            titleText.alignment = TextAlignmentOptions.Center;
            LayoutElement titleLayout = title.AddComponent<LayoutElement>();
            titleLayout.preferredHeight = 30;

            // 目标按钮容器
            GameObject container = new GameObject("TargetButtonContainer");
            container.transform.SetParent(panel.transform, false);
            VerticalLayoutGroup containerLayout = container.AddComponent<VerticalLayoutGroup>();
            containerLayout.spacing = 5;
            containerLayout.childForceExpandWidth = true;
            containerLayout.childForceExpandHeight = false;
            LayoutElement containerLE = container.AddComponent<LayoutElement>();
            containerLE.flexibleHeight = 1;

            // 创建目标按钮预制体
            GameObject targetBtnPrefab = CreateTargetButton();
            SavePrefab(targetBtnPrefab, "Assets/Prefabs/UI/TargetButton.prefab");

            panel.SetActive(false);
            return panel;
        }

        private static GameObject CreateTargetButton()
        {
            GameObject btn = new GameObject("TargetButton");
            
            RectTransform rect = btn.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 40);

            Image bg = btn.AddComponent<Image>();
            bg.color = new Color(0.3f, 0.3f, 0.4f);

            Button button = btn.AddComponent<Button>();
            button.targetGraphic = bg;

            LayoutElement layout = btn.AddComponent<LayoutElement>();
            layout.preferredHeight = 40;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btn.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 0);
            textRect.offsetMax = new Vector2(-10, 0);

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            SetChineseFont(text);
            text.text = "敌人名称\nHP: 20/20";
            text.fontSize = 14;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Left;

            return btn;
        }

        public static void CreateDiceUI()
        {
            Canvas canvas = FindOrCreateCanvas();
            GameObject diceUI = CreateDiceUIInternal(canvas.transform);
            SavePrefab(diceUI, "Assets/Prefabs/UI/DiceUI.prefab");
            Debug.Log("✅ 骰子UI已创建！");
        }

        private static GameObject CreateDiceUIInternal(Transform parent)
        {
            GameObject diceUI = new GameObject("DiceUI");
            diceUI.transform.SetParent(parent, false);

            RectTransform rect = diceUI.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(200, 200);

            CanvasGroup canvasGroup = diceUI.AddComponent<CanvasGroup>();

            // 背景
            Image bg = diceUI.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.8f);

            // 骰子图片
            GameObject diceImage = new GameObject("DiceImage");
            diceImage.transform.SetParent(diceUI.transform, false);
            RectTransform diceRect = diceImage.AddComponent<RectTransform>();
            diceRect.anchorMin = new Vector2(0.5f, 0.6f);
            diceRect.anchorMax = new Vector2(0.5f, 0.6f);
            diceRect.sizeDelta = new Vector2(80, 80);
            Image dice = diceImage.AddComponent<Image>();
            dice.color = Color.white;

            // 骰子数值
            GameObject valueObj = new GameObject("DiceValue");
            valueObj.transform.SetParent(diceUI.transform, false);
            RectTransform valueRect = valueObj.AddComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(0.5f, 0.35f);
            valueRect.anchorMax = new Vector2(0.5f, 0.35f);
            valueRect.sizeDelta = new Vector2(150, 50);
            TextMeshProUGUI valueText = valueObj.AddComponent<TextMeshProUGUI>();
            SetChineseFont(valueText);
            valueText.text = "20";
            valueText.fontSize = 48;
            valueText.color = Color.white;
            valueText.alignment = TextAlignmentOptions.Center;
            valueText.fontStyle = FontStyles.Bold;

            // 骰子类型
            GameObject typeObj = new GameObject("DiceType");
            typeObj.transform.SetParent(diceUI.transform, false);
            RectTransform typeRect = typeObj.AddComponent<RectTransform>();
            typeRect.anchorMin = new Vector2(0.5f, 0.15f);
            typeRect.anchorMax = new Vector2(0.5f, 0.15f);
            typeRect.sizeDelta = new Vector2(150, 25);
            TextMeshProUGUI typeText = typeObj.AddComponent<TextMeshProUGUI>();
            SetChineseFont(typeText);
            typeText.text = "1d20+5 = 25";
            typeText.fontSize = 14;
            typeText.color = new Color(0.7f, 0.7f, 0.7f);
            typeText.alignment = TextAlignmentOptions.Center;

            // 结果文本
            GameObject resultObj = new GameObject("ResultText");
            resultObj.transform.SetParent(diceUI.transform, false);
            RectTransform resultRect = resultObj.AddComponent<RectTransform>();
            resultRect.anchorMin = new Vector2(0.5f, 0.05f);
            resultRect.anchorMax = new Vector2(0.5f, 0.05f);
            resultRect.sizeDelta = new Vector2(150, 25);
            TextMeshProUGUI resultText = resultObj.AddComponent<TextMeshProUGUI>();
            SetChineseFont(resultText);
            resultText.text = "";
            resultText.fontSize = 18;
            resultText.color = new Color(1f, 0.8f, 0f);
            resultText.alignment = TextAlignmentOptions.Center;
            resultText.fontStyle = FontStyles.Bold;

            // 添加DiceRollUI组件
            var diceRollUI = diceUI.AddComponent<UI.DiceRollUI>();
            SerializedObject so = new SerializedObject(diceRollUI);
            so.FindProperty("dicePanel").objectReferenceValue = diceUI;
            so.FindProperty("diceImage").objectReferenceValue = dice;
            so.FindProperty("diceValueText").objectReferenceValue = valueText;
            so.FindProperty("diceTypeText").objectReferenceValue = typeText;
            so.FindProperty("resultText").objectReferenceValue = resultText;
            so.ApplyModifiedProperties();

            diceUI.SetActive(false);
            return diceUI;
        }

        public static void CreateCombatLog()
        {
            Canvas canvas = FindOrCreateCanvas();
            GameObject log = CreateCombatLogInternal(canvas.transform);
            SavePrefab(log, "Assets/Prefabs/UI/CombatLog.prefab");
            Debug.Log("✅ 战斗日志已创建！");
        }

        private static GameObject CreateCombatLogInternal(Transform parent)
        {
            GameObject logPanel = new GameObject("CombatLog");
            logPanel.transform.SetParent(parent, false);

            RectTransform rect = logPanel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(0.35f, 0.3f);
            rect.offsetMin = new Vector2(10, 10);
            rect.offsetMax = new Vector2(-10, -10);

            Image bg = logPanel.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.7f);

            // ScrollRect
            ScrollRect scroll = logPanel.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            // Viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(logPanel.transform, false);
            RectTransform viewRect = viewport.AddComponent<RectTransform>();
            viewRect.anchorMin = Vector2.zero;
            viewRect.anchorMax = Vector2.one;
            viewRect.offsetMin = new Vector2(5, 5);
            viewRect.offsetMax = new Vector2(-5, -5);
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            viewport.AddComponent<Image>();

            // Content
            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0, 1);
            contentRect.sizeDelta = new Vector2(0, 0);

            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Log Text
            GameObject textObj = new GameObject("LogText");
            textObj.transform.SetParent(content.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = new Vector2(1, 1);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI logText = textObj.AddComponent<TextMeshProUGUI>();
            SetChineseFont(logText);
            logText.text = "[00:00:00] 战斗日志...\n[00:00:01] 测试内容...";
            logText.fontSize = 12;
            logText.color = new Color(0.9f, 0.9f, 0.9f);
            logText.alignment = TextAlignmentOptions.TopLeft;

            scroll.viewport = viewRect;
            scroll.content = contentRect;

            return logPanel;
        }

        private static GameObject CreateActionOrderPreview(Transform parent)
        {
            GameObject orderPanel = new GameObject("ActionOrderPreview");
            orderPanel.transform.SetParent(parent, false);

            RectTransform rect = orderPanel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1);
            rect.anchorMax = new Vector2(0.5f, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, -10);
            rect.sizeDelta = new Vector2(300, 50);

            Image bg = orderPanel.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.5f);

            HorizontalLayoutGroup layout = orderPanel.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 5;
            layout.padding = new RectOffset(10, 10, 5, 5);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            // 创建预览图标预制体
            GameObject iconPrefab = new GameObject("ActionOrderIcon");
            RectTransform iconRect = iconPrefab.AddComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(40, 40);
            Image iconImage = iconPrefab.AddComponent<Image>();
            iconImage.color = Color.white;
            LayoutElement iconLayout = iconPrefab.AddComponent<LayoutElement>();
            iconLayout.preferredWidth = 40;
            iconLayout.preferredHeight = 40;

            SavePrefab(iconPrefab, "Assets/Prefabs/UI/ActionOrderIcon.prefab");

            return orderPanel;
        }

        public static void CreateResultPanels()
        {
            Canvas canvas = FindOrCreateCanvas();
            CreateResultPanelsInternal(canvas.transform, out GameObject victory, out GameObject defeat);
            SavePrefab(victory, "Assets/Prefabs/UI/VictoryPanel.prefab");
            SavePrefab(defeat, "Assets/Prefabs/UI/DefeatPanel.prefab");
            Debug.Log("✅ 结果面板已创建！");
        }

        private static void CreateResultPanelsInternal(Transform parent, out GameObject victoryPanel, out GameObject defeatPanel)
        {
            victoryPanel = CreateResultPanel(parent, "VictoryPanel", "胜 利 ！", new Color(1f, 0.85f, 0.2f));
            defeatPanel = CreateResultPanel(parent, "DefeatPanel", "失 败 ...", new Color(0.6f, 0.2f, 0.2f));
        }

        private static GameObject CreateResultPanel(Transform parent, string name, string text, Color color)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.8f);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(panel.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.sizeDelta = new Vector2(400, 100);

            TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
            SetChineseFont(tmpText);
            tmpText.text = text;
            tmpText.fontSize = 72;
            tmpText.color = color;
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.fontStyle = FontStyles.Bold;

            panel.SetActive(false);
            return panel;
        }

        private static GameObject CreateBattleStateText(Transform parent)
        {
            GameObject stateObj = new GameObject("BattleStateText");
            stateObj.transform.SetParent(parent, false);

            RectTransform rect = stateObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.85f);
            rect.anchorMax = new Vector2(0.5f, 0.85f);
            rect.sizeDelta = new Vector2(300, 40);

            TextMeshProUGUI text = stateObj.AddComponent<TextMeshProUGUI>();
            SetChineseFont(text);
            text.text = "战斗进行中...";
            text.fontSize = 24;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;

            return stateObj;
        }

        private static Canvas FindOrCreateCanvas()
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGO = new GameObject("Canvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasGO.AddComponent<GraphicRaycaster>();
            }
            return canvas;
        }

        private static void SavePrefab(GameObject obj, string path)
        {
            // 确保目录存在
            string directory = System.IO.Path.GetDirectoryName(path);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            // 如果对象在场景中，保存为预制体
            if (obj.scene.IsValid())
            {
                PrefabUtility.SaveAsPrefabAsset(obj, path);
            }
            else
            {
                // 临时添加到场景再保存
                GameObject tempParent = new GameObject("_TempPrefabParent");
                obj.transform.SetParent(tempParent.transform);
                PrefabUtility.SaveAsPrefabAsset(obj, path);
                DestroyImmediate(tempParent);
            }
        }
    }
}
#endif

