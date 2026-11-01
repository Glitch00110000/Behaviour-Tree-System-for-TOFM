using System;
using System.Collections.Generic;
using Pathfinding.BehaviourTrees;
using Perception;
using UnityEngine;
// using UnityEngine.Serialization;

// using UnityEngine.Serialization;

namespace BehaviourTreeEditor
{
    [CreateAssetMenu(menuName = "Behaviour Tree")]
    public class BehaviourTreeAsset : ScriptableObject
    {
        
        [SerializeReference]
        public BehaviourNodeData root;
    }
    
    // base class for all node Data
    [Serializable]
    public abstract class BehaviourNodeData
    {
        public Vector2 editorPosition;
        
        public string name = "node";
        public int priority = 0;
        [SerializeReference]
        public List<BehaviourNodeData> Children = new();
    }

    [Serializable]
    public class NodeData : BehaviourNodeData
    {
        
    }
    
    [Serializable]
    public class PatrolData : BehaviourNodeData
    {
        public AgentSpeedType speed;
    }
    
    [Serializable]
    public class UntilFailData : BehaviourNodeData
    {
        //all already in base class
        //CAN HAVE ONLY 1 CHILD
    }
    
    [Serializable]
    public class InverterData : BehaviourNodeData
    {
        //all already in base class
        //CAN HAVE ONLY 1 CHILD
    }
    
    
    [Serializable]
    public class RandomSelectorData : BehaviourNodeData
    {
        //all already in base class
    }
    
    [Serializable]
    public class PrioritySelectorData : BehaviourNodeData
    {
        //all already in base class
    }
    
    [Serializable]
    public class SelectorData : BehaviourNodeData
    {
        //all already in base class
    }
    
    [Serializable]
    public class SequenceData : BehaviourNodeData
    {
        //all already in base class
    }
    
    [Serializable]
    public class LeafData : BehaviourNodeData
    {
        //all already in base class
        //this is here just to be here it wont be used as all
        // leafes recquire a strategy
    }
    
    [Serializable]
    public class BehaviourTreeData : BehaviourNodeData
    {
        //som nevedel prist na to jak normalne serializovat Ipolicy
        // takze to pojde ako enum a builder si to zrobi sam
        public BehaviourTreePolicies policy;
    }
    
    
    
    // [Serializable]
    // public class LeafData : BehaviourNodeData
    // {
    //     public AgentSpeedType speed;
    // }

    
    // [Serializable]
    // public abstract class NodeStrategyData
    // {
    //     //no op
    // }
 
    // public class LeafMultiStrategy : Node // vzniklo to z lenivosti takze idealne nepouzivat XD
    // public class UntilFail : Node {
    //     public class Inverter : Node {
    //         public class RandomSelector : PrioritySelector {
    //             public class PrioritySelector : Selector {
    //                 public class Selector : Node {
    //                     public class Sequence : Node {
    //                         public class Leaf : Node {
    //                             public class Node {
    //                                 public class BehaviourTree : Node {
}