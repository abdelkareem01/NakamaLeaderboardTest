using System.Collections.Generic;
using Nakama;
using System.Threading.Tasks;
using System;
using System.Linq;
using Newtonsoft.Json;
using static ThreadContextManager;

public static class CloudConfigConsts
{
    public const string Snapshot = "PlayerSnapshot";
}

public static class NakamaAPI
{
    private const string scheme = "http";
    private const string host = "localhost";
    private const int port = 7350;
    private const string serverKey = "defaultkey";

    private static Client client = null;
    private static ISession session = null;
    private const string LeaderboardId = "testLeaderboard";
    public static string CurrentPlayerId
    {
        get
        {
            return session.UserId;
        }
    }

    public static void Init(Action<bool, string> onInitCallback)
    {
        var handle = ThreadContextManager.GetSynchronizeCallbackHandler(onInitCallback);

        Task.Run(async () =>
        {
            if (client != null)
            {
                Logger.LogWarn($"Client already initialized");
            }

            Logger.LogVerbose($"Client initialization request sending with scheme: {scheme}, host: {host}, port: {port}");

            try
            {
                client = new Client(scheme, host, port, serverKey);

                Dispatch(handle, onInitCallback, true, string.Empty);
            }
            catch (Exception e)
            {
                Logger.LogError($"Client initialization failed: {e.Message}" + "-" + e.StackTrace);
                handle.Invoke(false, e.Message);

                Dispatch(handle, onInitCallback, false, e.Message);
            }
        });
    }

    public static void Login(string userName, Action<bool, string> onLoginCallback = null)
    {
        var handle = ThreadContextManager.GetSynchronizeCallbackHandler(onLoginCallback);
        Task.Run(async () =>
        {
            Logger.LogVerbose($"Player login request sending with id: {userName}");

            try
            {
                session = await client.AuthenticateDeviceAsync(userName);

                Dispatch(handle, onLoginCallback, true, string.Empty);
            }
            catch (Exception e)
            {
                Logger.LogError($"Player login request failed: {e.Message}" + "-" + e.StackTrace);

                Dispatch(handle, onLoginCallback, false, e.Message);
            }
        });
    }

    public static async Task SetVariableCloudAsync<T>(string variableName, T obj)
    {
        try
        {
            Logger.LogVerbose($"SetVariableCloud request sending with variableName: {variableName}, obj: {obj}");

            var writeObject = new WriteStorageObject
            {
                Collection = "personal",
                Key = variableName,
                Value = JsonConvert.SerializeObject(obj),
                PermissionRead = 1,
                PermissionWrite = 1
            };

            await client.WriteStorageObjectsAsync(session, new[] { writeObject });
        }
        catch (Exception e)
        {
            Logger.LogError($"Player login request failed: {e.Message}" + "-" + e.StackTrace);
        }
    }

    public static void SetVariableCloud<T>(string variableName, T obj, Action<bool, string> onCompleteCallback = null)
    {
        var handle = ThreadContextManager.GetSynchronizeCallbackHandler(onCompleteCallback);
        Task.Run(async () =>
        {
            try
            {
                var writeObject = new WriteStorageObject
                {
                    Collection = "personal",
                    Key = variableName,
                    Value = JsonConvert.SerializeObject(obj),
                    PermissionRead = 1,
                    PermissionWrite = 1
                };

                await client.WriteStorageObjectsAsync(session, new[] { writeObject });

                Dispatch(handle, onCompleteCallback, true, string.Empty);
            }
            catch (Exception e)
            {
                Logger.LogError($"Player set variable request failed: {e.Message}" + "-" + e.StackTrace);
                Dispatch(handle, onCompleteCallback, false, e.Message);
            }
        });
    }

    public static void DeleteVariableCloud(string variableName, Action<bool, string> onCompleteCallback = null)
    {
        var handle = ThreadContextManager.GetSynchronizeCallbackHandler(onCompleteCallback);
        Task.Run(async () =>
        {
            try
            {
                var storageObjectIds = new List<StorageObjectId>
                {
                    new StorageObjectId
                    {
                        Collection = "personal",
                        Key = variableName,
                    }
                };

                await client.DeleteStorageObjectsAsync(session, storageObjectIds.ToArray());


                Dispatch(handle, onCompleteCallback, true, string.Empty);
            }
            catch (Exception e)
            {
                Logger.LogError($"Player set variable request failed: {e.Message}" + "-" + e.StackTrace);
                Dispatch(handle, onCompleteCallback, false, e.Message);
            }
        });
    }

    public static void GetVariableCloudJson(string variableName, Type type, Action<bool, string, string> onCompleteCallback)
    {
        var handle = ThreadContextManager.GetSynchronizeCallbackHandler(onCompleteCallback);
        Task.Run(async () =>
        {
            try
            {
                var objectId = new StorageObjectId
                {
                    Collection = "personal",
                    Key = variableName,
                    UserId = session.UserId
                };

                var result = await client.ReadStorageObjectsAsync(session, new[] { objectId });

                if (result.Objects.Count() == 0)
                {
                    Logger.LogError($"GetVariableCloud request failed: {variableName} not found");
                    handle?.Invoke(false, $"{variableName} not found", null);
                    return;
                }

                Dispatch(handle, onCompleteCallback, true, string.Empty, result.Objects.FirstOrDefault().Value);
            }
            catch (Exception e)
            {
                Logger.LogError($"Get variable cloud request failed: {e.Message}" + "-" + e.StackTrace + " Inner: " + e.InnerException);
                Dispatch(handle, onCompleteCallback, false, e.Message, null);
            }
        });
    }

