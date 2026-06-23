using UnityEngine;

/// <summary>
/// Represents a single slot-machine symbol and its payout data.
/// </summary>
[System.Serializable]
public class SlotSymbol
{
    public string symbolName;
    public int payoutMultiplier;
    public Color symbolColor;
    public bool isWild;
}
