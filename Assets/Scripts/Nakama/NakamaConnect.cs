using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;


public class NakamaConnect : MonoBehaviour
{
    private bool canContinue = false;
    public TextMeshProUGUI loadingText;
    public Button continueButton;
    void Start()
    {
        continueButton.onClick.AddListener(OnClick);
        continueButton.interactable = false;
        NakamaAPI.Init((isSuccess, message) =>
        {
            if (isSuccess)
            {
                NakamaAPI.Login(userName: SystemInfo.deviceUniqueIdentifier, (isSuccess, message) =>
                {
                    if (!isSuccess)
                    {
                        Logger.LogError("Login failed");
                        return;
                    }
                    Logger.Log("Login success");
                    loadingText.text = "Login Success";
                    OnLogin();
                });
            }
            else
            {
                Logger.LogError("Init failed");
            }
        });

    }

    private void OnHideUnity(bool isGameShown)
    {
        if (!isGameShown)
        {
            Time.timeScale = 0;
        }
        else
        {
            Time.timeScale = 1;
        }
    }


    private void OnLogin()
    {
        
        NakamaAPI.CheckVariableIsExist(nameof(PlayerSnapshot), (successCheck, snapshotExist) =>
        {
            if (!successCheck)
            {
                Logger.LogError("An error occured during variable check, var: " + nameof(PlayerSnapshot));
                return;
            }

            if (!snapshotExist)
            {
                Snapshot.CurrentSnapshot = new PlayerSnapshot();

                NakamaAPI.SetVariableCloud<PlayerSnapshot>(nameof(PlayerSnapshot), Snapshot.CurrentSnapshot, (successSet, message) =>
                {
                    if (!successSet)
                    {
                        Logger.LogError("An error occured during variable setting, var: " + nameof(PlayerSnapshot) + " message: " + message);
                        return;
                    }
                    canContinue = true;
                    Logger.LogVerbose("Player snapshot created on cloud : \n" + JsonUtility.ToJson(Snapshot.CurrentSnapshot));

                    OnSnapshotCreated();
                });
            }
            else
            {
                
                NakamaAPI.GetVariableCloud<PlayerSnapshot>(nameof(PlayerSnapshot), (successsGet, message, snapshot) =>
                {
                    if (!successsGet)
                    {
                        Logger.LogError("An error occured during getting variable var: " + nameof(PlayerSnapshot) + " message: " + message);
                        return;
                    }

                    Snapshot.CurrentSnapshot = snapshot;
                    canContinue = true;
                    Logger.LogVerbose("Player snapshot acquired from cloud :" + JsonUtility.ToJson(Snapshot.CurrentSnapshot));
                    OnSnapshotCreated();
                });
            }
        });
    }

    private void OnSnapshotCreated()
    {
        loadingText.text = "Checking Leaderboard records...";
        if (Snapshot.CurrentSnapshot.PlayerName == null)
        {

            using (var snap = new SnapshotHandle(AcquireType.ReadWrite))
            {
                snap.Value.PlayerName = "DummyPlayer" + UnityEngine.Random.Range(100, 999);
                snap.Value.Username = NakamaAPI.GetUsername();
                snap.Value.Score = 10;
                NakamaAPI.WriteLeaderboardEntry(10, (successSet, message) =>
                {

                    if (!successSet)
                    {
                        Logger.LogError("An error occured during leaderboard record setting");
                        loadingText.text = "Leaderboard retrieval failed!";
                        return;
                    }

                });
            }
        }
          loadingText.text = "Click to continue";
          continueButton.interactable = true;
    }

    private void Update()
    {
    if (Snapshot.CurrentSnapshot == null) return;
    }


    public void OnAfterLogin()
    {
        //TODO
    }

    public void OnClick()
    {
        //OnAfterLogin();

        Logger.LogVerbose("Success");
        
        SceneManager.LoadScene("Home");
        //go to home screen
    }
}