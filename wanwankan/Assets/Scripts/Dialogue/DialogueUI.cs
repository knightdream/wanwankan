using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

namespace Dialogue
{
    /// <summary>
    /// 对话UI控制器
    /// 负责显示对话内容、人物立绘、打字机效果等
    /// </summary>
    public class DialogueUI : MonoBehaviour, IPointerClickHandler
    {
        [Header("主面板")]
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private Image blockingPanel; // 阻挡点击的透明面板

        [Header("人物立绘")]
        [SerializeField] private Image leftCharacterImage;
        [SerializeField] private Image rightCharacterImage;
        [SerializeField] private Color highlightColor = Color.white;
        [SerializeField] private Color dimColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        [Header("对话框")]
        [SerializeField] private Image dialogueBox;
        [SerializeField] private TextMeshProUGUI speakerNameText;
        [SerializeField] private TextMeshProUGUI dialogueText;

        [Header("指示器")]
        [SerializeField] private GameObject nextIndicator; // 下一句指示箭头
        [SerializeField] private float indicatorPulseSpeed = 2f;

        [Header("选项")]
        [SerializeField] private Transform choicesContainer;
        [SerializeField] private GameObject choiceButtonPrefab;

        [Header("打字机效果")]
        [SerializeField] private AudioSource typingAudioSource;
        [SerializeField] private AudioClip typingSound;
        [SerializeField] private bool playTypingSound = true;

        // 当前对话数据
        private DialogueLine currentLine;
        private Action onTypewriterComplete;
        private Coroutine typewriterCoroutine;
        private bool isTyping = false;
        private string fullText = "";

        // 选项相关
        private List<GameObject> choiceButtons = new List<GameObject>();
        private Action<DialogueChoice> onChoiceSelected;
        private bool isChoicesTyping = false;
        private List<Coroutine> choiceTypewriterCoroutines = new List<Coroutine>();

        private void Awake()
        {
            // 初始隐藏
            if (dialoguePanel != null)
                dialoguePanel.SetActive(false);

            // 设置阻挡面板
            if (blockingPanel != null)
            {
                blockingPanel.raycastTarget = true;
                blockingPanel.color = new Color(0, 0, 0, 0); // 完全透明但可点击
            }

            // 隐藏选项容器
            if (choicesContainer != null)
                choicesContainer.gameObject.SetActive(false);

            // 隐藏指示器
            if (nextIndicator != null)
                nextIndicator.SetActive(false);
        }

        private void Update()
        {
            // 指示器脉冲动画
            if (nextIndicator != null && nextIndicator.activeSelf)
            {
                float scale = 1f + Mathf.Sin(Time.time * indicatorPulseSpeed) * 0.1f;
                nextIndicator.transform.localScale = Vector3.one * scale;
            }
        }

        /// <summary>
        /// 显示对话面板
        /// </summary>
        public void Show()
        {
            if (dialoguePanel != null)
                dialoguePanel.SetActive(true);

            if (blockingPanel != null)
                blockingPanel.gameObject.SetActive(true);
        }

        /// <summary>
        /// 隐藏对话面板
        /// </summary>
        public void Hide()
        {
            if (dialoguePanel != null)
                dialoguePanel.SetActive(false);

            if (blockingPanel != null)
                blockingPanel.gameObject.SetActive(false);

            ClearChoices();
        }

        /// <summary>
        /// 显示一句对话
        /// </summary>
        public void ShowLine(DialogueLine line, Action onComplete)
        {
            currentLine = line;
            onTypewriterComplete = onComplete;
            fullText = line.content;

            // 设置说话者名称
            if (speakerNameText != null)
                speakerNameText.text = line.speakerName;

            // 设置人物立绘
            SetCharacterImages(line);

            // 隐藏选项
            ClearChoices();

            // 隐藏指示器
            if (nextIndicator != null)
                nextIndicator.SetActive(false);

            // 停止之前的打字机效果
            if (typewriterCoroutine != null)
                StopCoroutine(typewriterCoroutine);

            // 开始显示文字
            if (line.typewriterEffect)
            {
                typewriterCoroutine = StartCoroutine(TypewriterEffect(line.content, line.typewriterSpeed));
            }
            else
            {
                // 直接显示全部文字
                if (dialogueText != null)
                    dialogueText.text = line.content;
                
                isTyping = false;
                OnTypewriterFinished();
            }
        }

