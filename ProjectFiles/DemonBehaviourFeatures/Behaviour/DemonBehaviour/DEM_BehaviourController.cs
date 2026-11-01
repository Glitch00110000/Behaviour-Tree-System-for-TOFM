using UnityEngine;
using Pathfinding.BehaviourTrees;
using UnityEngine.AI;
using System.Collections.Generic;
using BlackboardSystem;
using UnityServiceLocator;
using System;

namespace Perception {
    public class DEM_BehaviourController : PerceptionAgent , IExpert
    {
        #region Fields Behaviour Tree

        [Header("Agent Dependencies")]
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private GameObject player;

        [Header("Agent Configuration")]
        // [SerializeField] private float patrolSpeed = 2.4f;
        // [SerializeField] private float chaseSpeed = 3.2f;
        [SerializeField]
        private float MaxRunningSpeed = 3.2f;
        [SerializeField]
        private float MaxWalkingSpeed = 2.4f;
        [SerializeField]
        private float MaxSneakingSpeed = 1.2f;

        public GameObject Player => player;

        #endregion

        #region Fields Blackboard
        
        private Blackboard blackboard;
        
        private BlackboardKey playerInSightBBK;
        private BlackboardKey BestPositionBBK;
        private BlackboardKey patrolPointsBBK;
        private BlackboardKey isPerceivingBBK;
        private BlackboardKey agentSpeedBBK;
        private BlackboardKey isRunningBBK;
        
        private bool handUp = false;
        private BehaviourTree tree;
        // private readonly ActionQueue pendingBBactions = new ActionQueue();
        private readonly BlackboardUpdateQueue pendingBBactions = new BlackboardUpdateQueue();
        #endregion

        #region StaminaFields
        [Header("Stamina Configuration")]
        [SerializeField]
        private AnimationCurve staminaToSpeedCurve =
            AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [SerializeField, Tooltip("How much stamina does the agent have")]
        private float MaxStaminaScore = 15;
        private float currentStamina = 15;
        [SerializeField, Tooltip("How long is the agent exhausted")]
        private float MaxExhaustion = 50;
        private float exhaustion = 50;
        

        
        // public enum AgentSpeedType
        // {
        //     sneaking,
        //     Walking,
        //     Running
        // }

        #endregion
        
        public Blackboard GetBlackboard() => blackboard;
        
        //TODO: toto bude treba prerobit at si ich to berie dinamicky lebo takto sem mozem pridavat veci kym mi z toho nepreskoci
        
        //Pravdepodobne budem pokracovat v tom pridavat veci kym mi z toho nepreskoci lebo sa mi nechce
        public List<BlackboardKey> GetKeys()
        {
            return new List<BlackboardKey>(){playerInSightBBK,BestPositionBBK,patrolPointsBBK,isPerceivingBBK,agentSpeedBBK,isRunningBBK};
        }
        
        
        //Calculates the speed at which the agent should move
        private (float sneekingSpeed, float walkingSpeed, float runningSpeed) CalculateSpeedBasedOnStamina()
        {
            
            //calculate exhaustion & speed
            if (currentStamina <= 0)
            {
                if (exhaustion <= 0)
                {
                    exhaustion = MaxExhaustion;
                    currentStamina = MaxStaminaScore;
                    return (MaxSneakingSpeed, MaxWalkingSpeed, MaxRunningSpeed);
                }
                
                exhaustion -= Time.deltaTime;
                float exhaustion01 = exhaustion / MaxExhaustion;

                float penalty;
                

                if (exhaustion01 < 0.3f)
                {
                    penalty = Mathf.Lerp(1f, 0.75f, exhaustion01 * 2f);
                }
                else if (exhaustion01 > 0.5f)
                {
                    penalty = Mathf.Lerp(0.75f, 1f, (exhaustion01 - 0.5f) * 2f);
                }
                else
                {
                    penalty = 0.75f;
                }
                
                return (MaxSneakingSpeed*penalty, MaxWalkingSpeed*penalty, MaxRunningSpeed *penalty * 0.80f);
            }
            
            //Calculate stamina & speed while running
            if (!blackboard.TryGetValue(isRunningBBK, out bool running)) 
                return(
                    MaxSneakingSpeed, 
                    MaxWalkingSpeed,
                    MaxRunningSpeed
                    );
            
            if (running && currentStamina > 0)
            {
                currentStamina  -= Time.deltaTime;
                
                float stamina01 = currentStamina / MaxStaminaScore;

                float t = staminaToSpeedCurve.Evaluate(stamina01);
                // float t = staminaToSpeedCurve.Evaluate(1f - stamina01);
    
                float runningSpeed = Mathf.Lerp(
                    MaxWalkingSpeed,
                    MaxRunningSpeed,
                    t
                );
                
                // Debug.Log(runningSpeed);
                return (MaxSneakingSpeed,MaxWalkingSpeed,runningSpeed);
            }
            
            return (MaxSneakingSpeed, MaxWalkingSpeed, MaxRunningSpeed);
        }

