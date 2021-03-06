using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;
using TMPro;
using System;
using UnityEngine.UI;

public class PostingData : MonoBehaviour
{
    public DatabaseReference dbReference;
    public RetrievingData retrievingData;


    public static bool anotherLoginPostingData = true;
    public string userId;

    [SerializeField]
    GameObject goalContent, howToAchieve;

    private bool allowDataCreation = false;

    public GameObject confirmPlacementButton;
    //Goals that are going to be created
    public GameObject createGoalPrefab;
    public Transform createGoalArea;

    public GameObject goalSlotsText;

    private void Awake()
    {
        // initialise firebase
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Authentication.loggedIn && anotherLoginPostingData)
        {
            userId = Authentication.userId;
            anotherLoginPostingData = false;
        }

        // check if building is allowed to be saved in firebase
        if (allowDataCreation)
        {
            CreateBuildingData(userId);
            allowDataCreation = false;
        }

        // PostingBuildingData();
    }

    // create a new goal
    public void CreateNewGoalButton(GameObject goalPrefab, GameObject inputField1, GameObject inputField2)
    {
        string tempGoal, tempHow;

        if (GoalsList.maxNumGoals > 0)
        {
            tempGoal = inputField1.GetComponent<TMP_InputField>().text;
            tempHow = inputField2.GetComponent<TMP_InputField>().text;

            CreateNewGoal(userId, tempGoal, tempHow);

            Destroy(goalPrefab);

            GoalsList.maxNumGoals -= 1;
            dbReference.Child("playerStats/" + userId + "/goalSlotsLeft").SetValueAsync(GoalsList.maxNumGoals);

            goalSlotsText.GetComponent<TMP_Text>().text = "You can add " + GoalsList.maxNumGoals + " more goals.";

            // if mission 1 is onGoing, update mission to completed and display claim button
            if(RetrievingData.mission1Status == "onGoing")
            {
                RetrievingData.mission1Status = "completed";
                Shop.buttonList["mission1"].SetActive(true);
                UpdateMission1Completed();
            }

            // retrieve goals again
            retrievingData.RetrieveGoals();
        }
    }

    // reference to goals class
    public void CreateNewGoal(string uuid, string goalContent, string howToAchieve)
    {
        Goals createGoals = new Goals(goalContent, howToAchieve);

        string key = dbReference.Child(uuid).Push().Key;

        dbReference.Child("currentGoals/" + uuid + "/" + key).SetRawJsonValueAsync(createGoals.GoalsToJson());
    }

    // update total building count in firebase
    public void UpdateTotalBuildingCount()
    {
        dbReference.Child("playerStats/" + userId + "/totalBuildingCount").SetValueAsync(RetrievingData.totalBuildingCount);

        // update leaderboard
        //retrievingData.GetLeaderboard();

        // update timestamp
        UpdatePlayerStatsTimestamp();
    }

    // update credits in firebase
    public void UpdateCredits()
    {
        // takes in new value of money and updates database
        dbReference.Child("playerStats/" + userId + "/credits").SetValueAsync(RetrievingData.credits);

        // update timestamp
        UpdatePlayerStatsTimestamp();

    }

    public void UpdatePlayerStatsTimestamp()
    {
        // timestamp properties
        var timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();

        dbReference.Child("players/" + userId + "/updatedOn").SetValueAsync(timestamp);
    }

    public void CreateBuildingData(string uuid)
    {
        // BuildingData createBuildingData = new BuildingData(transformPosition, transformRotation, transformScale, meshId, buildingType, buildingId, cellLocation);

        BuildingData createBuildingData = BuildingDescription.GenerateBuildingData(SocketInteractorFunctions.buildingGameObject);

        // createBuildingData.storedInDatabase = true;

        string key = dbReference.Child(uuid).Push().Key;

        dbReference.Child("buildingData/" + uuid + "/" + key).SetRawJsonValueAsync(createBuildingData.BuildingDataToJson());
    }

    public void PostingBuildingData()
    {
        // if (SocketInteractorFunctions.counter == 1)
        // {

        DeleteBuildingData();

        UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.SetActive(false);

        // DeleteBuildingData();
        // 
        // CreateBuildingData(userId);

        // SocketInteractorFunctions.counter = 2;
        // }
        // Delete previous entry
        // Turn off button.
    }

    private void DeleteBuildingData()
    {
        Debug.Log("SocketInteractorFunctions.buildingIdToDelete: " + SocketInteractorFunctions.buildingIdToDelete);

        string parentName = "";

        dbReference.Child("buildingData/" + Authentication.userId).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError("Something went wrong when reading the data, ERROR: " + task.Exception);
                return;
            }

            else if (task.IsCompleted)
            {
                Debug.Log("1");
                DataSnapshot snapshot = task.Result;

                if (snapshot.Exists)
                {
                    Debug.Log("2");// Last code that runs
                    foreach (var child in snapshot.Children)//This line is not running
                    {
                        Debug.Log(int.Parse(child.Child("buildingId").Value.ToString()));
                        if (int.Parse(child.Child("buildingId").Value.ToString()) == SocketInteractorFunctions.buildingIdToDelete)
                        {
                            Debug.Log("int.Parse(child.Child(\"buildingId\").Value.ToString(): " + int.Parse(child.Child("buildingId").Value.ToString()));
                            parentName = child.Child("buildingId").Reference.Parent.Key;
                        }
                        Debug.Log("parentName: " + parentName);

                        if (parentName != "")
                        {
                            dbReference.Child("buildingData/" + Authentication.userId + "/" + parentName).SetValueAsync(null);
                        }
                    }

                    allowDataCreation = true;

                    Debug.Log("allowDataCreation: " + allowDataCreation);
                }

                else if (!snapshot.Exists)
                {
                    allowDataCreation = true;
                    return;
                }

                // allowDataCreation = true;
            }
        });
    }

    //create new prefab
    //new prefab has 2 input field and the border
    //prefab is with the input box, set active text to false
    public void CreateGoalPrefab()
    {
        if (GoalsList.maxNumGoals > 0)
        {
            if (createGoalArea.transform.childCount == 0)
            {
                GameObject entry = Instantiate(createGoalPrefab, createGoalArea);
                TMP_InputField[] inputFieldDetails = entry.GetComponentsInChildren<TMP_InputField>();
                Button[] buttonDetails = entry.GetComponentsInChildren<Button>();
                inputFieldDetails[0].gameObject.SetActive(true);
                inputFieldDetails[1].gameObject.SetActive(true);
                buttonDetails[0].onClick.AddListener(delegate () { CreateNewGoalButton(entry, inputFieldDetails[0].gameObject, inputFieldDetails[1].gameObject); });
                buttonDetails[1].onClick.AddListener(delegate () { CancelGoalPrefab(); });
            }

            else
            {
                Debug.Log("Finish setting up your goal");
            }
        }
    }

    public void CancelGoalPrefab()
    {
        Destroy(createGoalArea.transform.GetChild(0).gameObject);
    }

    public void UpdateMission1NoAttempt()
    {
        dbReference.Child("missionLogs/" + userId + "/mission1/missionStatus").SetValueAsync("noAttempt");

        RetrievingData.missionList[0].missionStatus = "noAttempt";

        // update timestamp
        UpdatePlayerStatsTimestamp();
    }

    public void UpdateMission2NoAttempt()
    {
        dbReference.Child("missionLogs/" + userId + "/mission2/missionStatus").SetValueAsync("noAttempt");

        RetrievingData.missionList[1].missionStatus = "noAttempt";

        // update timestamp
        UpdatePlayerStatsTimestamp();
    }

    public void UpdateMission1Ongoing()
    {
        dbReference.Child("missionLogs/" + userId + "/mission1/missionStatus").SetValueAsync("onGoing");

        RetrievingData.missionList[0].missionStatus = "onGoing";

        // update timestamp
        UpdatePlayerStatsTimestamp();
    }

    public void UpdateMission2Ongoing()
    {
        dbReference.Child("missionLogs/" + userId + "/mission2/missionStatus").SetValueAsync("onGoing");

        RetrievingData.missionList[1].missionStatus = "onGoing";

        // update timestamp
        UpdatePlayerStatsTimestamp();
    }
    public void UpdateMission1Completed()
    {
        dbReference.Child("missionLogs/" + userId + "/mission1/missionStatus").SetValueAsync("completed");

        RetrievingData.missionList[0].missionStatus = "completed";

        // update timestamp
        UpdatePlayerStatsTimestamp();
    }

    public void UpdateMission2Completed()
    {
        dbReference.Child("missionLogs/" + userId + "/mission2/missionStatus").SetValueAsync("completed");

        RetrievingData.missionList[1].missionStatus = "completed";

        // update timestamp
        UpdatePlayerStatsTimestamp();
    }

    public void StartMission2()
    {
        retrievingData.RetrieveNumberOfGoalsCompleted();

        int targetNumberOfGoals = RetrievingData.numberOfGoalsCompleted + 3;

        dbReference.Child("missionLogs/" + userId + "/mission2/targetNumberOfGoals").SetValueAsync(targetNumberOfGoals);
    }

    // IEnumerator DeleteAndCreateBuildingData()
    // {
    //     DeleteBuildingData();
    // 
    //     yield return new WaitForSeconds(3f);
    // 
    //     if (allowDataCreation)
    //     {
    //         CreateBuildingData(userId);
    //         allowDataCreation = false;
    //     }
    // }
}