        /// <summary>
        /// 设置人物立绘
        /// </summary>
        private void SetCharacterImages(DialogueLine line)
        {
            // 加载并设置左侧立绘
            if (leftCharacterImage != null)
            {
                if (!string.IsNullOrEmpty(line.leftCharacterImage))
                {
                    Sprite sprite = Resources.Load<Sprite>($"Characters/{line.leftCharacterImage}");
                    if (sprite != null)
                    {
                        leftCharacterImage.sprite = sprite;
                        leftCharacterImage.gameObject.SetActive(true);
                    }
                    else
                    {
                        leftCharacterImage.gameObject.SetActive(false);
                    }
                }
                else
                {
                    leftCharacterImage.gameObject.SetActive(false);
                }

                // 设置高亮/变暗
                leftCharacterImage.color = line.highlightLeft ? highlightColor : dimColor;
            }

            // 加载并设置右侧立绘
            if (rightCharacterImage != null)
            {
                if (!string.IsNullOrEmpty(line.rightCharacterImage))
                {
                    Sprite sprite = Resources.Load<Sprite>($"Characters/{line.rightCharacterImage}");
                    if (sprite != null)
                    {
                        rightCharacterImage.sprite = sprite;
                        rightCharacterImage.gameObject.SetActive(true);
                    }
                    else
                    {
                        rightCharacterImage.gameObject.SetActive(false);
                    }
                }
                else
                {
                    rightCharacterImage.gameObject.SetActive(false);
                }

                // 设置高亮/变暗
                rightCharacterImage.color = line.highlightRight ? highlightColor : dimColor;
            }
        }

        /// <summary>
        /// 打字机效果协程
        /// </summary>
        private IEnumerator TypewriterEffect(string text, float speed)
        {
            isTyping = true;
            
            if (DialogueManager.Instance != null)
                DialogueManager.Instance.SetTypingState(true);

            if (dialogueText != null)
                dialogueText.text = "";

            for (int i = 0; i < text.Length; i++)
            {
                if (!isTyping) break; // 被跳过

                if (dialogueText != null)
                    dialogueText.text = text.Substring(0, i + 1);

                // 播放打字音效
                if (playTypingSound && typingAudioSource != null && typingSound != null)
                {
                    // 跳过空格和标点不播放音效
                    char c = text[i];
                    if (!char.IsWhiteSpace(c) && !char.IsPunctuation(c))
                    {
                        typingAudioSource.PlayOneShot(typingSound);
                    }
                }

                yield return new WaitForSeconds(speed);
            }

            // 确保显示完整文字
            if (dialogueText != null)
                dialogueText.text = text;

            isTyping = false;
            
            if (DialogueManager.Instance != null)
                DialogueManager.Instance.SetTypingState(false);

            OnTypewriterFinished();
        }

        /// <summary>
        /// 跳过打字机效果
        /// </summary>
        public void SkipTypewriter()
        {
            if (!isTyping) return;

            isTyping = false;

            if (typewriterCoroutine != null)
                StopCoroutine(typewriterCoroutine);

            // 显示完整文字
            if (dialogueText != null)
                dialogueText.text = fullText;

            if (DialogueManager.Instance != null)
                DialogueManager.Instance.SetTypingState(false);

            OnTypewriterFinished();
        }

        /// <summary>
        /// 打字机效果完成
        /// </summary>
        private void OnTypewriterFinished()
        {
            // 显示下一句指示器（如果没有选项）
            if (nextIndicator != null && (currentLine.choices == null || currentLine.choices.Count == 0))
            {
                nextIndicator.SetActive(true);
            }

            onTypewriterComplete?.Invoke();
        }

        /// <summary>
        /// 显示选项
        /// </summary>
        public void ShowChoices(List<DialogueChoice> choices, Action<DialogueChoice> onSelect)
        {
            onChoiceSelected = onSelect;

            // 隐藏指示器
            if (nextIndicator != null)
                nextIndicator.SetActive(false);

            // 清除旧选项
            ClearChoices();

            if (choicesContainer == null || choiceButtonPrefab == null) return;

            // 启动选项显示协程（支持延迟和逐字效果）
            StartCoroutine(ShowChoicesCoroutine(choices));
        }

