using System;
using System.Collections;
using UnityEngine;

namespace Dialogue
{
    /// <summary>
    /// 对话管理器 - 单例模式
    /// 负责控制对话的播放、暂停、跳过等逻辑
    /// </summary>
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        [Header("引用")]
        [SerializeField] private DialogueUI dialogueUI;

        [Header("调试")]
        [SerializeField] private bool debugMode = true;

        // 当前对话配置
        private DialogueConfig currentConfig;
        
        // 当前对话索引
        private int currentLineIndex = 0;
        
        // 是否正在显示对话
        public bool IsDialogueActive { get; private set; }
        
        // 是否正在打字机效果中
        public bool IsTyping { get; private set; }

        // 事件
        public event Action<DialogueConfig> OnDialogueStarted;
        public event Action OnDialogueEnded;
        public event Action<DialogueLine> OnLineStarted;
        public event Action<DialogueLine> OnLineEnded;
        public event Action<DialogueChoice> OnChoiceSelected;

        private Coroutine typewriterCoroutine;

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

        /// <summary>
        /// 开始播放对话（通过文件名）
        /// </summary>
        /// <param name="dialogueName">对话文件名（Resources/Dialogues/下）</param>
        public void StartDialogue(string dialogueName)
        {
            DialogueConfig config = DialogueLoader.LoadDialogue(dialogueName);
            if (config != null)
            {
                StartDialogue(config);
            }
        }

        /// <summary>
        /// 开始播放对话（通过配置对象）
        /// </summary>
        public void StartDialogue(DialogueConfig config)
        {
            if (config == null || config.dialogues == null || config.dialogues.Count == 0)
            {
                Debug.LogWarning("[DialogueManager] 对话配置为空");
                return;
            }

            currentConfig = config;
            currentLineIndex = 0;
            IsDialogueActive = true;

            if (dialogueUI != null)
            {
                dialogueUI.Show();
            }

            OnDialogueStarted?.Invoke(config);

            if (debugMode)
                Debug.Log($"[DialogueManager] 开始对话: {config.title}");

            ShowCurrentLine();
        }

        /// <summary>
        /// 显示当前对话
        /// </summary>
        private void ShowCurrentLine()
        {
            if (currentConfig == null || currentLineIndex >= currentConfig.dialogues.Count)
            {
                EndDialogue();
                return;
            }

            DialogueLine line = currentConfig.dialogues[currentLineIndex];
            OnLineStarted?.Invoke(line);

            if (dialogueUI != null)
            {
                dialogueUI.ShowLine(line, OnTypewriterComplete);
            }

            if (debugMode)
                Debug.Log($"[DialogueManager] 显示对话 [{currentLineIndex + 1}/{currentConfig.dialogues.Count}]: {line.speakerName}: {line.content}");
        }

        /// <summary>
        /// 打字机效果完成回调
        /// </summary>
        private void OnTypewriterComplete()
        {
            IsTyping = false;

            DialogueLine line = currentConfig.dialogues[currentLineIndex];
            
            // 如果有选项，显示选项
            if (line.choices != null && line.choices.Count > 0)
            {
                if (dialogueUI != null)
                {
                    dialogueUI.ShowChoices(line.choices, OnChoiceClicked);
                }
            }
            // 如果设置了自动播放
            else if (line.autoPlay)
            {
                StartCoroutine(AutoPlayNextLine(line.autoPlayDelay));
            }
        }

        /// <summary>
        /// 自动播放下一句
        /// </summary>
        private IEnumerator AutoPlayNextLine(float delay)
        {
            yield return new WaitForSeconds(delay);
            NextLine();
        }

