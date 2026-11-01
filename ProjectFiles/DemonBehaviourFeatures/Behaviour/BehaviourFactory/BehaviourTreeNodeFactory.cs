using System;
using System.Collections.Generic;
using BlackboardSystem;
using Perception;
using UnityEngine;
using UnityEngine.AI;
using BehaviourTreeEditor;
using UnityEditor; // <- TOTO VYMAZAT POTOM

namespace Pathfinding.BehaviourTrees
{
    public enum BehaviourTreePolicies
    {
        RunForeverPolicy,
        RunUntilSuccessPolicy,
        RunUntilFailurePolicy,
    }
    
    public interface INodeFactory
    {
        Node Create(BehaviourNodeData data, BehaviourContext ctx);
    }

    public abstract class NodeFactory<TData> : INodeFactory where TData : BehaviourNodeData
    {
        public Node Create(BehaviourNodeData data, BehaviourContext ctx) => Create((TData)data, ctx);
        protected abstract Node Create(TData data, BehaviourContext ctx);
    }

    public class GenericNodeFactory : NodeFactory<NodeData>
    {
        protected override Node Create(NodeData data, BehaviourContext ctx)
        {
            return new Node(data.name, data.priority);
        }
    }
    
    public class UntilFailFactory : NodeFactory<UntilFailData>
    {
        protected override Node Create(UntilFailData data, BehaviourContext ctx)
        {
            return new UntilFail(data.name, data.priority);
        }
    }
    
    public class InverterFactory : NodeFactory<InverterData>
    {
        protected override Node Create(InverterData data, BehaviourContext ctx)
        {
            return new UntilFail(data.name, data.priority);
        }
    }
    
    public class RandomSelectorFactory : NodeFactory<RandomSelectorData>
    {
        protected override Node Create(RandomSelectorData data, BehaviourContext ctx)
        {
            return new UntilFail(data.name, data.priority);
        }
    }

    public class PrioritySelectorFactory : NodeFactory<PrioritySelectorData>
    {
        protected override Node Create(PrioritySelectorData data, BehaviourContext ctx)
        {
            return new PrioritySelector(data.name, data.priority);
        }
    }

    public class SelectorFactory : NodeFactory<SelectorData>
    {
        protected override Node Create(SelectorData data, BehaviourContext ctx)
        {
            return new Selector(data.name, data.priority);
        }
    }

    public class SequenceFactory : NodeFactory<SequenceData>
    {
        protected override Node Create(SequenceData data, BehaviourContext ctx)
        {
            return new Sequence(data.name, data.priority);
        }
    }
    
    
    
    
    
    
    

    public class BehaviourTreeFactory : NodeFactory<BehaviourTreeData>
    {
        protected override Node Create(BehaviourTreeData data, BehaviourContext ctx)
        {
            BehaviourTreePolicies enumPolicy = data.policy;
            IPolicy policy = enumPolicy switch
            {
                BehaviourTreePolicies.RunForeverPolicy => Policies.RunForever,
                BehaviourTreePolicies.RunUntilSuccessPolicy => Policies.RunUntilSuccess,
                BehaviourTreePolicies.RunUntilFailurePolicy => Policies.RunUntilFailure,
                _ => throw new ArgumentOutOfRangeException(nameof(enumPolicy), enumPolicy, null)
            };
            return new BehaviourTree(data.name, policy);
        }
    }
    
    // public class PatrolFactory : NodeFactory<PatrolData>
    // {
    //     protected override Node Create(PatrolData data, BehaviourContext ctx)
    //     {
    //         var strategy = new PatrolStrategy(ctx.Transform, ctx.Agent,);
    //         return new Leaf("Patrol", strategy);
    //     }
    // }
    
    public class BehaviourTreeBuilder
    {
        readonly Dictionary<Type, INodeFactory> factories = new();

        public BehaviourTreeBuilder()
        { 
            //TODO
            // auto-register every concrete NodeFactory<T> via reflection     REWRITE TO STATIC REGISTRY PLS MAN
            foreach (var type in TypeCache.GetTypesDerivedFrom<INodeFactory>()) // editor-only; use Assembly scanning at runtime
            {
                if (type.IsAbstract) continue;
                var dataType = type.BaseType.GetGenericArguments()[0];
                factories[dataType] = (INodeFactory)Activator.CreateInstance(type);
            }
        }

        public BehaviourTree Build(BehaviourTreeAsset asset, BehaviourContext ctx)
        {
            var tree = new BehaviourTree(asset.name);
            tree.AddChild(BuildNode(asset.root, ctx));
            return tree;
        }

        Node BuildNode(BehaviourNodeData data, BehaviourContext ctx)
        {
            if (!factories.TryGetValue(data.GetType(), out var factory))
                throw new InvalidOperationException($"No factory for {data.GetType().Name}");

            Node node = factory.Create(data, ctx);
            foreach (var childData in data.Children)
                node.AddChild(BuildNode(childData, ctx));
            return node;
        }
    }
    
    public class BehaviourContext
    {
        public Transform Transform;
        public NavMeshAgent Agent;
        public Blackboard Blackboard;
        public GameObject Player;

        public Func<AgentSpeedType, float> GetSpeed;
    }
}