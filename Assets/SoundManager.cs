using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

    }

    private void Start()
    {
        foreach (var elem in soundDictElements)
        {
            soundDict[elem._key] = elem._value;
        }
        if (soundDictElements.Count == soundDict.Count) Debug.Log("Diccionario de sonidos completo");
        else Debug.Log("Diccionario de sonidos incompleto  ->  Alguna key est? repetida");

    }


    [Header("Sounds")]
    public List<DictionaryElement<SoundName, AudioClip>> soundDictElements = new List<DictionaryElement<SoundName, AudioClip>>();
    Dictionary<SoundName, AudioClip> soundDict = new Dictionary<SoundName, AudioClip>();

    [Header("Reproductores")]
    [SerializeField] AudioSource musicSource;
    [SerializeField] AudioSource sfxSource;

    public void PlaySFX(int s_name) => PlaySFX((SoundName)s_name);
    public void PlaySFX(SoundName s_name)
    {
        // if (soundDict.ContainsKey(s_name) && soundDict[s_name] != null)
        // {
        //     // Usar pool para reproducir m�ltiples sonidos simult�neamente
        //     sfxSource.resource = soundDict[s_name];
        // }

        sfxSource.PlayOneShot(soundDict[s_name]);
    }

    public void PlayMusic(int s_name) => PlaySFX((SoundName)s_name);
    public void PlayMusic(SoundName s_name)
    {
        if (soundDict.ContainsKey(s_name) && soundDict[s_name] != null)
        {
            // Usar pool para reproducir m�ltiples sonidos simult�neamente
            //musicSource.Play(soundDict[s_name]);
            musicSource.resource = soundDict[s_name];
        }
    }

    public void PlayBGM(int s_name) => PlaySFX((SoundName)s_name);
    public void PlayBGM(SoundName s_name)
    {
        if (musicSource && soundDict[s_name]) musicSource.PlayOneShot(soundDict[s_name]);
    }

}

//-----------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------

[Serializable]
public struct DictionaryElement<T, K>
{
    public T _key;
    public K _value;
}

public enum SoundName
{
    Ruta3,
    Lavanda,
    Rocket,
    CaminoVictoria,
    GoTour,
    Masara,
    Encounter,
    PC_ON,
    PC_OFF,
    PC_Button1,
    PC_Button2,
    PC_Button3,
    PC_Error,
    PokeballThrow,
    PokeballHit,
    Pokeball1,
    Pokeball2,
    Pokeball3,
    PokeballSuccess,
    PokeballFlee
    }
