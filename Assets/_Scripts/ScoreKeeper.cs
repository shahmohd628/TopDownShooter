using UnityEngine;

public class ScoreKeeper : MonoBehaviour
{
    public static int score {get; private set;}
    float lastEnemyKilledTime;
    int streakCount;
    float streakExpiryTime = 1;
    void Start()
    {
        Enemy.OnDeathStatic += OnEnemyKilled;
        FindAnyObjectByType<Player>().OnDeath += OnPlayerDeath;
    }
    
    void OnEnemyKilled()
    {
        if(Time.time < lastEnemyKilledTime + streakExpiryTime)
        {
            streakCount ++;
        }
        else
        {
            streakCount = 0;
        }
        lastEnemyKilledTime = Time.time;
        score += 5 + (int)Mathf.Pow(2,streakCount);

    }
    
    void OnPlayerDeath()
    {
         Enemy.OnDeathStatic -= OnEnemyKilled;
    }
}
