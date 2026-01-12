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
            so.ApplyModifiedProperties();

            // 保存预制体
            SavePrefab(mapUI, "Assets/Prefabs/UI/MapUI.prefab");
            SavePrefab(roomNodePrefab, "Assets/Prefabs/UI/RoomNode.prefab");
            SavePrefab(connectionLinePrefab, "Assets/Prefabs/UI/ConnectionLine.prefab");

            Debug.Log("✅ 地图UI创建完成！预制体已保存到 Assets/Prefabs/UI/");
            Selection.activeGameObject = mapUI;
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

            // 地图容器（可滚动）
            GameObject mapContainer = new GameObject("MapContainer");
            mapContainer.transform.SetParent(panel.transform, false);
            RectTransform mapContainerRect = mapContainer.AddComponent<RectTransform>();
            mapContainerRect.anchorMin = new Vector2(0, 0);
            mapContainerRect.anchorMax = new Vector2(1, 1);
            mapContainerRect.offsetMin = new Vector2(20, 80);
            mapContainerRect.offsetMax = new Vector2(-20, -20);

            // 添加ScrollRect
            ScrollRect scrollRect = mapContainer.AddComponent<ScrollRect>();
            scrollRect.horizontal = true;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;

            // 内容区域
            GameObject content = new GameObject("Content");
            content.transform.SetParent(mapContainer.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.sizeDelta = new Vector2(1000, 1000);
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(0, 1);
            contentRect.pivot = new Vector2(0, 1);

            Image contentBg = content.AddComponent<Image>();
            contentBg.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);

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

            // 添加RoomNodeUI组件
            var roomNodeUI = node.AddComponent<UI.RoomNodeUI>();
            SerializedObject so = new SerializedObject(roomNodeUI);
            so.FindProperty("roomIcon").objectReferenceValue = iconImage;
            so.FindProperty("roomBackground").objectReferenceValue = bgImage;
            so.FindProperty("roomNameText").objectReferenceValue = nameText;
            so.FindProperty("highlightEffect").objectReferenceValue = highlightObj;
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

