using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    public float MusicVolume { get; private set; } = 1f;
    public float SfxVolume { get; private set; } = 1f;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        MusicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        SfxVolume = PlayerPrefs.GetFloat("SfxVolume", 1f);
    }

    public void SetMusicVolume(float value)
    {
        MusicVolume = value;
        PlayerPrefs.SetFloat("MusicVolume", value);
    }

    public void SetSfxVolume(float value)
    {
        SfxVolume = value;
        PlayerPrefs.SetFloat("SfxVolume", value);
    }
}