    public static void GetVariableCloud<T>(string variableName, Action<bool, string, T> onCompleteCallback)
    {
        var handle = ThreadContextManager.GetSynchronizeCallbackHandler(onCompleteCallback);
        Task.Run(async () =>
        {
            try
            {
                Logger.LogVerbose($"GetVariableCloud request sending with variableName: {variableName}");

                var objectId = new StorageObjectId
                {
                    Collection = "personal",
                    Key = variableName,
                    UserId = session.UserId
                };

                var result = await client.ReadStorageObjectsAsync(session, new[] { objectId });

                if (result.Objects.Count() == 0)
                {
                    Logger.LogError($"GetVariableCloud request failed: {variableName} not found");
                    handle?.Invoke(false, $"{variableName} not found", null);
                    return;
                }

                Dispatch(handle, onCompleteCallback, true, string.Empty, JsonConvert.DeserializeObject<T>(result.Objects.FirstOrDefault().Value));
            }
            catch (Exception e)
            {
                Logger.LogError($"Get variable cloud request failed: {e.Message}" + "-" + e.StackTrace);
                Dispatch(handle, onCompleteCallback, false, e.Message, null);
            }
        });
    }

    public static void Dispatch(SyncGenericCallback handle, Delegate callback, params object[] parameters)
    {
        if (handle == null && callback != null)
        {
            Logger.LogWarn("Couldn't find thread context manager fallback to action invocation");
            callback.DynamicInvoke(parameters);
        }
        else
        {
            handle.Invoke(parameters);
        }
    }

    public static void CheckVariableIsExist(string variableName, Action<bool, bool> onCompleteCallback)
    {
        var handle = ThreadContextManager.GetSynchronizeCallbackHandler(onCompleteCallback);
        Task.Run(async () =>
        {
            try
            {
                Logger.LogVerbose($"GetVariableCloud request sending with variableName: {variableName}");

                var objectId = new StorageObjectId
                {
                    Collection = "personal",
                    Key = variableName,
                    UserId = session.UserId
                };

                var result = await client.ReadStorageObjectsAsync(session, new[] { objectId });

                Dispatch(handle, onCompleteCallback, true, result.Objects.Count() > 0);
            }
            catch (Exception e)
            {
                Logger.LogError($"Get variable cloud request failed: {e.Message}" + "-" + e.StackTrace);

                Dispatch(handle, onCompleteCallback, false, false);
            }
        });
    }

    public static void GetAllEntriesUser(string userId, Action<bool, Dictionary<string, string>> onCompleteCallback)
    {
        var handle = ThreadContextManager.GetSynchronizeCallbackHandler(onCompleteCallback);
        Task.Run(async () =>
        {
            try
            {
                var objects = await client.ListUsersStorageObjectsAsync(session, "personal", userId);
                var dictionary = objects.Objects.ToDictionary(obj => obj.Key, obj => obj.Value);
                Dispatch(handle, onCompleteCallback, true, dictionary);
            }
            catch (Exception e)
            {
                Logger.LogError($"Get all entries cloud request failed: {e.Message}" + "-" + e.StackTrace);

                Dispatch(handle, onCompleteCallback, false, false);
            }
        });
    }
    public static void WriteLeaderboardEntry(long score, Action<bool, bool> onCompleteCallback)
    {
        var handle = ThreadContextManager.GetSynchronizeCallbackHandler(onCompleteCallback);
        IApiLeaderboardRecord _leaderboardRecord = null;
        Task.Run(async () =>
        {
            try
            {
                _leaderboardRecord = await client.WriteLeaderboardRecordAsync(session, LeaderboardId, score);
                var dictionary = JsonConvert.SerializeObject(_leaderboardRecord);
                Logger.Log($"Successfully wrote leaderboard entry. Record: {JsonConvert.SerializeObject(_leaderboardRecord)}");
                Dispatch(handle, onCompleteCallback, true, _leaderboardRecord != null);

            }
            catch (ApiResponseException e)
            {
                Logger.LogError($"Error writing leaderboard entry: {e.Message}");
                Dispatch(handle, onCompleteCallback, false, false);
            }
        });
    }

    public static void GetApiLeaderboardRecord<T>(Action<bool, bool, T> onCompleteCallback) {
        var handle = ThreadContextManager.GetSynchronizeCallbackHandler(onCompleteCallback);
        Task.Run(async () =>
        {
            try
            {
                List<LeaderboardRecord> leaderboardRecords = new List<LeaderboardRecord>();
                IApiLeaderboardRecordList _leaderboardRecordList = await client.ListLeaderboardRecordsAsync(session, LeaderboardId, limit: 1000);
                Logger.Log($"Successfully retrieved leaderboard records for users. Records: {JsonConvert.SerializeObject(_leaderboardRecordList)}");

                foreach (IApiLeaderboardRecord record in _leaderboardRecordList.Records)
                {
                    leaderboardRecords.Add(new()
                    {
                        Rank = record.Rank,
                        Username = record.Username,
                        Score = record.Score
                    });
                   
                }
                Dispatch(handle, onCompleteCallback, true, _leaderboardRecordList != null, leaderboardRecords);
            }
            catch (ApiResponseException e)
            {
                Logger.LogError($"Error writing leaderboard entry: {e.Message}");
                Dispatch(handle, onCompleteCallback, false, false);
            }
        });
    }
    public static string GetUsername() { 
    
        return session.Username;
    }
}