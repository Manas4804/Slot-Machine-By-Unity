using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main controller for slot-machine spins, payouts, and auto-spin.
/// </summary>
public class SlotMachine : MonoBehaviour
{
    [Header("Reels")]
    public Reel[] reels = new Reel[3];

    [Header("Symbols")]
    public SlotSymbol[] symbols;
    public Sprite[] symbolSprites;

    [Header("Managers")]
    public UIManager uiManager;
    public AudioSource spinSound;
    public AudioSource winSound;
    public AudioSource buttonClickSound;

    private Coroutine spinCoroutine;
    private Coroutine autoSpinCoroutine;

    public bool IsSpinning => GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Spinning;

    private void Start()
    {
        if (symbols == null || symbols.Length == 0)
        {
            symbols = CreateDefaultSymbols();
        }

        if (uiManager == null)
        {
            uiManager = FindObjectOfType<UIManager>();
        }

        ConfigureReels();
        uiManager?.Initialize(this, symbols);
    }

    public void Configure(Reel[] reelReferences, UIManager ui, AudioSource spinAudio, AudioSource winAudio, AudioSource buttonAudio, Sprite[] sprites)
    {
        reels = reelReferences;
        uiManager = ui;
        spinSound = spinAudio;
        winSound = winAudio;
        buttonClickSound = buttonAudio;
        symbolSprites = sprites;
        symbols = CreateDefaultSymbols();

        ConfigureReels();
        uiManager?.Initialize(this, symbols);
    }

    public void Spin()
    {
        PlayButtonClick();

        if (spinCoroutine != null || GameManager.Instance == null)
        {
            return;
        }

        if (GameManager.Instance.CurrentState == GameState.GameOver)
        {
            uiManager?.ShowGameOverPanel();
            return;
        }

        if (!GameManager.Instance.CanAffordCurrentBet())
        {
            uiManager?.ShowTryAgain("Not enough coins");
            CheckGameOver();
            return;
        }

        spinCoroutine = StartCoroutine(SpinRoutine());
    }

    public bool DetermineOutcome(out SlotSymbol winningSymbol)
    {
        winningSymbol = null;

        if (reels == null || reels.Length != 3)
        {
            return false;
        }

        SlotSymbol targetSymbol = null;

        foreach (Reel reel in reels)
        {
            SlotSymbol middleSymbol = reel.GetMiddleSymbol();

            if (middleSymbol == null)
            {
                return false;
            }

            if (middleSymbol.isWild)
            {
                continue;
            }

            if (targetSymbol == null)
            {
                targetSymbol = middleSymbol;
                continue;
            }

            if (middleSymbol.symbolName != targetSymbol.symbolName)
            {
                return false;
            }
        }

        winningSymbol = targetSymbol ?? reels[0].GetMiddleSymbol();
        return winningSymbol != null;
    }

    public int CalculatePayout(SlotSymbol winSymbol, int bet)
    {
        if (winSymbol == null)
        {
            return 0;
        }

        return Mathf.Max(0, bet * winSymbol.payoutMultiplier);
    }

    public void UpdateBalance(int amount)
    {
        GameManager.Instance?.AddBalance(amount);
        uiManager?.UpdateBalanceDisplay();
    }

