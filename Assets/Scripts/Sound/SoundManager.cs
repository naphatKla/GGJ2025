using System;
using UnityEngine;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEngine.UI;

public class SoundManager : MMSingleton<SoundManager>
{
    [Title("UI Sliders")]
    public GameObject settingMenu;
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;

    private MMSoundManager _mmSoundManager;
    

    protected override void Awake()
    {
        DontDestroyOnLoad(gameObject);
        _mmSoundManager = FindObjectOfType<MMSoundManager>();
        
        //Get
        settingMenu = GameObject.FindGameObjectWithTag("Menu");
        masterSlider = settingMenu.transform.Find("MasterSlider").gameObject.GetComponent<Slider>();
        musicSlider = settingMenu.transform.Find("MusicSlider").gameObject.GetComponent<Slider>();
        sfxSlider = settingMenu.transform.Find("SfxSlider").gameObject.GetComponent<Slider>();
        
        
        masterSlider.value = 1f;
        musicSlider.value = 1f;
        sfxSlider.value = 1f;
            
        _mmSoundManager.SetVolumeMaster(masterSlider.value);
        _mmSoundManager.SetVolumeMusic(musicSlider.value);
        _mmSoundManager.SetVolumeSfx(sfxSlider.value);
        
        masterSlider.onValueChanged.AddListener(SetMasterVolume);
        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(SetSfxVolume);
    }

    /*public void Start()
    {
        //Get
        settingMenu = GameObject.FindGameObjectWithTag("Menu");
        masterSlider = settingMenu.transform.Find("MasterSlider").gameObject.GetComponent<Slider>();
        musicSlider = settingMenu.transform.Find("MusicSlider").gameObject.GetComponent<Slider>();
        sfxSlider = settingMenu.transform.Find("SfxSlider").gameObject.GetComponent<Slider>();
        
        
        masterSlider.value = 1f;
        musicSlider.value = 1f;
        sfxSlider.value = 1f;
            
        _mmSoundManager.SetVolumeMaster(masterSlider.value);
        _mmSoundManager.SetVolumeMusic(musicSlider.value);
        _mmSoundManager.SetVolumeSfx(sfxSlider.value);
        
        masterSlider.onValueChanged.AddListener(SetMasterVolume);
        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(SetSfxVolume);
    }*/
    


    private void SetMasterVolume(float value)
    {
        masterSlider.value = value;
        _mmSoundManager.SetVolumeMaster(value);
    }
    
    private void SetMusicVolume(float value)
    {
        musicSlider.value = value;
        _mmSoundManager.SetVolumeMusic(value);
    }
    
    private void SetSfxVolume(float value)
    {
        sfxSlider.value = value;
        _mmSoundManager.SetVolumeSfx(value);
    }
    
}
