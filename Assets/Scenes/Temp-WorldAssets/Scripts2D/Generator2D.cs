/*=======================================================================*\
| Daniel Díaz Romero........A01801486                        | 14-10-2026 | 
|                                                                         |
| Generator2D.cs | Main class that coordinates the 2D dungeon generation. |                         |                             |
|                                                                         |
\*=======================================================================*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;
using Graphs;

/*--<summary>-----------------------------------------------------------------*\
| Generates the dungeon with prefabs of the rooms and hallways.                |
|--<returns>-------------------------------------------------------------------|
|...                                                                           |
\*----------------------------------------------------------------------------*/
public class Generator2D : MonoBehaviour {
    enum CellType 
    {
        None,
        Room,
        Hallway
    }

    class Room 
    {
        public RectInt bounds;
        public RoomPrefabOption prefabOption;

        public Room(Vector2Int location, Vector2Int size) 
        {
            bounds = new RectInt(location, size);
        }

        public static bool Intersect(Room a, Room b) 
        {
            return a.bounds.Overlaps(b.bounds);
        }
    }

    [Header("Generation")]
    [SerializeField] private Vector2Int size = new Vector2Int(80, 80);
    [SerializeField, Min(1)] private int roomCount = 10;
    [SerializeField] private int maxAttemptsPerRoom = 100;
    
    [Header("Room Prefabs")]
    [SerializeField] private List<RoomPrefabOption> roomPrefabs = 
        new List<RoomPrefabOption>();
    [SerializeField] private bool applyRoomMaterial = false;

    [Header("Hallway Prefab")]
    [SerializeField] private GameObject hallwayPrefab;

    [SerializeField] private Material hallwayMaterial;

    [Header("Player Prefab")]
    [Tooltip("Drag the player from Hierarchy.")]
    [SerializeField] private GameObject playerInScene;
    [SerializeField] private float playerSpawnHeight = 0.5f;

    [Header("Installation Organization")]
    [Tooltip("Optional. If empty, instances will be childs of this generator.")]
    [SerializeField] private Transform generatedInstallationParent;

    [Header("Randomness")]
    [Tooltip("Activate for a fixed seed.")]
    [SerializeField] private bool useFixedSeed = false;
    [SerializeField] private int seed = 0;

    private Random random;
    private Grid2D<CellType> grid;
    private List<Room> rooms = new List<Room>();
    private Delaunay2D delaunay;
    private HashSet<Prim.Edge> selectedEdges = new HashSet<Prim.Edge>();
    private List<Vertex> roomVertices = new List<Vertex>();
    private HashSet<Vector2Int> hallwayCells = new HashSet<Vector2Int>();
    private List<GameObject> spawnedObjects = new List<GameObject>();

    private void Start() 
    {
        Generate();
    }

    /*--<summary>-----------------------------------------------------------------*\
    | Executes once again the instalation generation.                              |
    |--<returns>-------------------------------------------------------------------|
    |...                                                                           |
    \*----------------------------------------------------------------------------*/
    public void Generate() 
    {
        ClearPreviousGeneration();

        random = useFixedSeed ? new Random(seed) : new Random();
        grid = new Grid2D<CellType>(size, Vector2Int.zero);
        rooms = new List<Room>();
        selectedEdges = new HashSet<Prim.Edge>();
        roomVertices = new List<Vertex>();
        hallwayCells = new HashSet<Vector2Int>();

        if (size.x < 3 || size.y < 3)
        {
            Debug.LogError("Generator2D: Grid size must be at least 3x3.", this);
            return;
        }

        if (roomPrefabs == null || roomPrefabs.Count == 0)
        {
            Debug.LogError("Generator2D: add at least one valid element to Room Prefabs.", this);
            return;
        }

        PlaceRooms();

        if (rooms.Count == 0)
        {
            Debug.LogError("Generator2D: No room could be placed. Check the grid size and the footprints.", this);
            return;
        }

        Triangulate();
        CreateHallways();
        PathfindHallways();
        SpawnOrMovePlayer();

        if (rooms.Count < roomCount)
        {
            Debug.LogWarning(
                $"Generator2D: {roomCount} rooms were requested, but only {rooms.Count} were placed. " +
                "Try a larger grid, smaller footprints, or more attempts.", this);
        }
    }

