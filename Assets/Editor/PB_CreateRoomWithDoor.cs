#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.Shapes;

public class PB_CreateRoomWithDoor : EditorWindow
{
    public enum WallSide { North, South, East, West } // North=+Z, South=-Z, East=+X, West=-X

    private Vector3 roomSize = new Vector3(10f, 3f, 10f); // width(X), height(Y), depth(Z)
    private float wallThickness = 0.25f;

    private WallSide doorWall = WallSide.North;
    private float doorWidth = 1.4f;
    private float doorHeight = 2.2f;

    [MenuItem("Tools/Level Gen/Simple/Create Room With Door (ProBuilder)")]
    public static void Open() => GetWindow<PB_CreateRoomWithDoor>("Room + Door");

    private void OnGUI()
    {
        GUILayout.Label("Create Room With Center Door", EditorStyles.boldLabel);

        roomSize = EditorGUILayout.Vector3Field("Room Size (X,Y,Z)", roomSize);
        wallThickness = EditorGUILayout.FloatField("Wall Thickness", wallThickness);

        GUILayout.Space(6);
        doorWall = (WallSide)EditorGUILayout.EnumPopup("Door Wall", doorWall);
        doorWidth = EditorGUILayout.FloatField("Door Width", doorWidth);
        doorHeight = EditorGUILayout.FloatField("Door Height", doorHeight);

        GUILayout.Space(8);
        if (GUILayout.Button("Create Room"))
        {
            CreateRoom();
        }
    }

    private void CreateRoom()
    {
        var root = new GameObject("PB_RoomWithDoor");
        root.transform.position = Vector3.zero;

        CreateHollowRoomWithSingleDoor(
            namePrefix: "Room",
            center: Vector3.zero,
            size: roomSize,
            thickness: wallThickness,
            doorWall: doorWall,
            doorWidth: doorWidth,
            doorHeight: doorHeight,
            parent: root.transform
        );

        Selection.activeGameObject = root;
        SceneView.lastActiveSceneView?.FrameSelected();
    }

    private static void CreateHollowRoomWithSingleDoor(
        string namePrefix,
        Vector3 center,
        Vector3 size,
        float thickness,
        WallSide doorWall,
        float doorWidth,
        float doorHeight,
        Transform parent)
    {
        var container = new GameObject(namePrefix);
        container.transform.SetParent(parent, false);
        container.transform.localPosition = center;

        float halfX = size.x * 0.5f;
        float halfY = size.y * 0.5f;
        float halfZ = size.z * 0.5f;

        // Floor & ceiling (positioned so inner surfaces align with room boundaries)
        CreatePBBox($"{namePrefix}_Floor", new Vector3(0f, -halfY - thickness * 0.5f, 0f), new Vector3(size.x, thickness, size.z), container.transform);
        CreatePBBox($"{namePrefix}_Ceiling", new Vector3(0f, +halfY + thickness * 0.5f, 0f), new Vector3(size.x, thickness, size.z), container.transform);

        // West/East walls (±X) span FULL Z depth
        if (doorWall == WallSide.West)
            CreateWallWithDoorGap($"{namePrefix}_Wall_West", container.transform,
                wallCenterLocal: new Vector3(-halfX + thickness * 0.5f, 0f, 0f),
                span: size.z, height: size.y, thickness: thickness,
                holeWidth: doorWidth, holeHeight: doorHeight,
                wallAxisIsXSpan: false);
        else
            CreatePBBox($"{namePrefix}_Wall_West", new Vector3(-halfX + thickness * 0.5f, 0f, 0f), new Vector3(thickness, size.y, size.z), container.transform);

        if (doorWall == WallSide.East)
            CreateWallWithDoorGap($"{namePrefix}_Wall_East", container.transform,
                wallCenterLocal: new Vector3(+halfX - thickness * 0.5f, 0f, 0f),
                span: size.z, height: size.y, thickness: thickness,
                holeWidth: doorWidth, holeHeight: doorHeight,
                wallAxisIsXSpan: false);
        else
            CreatePBBox($"{namePrefix}_Wall_East", new Vector3(+halfX - thickness * 0.5f, 0f, 0f), new Vector3(thickness, size.y, size.z), container.transform);

        // South/North walls (±Z) span REDUCED X width (to fit between East/West walls)
        float reducedWidth = size.x - 2f * thickness;

        if (doorWall == WallSide.South)
            CreateWallWithDoorGap($"{namePrefix}_Wall_South", container.transform,
                wallCenterLocal: new Vector3(0f, 0f, -halfZ + thickness * 0.5f),
                span: reducedWidth, height: size.y, thickness: thickness,
                holeWidth: doorWidth, holeHeight: doorHeight,
                wallAxisIsXSpan: true);
        else
            CreatePBBox($"{namePrefix}_Wall_South", new Vector3(0f, 0f, -halfZ + thickness * 0.5f), new Vector3(reducedWidth, size.y, thickness), container.transform);

        if (doorWall == WallSide.North)
            CreateWallWithDoorGap($"{namePrefix}_Wall_North", container.transform,
                wallCenterLocal: new Vector3(0f, 0f, +halfZ - thickness * 0.5f),
                span: reducedWidth, height: size.y, thickness: thickness,
                holeWidth: doorWidth, holeHeight: doorHeight,
                wallAxisIsXSpan: true);
        else
            CreatePBBox($"{namePrefix}_Wall_North", new Vector3(0f, 0f, +halfZ - thickness * 0.5f), new Vector3(reducedWidth, size.y, thickness), container.transform);
    }

