using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityStandardAssets.Vehicles.Car.Map;


namespace UnityStandardAssets.Vehicles.Car
{
    [RequireComponent(typeof(CarController))]
    public class CarAI : MonoBehaviour
    {
        private CarController m_Car; // the car controller we want to use
        private MapManager mapManager;
        private BoxCollider carCollider;
        
        private List<Vector3> path;
        private List<Vector3> obstacleMap;
        private Vector3 previousPosition;
        private float timeSinceLastProgress;
        private const float progressCheckInterval = 1.0f; // Time interval in seconds to check for progress
        private const float reverseDuration = 10.0f; // Time duration in seconds to reverse
        private bool isReversing = false;
        private float reverseStartTime;

        private int scaleFactor = 5;

        public class Node
        {
            public Vector2Int worldPosition;
            public Node parent;
            public int gCost;
            public int hCost;
            public int fCost { get { return gCost + hCost; } }

            public Node(Vector2Int _worldPos, Vector2Int _startPos, Vector2Int _targetPos)
            {
                parent = null;
                worldPosition = _worldPos;
                gCost = GetDistance(_startPos, _worldPos );
                hCost = GetDistance(_worldPos, _targetPos);
            }

            public override bool Equals(object obj)
            {
                return obj is Node node && worldPosition.Equals(node.worldPosition);
            }

            public override int GetHashCode()
            {
                return worldPosition.GetHashCode();
            }




        }
        private List<Vector3> FindPath(Vector2Int startWorldPos, Vector2Int targetWorldPos)
        {
            // NOTES: We are creating new Nodes in "Neighbour" and we are using these new Node Objects in the open/closed sets. When we try to use equal we are doing object reference. 
            //        so even if Nodes have same griddPoint they will be treated as different Nodes whhich in turn makes use pick the same Node multiple times thus a infinite loop. 
            // Neighbour added into OpenSet can be a Node we have already traversed. The algorithm wrongly chooses the node, it does not take the node with the lowest hCost when multiple nodes have same fCost
            startWorldPos *= scaleFactor;
            targetWorldPos *= scaleFactor;
            
            Debug.Log(startWorldPos);
            Debug.Log(targetWorldPos)
            ;

  
            Node startNode = new Node(startWorldPos, startWorldPos, targetWorldPos);
            Node targetNode = new Node(targetWorldPos, startWorldPos, targetWorldPos);



            Debug.Log(startNode.hCost);
            Debug.Log(targetNode.hCost);

            if (targetNode == null || startNode == null) {
                Debug.Log("Start/Target = Null");
                return new List<Vector3>();
            }

            
            //List<Node> openSet = new List<Node>();
            //HashSet<Node> closedSet = new HashSet<Node>();

            Dictionary<Vector2Int, Node> openSet1 = new Dictionary<Vector2Int, Node>();
            HashSet<Vector2Int> closedSet1 = new HashSet<Vector2Int>();


            var obstacleMap = mapManager.GetObstacleMap();
            //Dictionary<Vector2Int, ObstacleMap.Traversability> mapData = obstacleMap.traversabilityPerCell;


            Dictionary<Vector2Int, ObstacleMap.Traversability> mapData = increaseResolution(obstacleMap.traversabilityPerCell);
            Dictionary<Vector2Int, List<GameObject>> gameObjectsData = obstacleMap.gameGameObjectsPerCell; //LÄS: använd obstacleMap för att kolla för partial Blocks. Dom innehåller information om
                                                                                                                // hur blocken är blockerade

            Debug.LogError(mapData.Count);
            //Debug.LogError(newMapData.Count);
            //openSet.Add(startNode);
            openSet1.Add(startWorldPos, startNode);
            var j = 0;
            Node oldNode = startNode;
            while (openSet1.Count > 0 )
            {
                Node currentNode = GetLowestCostNode(openSet1);


                foreach(var pair in openSet1)
                {
                    Node node = pair.Value;
                    if(node.fCost < currentNode.fCost || (node.fCost == currentNode.fCost && node.hCost < currentNode.hCost )){
                        currentNode = node;

                    }

                }
                Debug.Log("Current Node in OpenSet: " + currentNode.worldPosition);
                if (currentNode == null){
                    Debug.Log("CurrentNode == null!");
                    return new List<Vector3>();
                }



                //for (int i = 1; i < openSet1.Count; i++)
                //{
                //   if ((openSet1[i].fCost < currentNode.fCost) || openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost)
                //    {
                //        currentNode = openSet[i];
                //    }
                //}

                //openSet.Remove(currentNode);
                //closedSet.Add(currentNode);

                openSet1.Remove(currentNode.worldPosition);
                closedSet1.Add(currentNode.worldPosition);

                if (currentNode.worldPosition == targetWorldPos)
                {   targetNode.parent= oldNode;
                    Debug.Log("Target Reached!");
                    Debug.Log("Target Reached: startPos: " + startNode.worldPosition);
                    Debug.Log("Target Reached: goalPos: " + targetNode.worldPosition);

                    return RetracePath(startNode, targetNode);
                }


                List<Node> neighbours = GetNeighbours(currentNode, startWorldPos, targetWorldPos, mapData, gameObjectsData);

                Debug.Log("GetNeighbours Done!");
                foreach (Node neighbour in neighbours)
                {
                    ObstacleMap.Traversability traversability = mapData[neighbour.worldPosition];

                    if (closedSet1.Contains(neighbour.worldPosition) || traversability == ObstacleMap.Traversability.Blocked)
                    {
                        continue;
                    }

                    int newCostToNeighbour = currentNode.gCost + GetDistance(currentNode.worldPosition, neighbour.worldPosition);
                    if (newCostToNeighbour < neighbour.gCost || !openSet1.ContainsKey(neighbour.worldPosition))
                    {
                        neighbour.gCost = newCostToNeighbour;
                        neighbour.hCost = GetDistance(neighbour.worldPosition, targetNode.worldPosition);
                        neighbour.parent = currentNode;

                        if (!openSet1.ContainsKey(neighbour.worldPosition))
                            openSet1[neighbour.worldPosition] = neighbour;
                            Debug.Log(currentNode.worldPosition);
                            Debug.Log("Node Parent" + openSet1[neighbour.worldPosition].parent.worldPosition);
                    }
                }
                oldNode = currentNode;


                j = j + 1;
            }
            Debug.Log("Did not reach target (end of function)!" + " J: " + j);
            return new List<Vector3>();
        }

