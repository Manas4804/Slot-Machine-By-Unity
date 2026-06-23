using System;
using UnityEngine;

public enum GameState
{
    Idle,
    Spinning,
    Win,
    GameOver
}

/// <summary>
/// Singleton that owns persistent game state for the slot machine.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Player State")]
    public int playerBalance = 1000;
    public int currentBet = 10;
    public int totalSpins;
    public int lastWinAmount;

    public GameState CurrentState { get; private set; } = GameState.Idle;

    private const int StartingBalance = 1000;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        unchecked
        {
            int seed = (int)DateTime.UtcNow.Ticks ^ Environment.TickCount ^ Guid.NewGuid().GetHashCode();
            UnityEngine.Random.InitState(seed);
        }
    }

    public void SetState(GameState newState)
    {
        CurrentState = newState;
    }

    public void SetBet(int betAmount)
    {
        if (CurrentState == GameState.Spinning || CurrentState == GameState.GameOver)
        {
            return;
        }

        currentBet = Mathf.Max(1, betAmount);
    }

    public void AddBalance(int amount)
    {
        playerBalance = Mathf.Max(0, playerBalance + amount);
    }

    public bool CanAffordCurrentBet()
    {
        return playerBalance >= currentBet;
    }

    public void RecordSpin()
    {
        totalSpins++;
    }

    public void ResetGame()
    {
        playerBalance = StartingBalance;
        currentBet = 10;
        totalSpins = 0;
        lastWinAmount = 0;
        CurrentState = GameState.Idle;
    }
}
