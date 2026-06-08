using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class AudioOptions : MonoBehaviour
{
    public GameObject[] hideOnOpen;
    public AudioSource musicSource;
    public AudioSource sfxPreviewSource;
    public AudioClip sfxPreviewClip;

    private GameObject audioOptionsPanel;
    private Slider musicSlider;
    private Slider sfxSlider;
    private bool slidersBound;

    void Start()
    {
        EnsureReady();
    }

    public void OpenOptions()
    {
        if (!EnsureReady())
            return;

        audioOptionsPanel.SetActive(true);

        foreach (GameObject obj in hideOnOpen)
        {
            if (obj != null)
                obj.SetActive(false);
        }
    }

    public void CloseOptions()
    {
        if (audioOptionsPanel == null)
            return;

        audioOptionsPanel.SetActive(false);

        foreach (GameObject obj in hideOnOpen)
        {
            if (obj != null)
                obj.SetActive(true);
        }
    }

    void OnMusicChanged(float value)
    {
        AudioManager.Instance?.SetMusicVolume(value);

        if (musicSource != null)
        {
            musicSource.volume = value * 0.6f;

            if (value > 0.001f && musicSource.clip != null && !musicSource.isPlaying)
                musicSource.Play();
        }
    }

    void OnSfxChanged(float value)
    {
        AudioManager.Instance?.SetSfxVolume(value);

        if (musicSource != null)
        {
            musicSource.volume = (AudioManager.Instance != null ? AudioManager.Instance.MusicVolume : 1f) * 0.6f;

            if (musicSource.volume > 0.001f && musicSource.clip != null && !musicSource.isPlaying)
                musicSource.Play();
        }

        if (value > 0.001f && sfxPreviewSource != null && sfxPreviewClip != null)
        {
            sfxPreviewSource.Stop();
            sfxPreviewSource.PlayOneShot(sfxPreviewClip, value);
        }
    }

    void EnsureAudioManager()
    {
        if (AudioManager.Instance == null)
            gameObject.AddComponent<AudioManager>();
    }

    bool EnsureReady()
    {
        EnsureAudioManager();
        EnsureCanvasReferences();
        EnsureOptionsButton();
        EnsureOptionsPanel();
        SyncSliderValues();
        BindSliderListeners();
        RefreshMusicVolume();

        if (audioOptionsPanel != null)
            audioOptionsPanel.SetActive(false);

        return audioOptionsPanel != null && musicSlider != null && sfxSlider != null;
    }

    void EnsureCanvasReferences()
    {
        if (hideOnOpen == null || hideOnOpen.Length == 0)
        {
            hideOnOpen = new[]
            {
                GameObject.Find("StartGame"),
                GameObject.Find("Options"),
                GameObject.Find("Quit")
            };
        }

        if (musicSource == null)
        {
            GameObject musicPlayer = GameObject.Find("MusicPlayer");
            if (musicPlayer != null)
                musicSource = musicPlayer.GetComponent<AudioSource>();
        }

        if (musicSource != null)
        {
            musicSource.loop = true;
            musicSource.playOnAwake = true;
            musicSource.spatialBlend = 0f;
            musicSource.ignoreListenerPause = true;

            if (musicSource.volume <= 0f)
                musicSource.volume = 0.6f;

            if (musicSource.clip != null && !musicSource.isPlaying)
                musicSource.Play();
        }

        if (sfxPreviewSource == null)
            sfxPreviewSource = gameObject.AddComponent<AudioSource>();

        sfxPreviewSource.playOnAwake = false;
        sfxPreviewSource.loop = false;
        sfxPreviewSource.spatialBlend = 0f;
        sfxPreviewSource.ignoreListenerPause = true;

#if UNITY_EDITOR
        if (sfxPreviewClip == null)
            sfxPreviewClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SpielenButton.mp3");
#endif
    }

    void EnsureOptionsButton()
    {
        GameObject optionsButtonObject = GameObject.Find("Options");
        if (optionsButtonObject == null)
            return;

        Button optionsButton = optionsButtonObject.GetComponent<Button>();
        if (optionsButton == null)
            optionsButton = optionsButtonObject.AddComponent<Button>();

        Graphic graphic = optionsButtonObject.GetComponent<Graphic>();
        if (graphic != null)
            optionsButton.targetGraphic = graphic;

        ColorBlock colors = optionsButton.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.selectedColor = colors.highlightedColor;
        optionsButton.colors = colors;
        optionsButton.transition = Selectable.Transition.ColorTint;
        optionsButton.interactable = true;

        Navigation navigation = optionsButton.navigation;
        navigation.mode = Navigation.Mode.None;
        optionsButton.navigation = navigation;

        optionsButton.onClick.RemoveAllListeners();
        optionsButton.onClick.AddListener(OpenOptions);
    }

    void EnsureOptionsPanel()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        GameObject optionsButtonObject = GameObject.Find("Options");
        TMP_Text styleText = FindFirstObjectByType<TMP_Text>();
        Image styleImage = optionsButtonObject != null ? optionsButtonObject.GetComponent<Image>() : null;

        if (canvas == null || styleText == null)
            return;

        if (audioOptionsPanel == null)
        {
            Transform existingPanel = canvas.transform.Find("AudioOptionsPanel");
            if (existingPanel != null)
            {
                audioOptionsPanel = existingPanel.gameObject;
                musicSlider = audioOptionsPanel.transform.Find("MusicSlider")?.GetComponent<Slider>();
                sfxSlider = audioOptionsPanel.transform.Find("SfxSlider")?.GetComponent<Slider>();
            }
        }

        if (audioOptionsPanel != null)
            PositionOptionsPanel();

        if (audioOptionsPanel != null && musicSlider != null && sfxSlider != null)
            return;

        audioOptionsPanel = new GameObject("AudioOptionsPanel", typeof(RectTransform), typeof(Image));
        audioOptionsPanel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = audioOptionsPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(640f, 430f);

        Image panelImage = audioOptionsPanel.GetComponent<Image>();
        if (styleImage != null)
        {
            panelImage.sprite = styleImage.sprite;
            panelImage.type = styleImage.type;
            panelImage.color = styleImage.color;
        }
        else
        {
            panelImage.color = new Color(0.35f, 0.2f, 0.09f, 0.96f);
        }

        CreateLabel(audioOptionsPanel.transform, "Title", "OPTIONS", new Vector2(0f, 154f), new Vector2(420f, 60f), 68f, styleText, true);
        CreateLabel(audioOptionsPanel.transform, "MusicLabel", "Music Volume", new Vector2(0f, 66f), new Vector2(360f, 46f), 34f, styleText, false);
        musicSlider = CreateSlider(audioOptionsPanel.transform, "MusicSlider", new Vector2(0f, 18f), styleImage);
        CreateLabel(audioOptionsPanel.transform, "SfxLabel", "SFX Volume", new Vector2(0f, -54f), new Vector2(360f, 46f), 34f, styleText, false);
        sfxSlider = CreateSlider(audioOptionsPanel.transform, "SfxSlider", new Vector2(0f, -102f), styleImage);

        Button backButton = CreateButton(audioOptionsPanel.transform, "BackButton", "BACK", new Vector2(0f, -170f), styleImage, styleText);
        backButton.onClick.AddListener(CloseOptions);
    }

    void PositionOptionsPanel()
    {
        RectTransform panelRect = audioOptionsPanel.GetComponent<RectTransform>();
        if (panelRect == null)
            return;

        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(640f, 430f);
    }

    void SyncSliderValues()
    {
        if (musicSlider == null || sfxSlider == null || AudioManager.Instance == null)
            return;

        musicSlider.SetValueWithoutNotify(AudioManager.Instance.MusicVolume);
        sfxSlider.SetValueWithoutNotify(AudioManager.Instance.SfxVolume);
    }

    void BindSliderListeners()
    {
        if (slidersBound || musicSlider == null || sfxSlider == null)
            return;

        musicSlider.onValueChanged.AddListener(OnMusicChanged);
        sfxSlider.onValueChanged.AddListener(OnSfxChanged);
        slidersBound = true;
    }

    void RefreshMusicVolume()
    {
        if (musicSource == null || AudioManager.Instance == null)
            return;

        musicSource.volume = AudioManager.Instance.MusicVolume * 0.6f;

        if (musicSource.volume > 0.001f && musicSource.clip != null && !musicSource.isPlaying)
            musicSource.Play();
    }

    TMP_Text CreateLabel(Transform parent, string objectName, string text, Vector2 anchoredPosition, Vector2 sizeDelta, float fontSize, TMP_Text styleSource, bool bold)
    {
        GameObject labelObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(parent, false);

        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.font = styleSource.font;
        label.fontSharedMaterial = styleSource.fontSharedMaterial;
        label.color = styleSource.color;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        label.textWrappingMode = TextWrappingModes.NoWrap;

        return label;
    }

    Slider CreateSlider(Transform parent, string objectName, Vector2 anchoredPosition, Image styleImage)
    {
        GameObject sliderObject = new GameObject(objectName, typeof(RectTransform), typeof(Slider));
        sliderObject.transform.SetParent(parent, false);

        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.5f, 0.5f);
        sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
        sliderRect.pivot = new Vector2(0.5f, 0.5f);
        sliderRect.anchoredPosition = anchoredPosition;
        sliderRect.sizeDelta = new Vector2(380f, 32f);

        GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(sliderObject.transform, false);
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;
        Image backgroundImage = background.GetComponent<Image>();
        backgroundImage.color = new Color(0.24f, 0.14f, 0.07f, 1f);

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObject.transform, false);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(12f, 7f);
        fillAreaRect.offsetMax = new Vector2(-12f, -7f);

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        Image fillImage = fill.GetComponent<Image>();
        fillImage.color = new Color(0.94f, 0.82f, 0.42f, 1f);

        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(sliderObject.transform, false);
        RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(12f, 0f);
        handleAreaRect.offsetMax = new Vector2(-12f, 0f);

        GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(28f, 44f);
        Image handleImage = handle.GetComponent<Image>();
        if (styleImage != null)
        {
            handleImage.sprite = styleImage.sprite;
            handleImage.type = styleImage.type;
            handleImage.color = Color.white;
        }
        else
        {
            handleImage.color = new Color(0.92f, 0.92f, 0.92f, 1f);
        }

        Slider slider = sliderObject.GetComponent<Slider>();
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;

        return slider;
    }

    Button CreateButton(Transform parent, string objectName, string labelText, Vector2 anchoredPosition, Image styleImage, TMP_Text styleText)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(300f, 92f);

        Image image = buttonObject.GetComponent<Image>();
        if (styleImage != null)
        {
            image.sprite = styleImage.sprite;
            image.type = styleImage.type;
            image.color = styleImage.color;
        }
        else
        {
            image.color = Color.white;
        }

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        CreateLabel(buttonObject.transform, "Label", labelText, new Vector2(0f, 0f), new Vector2(280f, 60f), 42f, styleText, false);

        return button;
    }
}