    /// <summary>
    /// Builds a wall with a centered door gap by composing 3 boxes:
    /// left segment, right segment, and top lintel. Door reaches the floor.
    /// wallAxisIsXSpan:
    /// - true: wall spans local X (front/back walls)
    /// - false: wall spans local Z (left/right walls)
    /// </summary>
    private static void CreateWallWithDoorGap(
        string wallName,
        Transform parent,
        Vector3 wallCenterLocal,
        float span,
        float height,
        float thickness,
        float holeWidth,
        float holeHeight,
        bool wallAxisIsXSpan)
    {
        holeWidth = Mathf.Clamp(holeWidth, 0.5f, span - 0.5f);
        holeHeight = Mathf.Clamp(holeHeight, 1.0f, height - 0.2f);

        float halfSpan = span * 0.5f;
        float halfH = height * 0.5f;

        float holeLeft = -holeWidth * 0.5f;
        float holeRight = holeWidth * 0.5f;

        float leftWidth = holeLeft + halfSpan;     // from -halfSpan to holeLeft
        float rightWidth = halfSpan - holeRight;   // from holeRight to +halfSpan

        float topHeight = height - holeHeight;     // lintel height
        float doorTopY = -halfH + holeHeight;
        float lintelCenterY = doorTopY + topHeight * 0.5f;

        Vector3 SpanOffset(float s, float y)
            => wallAxisIsXSpan ? new Vector3(s, y, 0f) : new Vector3(0f, y, s);

        // Left segment
        if (leftWidth > 0.001f)
        {
            float centerS = -halfSpan + leftWidth * 0.5f;
            Vector3 segSize = wallAxisIsXSpan
                ? new Vector3(leftWidth, height, thickness)
                : new Vector3(thickness, height, leftWidth);

            CreatePBBox($"{wallName}_Left", wallCenterLocal + SpanOffset(centerS, 0f), segSize, parent);
        }

        // Right segment
        if (rightWidth > 0.001f)
        {
            float centerS = halfSpan - rightWidth * 0.5f;
            Vector3 segSize = wallAxisIsXSpan
                ? new Vector3(rightWidth, height, thickness)
                : new Vector3(thickness, height, rightWidth);

            CreatePBBox($"{wallName}_Right", wallCenterLocal + SpanOffset(centerS, 0f), segSize, parent);
        }

        // Top lintel
        if (topHeight > 0.001f)
        {
            Vector3 lintelSize = wallAxisIsXSpan
                ? new Vector3(holeWidth, topHeight, thickness)
                : new Vector3(thickness, topHeight, holeWidth);

            CreatePBBox($"{wallName}_Top", wallCenterLocal + SpanOffset(0f, lintelCenterY), lintelSize, parent);
        }
    }

    private static GameObject CreatePBBox(string name, Vector3 localPos, Vector3 localSize, Transform parent)
    {
        ProBuilderMesh pb = ShapeGenerator.GenerateCube(PivotLocation.Center, localSize);
        pb.gameObject.name = name;
        pb.transform.SetParent(parent, false);
        pb.transform.localPosition = localPos;

        if (!pb.gameObject.TryGetComponent<MeshCollider>(out _))
            pb.gameObject.AddComponent<MeshCollider>();

        pb.ToMesh();
        pb.Refresh();
        return pb.gameObject;
    }
}
#endif