        private Node GetLowestCostNode(Dictionary<Vector2Int, Node> openSet)
        {
        Node lowestCostNode = null;
        float lowestCost = float.MaxValue;
        foreach (var node in openSet.Values)
            {
            if (node.fCost < lowestCost)
                {
                lowestCostNode = node;
                lowestCost = node.fCost;
                }
            }

        Debug.Log("LowestCostNode: " + lowestCostNode.worldPosition);
        return lowestCostNode;
        }       

        private List<Vector3> RetracePath(Node startNode, Node endNode)
        {
            Debug.Log("reTrace Entered");
            

            ObstacleMap obstacleMap = mapManager.GetObstacleMap();

            List<Vector3> path = new List<Vector3>();
            Debug.Log("reTrace: goalPos: " + endNode.worldPosition);

            Node currentNode = endNode;
            Node firstNode = startNode;

            while (currentNode.worldPosition != firstNode.worldPosition)
            {
                Debug.Log("Retracing Path");
                Vector2Int gridPosition = currentNode.worldPosition;

                Vector3 worldPosition = obstacleMap.grid.CellToLocalInterpolated(new Vector3Int(gridPosition.x/scaleFactor, gridPosition.y/scaleFactor, 0));
                Debug.Log("Added Path: " + worldPosition);

                path.Add(worldPosition);
                 if (currentNode.parent == null) 
                 {
                    Debug.LogError("Found a node with no parent before reaching the start node");
                    break; 
                }
                Debug.Log("RETRACE PATH: Current Node Parent: " + currentNode.parent.worldPosition);

                currentNode = currentNode.parent;
            }
            path.Reverse();
            return path;
        }

