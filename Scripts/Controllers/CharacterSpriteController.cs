using UnityEngine;
using System.Collections.Generic;

public class CharacterSpriteController : MonoBehaviour
{
    Dictionary<Character, GameObject> characterGameObjectMap;

    Dictionary<string, Sprite> characterSprites;

    World world
    {
        get
        {
            return WorldController.Instance.world;
        }
    }


    // Use this for initialization
    void Start ()
    {
        LoadSprites();

        world.RegisterCharacterCreated(OnCharacterCreated);

        // Instantiate our dictionary that tracks which GameObject is rendering which Tile data.
        characterGameObjectMap = new Dictionary<Character, GameObject>();

        //If world was loaded from a save file, there will be existing characters.
        foreach(Character c in world.characterList)
        {
            OnCharacterCreated(c);
        }
    }
    void LoadSprites()
    {
        characterSprites = new Dictionary<string, Sprite>();
        Sprite[] sprites = Resources.LoadAll<Sprite>("Images/Characters/");

       // Debug.Log("LOADED RESOURCE:");
        foreach (Sprite s in sprites)
        {
            //Debug.Log(s);
            characterSprites[s.name] = s;
        }
    }

    public void OnCharacterCreated(Character character)
    {
        // Create a visual GameObject linked to this data.

        // This creates a new GameObject and adds it to our scene.
        GameObject char_go = new GameObject();

        // Add our tile/GO pair to the dictionary.
        characterGameObjectMap.Add(character, char_go);

        char_go.name = "Character";
        char_go.transform.position = new Vector3(character.X, character.Y, 0);
        char_go.transform.SetParent(this.transform, true);

        SpriteRenderer sr = char_go.AddComponent<SpriteRenderer>();
        sr.sprite = characterSprites["p1_front"]; //GetSpriteForCharacter(character);
        sr.sortingLayerName = "Characters";

        // Register our callback so that our GameObject gets updated whenever
        // the object's into changes.
        character.RegisterCharacterChangedCallback(OnCharacterChanged);

    }

    void OnCharacterChanged(Character character)
    {
        //Debug.Log("OnFurnitureChanged");
        // Make sure the furniture's graphics are correct.

        if (characterGameObjectMap.ContainsKey(character) == false)
        {
            Debug.LogError("OnCharacterChanged -- trying to change visuals for Character not in our map.");
            return;
        }

        GameObject char_go = characterGameObjectMap[character];

        //char_go.GetComponent<SpriteRenderer>().sprite = GetSpriteForCharacter(character);
        char_go.transform.position = new Vector3(character.X, character.Y, 0);
    }
}
