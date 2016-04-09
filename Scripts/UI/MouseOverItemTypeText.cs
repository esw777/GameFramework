using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class MouseOverItemTypeText : MonoBehaviour
{
    //This script runs every frame.
    //It identifies the tile currently under the mouse and updates 
    //  the text parameter of the UI component it is attached to.

    Text myText;
    MouseController mouseController;
    Tile tileUnderMouse;

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

        mouseController = GameObject.FindObjectOfType<MouseController>(); //TODO not the greatest way to access this.
        if (mouseController == null)
        {
            Debug.LogError("MouseController does not exist somehow");
            return;
        }
    }

    // Update is called once per frame
    void Update()
    {
        tileUnderMouse = mouseController.GetTileUnderMouse();

        if (tileUnderMouse == null || tileUnderMouse.inventory == null)
        {
            myText.text = "No Items";
        }

        else
        {
            myText.text = "Item Type: " + tileUnderMouse.inventory.objectType.ToString() + ", StackSize: " + tileUnderMouse.inventory.stackSize.ToString();
        }
    }
}