    private void ClearPreviousGeneration()
    {
        for (int i = spawnedObjects.Count - 1; i >= 0; i--)
        {
            if (spawnedObjects[i] != null)
                Destroy(spawnedObjects[i]);
        }

        spawnedObjects.Clear();
    }

    private Transform GetInstallationParent()
    {
        return generatedInstallationParent != null ? generatedInstallationParent : transform;
    }

    private GameObject SpawnPrefab(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
            return null;

        GameObject instance = Instantiate(prefab, position, rotation, GetInstallationParent());
        spawnedObjects.Add(instance);
        return instance;
    }

    private void ApplyMaterial(GameObject instance, Material material)
    {
        if (instance == null || material == null)
            return;

        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
                materials[i] = material;

            renderer.sharedMaterials = materials;
        }
    }

    private RoomPrefabOption ChooseRoomPrefab()
    {
        if (roomPrefabs == null || roomPrefabs.Count == 0)
            return null;

        float totalWeight = 0f;
        foreach (RoomPrefabOption option in roomPrefabs)
        {
            if (option != null && option.prefab != null && option.weight > 0f)
                totalWeight += option.weight;
        }

        if (totalWeight <= 0f)
            return null;

        float choice = (float)random.NextDouble() * totalWeight;
        foreach (RoomPrefabOption option in roomPrefabs)
        {
            if (option == null || option.prefab == null || option.weight <= 0f)
                continue;

            choice -= option.weight;
            if (choice < 0f)
                return option;
        }

        return null;
    }

    private void PlaceRooms()
    {
        for (int i = 0; i < roomCount; i++)
        {
            bool placed = false;

            for (int attempt = 0; attempt < maxAttemptsPerRoom; attempt++)
            {
                RoomPrefabOption option = ChooseRoomPrefab();
                if (option == null)
                {
                    Debug.LogError("Generator2D: There are no valid room options with a weight greater than zero.", this);
                    return;
                }

                Vector2Int footprint = option.footprint;
                // We leave at least one empty cell outside each wall so that
                // the corridors can connect to the doors without leaving the grid.
                if (footprint.x <= 0 || footprint.y <= 0 ||
                    footprint.x + 2 > size.x || footprint.y + 2 > size.y)
                {
                    continue;
                }

                Vector2Int location = new Vector2Int(
                    random.Next(1, size.x - footprint.x),
                    random.Next(1, size.y - footprint.y));

                if (!TryCreateRoom(location, option, out Room newRoom))
                    continue;

                foreach (Vector2Int pos in newRoom.bounds.allPositionsWithin)
                    grid[pos] = CellType.Room;

                PlaceRoom(newRoom);
                placed = true;
                break;
            }

            if (!placed)
            {
                Debug.LogWarning(
                    $"Generator2D: Room {i + 1} could not be placed. Processing will continue with the rooms already placed.",
                    this);
                break;
            }
        }
    }

    private bool TryCreateRoom(Vector2Int position, RoomPrefabOption option, out Room room)
    {
        room = null;

        if (option == null || option.prefab == null)
            return false;

        Vector2Int footprint = option.footprint;
        if (footprint.x <= 0 || footprint.y <= 0)
            return false;

        if (position.x < 1 || position.y < 1 ||
            position.x + footprint.x >= size.x ||
            position.y + footprint.y >= size.y)
            return false;

        // The correct constructor takes location and size, not RectInt.
        Room candidate = new Room(position, footprint);

        // Reserve a one-cell margin to reduce overlaps and leave space
        // for aisles. The margin can extend outside the grid.
        Room buffer = new Room(
            position + new Vector2Int(-1, -1),
            footprint + new Vector2Int(2, 2));

        foreach (Room existingRoom in rooms)
        {
            if (Room.Intersect(existingRoom, buffer))
                return false;
        }

        candidate.prefabOption = option;
        rooms.Add(candidate);
        room = candidate;
        return true;
    }
    
    private void PlaceRoom(Room room)
    {
        if (room == null || room.prefabOption == null)
            return;

        Vector2 center = room.bounds.center;

        // It is assumed that the dungeon is on the XZ plane and that the prefab's pivot
        // is at the center of the room.
        Vector3 worldPosition = new Vector3(center.x, 0f, center.y);
        GameObject instance = SpawnPrefab(
            room.prefabOption.prefab,
            worldPosition,
            Quaternion.identity);

        if (applyRoomMaterial)
            ApplyMaterial(instance, room.prefabOption.material);
    }

    private void Triangulate()
    {
        roomVertices.Clear();

        foreach (Room room in rooms)
        {
            Vector2 center = room.bounds.center;
            roomVertices.Add(new Vertex<Room>(center, room));
        }

        if (roomVertices.Count >= 3)
            delaunay = Delaunay2D.Triangulate(roomVertices);
        else
            delaunay = null;
    }

    private void CreateHallways()
    {
        selectedEdges.Clear();

        if (roomVertices.Count < 2)
            return;

        List<Prim.Edge> edges = new List<Prim.Edge>();

        if (delaunay != null)
        {
            foreach (var edge in delaunay.Edges)
                edges.Add(new Prim.Edge(edge.U, edge.V));
        }

        // Delaunay typically requires at least three non-coincident points. 
        // If no edges are returned, we connect consecutive rooms
        // so that an empty triangulation does not break generation.
        if (edges.Count == 0)
        {
            for (int i = 1; i < roomVertices.Count; i++)
                edges.Add(new Prim.Edge(roomVertices[i - 1], roomVertices[i]));
        }

        if (edges.Count == 0)
            return;

        List<Prim.Edge> mst = Prim.MinimumSpanningTree(edges, edges[0].U);
        selectedEdges = new HashSet<Prim.Edge>(mst);

        HashSet<Prim.Edge> remainingEdges = new HashSet<Prim.Edge>(edges);
        remainingEdges.ExceptWith(selectedEdges);

        foreach (Prim.Edge edge in remainingEdges)
        {
            if (random.NextDouble() < 0.125)
                selectedEdges.Add(edge);
        }
    }

    private void PathfindHallways()
    {
        if (selectedEdges == null || selectedEdges.Count == 0)
            return;

        InstallationPathfinder2D aStar = new InstallationPathfinder2D(size);

        foreach (Prim.Edge edge in selectedEdges)
        {
            Room startRoom = (edge.U as Vertex<Room>)?.Item;
            Room endRoom = (edge.V as Vertex<Room>)?.Item;

            if (startRoom == null || endRoom == null)
                continue;

            Vector2 startCenter = startRoom.bounds.center;
            Vector2 endCenter = endRoom.bounds.center;

            // Each room prefab must have an opening in the center
            // of its four sides. The path begins just outside those openings.
            Vector2Int startPos = GetOutsideDoorCell(startRoom, endCenter);
            Vector2Int endPos = GetOutsideDoorCell(endRoom, startCenter);

            if (!grid.InBounds(startPos) || !grid.InBounds(endPos))
                continue;

            var path = aStar.FindPath(startPos, endPos,
                (InstallationPathfinder2D.Node a, InstallationPathfinder2D.Node b) =>
                {
                    var pathCost = new InstallationPathfinder2D.PathCost();
                    pathCost.cost = Vector2Int.Distance(b.Position, endPos);

                    // We do not allow a route to pass through the interior of another room.
                    if (grid[b.Position] == CellType.Room)
                    {
                        pathCost.traversable = false;
                        return pathCost;
                    }

                    if (grid[b.Position] == CellType.None)
                        pathCost.cost += 5;
                    else if (grid[b.Position] == CellType.Hallway)
                        pathCost.cost += 1;

                    pathCost.traversable = true;
                    return pathCost;
                });

            if (path == null)
                continue;

            foreach (Vector2Int current in path)
            {
                if (!grid.InBounds(current))
                    continue;

                if (grid[current] == CellType.None)
                    grid[current] = CellType.Hallway;

                if (grid[current] == CellType.Hallway)
                    hallwayCells.Add(current);
            }
        }

        foreach (Vector2Int cell in hallwayCells)
            PlaceHallway(cell);
    }

    private Vector2Int GetOutsideDoorCell(Room room, Vector2 targetCenter)
    {
        Vector2 roomCenter = room.bounds.center;

        int centerX = Mathf.Clamp(
            Mathf.FloorToInt(roomCenter.x),
            room.bounds.xMin,
            room.bounds.xMax - 1);

        int centerY = Mathf.Clamp(
            Mathf.FloorToInt(roomCenter.y),
            room.bounds.yMin,
            room.bounds.yMax - 1);

        Vector2 delta = targetCenter - roomCenter;

        if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
        {
            return delta.x >= 0f
                ? new Vector2Int(room.bounds.xMax, centerY)
                : new Vector2Int(room.bounds.xMin - 1, centerY);
        }

        return delta.y >= 0f
            ? new Vector2Int(centerX, room.bounds.yMax)
            : new Vector2Int(centerX, room.bounds.yMin - 1);
    }

    private void PlaceHallway(Vector2Int position)
    {
        if (hallwayPrefab == null)
        {
            Debug.LogWarning("Generator2D: asign a prefab on Hallway Prefab.", this);
            return;
        }

        // Assumes a single-cell corridor segment, centered on that cell.
        Vector3 worldPosition = new Vector3(position.x + 0.5f, 0f, position.y + 0.5f);
        GameObject instance = SpawnPrefab(
            hallwayPrefab,
            worldPosition,
            Quaternion.identity);

        ApplyMaterial(instance, hallwayMaterial);
    }

    private void SpawnOrMovePlayer()
    {
        if (playerInScene == null)
        {
            Debug.LogWarning(
                "Generator2D: arrastra el jugador existente desde la Hierarchy al campo Player In Scene.",
                this);
            return;
        }

        if (rooms == null || rooms.Count == 0)
            return;

        Vector2 center = rooms[0].bounds.center;
        Vector3 spawnPosition = new Vector3(center.x, playerSpawnHeight, center.y);

        // A prefab is not instantiated; the character already in the scene is moved.
        playerInScene.transform.SetPositionAndRotation(spawnPosition, Quaternion.identity);

        // Resets speeds to prevent retaining movement from a previous generation.
        Rigidbody body = playerInScene.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
    }
    
    /*--<summary>-----------------------------------------------------------------*\
    | Auxiliar metods to place prefabs, asign optional materials to the renderers. |
    | and to maintain the instances of the dungeos in a parent object.             |
    |--<returns>-------------------------------------------------------------------|
    |...                                                                           |
    \*----------------------------------------------------------------------------*/ 
    

    /*--<summary>-----------------------------------------------------------------*\
    | Selects a room prefab from the list of available options based on their      |
    | weights. Returns null if no valid room prefab is found.                      |
    |--<returns>-------------------------------------------------------------------|
    | [null]: If no valid room prefa is found.                                     |
    \*----------------------------------------------------------------------------*/

    /*--<summary>-----------------------------------------------------------------*\
    | Method that creates every room so that a single option is selected, uses     |
    | it's size and avoids intersections.                                          |
    |--<returns>-------------------------------------------------------------------|
    | [False]: If ain't possible to place the room, and tries other position or    |
    | prefab.                                                                      |
    \*----------------------------------------------------------------------------*/    

    /*--<summary>-----------------------------------------------------------------*\
    | Takes a validated room definition and turns it into a visible in-world room  |
    | prefab in the generated dungeon.                                             |          
    |--<returns>-------------------------------------------------------------------|
    |...                                                                           |
    \*----------------------------------------------------------------------------*/ 

    /*--<summary>-----------------------------------------------------------------*\
    | Hallway spawn steps, converts the hallway tile positions into a world-space  |
    | position.                                                                    |                                             |          |
    |--<returns>-------------------------------------------------------------------|
    | [Warning]: If prefab doesn't exist and exits early.                          |                                                                        |
    \*----------------------------------------------------------------------------*/ 
    
    /*--<summary>-----------------------------------------------------------------*\
    | Method that references the position of tha player and updates their position |
    | at the center of the first room.                                             |
    |--<returns>-------------------------------------------------------------------|
    | Instantiation of the player and a warning if there is no rooms or player     |
    | prefab asigned                                                               |
    \*----------------------------------------------------------------------------*/ 
    
}
