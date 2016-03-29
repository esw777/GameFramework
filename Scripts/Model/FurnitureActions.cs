using UnityEngine;
using System.Collections;

public static class FurnitureActions
{
    public static void Door_UpdateAction(Furniture furn, float deltaTime)
    {
        //Debug.Log("Door_UpdateAction called");

        if (furn.GetParameter("is_opening") >= 1)
        {
            furn.ChangeParameter("openness", deltaTime * 4);

            if (furn.GetParameter("openness") >= 1) //Stay open for a second
            {
                //Start closing the door.
                furn.SetParameter("is_opening", 0);
            }
        }

        else
        {
            furn.ChangeParameter("openness", -1 * deltaTime * 4);
        }

        furn.SetParameter("openness", Mathf.Clamp01(furn.GetParameter("openness")));

        //TODO this gets called every frame - bad
        if (furn.cbOnChanged != null)
        {
            furn.cbOnChanged(furn);
        }
    }

    public static Enterability Door_IsEnterable(Furniture furn)
    {
        //If this is called, then something wants to enter the same tile as the door. So open the door
        furn.SetParameter("is_opening", 1);

        if (furn.GetParameter("openness") >= 1)
        {
            return Enterability.Yes;
        }

        return Enterability.Soon;
    }
}
