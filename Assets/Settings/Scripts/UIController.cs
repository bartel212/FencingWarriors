using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public class UIController : MonoBehaviour
{
    public static UIController Instance;

    public int winsToWin = 3;
    public AudioClip battleMusic;
    public float roundResetDelay = 0.2f;
    public Sprite medievalBoardSprite;

    private AudioSource musicSource;
    private int p1Score;
    private int p2Score;
    private bool gameOver;
    private bool roundLocked;
    private TextMeshProUGUI p1HudLabel;
    private TextMeshProUGUI p2HudLabel;
    private GameObject hudWinPanel;
    private TextMeshProUGUI hudWinLabel;
    private TextMeshProUGUI hudStatsHeaderLabel;
    private TextMeshProUGUI hudStatsLabel;
    private Button playAgainButton;
    private Button mainMenuButton;
    private Player1Controls player1Stats;
    private Player2Controls player2Stats;

    public bool IsRoundLocked => roundLocked || gameOver;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            enabled = false;
            return;
        }

        Instance = this;

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.clip = battleMusic;
        musicSource.loop = true;
        float vol = AudioManager.Instance != null ? AudioManager.Instance.MusicVolume : 1f;
        musicSource.volume = vol * 0.6f;
        if (battleMusic != null)
            musicSource.Play();
    }

    void Start()
    {
        var doc = GetComponent<UnityEngine.UIElements.UIDocument>();
        if (doc != null)
            doc.enabled = false;

        EnsureEventSystem();
        BuildRuntimeHud();

        UpdateLabels();
    }

    public bool ResolveRound(int scoringPlayer, Player1Controls player1, Player2Controls player2)
    {
        if (IsRoundLocked)
            return false;

        player1Stats = player1;
        player2Stats = player2;
        roundLocked = true;
        AddScore(scoringPlayer);

        if (gameOver)
            return true;

        StartCoroutine(ResetRoundAfterDelay(player1, player2));
        return true;
    }

    public void AddScore(int player)
    {
        if (gameOver)
            return;

        if (player == 1)
            p1Score++;
        else
            p2Score++;

        UpdateLabels();

        if (p1Score >= winsToWin || p2Score >= winsToWin)
            StartCoroutine(ShowWinner(player));
    }

    void UpdateLabels()
    {
        if (p1HudLabel != null) p1HudLabel.text = $"Player 1: {p1Score}";
        if (p2HudLabel != null) p2HudLabel.text = $"Player 2: {p2Score}";
    }

    IEnumerator ShowWinner(int player)
    {
        gameOver = true;
        roundLocked = true;

        if (hudWinPanel != null)
        {
            hudWinPanel.SetActive(true);
            if (hudWinLabel != null)
                hudWinLabel.text = $"PLAYER {player} WINS";
            if (hudStatsLabel != null)
                hudStatsLabel.text = BuildStatsText(player);
        }
        
        yield break;
    }

    IEnumerator ResetRoundAfterDelay(Player1Controls player1, Player2Controls player2)
    {
        player1?.PrepareForRoundReset();
        player2?.PrepareForRoundReset();

        yield return new WaitForSeconds(roundResetDelay);

        player1?.Restart();
        player2?.Restart();
        roundLocked = false;
    }

    void BuildRuntimeHud()
    {
        var canvasObject = new GameObject("GameplayHUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        CreateScoreBackdrop(canvasObject.transform, "P1ScoreBackdrop", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -12f), new Vector2(285f, 58f));
        CreateScoreBackdrop(canvasObject.transform, "P2ScoreBackdrop", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -12f), new Vector2(285f, 58f));

        p1HudLabel = CreateHudLabel(canvasObject.transform, "P1Score", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -20f), TextAlignmentOptions.TopLeft);
        p2HudLabel = CreateHudLabel(canvasObject.transform, "P2Score", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-32f, -20f), TextAlignmentOptions.TopRight);
        StyleScoreLabel(p1HudLabel);
        StyleScoreLabel(p2HudLabel);

        hudWinPanel = new GameObject("WinPanel", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        hudWinPanel.transform.SetParent(canvasObject.transform, false);

        var panelRect = hudWinPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var panelImage = hudWinPanel.GetComponent<UnityEngine.UI.Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.6f);

        var board = CreateHudImage(hudWinPanel.transform, "MedievalBoard", medievalBoardSprite, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -6f), new Vector2(500f, 590f));

        hudWinLabel = CreateHudLabel(board.transform, "WinLabel", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 44f), TextAlignmentOptions.Center);
        hudWinLabel.rectTransform.sizeDelta = new Vector2(440f, 60f);
        hudWinLabel.fontSize = 38;
        hudWinLabel.enableAutoSizing = true;
        hudWinLabel.fontSizeMin = 24;
        hudWinLabel.fontSizeMax = 38;
        hudWinLabel.color = new Color(0.98f, 0.91f, 0.73f, 1f);
        hudWinLabel.outlineColor = new Color(0.11f, 0.05f, 0.02f, 1f);
        hudWinLabel.outlineWidth = 0.3f;

        hudStatsHeaderLabel = CreateHudLabel(board.transform, "StatsHeaderLabel", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -152f), TextAlignmentOptions.Center);
        hudStatsHeaderLabel.rectTransform.sizeDelta = new Vector2(280f, 36f);
        hudStatsHeaderLabel.fontSize = 20;
        hudStatsHeaderLabel.color = new Color(0.23f, 0.11f, 0.04f, 1f);
        hudStatsHeaderLabel.outlineWidth = 0f;
        hudStatsHeaderLabel.text = "WINNER STATS";

        hudStatsLabel = CreateHudLabel(board.transform, "StatsLabel", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -192f), TextAlignmentOptions.TopLeft);
        hudStatsLabel.rectTransform.sizeDelta = new Vector2(290f, 300f);
        hudStatsLabel.fontSize = 19;
        hudStatsLabel.enableAutoSizing = true;
        hudStatsLabel.fontSizeMin = 16;
        hudStatsLabel.fontSizeMax = 20;
        hudStatsLabel.color = new Color(0.23f, 0.11f, 0.04f, 1f);
        hudStatsLabel.outlineWidth = 0f;
        hudStatsLabel.lineSpacing = -2f;
        hudStatsLabel.textWrappingMode = TextWrappingModes.Normal;

        playAgainButton = CreateHudButton(board.transform, "PlayAgainButton", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-118f, 56f), new Vector2(180f, 52f), "PLAY AGAIN");
        playAgainButton.onClick.AddListener(RestartGameplayScene);

        mainMenuButton = CreateHudButton(board.transform, "MainMenuButton", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(118f, 56f), new Vector2(180f, 52f), "MAIN MENU");
        mainMenuButton.onClick.AddListener(ReturnToMainMenu);

        hudWinPanel.SetActive(false);
    }

    void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        DontDestroyOnLoad(eventSystemObject);
    }

    void CreateScoreBackdrop(Transform parent, string objectName, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        var backingObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        backingObject.transform.SetParent(parent, false);

        var rect = backingObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(anchorMax.x, anchorMax.y);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        var image = backingObject.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.72f);
    }

    void StyleScoreLabel(TextMeshProUGUI label)
    {
        if (label == null)
            return;

        label.rectTransform.sizeDelta = new Vector2(300f, 64f);
        label.fontSize = 38;
        label.color = new Color(1f, 0.94f, 0.58f, 1f);
        label.outlineColor = Color.black;
        label.outlineWidth = 0.45f;
    }

    TextMeshProUGUI CreateHudLabel(Transform parent, string objectName, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, TextAlignmentOptions alignment)
    {
        var labelObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(parent, false);

        var rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(anchorMax.x, anchorMax.y);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(420f, 80f);

        var label = labelObject.GetComponent<TextMeshProUGUI>();
        label.font = TMP_Settings.defaultFontAsset;
        label.fontSize = 34;
        label.fontStyle = FontStyles.Bold;
        label.alignment = alignment;
        label.color = Color.white;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.outlineColor = Color.black;
        label.outlineWidth = 0.2f;

        return label;
    }

    Image CreateHudImage(Transform parent, string objectName, Sprite sprite, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        var imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        var rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        var image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;

        if (sprite != null)
            sprite.texture.filterMode = FilterMode.Point;

        return image;
    }

    Button CreateHudButton(Transform parent, string objectName, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, string labelText)
    {
        var buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        var rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        var image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.29f, 0.15f, 0.06f, 0.92f);

        var button = buttonObject.GetComponent<Button>();
        var colors = button.colors;
        colors.normalColor = new Color(0.29f, 0.15f, 0.06f, 0.92f);
        colors.highlightedColor = new Color(0.38f, 0.2f, 0.09f, 0.98f);
        colors.pressedColor = new Color(0.22f, 0.11f, 0.04f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.6f);
        button.colors = colors;

        var label = CreateHudLabel(buttonObject.transform, "Label", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, TextAlignmentOptions.Center);
        label.rectTransform.sizeDelta = sizeDelta;
        label.fontSize = 22;
        label.enableAutoSizing = true;
        label.fontSizeMin = 16;
        label.fontSizeMax = 22;
        label.color = new Color(0.98f, 0.91f, 0.73f, 1f);
        label.outlineWidth = 0.18f;
        label.text = labelText;

        return button;
    }

    string BuildStatsText(int winner)
    {
        int p1Pokes = player1Stats != null ? player1Stats.PokeCount : 0;
        int p1Lunges = player1Stats != null ? player1Stats.LungeCount : 0;
        int p1Parries = player1Stats != null ? player1Stats.ParryCount : 0;

        int p2Pokes = player2Stats != null ? player2Stats.PokeCount : 0;
        int p2Lunges = player2Stats != null ? player2Stats.LungeCount : 0;
        int p2Parries = player2Stats != null ? player2Stats.ParryCount : 0;

        string playerOneBlock = BuildPlayerStatsBlock(1, p1Score, p1Pokes, p1Lunges, p1Parries);
        string playerTwoBlock = BuildPlayerStatsBlock(2, p2Score, p2Pokes, p2Lunges, p2Parries);

        string winnerBlock = winner == 1 ? playerOneBlock : playerTwoBlock;
        string challengerBlock = winner == 1 ? playerTwoBlock : playerOneBlock;

        return
            winnerBlock +
            "\n\n<align=center><size=20><b>OTHER FENCER</b></size></align>\n\n" +
            challengerBlock;
    }

    string BuildPlayerStatsBlock(int playerNumber, int touches, int pokes, int lunges, int parries)
    {
        return
            $"<align=center><size=24><b>PLAYER {playerNumber}</b></size></align>\n" +
            $"Touches   {touches}\n" +
            $"Pokes     {pokes}\n" +
            $"Lunges    {lunges}\n" +
            $"Parries   {parries}";
    }

    void RestartGameplayScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void ReturnToMainMenu()
    {
        SceneManager.LoadScene("Startscreen");
    }
}
