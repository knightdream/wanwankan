using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WanWanKan.Map;

namespace WanWanKan.UI
{
    /// <summary>
    /// 地图UI - 显示地图布局和房间信息
    /// </summary>
    public class MapUI : MonoBehaviour
    {
        [Header("地图面板")]
        [SerializeField] private GameObject mapPanel;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button openMapButton;
        [SerializeField] private TextMeshProUGUI floorText;

        [Header("地图容器")]
        [SerializeField] private RectTransform mapContainer;
        [SerializeField] private GameObject roomNodePrefab;
        [SerializeField] private GameObject connectionLinePrefab;

        [Header("房间节点设置")]
        [SerializeField] private float roomNodeSize = 60f;
        [SerializeField] private float roomSpacing = 120f;

        [Header("房间状态颜色")]
        [SerializeField] private Color startRoomColor = new Color(0.2f, 0.8f, 0.2f);      // 绿色
        [SerializeField] private Color currentRoomColor = new Color(1f, 0.8f, 0f);        // 金色
        [SerializeField] private Color visitedRoomColor = new Color(0.5f, 0.5f, 0.5f);    // 灰色
        [SerializeField] private Color unvisitedRoomColor = new Color(0.3f, 0.3f, 0.8f);  // 蓝色
        [SerializeField] private Color bossRoomColor = new Color(0.8f, 0.2f, 0.2f);       // 红色
        [SerializeField] private Color accessibleRoomColor = new Color(1f, 1f, 0.5f);     // 浅黄色
        
        [Header("房间类型颜色")]
        [SerializeField] private Color shopRoomColor = new Color(0.2f, 0.7f, 0.9f);       // 青色
        [SerializeField] private Color treasureRoomColor = new Color(1f, 0.85f, 0.3f);    // 金黄色
        [SerializeField] private Color restRoomColor = new Color(0.6f, 0.9f, 0.6f);       // 浅绿色
        [SerializeField] private Color eventRoomColor = new Color(0.7f, 0.5f, 0.9f);      // 紫色
        [SerializeField] private Color sacrificeRoomColor = new Color(0.6f, 0.1f, 0.3f);  // 暗红色
        [SerializeField] private Color secretRoomColor = new Color(0.9f, 0.9f, 0.9f);     // 白色

        [Header("快捷键")]
        [SerializeField] private KeyCode toggleKey = KeyCode.M;

        // 房间节点字典
        private Dictionary<int, GameObject> roomNodeDict = new Dictionary<int, GameObject>();
        private Dictionary<int, RectTransform> roomNodePositions = new Dictionary<int, RectTransform>();
        private List<GameObject> connectionLines = new List<GameObject>();

        private MapManager mapManager;
        private bool isMapOpen = false;

        private void Awake()
        {
            // 初始隐藏地图面板
            if (mapPanel != null)
            {
                mapPanel.SetActive(false);
            }

            // 设置关闭按钮
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseMap);
            }

