using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameManager : MonoBehaviour
{
    [SerializeField] private string gameplaySceneName = "Gameplay";
    [SerializeField] private string menuSceneName = "Startscreen";
    [SerializeField] private string startButtonObjectName = "StartGame";
    [SerializeField] private string quitButtonObjectName = "Quit";
    [SerializeField] private AudioClip startButtonClip;
    [SerializeField] private float startButtonDelay = 0.18f;

    private AudioSource buttonAudioSource;
    private bool isLoadingGame;

    void Awake()
    {
        if (AudioManager.Instance == null)
            gameObject.AddComponent<AudioManager>();

        EnsureButtonAudio();
    }

    void Start()
    {
        ResolveAudioReferences();
        WireButton(startButtonObjectName, OnStartPressed);
        WireButton(quitButtonObjectName, QuitGame);
    }

    public void LoadGame()
    {
        SceneManager.LoadScene(gameplaySceneName);
    }

    public void LoadMenu()
    {
        SceneManager.LoadScene(menuSceneName);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnStartPressed()
    {
        if (isLoadingGame)
            return;

        StartCoroutine(LoadGameAfterButtonSound());
    }

    private IEnumerator LoadGameAfterButtonSound()
    {
        isLoadingGame = true;

        float delay = 0f;

        if (buttonAudioSource != null && startButtonClip != null)
        {
            float volume = AudioManager.Instance != null ? AudioManager.Instance.SfxVolume : 1f;
            buttonAudioSource.Stop();
            buttonAudioSource.PlayOneShot(startButtonClip, volume);
            delay = Mathf.Min(startButtonDelay, startButtonClip.length);
        }

        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        LoadGame();
    }

    private void WireButton(string objectName, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = GameObject.Find(objectName);
        if (buttonObject == null)
            return;

        Button button = buttonObject.GetComponent<Button>();
        if (button == null)
            button = buttonObject.AddComponent<Button>();

        Graphic graphic = buttonObject.GetComponent<Graphic>();
        if (graphic != null)
            button.targetGraphic = graphic;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
        button.transition = Selectable.Transition.ColorTint;
        button.interactable = true;
        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(onClick);
    }

    private void EnsureButtonAudio()
    {
        buttonAudioSource = GetComponent<AudioSource>();
        if (buttonAudioSource == null)
            buttonAudioSource = gameObject.AddComponent<AudioSource>();

        buttonAudioSource.playOnAwake = false;
        buttonAudioSource.loop = false;
        buttonAudioSource.spatialBlend = 0f;
        buttonAudioSource.ignoreListenerPause = true;
    }

    private void ResolveAudioReferences()
    {
#if UNITY_EDITOR
        if (startButtonClip == null)
            startButtonClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SpielenButton.mp3");
#endif
    }
}
