using System.Collections.Generic;
using NUnit.Framework.Constraints;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using Pathfinding.BehaviourTrees;
using Perception;
using UnityServiceLocator;
using BlackboardSystem;
using Blackboard = BlackboardSystem.Blackboard;


namespace BehaviourTreeEditor
{
    public class BehaviourTreeViewer: GraphView
    {
        
        public BehaviourTreeViewer()
        {
            style.flexGrow = 1;

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            GridBackground grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();
        }


        public UnityEditor.Experimental.GraphView.Node AddNode(string name, Vector2 position)
        {
            UnityEditor.Experimental.GraphView.Node node = new UnityEditor.Experimental.GraphView.Node
            {
                title = name
            };


            node.SetPosition(
                new Rect(position, new Vector2(200,100))
            );


            Port input = node.InstantiatePort(
                Orientation.Vertical,
                Direction.Input,
                Port.Capacity.Single,
                typeof(bool)
            );

            input.portName = "In";


            Port output = node.InstantiatePort(
                Orientation.Horizontal,
                Direction.Output,
                Port.Capacity.Multi,
                typeof(bool)
            );

            output.portName = "Out";


            node.inputContainer.Add(input);
            node.outputContainer.Add(output);


            node.RefreshPorts();
            node.RefreshExpandedState();


            AddElement(node);


            return node;
        }

        public Label AddLabel(string label)
        {

            Label l = new Label();
            
            
            l.style.position = new StyleEnum<Position>(Position.Absolute);
            l.style.left = 10;
            l.style.top = 10;
            l.style.backgroundColor = new Color(0, 0, 0, 0.7f);
            l.style.color = Color.white;
            l.style.paddingLeft = 5;
            l.style.paddingRight = 5;
            l.style.paddingTop = 3;
            l.style.paddingBottom = 3;
            l.text  = label;
            
            Add(l);

            return l;
        }

    }

    public class BehaviourTreeWindow : EditorWindow
    {
        BehaviourTreeViewer viewer;

        BehaviourTree behaviourTree;

        // float xOffset = 5f;
        // float yOffset = 5f;

        BehNode root;

        private DEM_BehaviourController dEM_Behaviour;

        private Blackboard blackboard;
        private List<BlackboardKey> keys;

        [MenuItem("Tools/Behaviour Tree")]
        public static void Open()
        {
            GetWindow<BehaviourTreeWindow>("BehaviourTree");
        }

        void OnDisable()
        {
            EditorApplication.update -= UpdateEditor;
        }

        private Label statsLabel;
        private void OnEnable()
        {
            
            dEM_Behaviour = GameObject.FindGameObjectWithTag("Demon").GetComponent<DEM_BehaviourController>();
            behaviourTree = dEM_Behaviour.MakeTree();
            viewer = new BehaviourTreeViewer();

            rootVisualElement.Add(viewer);
            
            statsLabel = viewer.AddLabel(
                $"Is player in sight: Not in play mode\n" +
                $"Is Percieved: theres nothing usefull now\n" +
                $"best Position: so stop reading this\n" +
                $"PatrolPoints: theres nothing usefull now\n" +
                $"AlertLevel: dumbass"
                
            );
            

            float x = 0;

            root =
                BuildNode(
                    behaviourTree,
                    0,
                    ref x
                );

            DrawTree(root);

            EditorApplication.update += UpdateEditor;
        }
        