    public void CheckGameOver()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        if (GameManager.Instance.playerBalance <= 0)
        {
            GameManager.Instance.SetState(GameState.GameOver);
            uiManager?.ShowGameOverPanel();
        }
    }

    public void StartAutoSpin()
    {
        PlayButtonClick();

        if (autoSpinCoroutine != null || GameManager.Instance == null || GameManager.Instance.CurrentState == GameState.GameOver)
        {
            return;
        }

        autoSpinCoroutine = StartCoroutine(AutoSpinRoutine());
    }

    public void SetBet(int betAmount)
    {
        PlayButtonClick();
        GameManager.Instance?.SetBet(betAmount);
        uiManager?.UpdateBetDisplay();
    }

    public void ResetMachine()
    {
        GameManager.Instance?.ResetGame();
        ConfigureReels();
        ClearWinningHighlights();
        uiManager?.HideAllPanels();
        uiManager?.UpdateAllDisplays();
    }

    public void PlayButtonClick()
    {
        PlaySound(buttonClickSound);
    }

    public static SlotSymbol[] CreateDefaultSymbols()
    {
        return new[]
        {
            new SlotSymbol { symbolName = "Cherry", payoutMultiplier = 2, symbolColor = new Color(0.88f, 0.05f, 0.12f), isWild = false },
            new SlotSymbol { symbolName = "Lemon", payoutMultiplier = 3, symbolColor = new Color(0.95f, 0.86f, 0.12f), isWild = false },
            new SlotSymbol { symbolName = "Orange", payoutMultiplier = 4, symbolColor = new Color(1f, 0.48f, 0.05f), isWild = false },
            new SlotSymbol { symbolName = "Bell", payoutMultiplier = 5, symbolColor = new Color(1f, 0.67f, 0.22f), isWild = false },
            new SlotSymbol { symbolName = "Seven", payoutMultiplier = 10, symbolColor = new Color(0.88f, 0.04f, 0.02f), isWild = false },
            new SlotSymbol { symbolName = "Wild", payoutMultiplier = 15, symbolColor = new Color(0.34f, 0.23f, 0.86f), isWild = true }
        };
    }

    private IEnumerator SpinRoutine()
    {
        GameManager.Instance.SetState(GameState.Spinning);
        GameManager.Instance.lastWinAmount = 0;
        uiManager?.SetControlsInteractable(false);
        uiManager?.HideFeedbackPanels();
        ClearWinningHighlights();

        int[] targetIndexes = new int[reels.Length];

        for (int i = 0; i < reels.Length; i++)
        {
            targetIndexes[i] = Random.Range(0, symbols.Length);
            reels[i].StartSpin();
        }

        PlaySound(spinSound);

        yield return new WaitForSeconds(1.5f);
        reels[0].StopSpin(targetIndexes[0]);

        yield return new WaitForSeconds(0.5f);
        reels[1].StopSpin(targetIndexes[1]);

        yield return new WaitForSeconds(0.5f);
        reels[2].StopSpin(targetIndexes[2]);

        yield return new WaitUntil(() => reels.All(reel => !reel.isSpinning));

        GameManager.Instance.RecordSpin();

        if (DetermineOutcome(out SlotSymbol winningSymbol))
        {
            int payout = CalculatePayout(winningSymbol, GameManager.Instance.currentBet);
            GameManager.Instance.lastWinAmount = payout;
            UpdateBalance(payout);
            GameManager.Instance.SetState(GameState.Win);
            HighlightWinningSymbols();
            PlaySound(winSound);
            uiManager?.ShowWinPanel(payout);
        }
        else
        {
            GameManager.Instance.lastWinAmount = 0;
            UpdateBalance(-GameManager.Instance.currentBet);
            uiManager?.ShowTryAgain("Try Again");
        }

        uiManager?.UpdateAllDisplays();
        CheckGameOver();

        if (GameManager.Instance.CurrentState != GameState.GameOver)
        {
            GameManager.Instance.SetState(GameState.Idle);

            if (autoSpinCoroutine == null)
            {
                uiManager?.SetControlsInteractable(true);
            }
        }

        spinCoroutine = null;
    }

    private IEnumerator AutoSpinRoutine()
    {
        uiManager?.SetAutoSpinInteractable(false);

        for (int i = 0; i < 5; i++)
        {
            if (GameManager.Instance.CurrentState == GameState.GameOver || !GameManager.Instance.CanAffordCurrentBet())
            {
                break;
            }

            Spin();
            yield return new WaitUntil(() => spinCoroutine == null);
            yield return new WaitForSeconds(1f);
        }

        autoSpinCoroutine = null;

        if (GameManager.Instance.CurrentState != GameState.GameOver)
        {
            uiManager?.SetControlsInteractable(true);
        }
    }

    private void ConfigureReels()
    {
        if (reels == null || symbols == null)
        {
            return;
        }

        foreach (Reel reel in reels)
        {
            if (reel != null)
            {
                reel.Configure(symbols, symbolSprites);
            }
        }
    }

    private void HighlightWinningSymbols()
    {
        foreach (Reel reel in reels)
        {
            reel.SetMiddleHighlight(true);
        }
    }

    private void ClearWinningHighlights()
    {
        if (reels == null)
        {
            return;
        }

        foreach (Reel reel in reels)
        {
            if (reel != null)
            {
                reel.SetMiddleHighlight(false);
            }
        }
    }

    private void PlaySound(AudioSource source)
    {
        if (source != null && AudioListener.volume > 0f)
        {
            source.Play();
        }
    }
}