        private static int GetDistance1(Vector2Int worldPoisitionA, Vector2Int worldPositionB)
        {
            int distX = Mathf.Abs(Mathf.FloorToInt(worldPoisitionA.x - worldPositionB.x));
            int distY = Mathf.Abs(Mathf.FloorToInt(worldPoisitionA.y - worldPositionB.y));
            //if (distX > distY)
            //    return 14 * distY + 10 * (distX - distY);
            return distX +  distY ;
            //return Mathf.Max(Mathf.Abs(worldPoisitionA.x - worldPositionB.x), Mathf.Abs(worldPoisitionA.y - worldPositionB.y));
            //return Math.Abs(worldPoisitionA.x - worldPositionB.x) + Math.Abs(worldPoisitionA.y - worldPositionB.y);


            //return Mathf.RoundToInt(Vector3.Distance(worldPoisitionA, worldPositionB));
        }
        private static int GetDistance(Vector2Int positionA, Vector2Int positionB)
        {
            int dx = positionA.x - positionB.x;
            int dy = positionA.y - positionB.y;
             return Mathf.FloorToInt(Mathf.Sqrt(dx * dx + dy * dy));
        }
        


        private List<Node> GetNeighbours(Node node, Vector2Int startPos, Vector2Int targetPos, Dictionary<Vector2Int, ObstacleMap.Traversability> newMapData, Dictionary<Vector2Int, List<GameObject>> gameObjectsData)
        {           
            List<Node> neighbours = new List<Node>();
            var obstacleMap = mapManager.GetObstacleMap();
            Dictionary<Vector2Int, ObstacleMap.Traversability> mapData = newMapData;
    
            // Include diagonal directions
            Vector2Int[] directions = new Vector2Int[]
            {
                new Vector2Int(1, 0), new Vector2Int(-1, 0),
                new Vector2Int(0, 1), new Vector2Int(0, -1)
                ,new Vector2Int(1, 1), new Vector2Int(-1, -1)
                //,new Vector2Int(1, -1), new Vector2Int(-1, 1)
            };

            foreach (var dir in directions)
                {
                Vector2Int neighbourPos = node.worldPosition + dir;

                if (mapData.ContainsKey(neighbourPos) && mapData[neighbourPos] == ObstacleMap.Traversability.Free)
                    {
                    Node neighbourNode = new Node(neighbourPos, startPos, targetPos);
                    Debug.Log("Added " + neighbourPos + " with fCost/hCost : " + neighbourNode.fCost + "/" + neighbourNode.hCost + " as Neighbour to " + node.worldPosition);

                    neighbours.Add(neighbourNode);
                    }
                }

            return neighbours;
        }

        private Dictionary<Vector2Int, ObstacleMap.Traversability> increaseResolution(Dictionary<Vector2Int, ObstacleMap.Traversability> oldMapData)
        {
            var obstacleMap = mapManager.GetObstacleMap();

            Dictionary<Vector2Int, ObstacleMap.Traversability> newMapData = new Dictionary<Vector2Int, ObstacleMap.Traversability>();
            Dictionary<Vector2Int, List<GameObject>> gameObjectsData = obstacleMap.gameGameObjectsPerCell;

            //int scaleFactor = 10;
            // iterating over all elements in oldMapdata
            foreach (KeyValuePair<Vector2Int, ObstacleMap.Traversability> entry in oldMapData){

                Vector2Int gridPos = entry.Key;
                ObstacleMap.Traversability traversability = entry.Value;

                if (traversability == ObstacleMap.Traversability.Blocked  ){
                    for (int x= 0; x < scaleFactor; x+= 1){
                        for (int y = 0; y < scaleFactor; y+= 1 ){
                            Vector2Int newPoint = new Vector2Int(gridPos.x *scaleFactor +x , gridPos.y *scaleFactor + y);

                            newMapData[newPoint] = ObstacleMap.Traversability.Blocked;
                        }
                    }
                }

                else if (traversability == ObstacleMap.Traversability.Free ||traversability == ObstacleMap.Traversability.Partial) {
                    for (int x= 0; x < scaleFactor; x+= 1){
                        for (int y = 0; y < scaleFactor; y+= 1 ){
                            Vector2Int newPoint = new Vector2Int(gridPos.x * scaleFactor + x, gridPos.y *scaleFactor+ y);

                            newMapData[newPoint] = ObstacleMap.Traversability.Free;
                        }
                    }
                }

                else if (traversability == ObstacleMap.Traversability.Partial) {  // disabled
                    for (int x= 0; x < scaleFactor; x+= 1){
                        for (int y = 0; y < scaleFactor; y+= 1 ){
                            Vector2Int newPoint = new Vector2Int(gridPos.x * scaleFactor + x, gridPos.y *scaleFactor+ y);
                            
                            //List <GameObject> objects = gameObjectsData[newPoint];
                            if(IsPointInsideAnyBuilding(newPoint, gameObjectsData[newPoint]) ){  // key not foiund
                                    newMapData[newPoint] = ObstacleMap.Traversability.Free;

                            }
                            else{
                                newMapData[newPoint] = ObstacleMap.Traversability.Blocked;

                            }
                        }
                    }
                }



                else {
                    Debug.LogError("IncreaseRes: GridPos not Blocked/Partial or Free ");
                }

            }
            
            return newMapData;
        }


