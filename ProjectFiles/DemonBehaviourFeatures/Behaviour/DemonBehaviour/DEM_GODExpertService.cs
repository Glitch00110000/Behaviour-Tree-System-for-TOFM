using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using BlackboardSystem;
using UnityServiceLocator;
using Pathfinding.BehaviourTrees;
using ActionQueueUtility;
using Perception;

public class DEM_GODExpertService : MonoBehaviour, IExpert
{
    #region Fields
    [Header("Agent Dependencies")]
    
    [SerializeField] private List<PatrolPoints> patrolPoints = new List<PatrolPoints>();

    #endregion

    #region Blackboard
    private Blackboard blackboard;
    private ActionQueue pendingBBactions = new ActionQueue();

    private BlackboardKey PatrolPointsBBK;

    #endregion
    
    
    #region Dependencies
    private DEM_BehaviourController demonBehaviourController;
    private GameObject player;

    #endregion

    private bool handUp = false;
    private float importanceWeight = 1f;


    [System.Serializable]
    private struct PatrolPoints
    {
        public string name;
        public List<Transform> point;

        public PatrolPoints(string name, List<Transform> point)
        {
            this.name = name;
            this.point = point;
        }
    }
    public void Execute(Blackboard blackboard)
    {
        blackboard.AddAction(() =>
        {
            pendingBBactions.Dequeue();
            handUp = false;
            importanceWeight = 1f;
        });
    }

    public int GetInsistence(Blackboard blackboard)
    {
        return handUp ? (int)(100 * importanceWeight) : 0;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        blackboard = ServiceLocator.For(this).Get<BlackboardController>().GetBlackboard();
        ServiceLocator.For(this).Get<BlackboardController>().RegisterExpert(this);

        demonBehaviourController = GetComponent<DEM_BehaviourController>();
        player = demonBehaviourController.Player;


        PatrolPointsBBK = blackboard.GetOrRegisterKey("PatrolPoints");
        pendingBBactions.Enqueue(() => blackboard.SetValue(PatrolPointsBBK, patrolPoints[0].point));
        handUp = true;
        importanceWeight = 5f;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}