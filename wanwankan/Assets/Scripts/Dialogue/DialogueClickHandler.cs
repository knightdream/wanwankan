using UnityEngine;
using UnityEngine.EventSystems;

namespace Dialogue
{
    /// <summary>
    /// 对话点击处理器
    /// 挂载到全屏遮罩上，用于接收全屏点击事件
    /// </summary>
    public class DialogueClickHandler : MonoBehaviour, IPointerClickHandler
    {
        /// <summary>
        /// 点击事件处理
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            // 调用DialogueManager处理点击
            if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
            {
                DialogueManager.Instance.OnClick();
            }
        }
    }
}

