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
