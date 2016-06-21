using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class FurnitureBuildMenu : MonoBehaviour
{
    //Linked to by editor
    public GameObject buildFurniturePrefab;

	// Use this for initialization
	void Start ()
    {
        //Add a button for building each type of furniture.

        BuildModeController bmc = GameObject.FindObjectOfType<BuildModeController>();

        foreach (string s in World.current.furniturePrototypes.Keys)
        {
            GameObject go = Instantiate(buildFurniturePrefab);
            go.transform.SetParent(this.transform);

            go.name = "btn_Build" + s;

            go.transform.GetComponentInChildren<Text>().text = "Build " + s;

            Button b = go.GetComponent<Button>();

            string objectId = s;
            b.onClick.AddListener(delegate { bmc.SetMode_BuildFurniture(objectId); });
        }
	
	}
}
