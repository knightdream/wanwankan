using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

namespace Dialogue.Editor
{
    /// <summary>
    /// 对话UI编辑器工具
    /// 一键创建对话系统预制体
    /// </summary>
    public class DialogueUICreator : EditorWindow
    {
        [MenuItem("Tools/汪汪看/创建对话系统UI")]
        public static void CreateDialogueUI()
        {
            CreateCompleteDialogueUI();
        }

        private static void CreateCompleteDialogueUI()
        {
            // 确保有Canvas
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100; // 高层级确保显示在最上方
                
                // 设置CanvasScaler为缩放模式
                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
                
                canvasObj.AddComponent<GraphicRaycaster>();
            }
            else
            {
                // 确保现有Canvas的CanvasScaler设置正确
                CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ConstantPixelSize)
                {
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920, 1080);
                    scaler.matchWidthOrHeight = 0.5f;
                }
            }

            // 创建对话系统根对象
            GameObject dialogueSystem = new GameObject("DialogueSystem");
            dialogueSystem.transform.SetParent(canvas.transform, false);

            // 添加RectTransform并设置为全屏
            RectTransform dialogueSystemRect = dialogueSystem.AddComponent<RectTransform>();
            dialogueSystemRect.anchorMin = Vector2.zero;
            dialogueSystemRect.anchorMax = Vector2.one;
            dialogueSystemRect.offsetMin = Vector2.zero;
            dialogueSystemRect.offsetMax = Vector2.zero;

            // 添加DialogueManager组件
            DialogueManager manager = dialogueSystem.AddComponent<DialogueManager>();

            // 创建阻挡面板（防止点击穿透到游戏）
            GameObject blockingPanel = CreateBlockingPanel(dialogueSystem.transform);

            // 创建对话面板
            GameObject dialoguePanel = CreateDialoguePanel(dialogueSystem.transform);

            // 创建DialogueUI组件
            DialogueUI dialogueUI = dialogueSystem.AddComponent<DialogueUI>();

            // 获取所有UI元素引用
            SetupDialogueUIReferences(dialogueUI, dialoguePanel, blockingPanel);

            // 设置Manager引用
            SerializedObject managerSO = new SerializedObject(manager);
            managerSO.FindProperty("dialogueUI").objectReferenceValue = dialogueUI;
            managerSO.ApplyModifiedProperties();

            // 创建预制体文件夹
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Dialogue"))
                AssetDatabase.CreateFolder("Assets/Prefabs", "Dialogue");

            // 保存预制体
            string prefabPath = "Assets/Prefabs/Dialogue/DialogueSystem.prefab";
            PrefabUtility.SaveAsPrefabAsset(dialogueSystem, prefabPath);

            // 创建选项按钮预制体
            CreateChoiceButtonPrefab();

            Debug.Log("✅ 对话系统UI创建完成！预制体已保存到 Assets/Prefabs/Dialogue/");
            Debug.Log("📝 请在 Resources/Dialogues/ 文件夹中创建JSON对话配置文件");

            Selection.activeGameObject = dialogueSystem;
        }

        /// <summary>
        /// 创建阻挡面板
        /// </summary>
        private static GameObject CreateBlockingPanel(Transform parent)
        {
            GameObject panel = new GameObject("BlockingPanel");
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = panel.AddComponent<Image>();
            image.color = new Color(0, 0, 0, 0.3f); // 半透明黑色背景
            image.raycastTarget = true; // 必须为true才能接收点击

            // 添加点击处理器 - 让全屏点击都能响应
            panel.AddComponent<DialogueClickHandler>();

            return panel;
        }

        /// <summary>
        /// 创建对话面板
        /// </summary>
        private static GameObject CreateDialoguePanel(Transform parent)
        {
            GameObject panel = new GameObject("DialoguePanel");
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            // 创建左侧人物立绘
            CreateCharacterImage(panel.transform, "LeftCharacter", true);

            // 创建右侧人物立绘
            CreateCharacterImage(panel.transform, "RightCharacter", false);

            // 创建对话框
            CreateDialogueBox(panel.transform);

            // 创建选项容器
            CreateChoicesContainer(panel.transform);

            // 创建下一句指示器
            CreateNextIndicator(panel.transform);

            return panel;
        }

        /// <summary>
        /// 创建人物立绘
        /// </summary>
        private static GameObject CreateCharacterImage(Transform parent, string name, bool isLeft)
        {
            GameObject character = new GameObject(name);
            character.transform.SetParent(parent, false);

            RectTransform rect = character.AddComponent<RectTransform>();
            
            // 设置锚点和位置
            if (isLeft)
            {
                rect.anchorMin = new Vector2(0, 0);
                rect.anchorMax = new Vector2(0, 1);
                rect.pivot = new Vector2(0, 0);
                rect.anchoredPosition = new Vector2(50, 50);
            }
            else
            {
                rect.anchorMin = new Vector2(1, 0);
                rect.anchorMax = new Vector2(1, 1);
                rect.pivot = new Vector2(1, 0);
                rect.anchoredPosition = new Vector2(-50, 50);
            }
            
            rect.sizeDelta = new Vector2(400, 600);

            Image image = character.AddComponent<Image>();
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;

            // 默认隐藏
            character.SetActive(false);

            return character;
        }

        /// <summary>
        /// 创建对话框
        /// </summary>
        private static GameObject CreateDialogueBox(Transform parent)
        {
            // 对话框容器
            GameObject dialogueBox = new GameObject("DialogueBox");
            dialogueBox.transform.SetParent(parent, false);

            RectTransform boxRect = dialogueBox.AddComponent<RectTransform>();
            // 使用左右拉伸，确保对话框有正确的宽度
            boxRect.anchorMin = new Vector2(0, 0);
            boxRect.anchorMax = new Vector2(1, 0);
            boxRect.pivot = new Vector2(0.5f, 0);
            // 使用offsetMin和offsetMax来设置边距，而不是sizeDelta
            boxRect.offsetMin = new Vector2(50, 20);   // 左边距50，底部边距20
            boxRect.offsetMax = new Vector2(-50, 220); // 右边距50，顶部位置220（高度200）

            Image boxImage = dialogueBox.AddComponent<Image>();
            boxImage.color = new Color(0, 0, 0, 0.85f);
            boxImage.raycastTarget = false;

            // 说话者名称
            GameObject speakerNameObj = new GameObject("SpeakerName");
            speakerNameObj.transform.SetParent(dialogueBox.transform, false);

            RectTransform nameRect = speakerNameObj.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 1);
            nameRect.anchorMax = new Vector2(1, 1);  // 改为拉伸
            nameRect.pivot = new Vector2(0, 1);
            nameRect.offsetMin = new Vector2(30, -50);  // 左边距30，底部位置
            nameRect.offsetMax = new Vector2(-30, -10); // 右边距30，顶部边距10

            TextMeshProUGUI nameText = speakerNameObj.AddComponent<TextMeshProUGUI>();
            nameText.text = "说话者";
            nameText.fontSize = 28;
            nameText.fontStyle = FontStyles.Bold;
            nameText.color = new Color(1f, 0.9f, 0.6f); // 金色
            nameText.alignment = TextAlignmentOptions.Left;
            nameText.raycastTarget = false;

            // 对话内容
            GameObject dialogueTextObj = new GameObject("DialogueText");
            dialogueTextObj.transform.SetParent(dialogueBox.transform, false);

            RectTransform textRect = dialogueTextObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.offsetMin = new Vector2(30, 20);   // 左边距30，底部边距20
            textRect.offsetMax = new Vector2(-30, -60); // 右边距30，顶部边距60（给名字留空间）

            TextMeshProUGUI dialogueText = dialogueTextObj.AddComponent<TextMeshProUGUI>();
            dialogueText.text = "这里是对话内容...";
            dialogueText.fontSize = 24;
            dialogueText.color = Color.white;
            dialogueText.alignment = TextAlignmentOptions.TopLeft;
            dialogueText.raycastTarget = false;

            return dialogueBox;
        }

        /// <summary>
        /// 创建选项容器
        /// </summary>
        private static GameObject CreateChoicesContainer(Transform parent)
        {
            GameObject container = new GameObject("ChoicesContainer");
            container.transform.SetParent(parent, false);

            RectTransform rect = container.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0);
            rect.anchorMax = new Vector2(0.5f, 0);
            rect.pivot = new Vector2(0.5f, 0);
            rect.anchoredPosition = new Vector2(0, 230);
            rect.sizeDelta = new Vector2(600, 200);

            VerticalLayoutGroup layout = container.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.padding = new RectOffset(10, 10, 10, 10);

            ContentSizeFitter fitter = container.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // 默认隐藏
            container.SetActive(false);

            return container;
        }

        /// <summary>
        /// 创建下一句指示器
        /// </summary>
        private static GameObject CreateNextIndicator(Transform parent)
        {
            GameObject indicator = new GameObject("NextIndicator");
            indicator.transform.SetParent(parent, false);

            RectTransform rect = indicator.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 0);
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(1, 0);
            rect.anchoredPosition = new Vector2(-80, 40);
            rect.sizeDelta = new Vector2(30, 30);

            TextMeshProUGUI text = indicator.AddComponent<TextMeshProUGUI>();
            text.text = "▼";
            text.fontSize = 24;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;

            // 默认隐藏
            indicator.SetActive(false);

            return indicator;
        }

        /// <summary>
        /// 创建选项按钮预制体
        /// </summary>
        private static void CreateChoiceButtonPrefab()
        {
            GameObject button = new GameObject("ChoiceButton");

            RectTransform rect = button.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(500, 50);

            Image image = button.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.3f, 0.9f);

            Button btn = button.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.normalColor = new Color(0.2f, 0.2f, 0.3f, 0.9f);
            colors.highlightedColor = new Color(0.3f, 0.3f, 0.5f, 1f);
            colors.pressedColor = new Color(0.4f, 0.4f, 0.6f, 1f);
            btn.colors = colors;

            // 按钮文字
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(button.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.offsetMin = new Vector2(20, 5);
            textRect.offsetMax = new Vector2(-20, -5);

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "选项";
            text.fontSize = 20;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;

            LayoutElement layout = button.AddComponent<LayoutElement>();
            layout.preferredHeight = 50;

            // 保存预制体
            string prefabPath = "Assets/Prefabs/Dialogue/ChoiceButton.prefab";
            PrefabUtility.SaveAsPrefabAsset(button, prefabPath);

            Object.DestroyImmediate(button);
        }

        /// <summary>
        /// 设置DialogueUI的引用
        /// </summary>
        private static void SetupDialogueUIReferences(DialogueUI dialogueUI, GameObject dialoguePanel, GameObject blockingPanel)
        {
            SerializedObject so = new SerializedObject(dialogueUI);

            // 主面板
            so.FindProperty("dialoguePanel").objectReferenceValue = dialoguePanel;
            so.FindProperty("blockingPanel").objectReferenceValue = blockingPanel.GetComponent<Image>();

            // 人物立绘
            Transform leftChar = dialoguePanel.transform.Find("LeftCharacter");
            Transform rightChar = dialoguePanel.transform.Find("RightCharacter");
            if (leftChar != null)
                so.FindProperty("leftCharacterImage").objectReferenceValue = leftChar.GetComponent<Image>();
            if (rightChar != null)
                so.FindProperty("rightCharacterImage").objectReferenceValue = rightChar.GetComponent<Image>();

            // 对话框
            Transform dialogueBox = dialoguePanel.transform.Find("DialogueBox");
            if (dialogueBox != null)
            {
                so.FindProperty("dialogueBox").objectReferenceValue = dialogueBox.GetComponent<Image>();
                
                Transform speakerName = dialogueBox.Find("SpeakerName");
                Transform dialogueText = dialogueBox.Find("DialogueText");
                
                if (speakerName != null)
                    so.FindProperty("speakerNameText").objectReferenceValue = speakerName.GetComponent<TextMeshProUGUI>();
                if (dialogueText != null)
                    so.FindProperty("dialogueText").objectReferenceValue = dialogueText.GetComponent<TextMeshProUGUI>();
            }

            // 指示器
            Transform nextIndicator = dialoguePanel.transform.Find("NextIndicator");
            if (nextIndicator != null)
                so.FindProperty("nextIndicator").objectReferenceValue = nextIndicator.gameObject;

            // 选项容器
            Transform choicesContainer = dialoguePanel.transform.Find("ChoicesContainer");
            if (choicesContainer != null)
                so.FindProperty("choicesContainer").objectReferenceValue = choicesContainer;

            // 加载选项按钮预制体
            GameObject choiceButtonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Dialogue/ChoiceButton.prefab");
            if (choiceButtonPrefab != null)
                so.FindProperty("choiceButtonPrefab").objectReferenceValue = choiceButtonPrefab;

            so.ApplyModifiedProperties();
        }

        [MenuItem("Tools/汪汪看/创建示例对话配置")]
        public static void CreateSampleDialogueConfig()
        {
            CreateSampleDialogueJSON();
        }

        /// <summary>
        /// 创建示例对话JSON
        /// </summary>
        private static void CreateSampleDialogueJSON()
        {
            string json = @"{
    ""dialogueId"": ""sample_dialogue_001"",
    ""title"": ""示例对话 - 咖啡店邂逅"",
    ""defaultTypewriterEffect"": true,
    ""defaultTypewriterSpeed"": 0.05,
    ""defaultCanSkipTypewriter"": true,
    ""backgroundImage"": """",
    ""bgmName"": """",
    ""dialogues"": [
        {
            ""id"": 1,
            ""speakerName"": ""丁曼大师"",
            ""content"": ""所以，为了防止店里的咖啡豆遭遇令人叹息的命运，我得好好地研磨它们..."",
            ""position"": ""right"",
            ""leftCharacterImage"": ""girl_police"",
            ""rightCharacterImage"": ""robot_barista"",
            ""highlightLeft"": false,
            ""highlightRight"": true,
            ""typewriterEffect"": true,
            ""typewriterSpeed"": 0.04,
            ""canSkipTypewriter"": true,
            ""autoPlay"": false,
            ""autoPlayDelay"": 0,
            ""triggerEvent"": """",
            ""choices"": []
        },
        {
            ""id"": 2,
            ""speakerName"": ""安比"",
            ""content"": ""参与活动一事，请恕我拒绝，失陪了。"",
            ""position"": ""left"",
            ""leftCharacterImage"": ""girl_police"",
            ""rightCharacterImage"": ""robot_barista"",
            ""highlightLeft"": true,
            ""highlightRight"": false,
            ""typewriterEffect"": true,
            ""typewriterSpeed"": 0.04,
            ""canSkipTypewriter"": true,
            ""autoPlay"": false,
            ""autoPlayDelay"": 0,
            ""triggerEvent"": """",
            ""choices"": []
        },
        {
            ""id"": 3,
            ""speakerName"": ""丁曼大师"",
            ""content"": ""等等！你还没有尝试过我的特调咖啡呢！"",
            ""position"": ""right"",
            ""leftCharacterImage"": ""girl_police"",
            ""rightCharacterImage"": ""robot_barista"",
            ""highlightLeft"": false,
            ""highlightRight"": true,
            ""typewriterEffect"": true,
            ""typewriterSpeed"": 0.04,
            ""canSkipTypewriter"": true,
            ""autoPlay"": false,
            ""autoPlayDelay"": 0,
            ""triggerEvent"": """",
            ""choices"": [
                {
                    ""text"": ""好吧，来一杯试试"",
                    ""nextDialogueId"": 4,
                    ""triggerEvent"": ""accept_coffee"",
                    ""moralityChange"": 5
                },
                {
                    ""text"": ""我真的没时间了"",
                    ""nextDialogueId"": 5,
                    ""triggerEvent"": ""reject_coffee"",
                    ""moralityChange"": -5
                }
            ]
        },
        {
            ""id"": 4,
            ""speakerName"": ""安比"",
            ""content"": ""...好吧，就一杯。"",
            ""position"": ""left"",
            ""leftCharacterImage"": ""girl_police"",
            ""rightCharacterImage"": ""robot_barista"",
            ""highlightLeft"": true,
            ""highlightRight"": false,
            ""typewriterEffect"": true,
            ""typewriterSpeed"": 0.04,
            ""canSkipTypewriter"": true,
            ""autoPlay"": false,
            ""autoPlayDelay"": 0,
            ""triggerEvent"": """",
            ""choices"": []
        },
        {
            ""id"": 5,
            ""speakerName"": ""安比"",
            ""content"": ""抱歉，下次再说吧。"",
            ""position"": ""left"",
            ""leftCharacterImage"": ""girl_police"",
            ""rightCharacterImage"": ""robot_barista"",
            ""highlightLeft"": true,
            ""highlightRight"": false,
            ""typewriterEffect"": true,
            ""typewriterSpeed"": 0.04,
            ""canSkipTypewriter"": true,
            ""autoPlay"": false,
            ""autoPlayDelay"": 0,
            ""triggerEvent"": """",
            ""choices"": []
        }
    ],
    ""onCompleteEvent"": ""dialogue_complete""
}";

            // 确保文件夹存在
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Dialogues"))
                AssetDatabase.CreateFolder("Assets/Resources", "Dialogues");

            // 写入文件
            string path = "Assets/Resources/Dialogues/sample_dialogue.json";
            System.IO.File.WriteAllText(path, json);
            AssetDatabase.Refresh();

            Debug.Log($"✅ 示例对话配置已创建: {path}");
            Debug.Log("📋 JSON配置说明：");
            Debug.Log("  - dialogueId: 对话唯一标识");
            Debug.Log("  - speakerName: 说话者名称");
            Debug.Log("  - content: 对话内容");
            Debug.Log("  - position: left/right 说话者位置");
            Debug.Log("  - leftCharacterImage/rightCharacterImage: 人物立绘资源名");
            Debug.Log("  - typewriterEffect: 是否逐字显示");
            Debug.Log("  - typewriterSpeed: 逐字速度（秒/字）");
            Debug.Log("  - choices: 对话选项（可选）");
        }
    }
}

