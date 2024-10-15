using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public enum AcquireType
{
    ReadOnly,
    ReadWrite
}

public class SnapshotHandle : IDisposable
{
    public PlayerSnapshot Value;
    private AcquireType _type;

    private static int _writeHandleIndex = 0;

    private static Queue<SnapshotHandle> _snapshotSyncObj = new Queue<SnapshotHandle>();
    private static Task syncTask;
    static float lastSyncTime = -1;

    public SnapshotHandle(AcquireType type)
    {
        if (_writeHandleIndex > 0 && type == AcquireType.ReadWrite)
        {
            throw new InvalidOperationException("Cannot create a nested ReadWrite handle.");
        }

        _type = type;
        Value = ObjectCopier.Clone<PlayerSnapshot>(Snapshot.CurrentSnapshot);

        if (_type == AcquireType.ReadWrite)
            _writeHandleIndex++;
    }

    public void Dispose()
    {
        if (_type == AcquireType.ReadWrite)
            _writeHandleIndex--;

        if (_type == AcquireType.ReadOnly)
        {
            if (ObjectCopier.IsDifferent<PlayerSnapshot>(Snapshot.CurrentSnapshot, Value))
                throw new Exception("PlayerSnapshot has marked readonly but tried to write!");

            return;
        }

        Snapshot.CurrentSnapshot = Value;
        _snapshotSyncObj.Enqueue(this);

        if (syncTask == null)
        {
            syncTask = Task.Run(() => SyncTaskUpdate());
        }
    }

    bool pushImmidietly = false;
    static double accumulatedTime = -1;
    private async Task SyncTaskUpdate()
    {
        while (true)
        {
            if (accumulatedTime != -1 && accumulatedTime > 1000f)
            {
                while (_snapshotSyncObj.Count > 0)
                {
                    var instanceTuple = _snapshotSyncObj.Dequeue();
                    await NakamaAPI.SetVariableCloudAsync(nameof(PlayerSnapshot), instanceTuple.Value);
                }
                accumulatedTime = 0;
            }

            await Task.Delay(16);
            accumulatedTime += 16;
        }
    }

    private async Task ForceSync()
    {
        while (_snapshotSyncObj.Count > 0)
        {
            while (_snapshotSyncObj.Count > 0)
            {
                var instanceTuple = _snapshotSyncObj.Dequeue();
                await NakamaAPI.SetVariableCloudAsync(nameof(PlayerSnapshot), instanceTuple.Value);
            }
        }
    }
}