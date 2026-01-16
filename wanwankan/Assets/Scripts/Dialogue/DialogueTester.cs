using UnityEngine;

namespace Dialogue
{
    /// <summary>
    /// 对话系统测试脚本
    /// 用于测试对话功能，正式版可删除
    /// </summary>
    public class DialogueTester : MonoBehaviour
    {
        [Header("测试配置")]
        [SerializeField] private string testDialogueName = "test_all_features";
        [SerializeField] private KeyCode startDialogueKey = KeyCode.T;

        private void Update()
        {
            // 按T键开始测试对话
            if (Input.GetKeyDown(startDialogueKey))
            {
                StartTestDialogue();
            }
        }

        /// <summary>
        /// 开始测试对话
        /// </summary>
        public void StartTestDialogue()
        {
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartDialogue(testDialogueName);
            }
            else
            {
                Debug.LogError("[DialogueTester] DialogueManager实例不存在！请先创建对话系统UI。");
            }
        }

        /// <summary>
        /// 通过代码直接创建对话（不使用JSON）
        /// </summary>
        public void StartCodeDialogue()
        {
            DialogueConfig config = new DialogueConfig
            {
                dialogueId = "code_dialogue",
                title = "代码创建的对话",
                defaultTypewriterEffect = true,
                defaultTypewriterSpeed = 0.05f,
                dialogues = new System.Collections.Generic.List<DialogueLine>
                {
                    new DialogueLine
                    {
                        id = 1,
                        speakerName = "系统",
                        content = "这是通过代码直接创建的对话。",
                        position = "left",
                        typewriterEffect = true,
                        typewriterSpeed = 0.05f,
                        canSkipTypewriter = true
                    },
                    new DialogueLine
                    {
                        id = 2,
                        speakerName = "系统",
                        content = "你可以在代码中动态创建对话内容。",
                        position = "left",
                        typewriterEffect = true,
                        typewriterSpeed = 0.05f,
                        canSkipTypewriter = true
                    }
                }
            };

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartDialogue(config);
            }
        }
    }
}

