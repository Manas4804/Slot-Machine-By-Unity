using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages one reel's visible symbols and spin animation.
/// </summary>
public class Reel : MonoBehaviour
{
    [Header("Runtime State")]
    public int currentSymbolIndex;
    public bool isSpinning;

    [Header("Symbol Slots")]
    [SerializeField] private Image[] symbolImages = new Image[3];
    [SerializeField] private Text[] symbolLabels = new Text[3];

    private SlotSymbol[] availableSymbols;
    private Sprite[] symbolSprites;
    private Coroutine spinLoopCoroutine;
    private Coroutine stopCoroutine;
    private Coroutine highlightCoroutine;

    public void Configure(SlotSymbol[] symbols, Sprite[] sprites)
    {
        availableSymbols = symbols;
        symbolSprites = sprites;
        currentSymbolIndex = UnityEngine.Random.Range(0, availableSymbols.Length);
        UpdateVisualDisplay();
    }

    public void SetSymbolSlots(Image topImage, Text topText, Image middleImage, Text middleText, Image bottomImage, Text bottomText)
    {
        symbolImages = new[] { topImage, middleImage, bottomImage };
        symbolLabels = new[] { topText, middleText, bottomText };
    }

    public void StartSpin()
    {
        if (availableSymbols == null || availableSymbols.Length == 0)
        {
            Debug.LogWarning($"{name} cannot spin because it has no symbols configured.");
            return;
        }

        StopHighlight();
        isSpinning = true;

        if (spinLoopCoroutine != null)
        {
            StopCoroutine(spinLoopCoroutine);
        }

        if (stopCoroutine != null)
        {
            StopCoroutine(stopCoroutine);
            stopCoroutine = null;
        }

        spinLoopCoroutine = StartCoroutine(SpinLoop());
    }

    public void StopSpin(int targetIndex)
    {
        StopSpin(targetIndex, null);
    }

    public void StopSpin(int targetIndex, Action<Reel> onStopped)
    {
        if (!isSpinning)
        {
            return;
        }

        if (spinLoopCoroutine != null)
        {
            StopCoroutine(spinLoopCoroutine);
            spinLoopCoroutine = null;
        }

        if (stopCoroutine != null)
        {
            StopCoroutine(stopCoroutine);
        }

        stopCoroutine = StartCoroutine(StopSpinRoutine(targetIndex, onStopped));
    }

    public void UpdateVisualDisplay()
    {
        if (availableSymbols == null || availableSymbols.Length == 0 || symbolImages == null || symbolLabels == null)
        {
            return;
        }

        int topIndex = WrapSymbolIndex(currentSymbolIndex - 1);
        int middleIndex = WrapSymbolIndex(currentSymbolIndex);
        int bottomIndex = WrapSymbolIndex(currentSymbolIndex + 1);

        ApplySymbolToSlot(0, topIndex);
        ApplySymbolToSlot(1, middleIndex);
        ApplySymbolToSlot(2, bottomIndex);
    }

    public SlotSymbol GetMiddleSymbol()
    {
        if (availableSymbols == null || availableSymbols.Length == 0)
        {
            return null;
        }

        return availableSymbols[WrapSymbolIndex(currentSymbolIndex)];
    }

    public void SetMiddleHighlight(bool enabled)
    {
        StopHighlight();

        if (enabled && symbolImages != null && symbolImages.Length > 1 && symbolImages[1] != null)
        {
            highlightCoroutine = StartCoroutine(HighlightMiddleSlot());
        }
    }

    private IEnumerator SpinLoop()
    {
        while (isSpinning)
        {
            AdvanceSymbols();
            yield return new WaitForSeconds(0.05f);
        }
    }

    private IEnumerator StopSpinRoutine(int targetIndex, Action<Reel> onStopped)
    {
        float elapsed = 0f;
        float updateDelay = 0.08f;

        while (elapsed < 0.5f)
        {
            AdvanceSymbols();
            updateDelay = Mathf.Lerp(0.08f, 0.18f, elapsed / 0.5f);
            elapsed += updateDelay;
            yield return new WaitForSeconds(updateDelay);
        }

        currentSymbolIndex = WrapSymbolIndex(targetIndex);
        UpdateVisualDisplay();
        isSpinning = false;
        stopCoroutine = null;
        onStopped?.Invoke(this);
    }

    private IEnumerator HighlightMiddleSlot()
    {
        Image middleImage = symbolImages[1];
        Vector3 originalScale = middleImage.rectTransform.localScale;

        while (true)
        {
            middleImage.rectTransform.localScale = originalScale * 1.08f;
            middleImage.color = Color.Lerp(middleImage.color, Color.yellow, 0.35f);
            yield return new WaitForSeconds(0.18f);

            middleImage.rectTransform.localScale = originalScale;
            UpdateVisualDisplay();
            yield return new WaitForSeconds(0.18f);
        }
    }

    private void StopHighlight()
    {
        if (highlightCoroutine != null)
        {
            StopCoroutine(highlightCoroutine);
            highlightCoroutine = null;
        }

        if (symbolImages != null && symbolImages.Length > 1 && symbolImages[1] != null)
        {
            symbolImages[1].rectTransform.localScale = Vector3.one;
        }

        UpdateVisualDisplay();
    }

    private void AdvanceSymbols()
    {
        currentSymbolIndex = WrapSymbolIndex(currentSymbolIndex + 1);
        UpdateVisualDisplay();
    }

    private void ApplySymbolToSlot(int slotIndex, int symbolIndex)
    {
        if (slotIndex < 0 || slotIndex >= symbolImages.Length || symbolImages[slotIndex] == null)
        {
            return;
        }

        SlotSymbol symbol = availableSymbols[symbolIndex];
        Image image = symbolImages[slotIndex];
        Text label = slotIndex < symbolLabels.Length ? symbolLabels[slotIndex] : null;
        Sprite sprite = symbolSprites != null && symbolIndex < symbolSprites.Length ? symbolSprites[symbolIndex] : null;

        image.sprite = sprite;
        image.preserveAspect = true;
        image.color = sprite != null ? Color.white : symbol.symbolColor;

        if (label != null)
        {
            label.text = sprite != null && !symbol.isWild ? string.Empty : GetSymbolLabel(symbol);
            label.color = Color.white;
        }
    }

    private string GetSymbolLabel(SlotSymbol symbol)
    {
        if (symbol == null || string.IsNullOrWhiteSpace(symbol.symbolName))
        {
            return "?";
        }

        return symbol.isWild ? "WILD" : symbol.symbolName.ToUpperInvariant();
    }

    private int WrapSymbolIndex(int index)
    {
        if (availableSymbols == null || availableSymbols.Length == 0)
        {
            return 0;
        }

        int count = availableSymbols.Length;
        return ((index % count) + count) % count;
    }
}