        private bool wasPlaying = false;
        void UpdateEditor()
        {
            
            if (EditorApplication.isPlaying && !wasPlaying)
            {
                dEM_Behaviour = GameObject.FindGameObjectWithTag("Demon").GetComponent<DEM_BehaviourController>();
                blackboard = dEM_Behaviour.GetBlackboard();
                RemoveDrawnTree(root);
                behaviourTree = dEM_Behaviour.GetTree();
                // behaviourTree.PrintTree();
                float x = 0;
                root =
                    BuildNode(
                        behaviourTree,
                        0,
                        ref x
                    );
                DrawTree(root);
                
                wasPlaying = true;
                
                keys = dEM_Behaviour.GetKeys();

            }
            wasPlaying = EditorApplication.isPlaying;

            if (blackboard != null && keys != null && wasPlaying)
            {
                statsLabel.text = 
                    $"Is Player In Sight: {(blackboard.TryGetValue(keys[0], out bool playerinsight) ? playerinsight : "ValueCouldNotBeRead")}\n" +
                    $"Is Percieved: {(blackboard.TryGetValue(keys[3], out bool isHeard) ? isHeard : "ValueCouldNotBeRead")}\n" +
                    $"BestPosition: {(blackboard.TryGetValue(keys[1], out Vector3 bestPosition) ? bestPosition : "ValueCouldNotBeRead")}\n" +
                    $"PointsToPatroll: {(blackboard.TryGetValue(keys[2], out List<Transform> PatrolPoins) ? PatrolPoins.Count : "ValueCouldNotBeRead")}\n" +
                    $"Agent Speeds: {(blackboard.TryGetValue(keys[4], out Vector3 speeds) ? speeds : "ValueCouldNotBeRead")}\n" +
                    $"Is Running?: {(blackboard.TryGetValue(keys[5], out bool running) ? running : "ValueCouldNotBeRead")}\n" +
                    $"Alert Level: {dEM_Behaviour.GetCurrentAlertLevel}";
            }
            else
            {
                statsLabel.text = 
                    $"Is player in sight: Not in play mode\n" +
                    "Is Percieved: theres nothing usefull now\n" +
                    $"best Position: so stop reading this\n" +
                    $"PatrolPoints: theres nothing usefull now\n" +
                    $"AlertLevel: dumbass";
            }
            
            root.TickColor();
            // Debug.Log(root.children[0].node.GetStatus());
            viewer.MarkDirtyRepaint();

            
        }

        void RemoveDrawnTree(BehNode node)
        {
            viewer.DeleteElements(viewer.graphElements.ToList());
        }

        void DrawTree(BehNode node)
        {
            node.graphNode =
                viewer.AddNode(
                    node.node.name,
                    node.position
                );


            foreach(var child in node.children)
            {
                DrawTree(child);


                ConnectNodes(
                    node,
                    child
                );
            }
        }

        void ConnectNodes(
            BehNode parent,
            BehNode child
        )
        {
            Port output =
                parent.graphNode.outputContainer[0]
                as Port;


            Port input =
                child.graphNode.inputContainer[0]
                as Port;


            Edge edge =
                output.ConnectTo(input);


            viewer.AddElement(edge);
        }
        
        
        static BehNode BuildNode(
            Pathfinding.BehaviourTrees.Node node,
            int depth,
            ref float x)
        {
            List<BehNode> children = new();

            float startX = x;

            foreach (var child in node.children)
            {
                children.Add(
                    BuildNode(
                        child,
                        depth + 1,
                        ref x
                    )
                );
            }

            float endX = x;

            float myX;

            if (children.Count == 0)
            {
                myX = x;
                x++;
            }
            else
            {
                // Center parent over its children
                myX = (startX + endX - 1) * 0.5f;
            }

            Vector2 position = new Vector2(
                myX * 170,
                depth * 130
            );

            return new BehNode(
                node,
                position,
                children
            );
        }
    }

    public class BehNode
    {
        public Pathfinding.BehaviourTrees.Node node;
        public Vector2 position;
        public List<BehNode> children;

        public UnityEditor.Experimental.GraphView.Node graphNode;

        public BehNode(Pathfinding.BehaviourTrees.Node node,Vector2 position,List<BehNode> children)
        {
            this.node = node;
            this.position = position;
            this.children = children;
        }

        public void TickColor()
        {
            switch (node.GetStatus())
            {
                case Pathfinding.BehaviourTrees.Node.Status.Running:
                    graphNode.style.backgroundColor =
                        new Color(1f,0.8f,0f);
                    break;
                case Pathfinding.BehaviourTrees.Node.Status.Success:
                    graphNode.style.backgroundColor =
                        new Color(0.2f,1f,0.2f);
                    break;
                case Pathfinding.BehaviourTrees.Node.Status.Failure:
                    graphNode.style.backgroundColor =
                        new Color(1f,0.0f,0f);
                    break;
                default:
                    graphNode.style.backgroundColor =
                        new Color(0.5f,0.5f,0.5f);
                    break;
            }

            foreach (BehNode child in children)
            {
                child.TickColor();
            }
        }
    }

}
