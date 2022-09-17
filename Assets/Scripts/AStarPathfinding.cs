using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

public class AStarPathfinding : MonoBehaviour
{
    [Header("Grid")]
    public Vector2Int gridSize;
    public int gridScale;

    [Header("Node")]
    private GameObject nodeHolder;
    public GameObject nodePrefab;
    public Material defaultMaterial;
    public Material wallMaterial;
    public Material closedMaterials;
    public Material openMaterials;
    public Material resultMaterials;
    public List<PathNode> pathNodes;
    private List<GameObject> pathNodeGameObjects;
    private List<Vector2Int> neighborDirections = new List<Vector2Int>()
    {
        new Vector2Int(-1,0),
        new Vector2Int(1,0),
        new Vector2Int(0,1),
        new Vector2Int(0,-1),
    };

    [Header("Path Lists")]
    private bool arrived;
    public List<PathNode> openLists;
    public List<PathNode> closedLists;
    public PathNode lastNode;
    private PathNode startNode;
    private PathNode endNode;

    [Header("Find Setting")]
    public bool update;
    public List<Vector2Int> walls = new List<Vector2Int>();

    void Start()
    {
        GenerateGrid();
        Setup();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) && !arrived || update && !arrived) FindPath(lastNode);

        if (Input.GetKeyDown(KeyCode.Space) && arrived) GetResultPath();

        if (Input.GetMouseButton(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100))
            {
                Node node = hit.collider.GetComponent<Node>();
                if (node != null)
                {
                    startNode = pathNodes[node.index];
                    openLists.Clear();
                    openLists.Add(startNode);
                    lastNode = startNode;
                    print("Start Node:" + startNode.position);
                }
            }
        }

        if (Input.GetMouseButton(1))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100))
            {
                Node node = hit.collider.GetComponent<Node>();
                if (node != null)
                {
                    endNode = pathNodes[node.index];
                    print("End Node:" + endNode.position);
                }
            }
        }

        if (Input.GetMouseButton(2))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100))
            {
                Node node = hit.collider.GetComponent<Node>();
                if (node != null)
                {
                    if (pathNodes[node.index].isWalkable)
                    {
                        walls.Add(node.position);
                        pathNodeGameObjects[node.index].GetComponent<Renderer>().material = wallMaterial;
                        pathNodes[node.index].isWalkable = false;
                    }
                }
            }
        }
    }

    void GenerateGrid()
    {
        nodeHolder = new GameObject("Node Holder");
        pathNodes = new List<PathNode>();
        pathNodeGameObjects = new List<GameObject>();

        for (int i = 0; i < gridSize.y; i++)
        {
            for (int j = 0; j < gridSize.x; j++)
            {
                Vector2Int position = new Vector2Int(j, i);
                GameObject node = Instantiate(nodePrefab, new Vector3(j, 0, i) * gridScale, Quaternion.identity);
                node.transform.parent = nodeHolder.transform;
                node.transform.localScale *= gridScale;
                node.transform.name = "Node:" + j + "," + i;

                node.GetComponentInChildren<TextMeshPro>().text = string.Format("Node:{0},{1} \n g:{2} \n h:{3} \n f:{4}", j, i, 0, 0, 0);
                node.GetComponent<Node>().index = GetIndexFromVector(position);
                node.GetComponent<Node>().position = position;

                pathNodeGameObjects.Add(node);

                PathNode path;
                if (walls.Contains(position))
                {
                    node.GetComponent<Renderer>().material = wallMaterial;
                    path = new PathNode(false, position);
                }
                else
                    path = new PathNode(true, position);

                pathNodes.Add(path);
            }
        }
    }

    void Setup()
    {
        arrived = false;
        openLists.Clear();
        closedLists.Clear();
    }

    void FindPath(PathNode currentPath)
    {
        if (currentPath == null) return;
        if (currentPath.position == endNode.position)
        {
            arrived = true;
            print("Arrived");
            return;
        }

        foreach (var dir in neighborDirections)
        {
            Vector2Int neighborPos = dir + currentPath.position;
            if (neighborPos.x < 0 || neighborPos.x > gridSize.x - 1) continue;
            if (neighborPos.y < 0 || neighborPos.y > gridSize.y - 1) continue;
            if (ClosedNode(neighborPos)) continue;

            int neighborIndex = GetIndexFromVector(neighborPos);
            PathNode neighborPath = pathNodes[neighborIndex];

            if (neighborPath.isWalkable == false) continue;

            float g = Vector2.Distance(currentPath.position, neighborPath.position) + currentPath.g;
            float h = Vector2.Distance(neighborPath.position, endNode.position);
            float f = g + h;

            pathNodeGameObjects[neighborIndex].GetComponentInChildren<TextMeshPro>().text = string.Format("Node:{0},{1} \n g:{2} \n h:{3} \n f:{4}",
                                                                                            neighborPath.position.x, neighborPath.position.y, g.ToString("0.00"), h.ToString("0.00"), f.ToString("0.00"));

            if (!UpdatePath(neighborPath))
            {
                neighborPath.g = g;
                neighborPath.h = h;
                neighborPath.f = f;
                neighborPath.previousPath = currentPath;
                openLists.Add(neighborPath);
                pathNodeGameObjects[neighborIndex].GetComponent<Renderer>().material = openMaterials;
            }
        }

        openLists = openLists.OrderBy(path => path.f).ThenBy(neighbor => neighbor.h).ToList<PathNode>();
        pathNodeGameObjects[GetIndexFromVector(openLists[0].position)].GetComponent<Renderer>().material = closedMaterials;
        closedLists.Add(openLists[0]);
        lastNode = openLists[0];
        openLists.RemoveAt(0);
    }

    void GetResultPath()
    {
        for (int i = 0; i < pathNodeGameObjects.Count; i++)
            if (pathNodes[i].isWalkable)
                pathNodeGameObjects[i].GetComponent<Renderer>().material = defaultMaterial;

        PathNode beginPath = lastNode;
        while (beginPath != null)
        {
            pathNodeGameObjects[GetIndexFromVector(beginPath.position)].GetComponent<Renderer>().material = resultMaterials;
            beginPath = beginPath.previousPath;
        }
    }

    bool UpdatePath(PathNode neighborPath)
    {
        foreach (PathNode p in openLists)
            if (p.position == neighborPath.position)
                return true;
        return false;
    }

    bool ClosedNode(Vector2Int pathPos)
    {
        foreach (PathNode p in closedLists)
            if (p.position == pathPos) return true;

        return false;
    }

    int GetIndexFromVector(Vector2Int vector)
    {
        return vector.x + (vector.y * gridSize.y);
    }
}

[System.Serializable]
public class PathNode
{
    public bool isWalkable;
    public Vector2Int position;
    public PathNode previousPath;

    [Header("A* Values")]
    public float g;
    public float h;
    public float f;

    public PathNode(bool isWalkable, Vector2Int position)
    {
        this.isWalkable = isWalkable;
        this.position = position;
        previousPath = null;
        g = 0;
        h = 0;
        f = 0;
    }
}