            // 设置打开地图按钮
            if (openMapButton != null)
            {
                openMapButton.onClick.AddListener(OpenMap);
            }
        }

        private void Start()
        {
            mapManager = MapManager.Instance;
            
            // 确保所有文本组件使用中文字体
            FontFixer.FixFontsInGameObject(gameObject);
            
            if (mapManager != null)
            {
                // 订阅地图事件
                mapManager.OnMapGenerated += OnMapGenerated;
                mapManager.OnRoomEntered += OnRoomEntered;
                mapManager.OnRoomCompleted += OnRoomCompleted;
            }
        }

        private void Update()
        {
            // 快捷键打开/关闭地图
            if (Input.GetKeyDown(toggleKey))
            {
                ToggleMap();
            }
        }

        /// <summary>
        /// 打开地图
        /// </summary>
        public void OpenMap()
        {
            if (mapPanel != null)
            {
                mapPanel.SetActive(true);
                isMapOpen = true;
                RefreshMap();
            }
            
            // 隐藏打开地图按钮
            if (openMapButton != null)
            {
                openMapButton.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 关闭地图
        /// </summary>
        public void CloseMap()
        {
            if (mapPanel != null)
            {
                mapPanel.SetActive(false);
                isMapOpen = false;
            }
            
            // 显示打开地图按钮
            if (openMapButton != null)
            {
                openMapButton.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// 切换地图显示状态
        /// </summary>
        public void ToggleMap()
        {
            if (isMapOpen)
            {
                CloseMap();
            }
            else
            {
                OpenMap();
            }
        }

        /// <summary>
        /// 刷新地图显示
        /// </summary>
        private void RefreshMap()
        {
            if (mapManager == null || mapManager.CurrentMap == null)
            {
                Debug.LogWarning("[MapUI] 没有可显示的地图");
                return;
            }

            MapLayout map = mapManager.CurrentMap;

            // 更新楼层文本
            if (floorText != null)
            {
                // 先设置字体，确保fallback配置正确
                FontFixer.SetChineseFont(floorText);
                // 然后设置文本内容
                floorText.text = $"第 {mapManager.CurrentFloor} 层";
            }

            // 清理旧节点和连线
            ClearMap();

            // 创建房间节点
            CreateRoomNodes(map);

            // 创建连接线
            CreateConnectionLines(map);

            // 更新房间状态
            UpdateRoomStates(map);
        }

        /// <summary>
        /// 创建房间节点
        /// </summary>
        private void CreateRoomNodes(MapLayout map)
        {
            if (roomNodePrefab == null || mapContainer == null)
            {
                Debug.LogError("[MapUI] 房间节点预制体或容器未设置");
                return;
            }

            // 计算地图边界以便居中
            Vector2Int minPos = new Vector2Int(int.MaxValue, int.MaxValue);
            Vector2Int maxPos = new Vector2Int(int.MinValue, int.MinValue);
            
            foreach (Room room in map.GetAllRooms())
            {
                minPos.x = Mathf.Min(minPos.x, room.Position.x);
                minPos.y = Mathf.Min(minPos.y, room.Position.y);
                maxPos.x = Mathf.Max(maxPos.x, room.Position.x);
                maxPos.y = Mathf.Max(maxPos.y, room.Position.y);
            }
            
            // 计算地图中心点（让地图以(0,0)为中心）
            float centerX = (minPos.x + maxPos.x) / 2f;
            float centerY = (minPos.y + maxPos.y) / 2f;
            
            // 计算地图实际大小并调整Content大小
            float mapWidth = (maxPos.x - minPos.x + 1) * roomSpacing + roomNodeSize * 2;
            float mapHeight = (maxPos.y - minPos.y + 1) * roomSpacing + roomNodeSize * 2;
            
            // 设置Content大小（添加边距）
            float padding = 100f;
            mapContainer.sizeDelta = new Vector2(
                Mathf.Max(mapWidth + padding, 800f),
                Mathf.Max(mapHeight + padding, 400f)
            );

            foreach (Room room in map.GetAllRooms())
            {
                // 创建房间节点
                GameObject nodeObj = Instantiate(roomNodePrefab, mapContainer);
                RectTransform nodeRect = nodeObj.GetComponent<RectTransform>();
                
                // 设置位置（相对于地图中心）
                Vector2 position = new Vector2(
                    (room.Position.x - centerX) * roomSpacing,
                    -(room.Position.y - centerY) * roomSpacing
                );
                nodeRect.anchoredPosition = position;
                nodeRect.sizeDelta = new Vector2(roomNodeSize, roomNodeSize);

                // 设置房间信息
                RoomNodeUI nodeUI = nodeObj.GetComponent<RoomNodeUI>();
                if (nodeUI != null)
                {
                    nodeUI.SetRoom(room);
                }

                // 存储节点引用
                roomNodeDict[room.Id] = nodeObj;
                roomNodePositions[room.Id] = nodeRect;
            }
        }

        /// <summary>
        /// 创建连接线
        /// </summary>
        private void CreateConnectionLines(MapLayout map)
        {
            if (connectionLinePrefab == null || mapContainer == null)
            {
                return;
            }

            foreach (Room room in map.GetAllRooms())
            {
                foreach (int connectedId in room.ConnectedRoomIds)
                {
                    // 避免重复创建连线（每个连接只创建一次）
                    if (room.Id < connectedId && roomNodePositions.ContainsKey(connectedId))
                    {
                        CreateLine(roomNodePositions[room.Id], roomNodePositions[connectedId]);
                    }
                }
            }
        }

        /// <summary>
        /// 创建一条连接线
        /// </summary>
        private void CreateLine(RectTransform from, RectTransform to)
        {
            GameObject lineObj = Instantiate(connectionLinePrefab, mapContainer);
            RectTransform lineRect = lineObj.GetComponent<RectTransform>();

            // 计算位置和角度
            Vector2 fromPos = from.anchoredPosition;
            Vector2 toPos = to.anchoredPosition;
            Vector2 direction = toPos - fromPos;
            float distance = direction.magnitude;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // 设置位置（两点中点）
            lineRect.anchoredPosition = (fromPos + toPos) / 2f;
            lineRect.sizeDelta = new Vector2(distance, 2f); // 宽度2像素
            lineRect.localRotation = Quaternion.Euler(0, 0, angle);

            // 设置颜色（浅灰色）
            Image lineImage = lineObj.GetComponent<Image>();
            if (lineImage != null)
            {
                lineImage.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            }

            connectionLines.Add(lineObj);
        }

        /// <summary>
        /// 更新房间状态（颜色、高亮等）
        /// </summary>
        private void UpdateRoomStates(MapLayout map)
        {
            Room currentRoom = map.GetCurrentRoom();
            List<Room> accessibleRooms = mapManager.GetAccessibleRooms();

            foreach (var kvp in roomNodeDict)
            {
                int roomId = kvp.Key;
                GameObject nodeObj = kvp.Value;
                Room room = map.GetRoom(roomId);

                if (room == null) continue;

                RoomNodeUI nodeUI = nodeObj.GetComponent<RoomNodeUI>();
                if (nodeUI != null)
                {
                    // 确定房间颜色
                    Color roomColor = GetRoomColor(room, currentRoom, accessibleRooms);
                    nodeUI.SetColor(roomColor);

                    // 设置高亮（当前房间）
                    bool isHighlighted = (currentRoom != null && room.Id == currentRoom.Id);
                    nodeUI.SetHighlighted(isHighlighted);
                }
            }
        }

        /// <summary>
        /// 获取房间颜色
        /// </summary>
        private Color GetRoomColor(Room room, Room currentRoom, List<Room> accessibleRooms)
        {
            // 当前房间优先显示金色
            if (currentRoom != null && room.Id == currentRoom.Id)
            {
                return currentRoomColor;
            }
            
            // 可访问的房间显示浅黄色
            if (accessibleRooms != null && accessibleRooms.Exists(r => r.Id == room.Id))
            {
                return accessibleRoomColor;
            }
            
            // 已访问的房间显示灰色
            if (room.IsVisited)
            {
                return visitedRoomColor;
            }
            
            // 未访问的房间根据类型显示不同颜色
            switch (room.Type)
            {
                case RoomType.Start:
                    return startRoomColor;
                case RoomType.Boss:
                    return bossRoomColor;
                case RoomType.Shop:
                    return shopRoomColor;
                case RoomType.Treasure:
                    return treasureRoomColor;
                case RoomType.Rest:
                    return restRoomColor;
                case RoomType.Event:
                    return eventRoomColor;
                case RoomType.Sacrifice:
                    return sacrificeRoomColor;
                case RoomType.Secret:
                    return secretRoomColor;
                case RoomType.Battle:
                default:
                    return unvisitedRoomColor;
            }
        }

        /// <summary>
        /// 清理地图显示
        /// </summary>
        private void ClearMap()
        {
            // 清理房间节点
            foreach (var nodeObj in roomNodeDict.Values)
            {
                if (nodeObj != null)
                {
                    Destroy(nodeObj);
                }
            }
            roomNodeDict.Clear();
            roomNodePositions.Clear();

            // 清理连接线
            foreach (var lineObj in connectionLines)
            {
                if (lineObj != null)
                {
                    Destroy(lineObj);
                }
            }
            connectionLines.Clear();
        }

        #region 事件回调

        private void OnMapGenerated(MapLayout map)
        {
            if (isMapOpen)
            {
                RefreshMap();
            }
        }

        private void OnRoomEntered(Room room)
        {
            if (isMapOpen)
            {
                RefreshMap();
            }
        }

        private void OnRoomCompleted(Room room)
        {
            if (isMapOpen)
            {
                RefreshMap();
            }
        }

        #endregion

        private void OnDestroy()
        {
            if (mapManager != null)
            {
                mapManager.OnMapGenerated -= OnMapGenerated;
                mapManager.OnRoomEntered -= OnRoomEntered;
                mapManager.OnRoomCompleted -= OnRoomCompleted;
            }
        }
    }
}