        protected override void React()
        {
            TargetTrack best = null;
            Transform bestTarget = null;

            foreach (var pair in tracks) {
                if (best == null || pair.Value.Score > best.Score) {
                    best = pair.Value;
                    bestTarget = pair.Key;
                }
            }

            var score = best != null ? best.Score : 0f;
            var isPerceiving = bestTarget && active.TryGetValue(bestTarget, out var set) && set.Count > 0;

            
            // EnqueueBlackBoardAction(() => blackboard.SetValue(isPerceivingBBK, isPerceiving));
            EnqueueBlackBoardAction(isPerceivingBBK, isPerceiving);
            
            
            
            // if (best != null) EnqueueBlackBoardAction(() => blackboard.SetValue(BestPositionBBK, best.LastKnownPosition));
            if (best != null) EnqueueBlackBoardAction(BestPositionBBK, best.LastKnownPosition);
            
            // if (best != null && score >= alertThreshold && isPerceiving) {
            //
            //     //NOOP
            //     // EnqueueBlackBoardAction(() => blackboard.SetValue(BestPositionBBK, best.LastKnownPosition));
            //     // handUp = true;
            //
            //
            // }else {
            //     // if (blackboard.TryGetValue(playerInSightBBK, out bool inSight) && inSight) {    
            //     //     Debug.Log($"Stopped Percieving");
            //     //     // EnqueueBlackBoardAction(() => blackboard.SetValue(playerInSightBBK, false));
            //     //     handUp = true;
            //     // }
            //
            // }

            if (score >= highAlertEnter) latchedHighAlert = true;
            else if (score < highAlertExit || score <= 0f) latchedHighAlert = false;

            if (verbose) Debug.Log($"Best target: {bestTarget}, score: {score}, isPerceiving: {isPerceiving}, latchedHighAlert: {latchedHighAlert}");
        }


        protected override void ScanSight() {   // TODO: this might have to be revriten later
            if (!player) return;

            var target = player.transform;
            var eye = transform.position + Vector3.up;
            var to = target.position + Vector3.up - eye;
            var dist = to.magnitude;
            if (dist > viewRange) return;

            var angle = Vector3.Angle(transform.forward, to);

            if (verbose) Debug.Log($"Angle to player: {angle}, primaryFov: {primaryFov}, peripheralFov: {peripheralFov}, distance{dist}");
            
            StimType? type = null;
            if (angle <= primaryFov * 0.5f) type = StimType.VisualPrimary;
            else if (angle <= peripheralFov * 0.5f) type = StimType.VisualPeripheral;

            //TODO: neskor treba vymenit transform agenta za transform hlavy
            if (type == null || !HasLineOfSight(transform.position, to, dist, target))
            {
                EnqueueBlackBoardAction(playerInSightBBK,false);
                return;
            };
            
            EnqueueBlackBoardAction(playerInSightBBK,true);
            if (verbose) Debug.Log($"Emitting stim for target: {target}, type: {type.Value}");
            PerceptionHub.Emit(new Stim(type.Value, target, target.position, viewRange), transform);
        }

        protected override bool HasLineOfSight(Vector3 origin, Vector3 direction, float distance, Transform target) {
            if (!Physics.Raycast(origin, direction, out var hit, distance, obstructionMask)) return true;
            return hit.transform == target || hit.transform.IsChildOf(target);


        }


        // Start is called once before the first execution of Update after the MonoBehaviour is created
         void Start()
        {
            handUp = true;
            RegisterBlackboardAndKeys();

            MakeTree();
        }

        private void RegisterBlackboardAndKeys()
        {
            blackboard = ServiceLocator.For(this).Get<BlackboardController>().GetBlackboard();
            ServiceLocator.For(this).Get<BlackboardController>().RegisterExpert(this);

            //register blackboard keys
            playerInSightBBK = blackboard.GetOrRegisterKey("PlayerInSight");
            BestPositionBBK = blackboard.GetOrRegisterKey("BestPosition");
            patrolPointsBBK = blackboard.GetOrRegisterKey("PatrolPoints");
            isPerceivingBBK = blackboard.GetOrRegisterKey("IsPerceiving");
            agentSpeedBBK = blackboard.GetOrRegisterKey("AgentMoveSpeed");
            isRunningBBK = blackboard.GetOrRegisterKey("IsRunning");
            
            blackboard.SetValue(isRunningBBK,false);
        }

