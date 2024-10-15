using System;
using System.Collections.Generic;
using UnityEngine;

public class ThreadContextManager : MonoBehaviour
{
    private static Queue<(Delegate callback, object[] args)> callbacks = new Queue<(Delegate, object[])>();
    private static ThreadContextManager _instance;
    public delegate void SyncGenericCallback(params object[] args);

    void Awake()
    {
        if (_instance != null)
        {
            if (_instance != this)
            {
                Destroy(this.gameObject);
            }
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(this);
        }
    }

    void Update()
    {
        while (callbacks.Count > 0)
        {
            (Delegate callback, object[] args) callbackObject;

            lock (callbacks)
                callbackObject = callbacks.Dequeue();

            if (callbackObject.callback == null)
            {
                Logger.LogError("Callback returned null possible race condition");
            }
            callbackObject.callback.DynamicInvoke(callbackObject.args);
        }
    }

    public static SyncGenericCallback GetSynchronizeCallbackHandler(Delegate callback)
    {
        if (_instance == null)
        {
            Logger.LogWarn("Cannot find instace of ThreadContextManager please try to add scene if does not exist");
            return null;
        }

        if (callback == null)
        {
            Logger.Log("Warn: callback is null");
            return null;
        }

        return (object[] args) =>
        {
            lock (callbacks)
                callbacks.Enqueue((callback, args));
        };
    }
}