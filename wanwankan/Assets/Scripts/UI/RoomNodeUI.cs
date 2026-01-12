using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WanWanKan.Map;

namespace WanWanKan.UI
{
    /// <summary>
    /// 房间节点UI组件 - 显示单个房间节点
    /// </summary>
    public class RoomNodeUI : MonoBehaviour
    {
        [Header("UI组件")]
        [SerializeField] private Image roomIcon;
        [SerializeField] private Image roomBackground;
        [SerializeField] private TextMeshProUGUI roomNameText;
        [SerializeField] private GameObject highlightEffect;

        [Header("房间图标")]
        [SerializeField] private Sprite startIcon;
        [SerializeField] private Sprite battleIcon;
        [SerializeField] private Sprite treasureIcon;
        [SerializeField] private Sprite eventIcon;
        [SerializeField] private Sprite shopIcon;
        [SerializeField] private Sprite restIcon;
        [SerializeField] private Sprite bossIcon;

        private Room currentRoom;

        /// <summary>
        /// 设置房间数据
        /// </summary>
        public void SetRoom(Room room)
        {
            currentRoom = room;
            UpdateRoomDisplay();
        }

        /// <summary>
        /// 设置房间颜色
        /// </summary>
        public void SetColor(Color color)
        {
            if (roomBackground != null)
            {
                roomBackground.color = color;
            }
        }

        /// <summary>
        /// 设置高亮状态
        /// </summary>
        public void SetHighlighted(bool highlighted)
        {
            if (highlightEffect != null)
            {
                highlightEffect.SetActive(highlighted);
            }
        }

        /// <summary>
        /// 更新房间显示
        /// </summary>
        private void UpdateRoomDisplay()
        {
            if (currentRoom == null) return;

            // 设置房间名称
            if (roomNameText != null)
            {
                roomNameText.text = GetRoomTypeName(currentRoom.Type);
            }

            // 设置房间图标
            if (roomIcon != null)
            {
                Sprite icon = GetRoomIcon(currentRoom.Type);
                if (icon != null)
                {
                    roomIcon.sprite = icon;
                }
            }
        }

        /// <summary>
        /// 获取房间类型名称
        /// </summary>
        private string GetRoomTypeName(RoomType type)
        {
            switch (type)
            {
                case RoomType.Start:
                    return "起点";
                case RoomType.Battle:
                    return "战斗";
                case RoomType.Treasure:
                    return "宝藏";
                case RoomType.Event:
                    return "事件";
                case RoomType.Shop:
                    return "商店";
                case RoomType.Rest:
                    return "休息";
                case RoomType.Boss:
                    return "BOSS";
                default:
                    return "未知";
            }
        }

        /// <summary>
        /// 获取房间图标
        /// </summary>
        private Sprite GetRoomIcon(RoomType type)
        {
            switch (type)
            {
                case RoomType.Start:
                    return startIcon;
                case RoomType.Battle:
                    return battleIcon;
                case RoomType.Treasure:
                    return treasureIcon;
                case RoomType.Event:
                    return eventIcon;
                case RoomType.Shop:
                    return shopIcon;
                case RoomType.Rest:
                    return restIcon;
                case RoomType.Boss:
                    return bossIcon;
                default:
                    return null;
            }
        }
    }
}

