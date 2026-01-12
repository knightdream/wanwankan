using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WanWanKan.Core;

namespace WanWanKan.UI
{
    /// <summary>
    /// 骰子投掷UI
    /// </summary>
    public class DiceRollUI : MonoBehaviour
    {
        [Header("UI组件")]
        [SerializeField] private GameObject dicePanel;
        [SerializeField] private Image diceImage;
        [SerializeField] private TextMeshProUGUI diceValueText;
        [SerializeField] private TextMeshProUGUI diceTypeText;
        [SerializeField] private TextMeshProUGUI resultText;

        [Header("骰子图片 (可选)")]
        [SerializeField] private Sprite[] d6Sprites;   // 6张，对应1-6
        [SerializeField] private Sprite d20Sprite;
        [SerializeField] private Sprite d100Sprite;

        [Header("动画设置")]
        [SerializeField] private float showDuration = 2f;
        [SerializeField] private float shakeIntensity = 10f;
        [SerializeField] private float shakeSpeed = 50f;

        [Header("颜色")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color critSuccessColor = new Color(1f, 0.8f, 0f);  // 金色
        [SerializeField] private Color critFailColor = new Color(0.8f, 0f, 0f);     // 红色

        private Coroutine currentAnimation;
        private Vector3 originalPosition;

        private void Start()
        {
            if (dicePanel != null)
            {
                originalPosition = dicePanel.transform.localPosition;
                dicePanel.SetActive(false);
            }

            // 订阅骰子系统事件
            if (DiceSystem.Instance != null)
            {
                DiceSystem.Instance.OnDiceRollStarted += OnRollStarted;
                DiceSystem.Instance.OnDiceRolling += OnRolling;
                DiceSystem.Instance.OnDiceRollCompleted += OnRollCompleted;
            }
        }

        /// <summary>
        /// 显示骰子投掷动画
        /// </summary>
        public void ShowDiceRoll(DiceResult result)
        {
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
            }
            currentAnimation = StartCoroutine(DiceRollAnimation(result));
        }

        private void OnRollStarted(DiceType type, int count)
        {
            if (dicePanel != null)
            {
                dicePanel.SetActive(true);
            }

            if (diceTypeText != null)
            {
                diceTypeText.text = $"{count}d{(int)type}";
            }

            // 设置骰子图片
            UpdateDiceSprite(type, 0);
        }

        private void OnRolling(int previewValue)
        {
            if (diceValueText != null)
            {
                diceValueText.text = previewValue.ToString();
            }

            // 抖动效果
            if (dicePanel != null)
            {
                float offsetX = Mathf.Sin(Time.time * shakeSpeed) * shakeIntensity;
                float offsetY = Mathf.Cos(Time.time * shakeSpeed * 1.3f) * shakeIntensity;
                dicePanel.transform.localPosition = originalPosition + new Vector3(offsetX, offsetY, 0);
            }
        }

        private void OnRollCompleted(DiceResult result)
        {
            ShowDiceRoll(result);
        }

        private IEnumerator DiceRollAnimation(DiceResult result)
        {
            if (dicePanel != null)
            {
                dicePanel.SetActive(true);
                dicePanel.transform.localPosition = originalPosition;
            }

            // 更新显示
            if (diceValueText != null)
            {
                diceValueText.text = result.FinalResult.ToString();
            }

            if (diceTypeText != null)
            {
                diceTypeText.text = result.ToString();
            }

            // 更新骰子图片
            if (result.Rolls.Length > 0)
            {
                UpdateDiceSprite(result.DiceType, result.Rolls[0]);
            }

            // 设置颜色
            Color displayColor = normalColor;
            string resultMessage = "";

            if (result.IsCriticalSuccess)
            {
                displayColor = critSuccessColor;
                resultMessage = "大成功！";
            }
            else if (result.IsCriticalFailure)
            {
                displayColor = critFailColor;
                resultMessage = "大失败...";
            }

            if (diceValueText != null)
            {
                diceValueText.color = displayColor;
            }

            if (resultText != null)
            {
                resultText.text = resultMessage;
                resultText.color = displayColor;
            }

            // 缩放动画
            if (dicePanel != null)
            {
                float elapsed = 0f;
                float scaleDuration = 0.3f;
                Vector3 startScale = Vector3.one * 0.5f;
                Vector3 endScale = Vector3.one;

                while (elapsed < scaleDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / scaleDuration;
                    t = 1f - Mathf.Pow(1f - t, 3f); // Ease out cubic
                    dicePanel.transform.localScale = Vector3.Lerp(startScale, endScale, t);
                    yield return null;
                }
            }

            // 等待显示
            yield return new WaitForSeconds(showDuration);

            // 淡出
            if (dicePanel != null)
            {
                float elapsed = 0f;
                float fadeDuration = 0.3f;
                CanvasGroup canvasGroup = dicePanel.GetComponent<CanvasGroup>();
                
                if (canvasGroup != null)
                {
                    while (elapsed < fadeDuration)
                    {
                        elapsed += Time.deltaTime;
                        canvasGroup.alpha = 1f - (elapsed / fadeDuration);
                        yield return null;
                    }
                    canvasGroup.alpha = 1f;
                }

                dicePanel.SetActive(false);
            }

            currentAnimation = null;
        }

        private void UpdateDiceSprite(DiceType type, int value)
        {
            if (diceImage == null) return;

            switch (type)
            {
                case DiceType.D6:
                    if (d6Sprites != null && value >= 1 && value <= 6 && d6Sprites.Length >= value)
                    {
                        diceImage.sprite = d6Sprites[value - 1];
                    }
                    break;
                case DiceType.D20:
                    if (d20Sprite != null)
                    {
                        diceImage.sprite = d20Sprite;
                    }
                    break;
                case DiceType.D100:
                    if (d100Sprite != null)
                    {
                        diceImage.sprite = d100Sprite;
                    }
                    break;
            }
        }

        /// <summary>
        /// 手动触发骰子动画（用于测试）
        /// </summary>
        public void TestRoll(DiceType type)
        {
            if (DiceSystem.Instance != null)
            {
                DiceSystem.Instance.RollWithAnimation(type, 1, 0, null);
            }
        }

        private void OnDestroy()
        {
            if (DiceSystem.Instance != null)
            {
                DiceSystem.Instance.OnDiceRollStarted -= OnRollStarted;
                DiceSystem.Instance.OnDiceRolling -= OnRolling;
                DiceSystem.Instance.OnDiceRollCompleted -= OnRollCompleted;
            }
        }
    }
}