        private void EnqueueBlackBoardAction<T>(BlackboardKey key, T value)
        {
            pendingBBactions.Enqueue(key, value);
            handUp = true;
        }
         
        // Update is called once per frame
        void Update()
        {
            // Debug.Log($"Queue size: {pendingBBactions.Count}");
            tree.Process();
        }

        protected override void LateUpdate()
        {
            var agentSpeed = CalculateSpeedBasedOnStamina();
            EnqueueBlackBoardAction(agentSpeedBBK, new Vector3(agentSpeed.sneekingSpeed,agentSpeed.walkingSpeed,agentSpeed.runningSpeed));
            ScanSight();
            base.TickTracks();
            React();
            active.Clear();
            
            
        }

        public int GetInsistence(Blackboard blackboard)
        {
            return handUp ? 120 : 0;
        }

        public void Execute(Blackboard blackboard)
        {
            blackboard.AddAction(() =>
            {
                pendingBBactions.Flush(blackboard);
                handUp = false;
            });
        }
        

        public BehaviourTree GetTree() { return tree; }

        public BehaviourTree MakeTree()
        {
            BehaviourTree newTree = new BehaviourTree("Demon");

            PrioritySelector AlertSelector = new PrioritySelector("AlertSelector");

            #region Idle Behaviour

            Sequence IdleActions = new Sequence("Idle", 0);

            Leaf IdleCheck = new Leaf("CheckIdle", new Condition(()
                => IsAlertLevel(AlertLevel.Idle, IdleActions)));

            Leaf DontRunIdle = new Leaf("DontRun Walk",
                new ActionStrategy(() => EnqueueBlackBoardAction(isRunningBBK, false)));

            Leaf Patrol = new Leaf("Patrol", new PatrolStrategy(transform, agent, ()
                    => blackboard.TryGetValue(patrolPointsBBK, out List<Transform> points)
                        ? points
                        : new List<Transform>(),
                () => agentSpeedProvider(AgentSpeedType.Walking)));

            #endregion

            #region Suspicious Behaviour

            Sequence SuspiciousActions = new Sequence("SuspiciosActions", 50);

            Leaf SuspiciousCheck = new Leaf("SuspicisousCheck", new Condition(()
                => IsAlertLevel(AlertLevel.Suspicious, SuspiciousActions)));

            Leaf DontRunSus = new Leaf("DontRun Walk",
                new ActionStrategy(() => EnqueueBlackBoardAction(isRunningBBK, false)));

            PrioritySelector followSourceOrSearch = new PrioritySelector("FollowSourceOrSearch");

            // follow source stimuli
            Sequence FollowCheckNFollow = new Sequence("FollowCheckNFollow", 100);

            Leaf checkFollow = new Leaf("CheckFollow", new Condition(() => IsPercieving(FollowCheckNFollow)));

            Leaf StopMoving = new Leaf("StopMoving",
                new ActionStrategy(() => agent.SetDestination(transform.position)));

            Leaf WaitABit = new Leaf("WaitABit", new WaitingStrategy(3f));

            Leaf FollowSource = new Leaf("FollowSourceStimuli",
                new MoveToTarget(
                    transform,
                    agent,
                    () => blackboard.TryGetValue(BestPositionBBK, out Vector3 bestPos) ? bestPos : transform.position,
                    () => agentSpeedProvider(AgentSpeedType.Running)));


            // search area
            float searchRadiusSuspicious = 10f;

            Leaf SearchAreaSus = new Leaf("SearchAreaSus", new SearchArea(transform, agent, searchRadiusSuspicious,
                speedProvider: () => agentSpeedProvider(AgentSpeedType.Walking)), 50);


            #endregion

            #region High Alert Behaviour

            //Determins if agent is in Hight alert
            Sequence HighAlertActions = new Sequence("HighAlertActions", 100);

            Leaf HighAlertCheck = new Leaf("HighAlertCheck", new Condition(()
                => IsAlertLevel(AlertLevel.Alert, HighAlertActions)));

            Sequence RunAndDecide = new Sequence("RunAndDecide", 100);

            Leaf RunHA = new Leaf("run", new ActionStrategy(() => EnqueueBlackBoardAction(isRunningBBK, true)));

            // figures out if it is activly chasing or searching for target
            PrioritySelector ChaseOrSearchHA = new PrioritySelector("ChaseOrSearchHA");


            // if the target is in sight it chases him
            Sequence ChasePlayerSeq = new Sequence("ChasePlayerSeq", 100);

            LeafMultiStrategy IsPlayerHeardNPathValid = new LeafMultiStrategy("isPlayerSeenHeardNPathValid", new List<IStrategy>
            {
                new Condition(() => AgentPathValid(agent)),
                new Condition(() => IsPlayerInSight(ChasePlayerSeq) || IsPercieving(ChasePlayerSeq))
            });

        Leaf ChasePlayer = new Leaf("ChasePlayer", 
                new MoveToTarget(
                    transform, 
                    agent,
                    () => blackboard.TryGetValue(BestPositionBBK, out Vector3 bestPos) ? bestPos : transform.position,
                    () => agentSpeedProvider(AgentSpeedType.Running)));
            

            //Search for the player if it is lost from sight

            Sequence SearchForPlayerHASeq = new Sequence("SearchForPlayer", 50);

            Leaf CanSearchAreaHA = new Leaf("CanSearchAreaHA", new Condition(() => !IsPlayerInSight(SearchForPlayerHASeq)));

            Func<Vector3?> searchPoint = () => blackboard.TryGetValue(BestPositionBBK, out Vector3 bestPos) ? bestPos : null;
            Leaf SearchAreaHA = new Leaf("SearchAreaHA",new SearchArea(transform,agent,5,() => agentSpeedProvider(AgentSpeedType.Running),searchPoint));



            #endregion

            #region TreeBuild

            AlertSelector.AddChild(IdleActions).AddChild(SuspiciousActions).AddChild(HighAlertActions);
            IdleActions.AddChild(IdleCheck).AddChild(DontRunIdle).AddChild(Patrol);
            SuspiciousActions.AddChild(SuspiciousCheck).AddChild(DontRunSus).AddChild(followSourceOrSearch);
            followSourceOrSearch.AddChild(FollowCheckNFollow).AddChild(SearchAreaSus);
            FollowCheckNFollow.AddChild(checkFollow).AddChild(StopMoving).AddChild(WaitABit).AddChild(FollowSource);
            HighAlertActions.AddChild(HighAlertCheck).AddChild(RunAndDecide);
            RunAndDecide.AddChild(RunHA).AddChild(ChaseOrSearchHA);
            ChaseOrSearchHA.AddChild(ChasePlayerSeq).AddChild(SearchForPlayerHASeq);
            ChasePlayerSeq.AddChild(IsPlayerHeardNPathValid).AddChild(ChasePlayer);
            SearchForPlayerHASeq.AddChild(CanSearchAreaHA).AddChild(SearchAreaHA);
            newTree.AddChild(AlertSelector);

            tree = newTree;
            
            return tree;

            #endregion

            #region Tree Providers
            
            
            float agentSpeedProvider(AgentSpeedType agentSpeedType)
            {
                Vector3 agentSpeedVec = blackboard.TryGetValue(agentSpeedBBK, out Vector3 agentSpeed)
                    ? agentSpeed
                    : new Vector3(MaxSneakingSpeed, MaxWalkingSpeed, MaxRunningSpeed);
                
                switch (agentSpeedType)
                {
                    case AgentSpeedType.sneaking:
                        return agentSpeedVec.x;
                    case AgentSpeedType.Walking:
                        return agentSpeedVec.y;
                    case AgentSpeedType.Running:
                        return agentSpeedVec.z;
                    default:
                        Debug.LogError($"{agentSpeedType} not supported, Tupelo si nevybral typ");
                        return 0f;
                }
            }
            
            #endregion
            #region Tree Conditions

            bool AgentPathValid(NavMeshAgent navAgent)
            {
                if (!navAgent.hasPath) return true;
                    
                return  navAgent.path.status == NavMeshPathStatus.PathComplete;
            }
            
            bool IsPlayerInSight(Sequence sequence) {
                if (blackboard.TryGetValue(playerInSightBBK, out bool inSight)) {
                    if (inSight) {
                        sequence.Reset();
                        return true;
                    }
                }
                return false;
            }

            bool IsPercieving(Sequence sequence)
            {
                if (blackboard.TryGetValue(isPerceivingBBK, out bool inSight)) {
                    if (inSight) {
                        sequence.Reset();
                        return true;
                    }
                }
                return false;
            }

            bool IsAlertLevel(AlertLevel alertLevel, Sequence sequence)
            {
                if(GetCurrentAlertLevel == alertLevel)
                {
                    // sequence.Reset();
                    return true;
                }
                return false;
            }
            #endregion
        }

        public BehaviourTree LoadTree()
        {
            throw new NotImplementedException();
        }
    }
    
}