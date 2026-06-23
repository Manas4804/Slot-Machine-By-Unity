using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Builds the required slot-machine scene hierarchy at runtime when no scene has been assembled.
/// This keeps the empty repository immediately playable after opening it in Unity.
/// </summary>
public class SlotMachineBootstrap : MonoBehaviour
{
    private const string ArtPath = "SlotMachineArt/";
    private static bool hasBootstrapped;
    private static Font defaultFont;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapOnLoad()
    {
        if (hasBootstrapped || FindObjectOfType<SlotMachine>() != null)
        {
            return;
        }

        GameObject bootstrapObject = new GameObject("SlotMachineBootstrap");
        bootstrapObject.AddComponent<SlotMachineBootstrap>().BuildScene();
        hasBootstrapped = true;
    }

    public void BuildScene()
    {
        EnsureEventSystem();
        EnsureCamera();
        EnsureAudioListener();
        defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject gameManagerObject = new GameObject("GameManager");
        GameManager gameManager = gameManagerObject.AddComponent<GameManager>();
        SlotMachine slotMachine = gameManagerObject.AddComponent<SlotMachine>();

        AudioSources audioSources = CreateAudioManager();
        UIManager uiManager = CreateCanvasAndUI(out Reel[] reels);
        Sprite[] symbolSprites = LoadSymbolSprites();

        slotMachine.Configure(
            reels,
            uiManager,
            audioSources.spinSound,
            audioSources.winSound,
            audioSources.buttonClickSound,
            symbolSprites);

        gameManager.ResetGame();
        uiManager.UpdateAllDisplays();
    }

    private static UIManager CreateCanvasAndUI(out Reel[] reels)
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        UIManager uiManager = canvasObject.AddComponent<UIManager>();

        CreateBackground(canvasObject.transform);
        CreateTitle(canvasObject.transform);