        /// <summary>
        /// 显示选项协程（支持延迟、逐字效果）
        /// </summary>
        private IEnumerator ShowChoicesCoroutine(List<DialogueChoice> choices)
        {
            // 等待选项显示延迟
            if (currentLine.choicesShowDelay > 0)
            {
                yield return new WaitForSeconds(currentLine.choicesShowDelay);
            }

            choicesContainer.gameObject.SetActive(true);
            isChoicesTyping = currentLine.choicesTypewriterEffect;

            // 创建所有选项按钮
            List<ChoiceButtonData> buttonDataList = new List<ChoiceButtonData>();

            foreach (var choice in choices)
            {
                GameObject buttonObj = Instantiate(choiceButtonPrefab, choicesContainer);
                choiceButtons.Add(buttonObj);

                TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                Button button = buttonObj.GetComponent<Button>();

                // 如果使用逐字效果，先清空文字并禁用按钮
                if (currentLine.choicesTypewriterEffect && buttonText != null)
                {
                    buttonText.text = "";
                    if (button != null)
                        button.interactable = false;
                }
                else if (buttonText != null)
                {
                    buttonText.text = choice.text;
                }

                // 设置按钮点击事件
                if (button != null)
                {
                    DialogueChoice capturedChoice = choice;
                    button.onClick.AddListener(() => OnChoiceButtonClicked(capturedChoice));
                }

                buttonDataList.Add(new ChoiceButtonData
                {
                    buttonObj = buttonObj,
                    buttonText = buttonText,
                    button = button,
                    choice = choice,
                    fullText = choice.text
                });
            }

            // 如果使用逐字效果
            if (currentLine.choicesTypewriterEffect)
            {
                if (currentLine.choicesAppearSimultaneously)
                {
                    // 所有选项同时逐字出现
                    foreach (var data in buttonDataList)
                    {
                        var coroutine = StartCoroutine(ChoiceTypewriterEffect(data));
                        choiceTypewriterCoroutines.Add(coroutine);
                    }
                }
                else
                {
                    // 选项依次逐字出现
                    foreach (var data in buttonDataList)
                    {
                        yield return StartCoroutine(ChoiceTypewriterEffect(data));
                        
                        // 选项之间的延迟
                        if (currentLine.choicesAppearDelay > 0)
                        {
                            yield return new WaitForSeconds(currentLine.choicesAppearDelay);
                        }
                    }
                }
            }

            isChoicesTyping = false;
        }

        /// <summary>
        /// 选项按钮数据
        /// </summary>
        private class ChoiceButtonData
        {
            public GameObject buttonObj;
            public TextMeshProUGUI buttonText;
            public Button button;
            public DialogueChoice choice;
            public string fullText;
        }

        /// <summary>
        /// 选项逐字显示效果
        /// </summary>
        private IEnumerator ChoiceTypewriterEffect(ChoiceButtonData data)
        {
            if (data.buttonText == null) yield break;

            string text = data.fullText;
            float speed = currentLine.choicesTypewriterSpeed;

            for (int i = 0; i < text.Length; i++)
            {
                data.buttonText.text = text.Substring(0, i + 1);
                yield return new WaitForSeconds(speed);
            }

            // 确保显示完整文字
            data.buttonText.text = text;

            // 启用按钮
            if (data.button != null)
                data.button.interactable = true;
        }

        /// <summary>
        /// 跳过选项逐字效果
        /// </summary>
        public void SkipChoicesTypewriter()
        {
            if (!isChoicesTyping) return;

            // 停止所有选项逐字协程
            foreach (var coroutine in choiceTypewriterCoroutines)
            {
                if (coroutine != null)
                    StopCoroutine(coroutine);
            }
            choiceTypewriterCoroutines.Clear();

            // 立即显示所有选项完整文字并启用按钮
            foreach (var buttonObj in choiceButtons)
            {
                if (buttonObj == null) continue;

                TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                Button button = buttonObj.GetComponent<Button>();

                // 从按钮的onClick事件中找到对应的choice
                // 这里简单处理：直接启用按钮
                if (button != null)
                    button.interactable = true;
            }

            isChoicesTyping = false;
        }

        /// <summary>
        /// 选项按钮点击
        /// </summary>
        private void OnChoiceButtonClicked(DialogueChoice choice)
        {
            ClearChoices();
            onChoiceSelected?.Invoke(choice);
        }

        /// <summary>
        /// 清除选项
        /// </summary>
        private void ClearChoices()
        {
            // 停止所有选项逐字协程
            foreach (var coroutine in choiceTypewriterCoroutines)
            {
                if (coroutine != null)
                    StopCoroutine(coroutine);
            }
            choiceTypewriterCoroutines.Clear();
            isChoicesTyping = false;

            foreach (var button in choiceButtons)
            {
                if (button != null)
                    Destroy(button);
            }
            choiceButtons.Clear();

            if (choicesContainer != null)
                choicesContainer.gameObject.SetActive(false);
        }

        /// <summary>
        /// 点击事件处理
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            // 如果选项正在逐字显示，点击跳过选项逐字效果
            if (isChoicesTyping)
            {
                SkipChoicesTypewriter();
                return;
            }

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.OnClick();
            }
        }

        /// <summary>
        /// 获取选项是否正在逐字显示
        /// </summary>
        public bool IsChoicesTyping => isChoicesTyping;

        /// <summary>
        /// 获取是否正在打字
        /// </summary>
        public bool IsTyping => isTyping;
    }
}

