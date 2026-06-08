using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    public AudioClip introMusic;
    public AudioClip buttonSound;

    private AudioSource musicSource;
    private AudioSource sfxSource;
    private VisualElement mainPanel;
    private VisualElement settingsPanel;

    void Start()
    {
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.clip = introMusic;
        musicSource.loop = true;
        float vol = AudioManager.Instance != null ? AudioManager.Instance.MusicVolume : 1f;
        musicSource.volume = vol * 0.6f;
        if (introMusic != null) musicSource.Play();

        sfxSource = gameObject.AddComponent<AudioSource>();

        var root = GetComponent<UIDocument>().rootVisualElement;

        mainPanel = root.Q<VisualElement>("MainPanel");
        settingsPanel = root.Q<VisualElement>("SettingsPanel");

        settingsPanel.style.display = DisplayStyle.None;

        root.Q<Button>("StartButton").clicked += OnStart;
        root.Q<Button>("SettingsButton").clicked += () => ShowSettings(true);
        root.Q<Button>("QuitButton").clicked += () => Application.Quit();
        root.Q<Button>("BackButton").clicked += () => ShowSettings(false);

        var musicSlider = root.Q<Slider>("MusicSlider");
        var sfxSlider = root.Q<Slider>("SfxSlider");

        if (AudioManager.Instance != null)
        {
            musicSlider.value = AudioManager.Instance.MusicVolume;
            sfxSlider.value = AudioManager.Instance.SfxVolume;
        }

        musicSlider.RegisterValueChangedCallback(evt =>
        {
            AudioManager.Instance?.SetMusicVolume(evt.newValue);
            musicSource.volume = evt.newValue * 0.6f;
        });

        sfxSlider.RegisterValueChangedCallback(evt =>
        {
            AudioManager.Instance?.SetSfxVolume(evt.newValue);
        });
    }

    void OnStart()
    {
        PlayClick();
        SceneManager.LoadScene("Gameplay");
    }

    void ShowSettings(bool show)
    {
        PlayClick();
        mainPanel.style.display = show ? DisplayStyle.None : DisplayStyle.Flex;
        settingsPanel.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
    }

    void PlayClick()
    {
        if (sfxSource != null && buttonSound != null)
            sfxSource.PlayOneShot(buttonSound);
    }
}
