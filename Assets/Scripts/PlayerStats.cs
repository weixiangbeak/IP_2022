using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStats
{
    public string username;
    public int totalBuildingCount;
    public int goalSlotsLeft;
    public int credits;
    public int numberOfGoalsCompleted;

    public PlayerStats()
    {

    }

    public PlayerStats(string username, int totalBuildingCount, int goalSlotsLeft, int credits, int numberOfGoalsCompleted)
    {
        this.username = username;
        this.totalBuildingCount = totalBuildingCount;
        this.goalSlotsLeft = goalSlotsLeft;
        this.credits = credits;
        this.numberOfGoalsCompleted = numberOfGoalsCompleted;
    }

    public string PlayerStatsToJson()
    {
        return JsonUtility.ToJson(this);
    }
}
