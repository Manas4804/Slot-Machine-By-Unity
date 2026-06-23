using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Owns all slot-machine UI references and screen feedback.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("Text")]
    public Text balanceText;
    public Text betText;
    public Text winText;
    public Text spinCountText;
    public Text feedbackText;
    public Text winAmountText;
    public Text paytableContentText;
    public Text muteButtonText;

    [Header("Buttons")]
    public Button spinButton;
    public Button autoSpinButton;
    public Button paytableButton;
    public Button muteButton;
    public Button restartButton;
    public Button closePaytableButton;
    public Button betButton10;
    public Button betButton20;
    public Button betButton50;
    public Button betButton100;

    [Header("Panels")]
    public GameObject winPanel;
    public GameObject gameOverPanel;
    public GameObject paytablePanel;

    private SlotMachine slotMachine;
    private SlotSymbol[] symbols;
    private Coroutine feedbackCoroutine;
    private Coroutine winPanelCoroutine;
    private bool isMuted;

    public void Initialize(SlotMachine machine, SlotSymbol[] symbolDefinitions)
    {
        slotMachine = machine;
        symbols = symbolDefinitions;

        WireButtons();
        BuildPaytableText();
        HideAllPanels();
        UpdateAllDisplays();
    }

    public void UpdateAllDisplays()
    {
        UpdateBalanceDisplay();
        UpdateBetDisplay();
        UpdateWinDisplay();
        UpdateSpinCountDisplay();
    }

    public void UpdateBalanceDisplay()
    {
        if (balanceText != null && GameManager.Instance != null)
        {
            balanceText.text = $"Balance: {GameManager.Instance.playerBalance}";
        }
    }

    public void UpdateBetDisplay()
    {
        if (betText != null && GameManager.Instance != null)
        {
            betText.text = $"Bet: {GameManager.Instance.currentBet}";
        }
    }

    public void UpdateWinDisplay()
    {
        if (winText != null && GameManager.Instance != null)
        {
            winText.text = $"Win: {GameManager.Instance.lastWinAmount}";
        }
    }

    public void UpdateSpinCountDisplay()
    {
        if (spinCountText != null && GameManager.Instance != null)
        {
            spinCountText.text = $"Spins: {GameManager.Instance.totalSpins}";
        }
    }

    public void ShowWinPanel(int amount)
    {
        if (winAmountText != null)
        {
            winAmountText.text = $"WIN! +{amount}";
        }

        if (winPanel != null)
        {
            if (winPanelCoroutine != null)
            {
                StopCoroutine(winPanelCoroutine);
            }

            winPanelCoroutine = StartCoroutine(FlashWinPanel());
        }
    }

    public void ShowTryAgain(string message)
    {
        if (feedbackText == null)
        {
            return;
        }

        if (feedbackCoroutine != null)
        {
            StopCoroutine(feedbackCoroutine);
        }

        feedbackCoroutine = StartCoroutine(ShowFeedbackRoutine(message));
    }

    public void ShowGameOverPanel()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        SetControlsInteractable(false);
    }

    public void ShowPaytable()
    {
        slotMachine?.PlayButtonClick();

        if (paytablePanel != null)
        {
            paytablePanel.SetActive(true);
        }
    }

    public void HidePaytable()
    {
        slotMachine?.PlayButtonClick();

        if (paytablePanel != null)
        {
            paytablePanel.SetActive(false);
        }
    }

    public void ToggleSound()
    {
        isMuted = !isMuted;
        AudioListener.volume = isMuted ? 0f : 1f;

        if (muteButtonText != null)
        {
            muteButtonText.text = isMuted ? "Muted" : "Sound";
        }
    }

    public void SetControlsInteractable(bool interactable)
    {
        if (spinButton != null) spinButton.interactable = interactable;
        if (autoSpinButton != null) autoSpinButton.interactable = interactable;
        if (paytableButton != null) paytableButton.interactable = interactable;
        if (muteButton != null) muteButton.interactable = true;
        if (betButton10 != null) betButton10.interactable = interactable;
        if (betButton20 != null) betButton20.interactable = interactable;
        if (betButton50 != null) betButton50.interactable = interactable;
        if (betButton100 != null) betButton100.interactable = interactable;
    }

    public void SetAutoSpinInteractable(bool interactable)
    {
        if (autoSpinButton != null)
        {
            autoSpinButton.interactable = interactable;
        }
    }

    public void HideFeedbackPanels()
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (feedbackText != null) feedbackText.gameObject.SetActive(false);
    }

    public void HideAllPanels()
    {
        HideFeedbackPanels();
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (paytablePanel != null) paytablePanel.SetActive(false);
    }

    private void WireButtons()
    {
        if (spinButton != null)
        {
            spinButton.onClick.RemoveAllListeners();
            spinButton.onClick.AddListener(() => slotMachine.Spin());
        }

        if (autoSpinButton != null)
        {
            autoSpinButton.onClick.RemoveAllListeners();
            autoSpinButton.onClick.AddListener(() => slotMachine.StartAutoSpin());
        }

        if (paytableButton != null)
        {
            paytableButton.onClick.RemoveAllListeners();
            paytableButton.onClick.AddListener(ShowPaytable);
        }

        if (muteButton != null)
        {
            muteButton.onClick.RemoveAllListeners();
            muteButton.onClick.AddListener(ToggleSound);
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(() => slotMachine.ResetMachine());
        }

        if (closePaytableButton != null)
        {
            closePaytableButton.onClick.RemoveAllListeners();
            closePaytableButton.onClick.AddListener(HidePaytable);
        }

        ConfigureBetButton(betButton10, 10);
        ConfigureBetButton(betButton20, 20);
        ConfigureBetButton(betButton50, 50);
        ConfigureBetButton(betButton100, 100);
    }

    private void ConfigureBetButton(Button button, int betAmount)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => slotMachine.SetBet(betAmount));
    }

    private void BuildPaytableText()
    {
        if (paytableContentText == null || symbols == null)
        {
            return;
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("PAYTABLE");
        builder.AppendLine();

        foreach (SlotSymbol symbol in symbols)
        {
            string note = symbol.isWild ? " - matches any symbol" : string.Empty;
            builder.AppendLine($"{symbol.symbolName}: {symbol.payoutMultiplier}x{note}");
        }

        builder.AppendLine();
        builder.AppendLine("Middle row pays when all three reels match.");
        paytableContentText.text = builder.ToString();
    }

    private IEnumerator ShowFeedbackRoutine(string message)
    {
        feedbackText.text = message;
        feedbackText.gameObject.SetActive(true);
        yield return new WaitForSeconds(1.2f);
        feedbackText.gameObject.SetActive(false);
        feedbackCoroutine = null;
    }

    private IEnumerator FlashWinPanel()
    {
        winPanel.SetActive(true);

        Image panelImage = winPanel.GetComponent<Image>();
        Color baseColor = panelImage != null ? panelImage.color : Color.white;
        Color flashColor = new Color(1f, 0.78f, 0.08f, 0.9f);

        for (int i = 0; i < 6; i++)
        {
            if (panelImage != null)
            {
                panelImage.color = i % 2 == 0 ? flashColor : baseColor;
            }

            yield return new WaitForSeconds(0.16f);
        }

        if (panelImage != null)
        {
            panelImage.color = baseColor;
        }

        yield return new WaitForSeconds(0.8f);
        winPanel.SetActive(false);
        winPanelCoroutine = null;
    }
}
