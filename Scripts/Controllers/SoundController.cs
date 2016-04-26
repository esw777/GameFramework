using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class SoundController : MonoBehaviour
{
    //How often sounds are allowed to be played.
    float soundCooldown = 0f;

    //TODO make different classes of sounds. Music, game sounds, master
    //Volume %
    float volume = 1f;

    Dictionary<string, AudioClip> soundClipDictionary;

    // Use this for initialization
    void Start()
    {
        soundClipDictionary = new Dictionary<string, AudioClip>();
        loadSounds();

        //Resgister callbacks. 
        WorldController.Instance.world.RegisterFurnitureCreated(OnFurnitureCreated);
        WorldController.Instance.world.RegisterTileChangedCallback(OnTileChanged);
    }

    // Update is called once per frame
    void Update()
    {
        soundCooldown -= Time.deltaTime;
    }

    //Load all sound clips into the dictionary
    void loadSounds()
    {
        DirectoryInfo dir = new DirectoryInfo("Assets/Resources/Sounds/");
        FileInfo[] info = dir.GetFiles("*.wav");
        foreach (FileInfo f in info)
        {
            //Prevent substring and file load errors.
            if (f.Name.Length > 4)
            {
                //Get name of file without 4 character extension (.wav)
                string fName = f.Name.Substring(0, f.Name.Length - 4);
                //Add sound to dictionary referenced by fileName
                soundClipDictionary.Add(fName, Resources.Load<AudioClip>("Sounds/" + fName));
            }
        }
    }

    //Used to set volume for all sounds.
    public void setVolume(float vol)
    {
        volume = vol;
    }

    void OnTileChanged(Tile tile_data)
    {
        if (soundCooldown > 0)
        {
            return;
        }

        AudioSource.PlayClipAtPoint(soundClipDictionary[(tile_data.Type.ToString() + "_OnCreate")], Camera.main.transform.position, volume);
        soundCooldown = 0.5f;
    }

    void OnFurnitureCreated(Furniture furn)
    {
        if (soundCooldown > 0)
        {
            return;
        }

        if (soundClipDictionary.ContainsKey(furn.objectType + "_OnCreate"))
        {
            AudioSource.PlayClipAtPoint(soundClipDictionary[(furn.objectType + "_OnCreate")], Camera.main.transform.position, volume);
            soundCooldown = 0.5f;
        }
        else
        {
            Debug.LogError("There is no sound for furniture_OnCreate for type: " + furn.objectType.ToString() + ".");
        }
    }
}