        public bool IsPointInsideAnyBuilding(Vector2Int gridPoint, List<GameObject> buildings)
            {

                if(buildings == null){
                    return false;
                }
                            ObstacleMap obstacleMap = mapManager.GetObstacleMap();

                Vector3 localPoint = obstacleMap.grid.CellToLocalInterpolated(new Vector3(gridPoint.x, 0, gridPoint.y)); 
            foreach (var building in buildings)
            {
                Vector3 pointInBuildingSpace = building.transform.InverseTransformPoint(localPoint);

                if (building.GetComponent<Collider>().bounds.Contains(pointInBuildingSpace))
                {
                    return true; // Point is inside this building
                }
            }

    return false; // Point is not inside any building
}

        private void Start()
        {
            carCollider = gameObject.transform.Find("Colliders/ColliderBottom").gameObject.GetComponent<BoxCollider>();
            // get the car controller
            m_Car = GetComponent<CarController>();
            mapManager = FindObjectOfType<GameManager>().mapManager;


            // Plan your path here
            Vector3 someLocalPosition = mapManager.grid.WorldToLocal(transform.position); // Position of car w.r.p map coordinate origin (not world global)
            // transform.localRotation;  Rotation w.r.p map coordinate origin (not world global)

            // This is how you access information about specific points
            var obstacleMap = mapManager.GetObstacleMap();
            obstacleMap.IsLocalPointTraversable(someLocalPosition);

            // Local to grid . See other methods for more.
            obstacleMap.grid.LocalToCell(someLocalPosition);

            // This is how you access a traversability grid or gameObjects in each cell.
            Dictionary<Vector2Int, ObstacleMap.Traversability> mapData = obstacleMap.traversabilityPerCell;
            Dictionary<Vector2Int, List<GameObject>> gameObjectsData = obstacleMap.gameGameObjectsPerCell;
            // Easy way to find all position vectors is either "Keys" in above dictionary or:
            foreach (var posThreeDim in obstacleMap.mapBounds.allPositionsWithin)
            {
                Vector2Int gridPos = new Vector2Int(posThreeDim.x, posThreeDim.z);
            }


            
            
            // If you need more details, feel free to check out the ObstacleMap class internals.


            // Replace the code below that makes a random path
            // ...

            Vector3 goal_pos = mapManager.localGoalPosition;

            //Debug.Log(start_pos);
            Vector3 start_pos = mapManager.localStartPosition;

            var grid_start_pos = obstacleMap.grid.LocalToCell(start_pos);
            var grid_goal_pos =  obstacleMap.grid.LocalToCell(goal_pos);
            
            //Debug.Log(grid_start_pos);
            List<Vector3> my_path = new List<Vector3>();

            
            Vector2Int start_vector2Int = new Vector2Int(Mathf.FloorToInt(grid_start_pos.x), Mathf.FloorToInt(grid_start_pos.y));
            Vector2Int goal_vector2Int = new Vector2Int(Mathf.FloorToInt(grid_goal_pos.x), Mathf.FloorToInt(grid_goal_pos.y));

            Debug.Log(start_vector2Int);
            path = FindPath(start_vector2Int, goal_vector2Int);
            //Debug.Log(my_path);
            // Plot your path to see if it makes sense
            // Note that path can only be seen in "Scene" window, not "Game" window
            Vector3 old_wp = start_pos;
            foreach (var wp in path)
            {
                //Debug.Log(wp);
                //Debug.Log("---");
                Debug.DrawLine(mapManager.grid.LocalToWorld(old_wp), mapManager.grid.LocalToWorld(wp), Color.cyan, 1000f);
                old_wp = wp;
            }
        }