        GameObject machineFrame = CreateImageObject("MachineFrame", canvasObject.transform, LoadSprite("slot-machine4"), Color.white);
        SetAnchoredRect(machineFrame, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-20f, 10f), new Vector2(800f, 624f));

        GameObject reelsContainer = CreateUIObject("ReelsContainer", canvasObject.transform);
        SetAnchoredRect(reelsContainer, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-20f, -4f), new Vector2(560f, 230f));
        HorizontalLayoutGroup reelLayout = reelsContainer.AddComponent<HorizontalLayoutGroup>();
        reelLayout.spacing = 24f;
        reelLayout.childAlignment = TextAnchor.MiddleCenter;
        reelLayout.childControlWidth = true;
        reelLayout.childControlHeight = true;
        reelLayout.childForceExpandWidth = true;
        reelLayout.childForceExpandHeight = true;

        reels = new Reel[3];
        for (int i = 0; i < reels.Length; i++)
        {
            reels[i] = CreateReel(reelsContainer.transform, i + 1);
        }

        GameObject payline = CreateImageObject("PaylineIndicator", canvasObject.transform, null, new Color(1f, 0f, 0f, 0.7f));
        SetAnchoredRect(payline, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-20f, -4f), new Vector2(610f, 5f));

        CreateInfoPanel(canvasObject.transform, uiManager);
        CreateBetPanel(canvasObject.transform, uiManager);
        CreateControlPanel(canvasObject.transform, uiManager);
        CreateWinPanel(canvasObject.transform, uiManager);
        CreateGameOverPanel(canvasObject.transform, uiManager);
        CreatePaytablePanel(canvasObject.transform, uiManager);

        return uiManager;
    }

    private static void CreateBackground(Transform parent)
    {
        GameObject background = CreateImageObject("Background", parent, LoadSprite("bg_gradient"), new Color(0.04f, 0.18f, 0.12f, 1f));
        RectTransform rect = background.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void CreateTitle(Transform parent)
    {
        Text title = CreateTextObject("TitleText", parent, "LUCKY SLOTS", 54, FontStyle.Bold, TextAnchor.MiddleCenter);
        title.color = new Color(1f, 0.78f, 0.16f);
        title.material = null;
        SetAnchoredRect(title.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(620f, 82f));
    }

    private static Reel CreateReel(Transform parent, int reelNumber)
    {
        GameObject reelObject = CreateUIObject($"Reel_{reelNumber}", parent);
        Image reelBackground = reelObject.AddComponent<Image>();
        reelBackground.color = new Color(0.79f, 0.97f, 1f, 0.95f);

        VerticalLayoutGroup layout = reelObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.spacing = 7f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        LayoutElement reelLayout = reelObject.AddComponent<LayoutElement>();
        reelLayout.preferredWidth = 150f;
        reelLayout.preferredHeight = 230f;

        Reel reel = reelObject.AddComponent<Reel>();

        Image topImage = CreateSymbolSlot(reelObject.transform, "Symbol_Top", out Text topText);
        Image middleImage = CreateSymbolSlot(reelObject.transform, "Symbol_Middle", out Text middleText);
        Image bottomImage = CreateSymbolSlot(reelObject.transform, "Symbol_Bottom", out Text bottomText);

        reel.SetSymbolSlots(topImage, topText, middleImage, middleText, bottomImage, bottomText);
        return reel;
    }

    private static Image CreateSymbolSlot(Transform parent, string name, out Text label)
    {
        GameObject slot = CreateImageObject(name, parent, null, Color.white);
        Image slotImage = slot.GetComponent<Image>();
        slotImage.type = Image.Type.Simple;

        LayoutElement layoutElement = slot.AddComponent<LayoutElement>();
        layoutElement.minHeight = 64f;
        layoutElement.preferredHeight = 68f;
        layoutElement.flexibleWidth = 1f;

        label = CreateTextObject("Text", slot.transform, string.Empty, 24, FontStyle.Bold, TextAnchor.MiddleCenter);
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 12;
        label.resizeTextMaxSize = 24;

        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        return slotImage;
    }

    private static void CreateInfoPanel(Transform parent, UIManager uiManager)
    {
        GameObject panel = CreatePanel("InfoPanel", parent, new Color(0.03f, 0.03f, 0.08f, 0.72f));
        SetAnchoredRect(panel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -126f), new Vector2(760f, 56f));

        HorizontalLayoutGroup layout = panel.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 8, 8);
        layout.spacing = 24f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childForceExpandWidth = true;

        uiManager.balanceText = CreatePanelText("BalanceText", panel.transform, "Balance: 1000");
        uiManager.betText = CreatePanelText("BetText", panel.transform, "Bet: 10");
        uiManager.winText = CreatePanelText("WinText", panel.transform, "Win: 0");
        uiManager.spinCountText = CreatePanelText("SpinCountText", panel.transform, "Spins: 0");
    }

    private static void CreateBetPanel(Transform parent, UIManager uiManager)
    {
        GameObject panel = CreatePanel("BetPanel", parent, new Color(0.04f, 0.04f, 0.1f, 0.78f));
        SetAnchoredRect(panel, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(238f, 78f), new Vector2(390f, 72f));

        HorizontalLayoutGroup layout = panel.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 10, 10);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childForceExpandWidth = true;

        uiManager.betButton10 = CreateButton("BetButton_10", panel.transform, "10");
        uiManager.betButton20 = CreateButton("BetButton_20", panel.transform, "20");
        uiManager.betButton50 = CreateButton("BetButton_50", panel.transform, "50");
        uiManager.betButton100 = CreateButton("BetButton_100", panel.transform, "100");
    }

    private static void CreateControlPanel(Transform parent, UIManager uiManager)
    {
        GameObject panel = CreatePanel("ControlPanel", parent, new Color(0.04f, 0.04f, 0.1f, 0.78f));
        SetAnchoredRect(panel, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-318f, 78f), new Vector2(560f, 72f));

        HorizontalLayoutGroup layout = panel.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 10, 10);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childForceExpandWidth = true;

        uiManager.spinButton = CreateButton("SpinButton", panel.transform, "SPIN");
        uiManager.autoSpinButton = CreateButton("AutoSpinButton", panel.transform, "AUTO x5");
        uiManager.paytableButton = CreateButton("PaytableButton", panel.transform, "?");
        uiManager.muteButton = CreateButton("MuteButton", panel.transform, "Sound");
        uiManager.muteButtonText = uiManager.muteButton.GetComponentInChildren<Text>();
    }

    private static void CreateWinPanel(Transform parent, UIManager uiManager)
    {
        GameObject panel = CreatePanel("WinPanel", parent, new Color(1f, 0.72f, 0.07f, 0.88f));
        SetAnchoredRect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -2f), new Vector2(430f, 130f));

        uiManager.winAmountText = CreateTextObject("WinAmountText", panel.transform, "WIN!", 42, FontStyle.Bold, TextAnchor.MiddleCenter);
        uiManager.winAmountText.color = Color.white;
        Stretch(uiManager.winAmountText.gameObject, new Vector2(20f, 20f), new Vector2(-20f, -20f));

        GameObject effect = CreateImageObject("WinEffectImage", panel.transform, null, new Color(1f, 1f, 1f, 0.18f));
        Stretch(effect, new Vector2(0f, 0f), new Vector2(0f, 0f));

        panel.SetActive(false);
        uiManager.winPanel = panel;

        uiManager.feedbackText = CreateTextObject("FeedbackText", parent, "Try Again", 34, FontStyle.Bold, TextAnchor.MiddleCenter);
        uiManager.feedbackText.color = Color.white;
        SetAnchoredRect(uiManager.feedbackText.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -190f), new Vector2(420f, 62f));
        uiManager.feedbackText.gameObject.SetActive(false);
    }

    private static void CreateGameOverPanel(Transform parent, UIManager uiManager)
    {
        GameObject panel = CreatePanel("GameOverPanel", parent, new Color(0f, 0f, 0f, 0.88f));
        SetAnchoredRect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 270f));

        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(28, 28, 30, 30);
        layout.spacing = 22f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;

        Text gameOverText = CreateTextObject("GameOverText", panel.transform, "GAME OVER", 44, FontStyle.Bold, TextAnchor.MiddleCenter);
        gameOverText.color = new Color(1f, 0.78f, 0.18f);
        gameOverText.gameObject.AddComponent<LayoutElement>().preferredHeight = 90f;

        uiManager.restartButton = CreateButton("RestartButton", panel.transform, "Restart");
        uiManager.restartButton.gameObject.GetComponent<LayoutElement>().preferredHeight = 64f;

        panel.SetActive(false);
        uiManager.gameOverPanel = panel;
    }

    private static void CreatePaytablePanel(Transform parent, UIManager uiManager)
    {
        GameObject panel = CreatePanel("PaytablePanel", parent, new Color(0.02f, 0.02f, 0.08f, 0.94f));
        SetAnchoredRect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(660f, 420f));

        Text content = CreateTextObject("PaytableContent", panel.transform, string.Empty, 26, FontStyle.Bold, TextAnchor.UpperLeft);
        content.color = Color.white;
        content.horizontalOverflow = HorizontalWrapMode.Wrap;
        content.verticalOverflow = VerticalWrapMode.Truncate;
        SetAnchoredRect(content.gameObject, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, -20f), new Vector2(-120f, -80f));
        content.GetComponent<RectTransform>().offsetMin = new Vector2(36f, 80f);
        content.GetComponent<RectTransform>().offsetMax = new Vector2(-36f, -32f);

        uiManager.closePaytableButton = CreateButton("CloseButton", panel.transform, "Close");
        SetAnchoredRect(uiManager.closePaytableButton.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 42f), new Vector2(150f, 52f));

        panel.SetActive(false);
        uiManager.paytablePanel = panel;
        uiManager.paytableContentText = content;
    }

    private static AudioSources CreateAudioManager()
    {
        GameObject audioManager = new GameObject("AudioManager");
        AudioSource spin = CreateAudioSource(audioManager.transform, "SpinSound", 220f, 0.16f, 0.18f);
        AudioSource win = CreateAudioSource(audioManager.transform, "WinSound", 660f, 0.35f, 0.22f);
        AudioSource click = CreateAudioSource(audioManager.transform, "ButtonClickSound", 440f, 0.08f, 0.16f);

        return new AudioSources
        {
            spinSound = spin,
            winSound = win,
            buttonClickSound = click
        };
    }

    private static AudioSource CreateAudioSource(Transform parent, string name, float frequency, float duration, float volume)
    {
        GameObject audioObject = new GameObject(name);
        audioObject.transform.SetParent(parent, false);

        AudioSource source = audioObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.clip = CreateToneClip(name, frequency, duration, volume);
        return source;
    }

    private static AudioClip CreateToneClip(string name, float frequency, float duration, float volume)
    {
        const int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float envelope = 1f - (i / (float)sampleCount);
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * volume * envelope;
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static Sprite[] LoadSymbolSprites()
    {
        return new[]
        {
            LoadSprite("slot-symbol2"),
            null,
            null,
            LoadSprite("slot-symbol3"),
            LoadSprite("slot-symbol1"),
            LoadSprite("slot-symbol4")
        };
    }

    private static Sprite LoadSprite(string assetName)
    {
        Texture2D texture = Resources.Load<Texture2D>(ArtPath + assetName);

        if (texture == null)
        {
            return null;
        }

        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static GameObject CreateImageObject(string name, Transform parent, Sprite sprite, Color color)
    {
        GameObject gameObject = CreateUIObject(name, parent);
        Image image = gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.preserveAspect = sprite != null;
        return gameObject;
    }

    private static GameObject CreatePanel(string name, Transform parent, Color color)
    {
        GameObject panel = CreateImageObject(name, parent, LoadSprite("popup"), color);
        Image image = panel.GetComponent<Image>();
        image.type = Image.Type.Simple;
        return panel;
    }

    private static Text CreateTextObject(string name, Transform parent, string text, int fontSize, FontStyle style, TextAnchor alignment)
    {
        GameObject textObject = CreateUIObject(name, parent);
        Text textComponent = textObject.AddComponent<Text>();
        textComponent.text = text;
        textComponent.font = defaultFont;
        textComponent.fontSize = fontSize;
        textComponent.fontStyle = style;
        textComponent.alignment = alignment;
        textComponent.color = Color.white;
        textComponent.raycastTarget = false;
        return textComponent;
    }

    private static Text CreatePanelText(string name, Transform parent, string text)
    {
        Text textComponent = CreateTextObject(name, parent, text, 23, FontStyle.Bold, TextAnchor.MiddleCenter);
        textComponent.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;
        return textComponent;
    }

    private static Button CreateButton(string name, Transform parent, string text)
    {
        GameObject buttonObject = CreateImageObject(name, parent, null, new Color(0.98f, 0.63f, 0.12f, 1f));
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonObject.GetComponent<Image>();

        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.98f, 0.63f, 0.12f, 1f);
        colors.highlightedColor = new Color(1f, 0.81f, 0.2f, 1f);
        colors.pressedColor = new Color(0.79f, 0.34f, 0.06f, 1f);
        colors.disabledColor = new Color(0.33f, 0.33f, 0.33f, 0.7f);
        button.colors = colors;

        LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
        layout.preferredWidth = 110f;
        layout.preferredHeight = 52f;
        layout.minHeight = 44f;

        Text label = CreateTextObject("Text", buttonObject.transform, text, 23, FontStyle.Bold, TextAnchor.MiddleCenter);
        label.color = Color.white;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 14;
        label.resizeTextMaxSize = 24;
        Stretch(label.gameObject, Vector2.zero, Vector2.zero);

        return button;
    }

    private static void SetAnchoredRect(GameObject gameObject, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    private static void Stretch(GameObject gameObject, Vector2 offsetMin, Vector2 offsetMax)
    {
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private static void EnsureCamera()
    {
        if (Camera.main != null || FindObjectOfType<Camera>() != null)
        {
            return;
        }

        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        cameraObject.tag = "MainCamera";
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.nearClipPlane = 0.3f;
        camera.farClipPlane = 1000f;
        camera.transform.position = new Vector3(0f, 0f, -10f);
    }

    private static void EnsureAudioListener()
    {
        if (FindObjectOfType<AudioListener>() != null)
        {
            return;
        }

        Camera camera = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();

        if (camera != null)
        {
            camera.gameObject.AddComponent<AudioListener>();
        }
    }

    private struct AudioSources
    {
        public AudioSource spinSound;
        public AudioSource winSound;
        public AudioSource buttonClickSound;
    }
}
