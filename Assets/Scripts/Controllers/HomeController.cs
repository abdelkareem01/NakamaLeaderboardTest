using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HomeController : MonoBehaviour
{

    public TextMeshProUGUI leaderboard;
    private List<LeaderboardRecord> leaderboardRecords = new List<LeaderboardRecord>();
    public TextMeshProUGUI loggedInUser;
    public Button increaseScoreButton;
    void Start()
    {
       loggedInUser.text = "Welcome " + Snapshot.CurrentSnapshot.Username;
        increaseScoreButton.onClick.AddListener(IncreaseScore);
        RefreshLeaderboard();
    }

    public void RefreshLeaderboard() {

        NakamaAPI.GetApiLeaderboardRecord<List<LeaderboardRecord>>((successGet, message, LeaderboardRecord) =>
        {
            if (successGet)
            {
                leaderboardRecords = LeaderboardRecord;
                SortList();
                Debug.Log(leaderboardRecords.Count);
            }
        });
    }

    public void IncreaseScore() {
        increaseScoreButton.interactable = false;
        int currentScore = Snapshot.CurrentSnapshot.Score;
        NakamaAPI.WriteLeaderboardEntry(((long)currentScore) + 10, (successSet, message) =>{
            if (successSet) {
                RefreshLeaderboard();
                increaseScoreButton.interactable = true;
                using (var snap = new SnapshotHandle(AcquireType.ReadWrite)) {
                    snap.Value.Score = currentScore + 10;
                }
            }
        });

    }

    public void SortList() { 
        leaderboard.text =  "Rank" + "\t  " + "Username" + "\t\t" + "Score" + "\n\n";
        for(int i = 0; i < leaderboardRecords.Count; i++)
        {
            leaderboard.text = String.Concat(leaderboard.text, leaderboardRecords[i].Rank + "." + "\t" + leaderboardRecords[i].Username + "\t  " + leaderboardRecords[i].Score + "\n\n");
        }
    
    }
}
