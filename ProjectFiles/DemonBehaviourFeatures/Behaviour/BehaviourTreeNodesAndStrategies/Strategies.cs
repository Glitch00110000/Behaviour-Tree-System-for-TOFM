using System;
using System.Collections.Generic;
using Unity.Mathematics;

// using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.AI;

namespace Pathfinding.BehaviourTrees
{
    public interface IStrategy
    {
        Node.Status Process();

        void Reset()
        {
            // Noop
        }
    }

    public class WaitingStrategy : IStrategy
    {

        float currentTime = 0f;
        float waitingTime;
        
        public WaitingStrategy(float waitingTime)
        {
            this.waitingTime = waitingTime;
            currentTime = waitingTime;
        }
        
        
        public Node.Status Process()
        {
            if (currentTime <= 0)
            {
                Reset();
                return Node.Status.Success;
            }
            
            currentTime -= Time.deltaTime;
            
            return Node.Status.Running;
        }

        public void Reset()
        {
            currentTime = waitingTime;
        }
    }


public class ActionStrategy : IStrategy {
        readonly Action doSomething;
        
        public ActionStrategy(Action doSomething) {
            this.doSomething = doSomething;
        }
        
        public Node.Status Process() {
            doSomething();
            return Node.Status.Success;
        }
    }

    public class Condition : IStrategy {
        readonly Func<bool> predicate;
        
        public Condition(Func<bool> predicate) {
            this.predicate = predicate;
        }
        
        public Node.Status Process() => predicate() ? Node.Status.Success : Node.Status.Failure;
    }

    public class PatrolStrategy : MovingStrategies {
        readonly Transform entity;
        // readonly NavMeshAgent agent;
        List<Transform> patrolPoints;

        readonly Func<List<Transform>> patrolPointsProvider;
        // readonly float patrolSpeed;
        int currentIndex;
        bool isPathCalculated;

        public PatrolStrategy(Transform entity, NavMeshAgent agent, Func<List<Transform>> patrolPointsProvider, Func<float> speedProvider):base(speedProvider,agent) {
            this.entity = entity;
            this.patrolPointsProvider = patrolPointsProvider;
        }

        public override Node.Status Process() {
            base.Process();
            agent.speed = speed;
            patrolPoints = patrolPointsProvider();
            if (currentIndex == patrolPoints.Count)
            {
                Reset();
                return Node.Status.Success;
            }
            
            // Debug.Log("Patrol");

            var target = patrolPoints[currentIndex];
            agent.SetDestination(target.position);
            // entity.LookAt(target.position.With(y:entity.position.y));
            
            if (isPathCalculated && agent.remainingDistance < 0.1f) {
                currentIndex++;
                isPathCalculated = false;
            }
            
            if (agent.pathPending) {
                isPathCalculated = true;
            }
            
            return Node.Status.Running;
        }
        
        public void Reset() => currentIndex = 0;
    }
    
    public class MoveToTarget : MovingStrategies {
        readonly Transform entity;
        readonly Func<Vector3> targetProvider;
        readonly Vector3 target;
        // bool isPathCalculated;
        
        
        private Transform agentLastTransform;
        private float timeOutTimer = 0f;

        public MoveToTarget(Transform entity, NavMeshAgent agent, Func<Vector3> targetProvider, Func<float> agentSpeedProvider, float pathTimeOut = 1f): base(agentSpeedProvider,agent) {
            this.entity = entity;
            this.targetProvider = targetProvider;
            this.timeOutTimer = pathTimeOut;
            agentLastTransform = agent.transform;
        }

        // public MoveToTarget(Transform entity, NavMeshAgent agent, Vector3 target)
        //     : this(entity, agent, () => target) { }

        public override Node.Status Process()
        {
            if (base.Process() == Node.Status.Failure)
            {
                Reset();
                return Node.Status.Failure;
            }
            agent.speed = speed;
            var t = targetProvider != null ? targetProvider() : target;

            if (Vector3.Distance(entity.position, t) < 1f) {
                return Node.Status.Success;
            }
            
            agent.SetDestination(t);
            entity.LookAt(t.With(y:entity.position.y));

            if (agent.pathPending) {
                if (agentLastTransform.position == agent.transform.position)
                {
                    timeOutTimer += Time.deltaTime;
                }
                else
                {
                    timeOutTimer = 0;
                }

                if (timeOutTimer > 1f)
                {
                    agent.SetDestination(agent.transform.position);
                    Reset();
                    return Node.Status.Failure;
                }
                agentLastTransform = agent.transform;
                // isPathCalculated = true;
            }
            return Node.Status.Running;
        }

        public void Reset()
        {
            // isPathCalculated = false;
            timeOutTimer = 0f;
        } 
    }



    public class SearchArea :MovingStrategies
    {
        readonly Transform entity;
        readonly float searchRadius;
        private Vector3? point;
        readonly Func<Vector3?> searchAroundPointProvider;

     
        public SearchArea(Transform entity, NavMeshAgent agent, float searchRadius,Func<float> speedProvider, Func<Vector3?> searchAroundPointProvider = null) :base(speedProvider, agent)
        {
            this.entity = entity;
            this.searchRadius = searchRadius;
            this.searchAroundPointProvider = searchAroundPointProvider;
        }

        public override Node.Status Process()
        {
            // Debug.Log($"Search point: {point}");
            // Debug.Log($"Remaining: {agent.remainingDistance}");
            // Debug.Log($"HasPath: {agent.hasPath}");
            // Debug.Log("speed: " + speed);
            base.Process();
            
            if (point == null)
            {
                Vector3 center = searchAroundPointProvider?.Invoke() ?? entity.position;

                point = GetRandomPoint(
                    searchRadius,
                    center
                );

                agent.speed = speed;
                agent.SetDestination(point.Value);
            }

            if (agent.pathPending)
                return Node.Status.Running;

            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                point = null; // nabudúce vyber nový bod
                return Node.Status.Success;
            }

            return Node.Status.Running;
        }

        public Vector3 GetRandomPoint(float range, Vector3 searchAround)
        {
            const int maxAttempts = 30;

            for (int i = 0; i < maxAttempts; i++)
            {
                Vector3 randomPoint = searchAround + UnityEngine.Random.insideUnitSphere * range;

                if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                {
                    return hit.position;
                }
            }

            // Ak sa nenašlo nič, zostaň na mieste
            return entity.position;
        }
        
        public void Reset() => point = null;
    }
    
    
    public class MovingStrategies : IStrategy
    {
        protected readonly Func<float> speedProvider;
        protected private float speed;
        protected readonly NavMeshAgent agent;
        
        
        public MovingStrategies(Func<float> speedProvider, NavMeshAgent agent)
        {
            this.speedProvider = speedProvider;
            this.agent = agent;
        }
        
        
        public virtual Node.Status Process()
        {
            speed = speedProvider != null ? speedProvider() : (agent.remainingDistance / agent.stoppingDistance);
            if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                return Node.Status.Failure;
            }
            return Node.Status.Running;
        }
    }
}
