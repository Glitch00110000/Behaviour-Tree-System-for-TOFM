using UnityEngine;
using BlackboardSystem;
using UnityServiceLocator;
using System.Collections.Generic;

public class PlayerSightExpert : MonoBehaviour , IExpert
{
    Blackboard blackboard;
    BlackboardKey playerInSightKey;

    bool playerInSight;

    bool changed = false;

    public GameObject player;

    public int GetInsistence(Blackboard blackboard)
    {
        return changed ? 100 : 0;
    }

    public void Execute(Blackboard blackboard)
    {
        changed = false;
        Debug.Log($"Player in sight: {playerInSight}, changed: {changed}");
        blackboard.AddAction(() => {
            if (blackboard.TryGetValue(playerInSightKey, out bool inSight)) {
                blackboard.SetValue(playerInSightKey, playerInSight);
            }
        });
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        blackboard = ServiceLocator.For(this).Get<BlackboardController>().GetBlackboard();
        ServiceLocator.For(this).Get<BlackboardController>().RegisterExpert(this);
        playerInSightKey = blackboard.GetOrRegisterKey("PlayerInSight");
    }

    // Update is called once per frame
    void Update()
    {
        bool newInSight = player.activeSelf;
        if (newInSight != playerInSight)
        {
            changed = true;
        }
        playerInSight = newInSight;
    }
}
