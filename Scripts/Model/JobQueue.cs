using UnityEngine;
using System.Collections.Generic;
using System;

public class JobQueue
{
    Queue<Job> jobQueue;

    Action<Job> cbJobCreated;

    public JobQueue()
    {
        jobQueue = new Queue<Job>();
    }

    // newJob = false when the job has been abandoned and needs to be put back into the queue.
    public void Enqueue(Job j, bool newJob)
    {
        if (j.jobTime < 0)
        {
            //Job is meant to be insta-completed by User. (defining stockpile zones, rally flag, etc.)
            j.DoWork(0);
            return;
        }

        jobQueue.Enqueue(j);

        if (cbJobCreated != null && newJob)
        {
            cbJobCreated(j);
        }

    }

    public Job DeQueue()
    {
        if (jobQueue.Count == 0)
        {
            return null;
        }

        return jobQueue.Dequeue();
    }

    //TODO check if Queue already has a solution for this.
    public void Remove(Job j)
    {
        List<Job> jobs = new List<Job>(jobQueue);

        if (jobs.Contains(j) == false)
        {
            //Mosty likely a character has this job at the moment.
            //Debug.LogError("Trying to remove a job that does not exist");
            return;
        }

        jobs.Remove(j);
        jobQueue = new Queue<Job>(jobs);
    }


    #region Callbacks
    public void RegisterJobCreationCallback(Action<Job> cb)
    {
        cbJobCreated += cb;
    }

    public void UnregisterJobCreationCallback(Action<Job> cb)
    {
        cbJobCreated -= cb;
    }
    #endregion
}
