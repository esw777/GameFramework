using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class UpdateStatsTypeCharJob : MonoBehaviour
{
    //This script runs every frame.
    //It identifies the tile currently under the mouse and updates 
    //  the text parameter of the UI component it is attached to.

    Text myText;
    Character myChar;
    WorldController worldController;

    // Use this for initialization
    void Start()
    {
        myText = GetComponent<Text>();

        if (myText == null)
        {
            Debug.LogError("MouseOverTileTypeText: No Text on the attached UI component.");
            this.enabled = false;
            return;
        }

        worldController = GameObject.FindObjectOfType<WorldController>();

        if (worldController != null)
        {
            if (worldController.world.characterList.Count > 0)
                myChar = worldController.world.characterList[0];
        }
    }

    // Update is called once per frame
    void Update()
    {
        /*
        if (myChar.getMyJob() != null)
            myText.text = "CharJob: " + myChar.getMyJob().jobObjectType;// + ": " + myChar.inventory.stackSize.ToString();
        else
            myText.text = "Null";
            */
    }
}
