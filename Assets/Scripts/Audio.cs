using System.Collections.Generic;
using UnityEngine;

public class Audio : MonoBehaviour {
    [SerializeField]
    private List<AudioClip> backgroundMusics;
    [SerializeField]
    private List<AudioClip> soundEffects;

    private AudioSource audioSource;
    private readonly Dictionary<string, AudioClip> backgroundMusicDict = new();
    private readonly Dictionary<string, AudioClip> soundEffectDict = new();

    public bool Mute { get { return audioSource.mute; } set { audioSource.mute = value; } }

    private void Awake() {
        audioSource = GetComponent<AudioSource>();
        DontDestroyOnLoad(gameObject);
        foreach (AudioClip audioClip in backgroundMusics) {
            backgroundMusicDict[audioClip.name] = audioClip;
        }
        foreach (AudioClip audioClip in soundEffects) {
            soundEffectDict[audioClip.name] = audioClip;
        }
    }

    public void ChangeBackgroundMusic(string audioClipName) {
        audioSource.clip = backgroundMusicDict[audioClipName];
        audioSource.Play();
    }

    public void StopBackgroundMusic() {
        audioSource.Stop();
    }

    public void PlaySoundEffect(string audioClipName) {
        audioSource.PlayOneShot(soundEffectDict[audioClipName]);
    }
}
