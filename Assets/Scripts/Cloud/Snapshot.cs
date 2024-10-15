using System.Threading.Tasks;
using System;

public class Snapshot
{
    public static PlayerSnapshot CurrentSnapshot;

    public static async Task<bool> UpdateSnapshotAsync()
    {
        var updatedSnapshot = await GetSnapshotAsync();
        SetCurrentSnapshot(updatedSnapshot);
        return true;
    }

    public static void UpdateSnapshot(Action<bool> onSnapshotUpdate = null)
    {
        GetSnapshot((status, snapshot) =>
        {
            if (snapshot == null)
            {
                onSnapshotUpdate(false);
            }

            SetCurrentSnapshot(snapshot);

            if (onSnapshotUpdate != null)
                onSnapshotUpdate(true);
        });
    }

    public static void GetSnapshot(Action<bool, PlayerSnapshot> onSnapshotRecieved)
    {
        var handle = ThreadContextManager.GetSynchronizeCallbackHandler(onSnapshotRecieved);

        Task.Run(async () =>
        {
            var snapshot = await GetSnapshotAsync();
            handle.Invoke(snapshot != null, snapshot);
        });
    }

    public static async Task<PlayerSnapshot> GetSnapshotAsync()
    {
        bool isCompleted = false;
        PlayerSnapshot result = null;
        try
        {
            NakamaAPI.GetVariableCloud<PlayerSnapshot>("profile", (success, message, profile) =>
            {
                if (!success)
                    throw new Exception(message);

                result = profile;
                isCompleted = true;
            });

            while (!isCompleted)
            {
                await Task.Delay(100);
            }
        }
        catch (Exception e)
        {
            return null;
        }

        return result;
    }

    public static void SetCurrentSnapshot(PlayerSnapshot snapshot)
    {
        CurrentSnapshot = snapshot;
    }
}