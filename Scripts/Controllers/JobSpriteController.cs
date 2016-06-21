using UnityEngine;
using System.Collections.Generic;

public class JobSpriteController : MonoBehaviour
{
    //Does not do much at the moment. Once more jobs and a full job system is in place, it will do more.

    FurnitureSpriteController fsc;

    Dictionary<Job, GameObject> jobGameObjectMap;

	// Use this for initialization
	void Start ()
    {
        fsc = GameObject.FindObjectOfType<FurnitureSpriteController>();
        jobGameObjectMap = new Dictionary<Job, GameObject>();

        //TODO needs to register with a JobQueue 
        WorldController.Instance.world.jobQueue.RegisterJobCreationCallback(OnJobCreated);
	
	}
	
    void OnJobCreated(Job job)
    {
        if (job.jobObjectType == null)
        {
            //Job does not have an assosiated sprite (build something in a workshop, hauling.)
            return;
        }
        
        //TODO sprite
        //Sprite theSprite = fsc.GetSpriteForFurniture(job.jobObjectType);

        GameObject job_go = new GameObject();

        // Add our tile/GO pair to the dictionary.
        jobGameObjectMap.Add(job, job_go);

        job_go.name = "JOB_" + job.jobObjectType + "_" + job.tile.X + "_" + job.tile.Y;
        job_go.transform.position = new Vector3(job.tile.X + ((job.furniturePrototype.Width-1)/2f), job.tile.Y + ((job.furniturePrototype.Height-1)/2f), 0);
        job_go.transform.SetParent(this.transform, true);

        SpriteRenderer sr = job_go.AddComponent<SpriteRenderer>();
        sr.sprite = fsc.GetSpriteForFurniture(job.jobObjectType);
        //Make it 30% transparent and tint green
        sr.color = new Color(0.5f, 1f, 0.5f, 0.3f);
        sr.sortingLayerName = "Jobs";

        //TODO remove hardcoding like this
        if (job.jobObjectType == "Door")
        {
            //Check to see if door needs to be rotated 90 degrees to NS from EW
            Tile northTile = World.current.GetTileAt(job.tile.X, job.tile.Y + 1);
            Tile southTile = World.current.GetTileAt(job.tile.X, job.tile.Y - 1);

            if (northTile != null && southTile != null && northTile.furniture != null && southTile.furniture != null &&
                    northTile.furniture.objectType == "Wall" && southTile.furniture.objectType == "Wall")
            {
                job_go.transform.rotation = Quaternion.Euler(0, 0, 90);
            }
        }

        //TODO Only does furniture atm.
        job.RegisterJobCompletedCallback(OnJobEnded);
        job.RegisterJobStoppedCallback(OnJobEnded);

    }

    void OnJobEnded(Job j)
    {
        //TODO Only does furniture atm.
        //Called when job is completed or cancled.

        //TODO delete sprite 

        GameObject job_go = jobGameObjectMap[j];

        j.UnregisterJobCompletedCallback(OnJobEnded);
        j.UnregisterJobStoppedCallback(OnJobEnded);

        Destroy(job_go);

    }
}