        /// <summary>
        /// 点击处理 - 由UI调用（全屏点击）
        /// </summary>
        public void OnClick()
        {
            if (!IsDialogueActive) return;
            if (currentConfig == null || currentLineIndex >= currentConfig.dialogues.Count) return;

            DialogueLine line = currentConfig.dialogues[currentLineIndex];

            // 优先级1：如果选项正在逐字显示，跳过选项逐字
            if (dialogueUI != null && dialogueUI.IsChoicesTyping)
            {
                dialogueUI.SkipChoicesTypewriter();
                return;
            }

            // 优先级2：如果对话正在逐字显示，且可以跳过
            if (IsTyping && line.canSkipTypewriter)
            {
                SkipTypewriter();
                return;
            }

            // 优先级3：如果文字显示完成，且没有选项（或选项还没显示），跳到下一句
            if (!IsTyping && (line.choices == null || line.choices.Count == 0))
            {
                NextLine();
                return;
            }

            // 如果有选项且选项已显示完成，不做任何事（等用户点击选项）
        }

        /// <summary>
        /// 跳过打字机效果，立即显示全部文字
        /// </summary>
        public void SkipTypewriter()
        {
            if (dialogueUI != null)
            {
                dialogueUI.SkipTypewriter();
            }
            IsTyping = false;
        }

        /// <summary>
        /// 跳到下一句对话
        /// </summary>
        public void NextLine()
        {
            if (!IsDialogueActive) return;

            DialogueLine currentLine = currentConfig.dialogues[currentLineIndex];
            OnLineEnded?.Invoke(currentLine);

            // 触发当前对话的事件
            if (!string.IsNullOrEmpty(currentLine.triggerEvent))
            {
                TriggerEvent(currentLine.triggerEvent);
            }

            currentLineIndex++;
            ShowCurrentLine();
        }

        /// <summary>
        /// 跳转到指定ID的对话
        /// </summary>
        public void JumpToDialogue(int dialogueId)
        {
            if (currentConfig == null) return;

            for (int i = 0; i < currentConfig.dialogues.Count; i++)
            {
                if (currentConfig.dialogues[i].id == dialogueId)
                {
                    currentLineIndex = i;
                    ShowCurrentLine();
                    return;
                }
            }

            Debug.LogWarning($"[DialogueManager] 找不到对话ID: {dialogueId}");
        }

        /// <summary>
        /// 选项点击处理
        /// </summary>
        private void OnChoiceClicked(DialogueChoice choice)
        {
            OnChoiceSelected?.Invoke(choice);

            // 触发选项事件
            if (!string.IsNullOrEmpty(choice.triggerEvent))
            {
                TriggerEvent(choice.triggerEvent);
            }

            // 跳转到指定对话
            if (choice.nextDialogueId > 0)
            {
                JumpToDialogue(choice.nextDialogueId);
            }
            else
            {
                NextLine();
            }
        }

        /// <summary>
        /// 结束对话
        /// </summary>
        public void EndDialogue()
        {
            if (!IsDialogueActive) return;

            IsDialogueActive = false;
            IsTyping = false;

            if (dialogueUI != null)
            {
                dialogueUI.Hide();
            }

            // 触发对话结束事件
            if (currentConfig != null && !string.IsNullOrEmpty(currentConfig.onCompleteEvent))
            {
                TriggerEvent(currentConfig.onCompleteEvent);
            }

            OnDialogueEnded?.Invoke();

            if (debugMode)
                Debug.Log("[DialogueManager] 对话结束");

            currentConfig = null;
            currentLineIndex = 0;
        }

        /// <summary>
        /// 强制关闭对话
        /// </summary>
        public void ForceClose()
        {
            StopAllCoroutines();
            EndDialogue();
        }

        /// <summary>
        /// 触发事件（可以扩展连接到其他系统）
        /// </summary>
        private void TriggerEvent(string eventName)
        {
            if (debugMode)
                Debug.Log($"[DialogueManager] 触发事件: {eventName}");

            // TODO: 这里可以连接到事件系统、任务系统等
            // 例如: EventManager.Trigger(eventName);
        }

        /// <summary>
        /// 设置打字状态（由UI调用）
        /// </summary>
        public void SetTypingState(bool typing)
        {
            IsTyping = typing;
        }

        /// <summary>
        /// 获取当前对话进度
        /// </summary>
        public (int current, int total) GetProgress()
        {
            if (currentConfig == null) return (0, 0);
            return (currentLineIndex + 1, currentConfig.dialogues.Count);
        }
    }
}