        private void FixedUpdate()
        {
            var obstacleMap = mapManager.GetObstacleMap();

            Vector3 start_pos = mapManager.localStartPosition;
            Vector3 currentGridPosition = mapManager.grid.WorldToLocal(transform.position);

            Vector3 currentt_pos = mapManager.grid.WorldToLocal(transform.position);

            var grid_start_pos = obstacleMap.grid.LocalToCell(currentt_pos);


            var exampleObstacle = mapManager.GetObstacleMap().obstacleObjects[0];

            var globalPosition = transform.position;

            bool overlapped = Physics.ComputePenetration(
                carCollider,
                globalPosition,
                transform.rotation, // Use global position 
                exampleObstacle.GetComponent<MeshCollider>(), // Can take any collider and "project" it using position and rotation vectors.
                exampleObstacle.transform.position,
                exampleObstacle.transform.rotation,
                out var direction,
                out var distance
            );
            // 'out's give shortest direction and distance to "uncollide" two objects.
            if (overlapped || distance > 0)
            {
                // Means collider inside another   
            }
            // For more details https:docs.unity3d.com/ScriptReference/Physics.ComputePenetration.html
            ///////////////////////////

            // This is how you access information about the terrain from a simulated laser range finder
            // It might be wise to use this for error recovery, but do most of the planning before the race clock starts
            RaycastHit hit;
            float maxRange = 50f;
            if (Physics.Raycast(globalPosition + transform.up, transform.TransformDirection(Vector3.forward), out hit, maxRange))
            {
                Vector3 closestObstacleInFront = transform.TransformDirection(Vector3.forward) * hit.distance;
                Debug.DrawRay(globalPosition, closestObstacleInFront, Color.yellow);
                //   Debug.Log("Did Hit");
            }

            Debug.DrawLine(globalPosition, mapManager.GetGlobalStartPosition(), Color.cyan); // Draw in global space
            Debug.DrawLine(globalPosition, mapManager.GetGlobalGoalPosition(), Color.blue);

            // Check and print traversability of currect position
            Vector3 myLocalPosition = mapManager.grid.WorldToLocal(transform.position); // Position of car w.r.p map coordinate origin (not world global)
            //(obstacleMap.IsLocalPointTraversable(myLocalPosition));

            // Execute your path here
            // ...
            Vector2Int vector2Int = new Vector2Int(Mathf.RoundToInt(grid_start_pos.x), Mathf.RoundToInt(grid_start_pos.y));

            Dictionary<Vector2Int, ObstacleMap.Traversability> mapData = obstacleMap.traversabilityPerCell;
            ObstacleMap.Traversability traversability = mapData[vector2Int];

            Debug.Log(traversability);
            // this is how you control the car
            //m_Car.Move(0f, 0f, -1f, 0f);
            FollowPath();
        }

private void FollowPath()
{
    if (path != null && path.Count > 0)
    {
        Vector3 nextPoint = path[0];
        Vector3 directionToNextPoint = (nextPoint - transform.position).normalized;

        float angleToNextPoint = Vector3.Angle(transform.forward, directionToNextPoint);

        // Check if the next point is behind the car
        bool isNextPointBehind = angleToNextPoint > 90f;
        
        float steeringAngle = 0f;
        float acceleration = 0f;
        float brake = 0f;

        if (isNextPointBehind)
        {
            // Reverse if the next point is behind
            steeringAngle = Vector3.SignedAngle(transform.forward, -directionToNextPoint, Vector3.up);
            steeringAngle = Mathf.Clamp(steeringAngle / 45.0f, -1f, 1f);
            brake = -1.0f; // Negative acceleration for reversing
        }
        else
        {
            // Regular path following if the next point is in front
            steeringAngle = Vector3.SignedAngle(transform.forward, directionToNextPoint, Vector3.up);
            steeringAngle = Mathf.Clamp(steeringAngle / 45.0f, -1f, 1f);

            float maxSpeedInTurn = 0.5f; // Adjust this value as needed
            acceleration = Mathf.Lerp(0.6f, maxSpeedInTurn, Mathf.Abs(steeringAngle));

            if (Mathf.Abs(steeringAngle) > 0.5f) 
            {
                brake = 0.5f; // Apply brake in sharp turns
            }
        }

        m_Car.Move(steeringAngle, acceleration, brake, 0f);

        // Remove the waypoint if it's close enough
        if (Vector3.Distance(transform.position, nextPoint) < 1.0f)
        {
            path.RemoveAt(0);
        }
    }
}}}

