# Lucky Slots

Lucky Slots is a playable 3-reel Unity slot machine game built for Unity 2022.3 LTS or newer. The player starts with 1000 coins, chooses a bet, spins three reels, and wins when the middle payline symbols match. Wild symbols can match any symbol.

## Game Overview

- Three vertical reels with top, middle, and bottom visible symbols.
- Middle row is the payline.
- Starting balance is 1000 coins.
- Bet options are 10, 20, 50, and 100 coins.
- Winning payout is `bet x symbol multiplier`.
- Losing spins deduct the current bet.
- Game over panel appears when balance reaches 0.

## Symbols and Payouts

- Cherry: 2x
- Lemon: 3x
- Orange: 4x
- Bell: 5x
- Seven: 10x
- Wild: matches any symbol

## Bonus Features

- Auto-spin for 5 spins.
- Paytable popup.
- Sound toggle.
- Spin counter.
- Win flash and symbol highlight effects.
- Restart button after game over.

## Instructions to Run in Unity

1. Open Unity Hub.
2. Add this project folder from disk.
3. Open the project with Unity 2022.3 LTS or newer.
4. Open `Assets/SlotMachine.unity`.
5. Press Play.

## Instructions to Run WebGL Build

After a WebGL build is generated, open the `Build/WebGL` folder with a local web server. Do not open `index.html` directly from Finder because browsers often block local WebGL files.

From the repository root, run:

```bash
python3 -m http.server 8000 --directory Build/WebGL
```

Then open:

```text
http://localhost:8000
```

## Creating the WebGL Build

1. In Unity, open `File > Build Profiles` or `File > Build Settings`.
2. Select `WebGL`.
3. Click `Switch Platform` if needed.
4. Set the output folder to `Build/WebGL`.
5. Click `Build`.

## Approach

The project uses separate object-oriented C# scripts for game state, reels, slot symbols, UI, and the main slot machine controller. A small bootstrap script builds the required scene hierarchy at runtime so the game can run from a clean scene while keeping the required folders and scripts organized.
