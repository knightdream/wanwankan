#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace WanWanKan.Editor
{
    /// <summary>
    /// 地图UI创建工具 - 在编辑器中一键生成地图UI预制体
    /// </summary>
    public class MapUICreator : EditorWindow
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

        [MenuItem("WanWanKan/创建地图UI")]
        public static void ShowWindow()
        {
            GetWindow<MapUICreator>("地图UI创建器");
        }

        private void OnGUI()
        {
            GUILayout.Label("地图UI创建工具", EditorStyles.boldLabel);
            GUILayout.Space(10);

            if (GUILayout.Button("创建完整地图UI", GUILayout.Height(40)))
            {
                CreateCompleteMapUI();
            }

            GUILayout.Space(10);
            GUILayout.Label("单独创建组件:", EditorStyles.boldLabel);

            if (GUILayout.Button("创建地图面板"))
            {
                CreateMapPanel();
            }

            if (GUILayout.Button("创建房间节点预制体"))
            {
                CreateRoomNodePrefab();
            }

            if (GUILayout.Button("创建连接线预制体"))
            {
                CreateConnectionLinePrefab();
            }
        }

        /// <summary>
        /// 创建完整的地图UI系统
        /// </summary>
        [MenuItem("WanWanKan/一键创建完整地图UI")]
        public static void CreateCompleteMapUI()
        {
            // 查找或创建Canvas
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGO = new GameObject("MapCanvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                canvasGO.AddComponent<GraphicRaycaster>();
                
                // 创建EventSystem
                if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
                {
                    GameObject eventSystem = new GameObject("EventSystem");
                    eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                    eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                }
            }

            // 创建地图UI根对象
            GameObject mapUI = new GameObject("MapUI");
            mapUI.transform.SetParent(canvas.transform, false);
            RectTransform mapUIRect = mapUI.AddComponent<RectTransform>();
            mapUIRect.anchorMin = Vector2.zero;
            mapUIRect.anchorMax = Vector2.one;
            mapUIRect.offsetMin = Vector2.zero;
            mapUIRect.offsetMax = Vector2.zero;

            // 添加MapUI组件
            var mapUIScript = mapUI.AddComponent<UI.MapUI>();

            // 创建地图面板
            GameObject mapPanel = CreateMapPanelInternal(mapUI.transform);
            
            // 创建"打开地图"按钮
            GameObject openMapButton = CreateOpenMapButtonInternal(mapUI.transform);
            
            // 创建房间节点预制体
            GameObject roomNodePrefab = CreateRoomNodePrefabInternal();
            
            // 创建连接线预制体
            GameObject connectionLinePrefab = CreateConnectionLinePrefabInternal();

            // 设置MapUI的引用
            SerializedObject so = new SerializedObject(mapUIScript);
            so.FindProperty("mapPanel").objectReferenceValue = mapPanel;
            so.FindProperty("closeButton").objectReferenceValue = mapPanel.transform.Find("Header/CloseButton")?.GetComponent<Button>();
            so.FindProperty("floorText").objectReferenceValue = mapPanel.transform.Find("Header/FloorText")?.GetComponent<TextMeshProUGUI>();
            // mapContainer应该指向Content（实际绘制区域），而不是ScrollRect
            so.FindProperty("mapContainer").objectReferenceValue = mapPanel.transform.Find("MapContainer/Content")?.GetComponent<RectTransform>();
            so.FindProperty("roomNodePrefab").objectReferenceValue = roomNodePrefab;
            so.FindProperty("connectionLinePrefab").objectReferenceValue = connectionLinePrefab;
            so.FindProperty("openMapButton").objectReferenceValue = openMapButton.GetComponent<Button>();
            so.ApplyModifiedProperties();

            // 保存预制体
            SavePrefab(mapUI, "Assets/Prefabs/UI/MapUI.prefab");
            SavePrefab(roomNodePrefab, "Assets/Prefabs/UI/RoomNode.prefab");
            SavePrefab(connectionLinePrefab, "Assets/Prefabs/UI/ConnectionLine.prefab");

            Debug.Log("✅ 地图UI创建完成！预制体已保存到 Assets/Prefabs/UI/");
            Debug.Log("📍 已创建[打开地图]按钮在右上角");
            Selection.activeGameObject = mapUI;
        }

        /// <summary>
        /// 创建"打开地图"按钮
        /// </summary>
        private static GameObject CreateOpenMapButtonInternal(Transform parent)
        {
            // 按钮容器
            GameObject buttonObj = new GameObject("OpenMapButton");
            buttonObj.transform.SetParent(parent, false);
            RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
            
            // 放在右上角
            buttonRect.anchorMin = new Vector2(1, 1);
            buttonRect.anchorMax = new Vector2(1, 1);
            buttonRect.pivot = new Vector2(1, 1);
            buttonRect.anchoredPosition = new Vector2(-20, -20);
            buttonRect.sizeDelta = new Vector2(120, 50);

            // 按钮背景
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.4f, 0.6f, 0.9f);

            // 添加Button组件
            Button button = buttonObj.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.2f, 0.4f, 0.6f, 0.9f);
            colors.highlightedColor = new Color(0.3f, 0.5f, 0.7f, 1f);
            colors.pressedColor = new Color(0.15f, 0.3f, 0.5f, 1f);
            button.colors = colors;

            // 按钮文本
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
            SetChineseFont(buttonText);
            buttonText.text = "🗺️ 地图";
            buttonText.fontSize = 20;
            buttonText.color = Color.white;
            buttonText.alignment = TextAlignmentOptions.Center;

            return buttonObj;
        }

        /// <summary>
        /// 创建地图面板
        /// </summary>
        public static void CreateMapPanel()
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("请先创建Canvas！");
                return;
            }

            GameObject mapPanel = CreateMapPanelInternal(canvas.transform);
            SavePrefab(mapPanel, "Assets/Prefabs/UI/MapPanel.prefab");
            Debug.Log("✅ 地图面板已创建！");
        }

        private static GameObject CreateMapPanelInternal(Transform parent)
        {
            // 主面板
            GameObject panel = new GameObject("MapPanel");
            panel.transform.SetParent(parent, false);
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // 背景
            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.8f);

            // 标题栏
            GameObject header = new GameObject("Header");
            header.transform.SetParent(panel.transform, false);
            RectTransform headerRect = header.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.offsetMin = new Vector2(0, -60);
            headerRect.offsetMax = new Vector2(0, 0);

            HorizontalLayoutGroup headerLayout = header.AddComponent<HorizontalLayoutGroup>();
            headerLayout.padding = new RectOffset(20, 20, 10, 10);
            headerLayout.childForceExpandWidth = false;
            headerLayout.childForceExpandHeight = true;

            // 标题文本
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(header.transform, false);
            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            SetChineseFont(titleText);
            titleText.text = "地图";
            titleText.fontSize = 24;
            titleText.color = Color.white;
            titleText.alignment = TextAlignmentOptions.Left;
            LayoutElement titleLayout = titleObj.AddComponent<LayoutElement>();
            titleLayout.flexibleWidth = 1;

            // 楼层文本
            GameObject floorObj = new GameObject("FloorText");
            floorObj.transform.SetParent(header.transform, false);
            TextMeshProUGUI floorText = floorObj.AddComponent<TextMeshProUGUI>();
            SetChineseFont(floorText);
            floorText.text = "第 1 层";
            floorText.fontSize = 20;
            floorText.color = Color.white;
            floorText.alignment = TextAlignmentOptions.Center;
            LayoutElement floorLayout = floorObj.AddComponent<LayoutElement>();
            floorLayout.minWidth = 150;

            // 关闭按钮
            GameObject closeBtn = new GameObject("CloseButton");
            closeBtn.transform.SetParent(header.transform, false);
            RectTransform closeBtnRect = closeBtn.AddComponent<RectTransform>();
            closeBtnRect.sizeDelta = new Vector2(40, 40);
            Button closeButton = closeBtn.AddComponent<Button>();
            Image closeBtnImage = closeBtn.AddComponent<Image>();
            closeBtnImage.color = new Color(0.8f, 0.2f, 0.2f);

            // 关闭按钮文本
            GameObject closeTextObj = new GameObject("Text");
            closeTextObj.transform.SetParent(closeBtn.transform, false);
            RectTransform closeTextRect = closeTextObj.AddComponent<RectTransform>();
            closeTextRect.anchorMin = Vector2.zero;
            closeTextRect.anchorMax = Vector2.one;
            closeTextRect.offsetMin = Vector2.zero;
            closeTextRect.offsetMax = Vector2.zero;
            TextMeshProUGUI closeText = closeTextObj.AddComponent<TextMeshProUGUI>();
            SetChineseFont(closeText);
            closeText.text = "×";
            closeText.fontSize = 30;
            closeText.color = Color.white;
            closeText.alignment = TextAlignmentOptions.Center;

            // 地图容器（可滚动 + 可拖动）
            GameObject mapContainer = new GameObject("MapContainer");
            mapContainer.transform.SetParent(panel.transform, false);
            RectTransform mapContainerRect = mapContainer.AddComponent<RectTransform>();
            mapContainerRect.anchorMin = new Vector2(0, 0);
            mapContainerRect.anchorMax = new Vector2(1, 1);
            mapContainerRect.offsetMin = new Vector2(20, 20);
            mapContainerRect.offsetMax = new Vector2(-20, -70);

            // 添加背景图片（RectMask2D需要）
            Image containerBg = mapContainer.AddComponent<Image>();
            containerBg.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);

            // 使用RectMask2D代替Mask（性能更好，不需要额外的Image）
            mapContainer.AddComponent<UnityEngine.UI.RectMask2D>();

            // 内容区域（直接放在MapContainer内）
            GameObject content = new GameObject("Content");
            content.transform.SetParent(mapContainer.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            // 初始大小，会根据地图动态调整
            contentRect.sizeDelta = new Vector2(1200, 600);
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.anchoredPosition = Vector2.zero;

            // 添加ScrollRect到容器
            ScrollRect scrollRect = mapContainer.AddComponent<ScrollRect>();
            scrollRect.horizontal = true;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.elasticity = 0.1f;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.135f;
            scrollRect.scrollSensitivity = 10f;
            scrollRect.content = contentRect;

            return panel;
        }

        /// <summary>
        /// 创建房间节点预制体
        /// </summary>
        public static void CreateRoomNodePrefab()
        {
            GameObject prefab = CreateRoomNodePrefabInternal();
            SavePrefab(prefab, "Assets/Prefabs/UI/RoomNode.prefab");
            Debug.Log("✅ 房间节点预制体已创建！");
        }

        private static GameObject CreateRoomNodePrefabInternal()
        {
            GameObject node = new GameObject("RoomNode");
            RectTransform nodeRect = node.AddComponent<RectTransform>();
            nodeRect.sizeDelta = new Vector2(60, 60);

            // 背景
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(node.transform, false);
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.3f, 0.3f, 0.8f);

            // 高亮效果
            GameObject highlightObj = new GameObject("Highlight");
            highlightObj.transform.SetParent(node.transform, false);
            RectTransform highlightRect = highlightObj.AddComponent<RectTransform>();
            highlightRect.anchorMin = Vector2.zero;
            highlightRect.anchorMax = Vector2.one;
            highlightRect.offsetMin = new Vector2(-5, -5);
            highlightRect.offsetMax = new Vector2(5, 5);
            Image highlightImage = highlightObj.AddComponent<Image>();
            highlightImage.color = new Color(1f, 0.8f, 0f, 0.5f);
            highlightObj.SetActive(false);

            // 房间图标
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(node.transform, false);
            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(40, 40);
            iconRect.anchoredPosition = Vector2.zero;
            Image iconImage = iconObj.AddComponent<Image>();
            iconImage.color = Color.white;

            // 房间名称
            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(node.transform, false);
            RectTransform nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.5f, 0);
            nameRect.anchorMax = new Vector2(0.5f, 0);
            nameRect.sizeDelta = new Vector2(80, 20);
            nameRect.anchoredPosition = new Vector2(0, -35);
            TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
            SetChineseFont(nameText);
            nameText.text = "房间";
            nameText.fontSize = 12;
            nameText.color = Color.white;
            nameText.alignment = TextAlignmentOptions.Center;

            // 当前位置指示器（箭头）
            GameObject indicatorObj = new GameObject("CurrentIndicator");
            indicatorObj.transform.SetParent(node.transform, false);
            RectTransform indicatorRect = indicatorObj.AddComponent<RectTransform>();
            indicatorRect.anchorMin = new Vector2(0.5f, 1);
            indicatorRect.anchorMax = new Vector2(0.5f, 1);
            indicatorRect.sizeDelta = new Vector2(20, 20);
            indicatorRect.anchoredPosition = new Vector2(0, 15);
            Image indicatorImage = indicatorObj.AddComponent<Image>();
            indicatorImage.color = new Color(1f, 0.9f, 0.2f); // 金黄色
            indicatorObj.SetActive(false);

            // "你在这里"文字
            GameObject labelObj = new GameObject("CurrentLabel");
            labelObj.transform.SetParent(node.transform, false);
            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 1);
            labelRect.anchorMax = new Vector2(0.5f, 1);
            labelRect.sizeDelta = new Vector2(80, 25);
            labelRect.anchoredPosition = new Vector2(0, 40);
            TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
            SetChineseFont(labelText);
            labelText.text = "▼ 你在这里";
            labelText.fontSize = 11;
            labelText.color = new Color(1f, 0.9f, 0.2f); // 金黄色
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.fontStyle = FontStyles.Bold;
            labelObj.SetActive(false);

            // 添加RoomNodeUI组件
            var roomNodeUI = node.AddComponent<UI.RoomNodeUI>();
            SerializedObject so = new SerializedObject(roomNodeUI);
            so.FindProperty("roomIcon").objectReferenceValue = iconImage;
            so.FindProperty("roomBackground").objectReferenceValue = bgImage;
            so.FindProperty("roomNameText").objectReferenceValue = nameText;
            so.FindProperty("highlightEffect").objectReferenceValue = highlightObj;
            so.FindProperty("currentIndicator").objectReferenceValue = indicatorObj;
            so.FindProperty("currentLabel").objectReferenceValue = labelText;
            so.ApplyModifiedProperties();

            return node;
        }

        /// <summary>
        /// 创建连接线预制体
        /// </summary>
        public static void CreateConnectionLinePrefab()
        {
            GameObject prefab = CreateConnectionLinePrefabInternal();
            SavePrefab(prefab, "Assets/Prefabs/UI/ConnectionLine.prefab");
            Debug.Log("✅ 连接线预制体已创建！");
        }

        private static GameObject CreateConnectionLinePrefabInternal()
        {
            GameObject line = new GameObject("ConnectionLine");
            RectTransform lineRect = line.AddComponent<RectTransform>();
            lineRect.sizeDelta = new Vector2(100, 2);

            Image lineImage = line.AddComponent<Image>();
            lineImage.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);

            return line;
        }

        /// <summary>
        /// 保存预制体
        /// </summary>
        private static void SavePrefab(GameObject obj, string path)
        {
            // 确保目录存在
            string directory = System.IO.Path.GetDirectoryName(path);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            // 保存预制体
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obj, path);
            if (prefab != null)
            {
                Debug.Log($"✅ 预制体已保存: {path}");
            }
            else
            {
                Debug.LogError($"❌ 预制体保存失败: {path}");
            }
        }
    }
}
#endif

