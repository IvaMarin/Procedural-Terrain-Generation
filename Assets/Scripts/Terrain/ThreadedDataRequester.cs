using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class ThreadedDataRequester : MonoBehaviour
{
    static ThreadedDataRequester instance;
    private Queue<ThreadInfo> _dataQueue = new();

    private void Awake()
    {
        instance = FindObjectOfType<ThreadedDataRequester>();
    }

    public static void RequestData(Func<object> generateData, Action<object> callback)
    {
        ThreadStart threadStart = delegate
        {
            instance.DataThread(generateData, callback);
        };

        new Thread(threadStart).Start();
    }

    private void DataThread(Func<object> generateData, Action<object> callback)
    {
        object data = generateData();
        lock (_dataQueue)
        {
            _dataQueue.Enqueue(new ThreadInfo(callback, data));
        }
    }

    private void Update()
    {
        while (_dataQueue.Count > 0)
        {
            ThreadInfo threadInfo = _dataQueue.Dequeue();
            threadInfo.callback(threadInfo.parameter);
        }
    }

    struct ThreadInfo
    {
        public readonly Action<object> callback;
        public readonly object parameter;

        public ThreadInfo(Action<object> callback, object parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}
