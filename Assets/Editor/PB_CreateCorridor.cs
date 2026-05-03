#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.Shapes;

public class PB_CreateCorridor : EditorWindow
{
    [Header("Corridor Dimensions")]
    private Vector3 corridorSize = new Vector3(7f, 5f, 7f); // width(X), height(Y), length(Z)
    private float wallThickness = 1f;

    [Header("Door Openings (Ends)")]
    private bool doorNorth = true;  // +Z end
    private bool doorSouth = true;  // -Z end
    private float doorWidth = 3f;
    private float doorHeight = 3f;

    [MenuItem("Tools/Level Gen/Simple/Create Corridor (ProBuilder)")]
    public static void Open() => GetWindow<PB_CreateCorridor>("Create Corridor");

    private void OnGUI()
    {
        GUILayout.Label("Create Corridor (Hollow Box + Optional End Doors)", EditorStyles.boldLabel);

        corridorSize = EditorGUILayout.Vector3Field("Corridor Size (X,Y,Z)", corridorSize);
        wallThickness = EditorGUILayout.FloatField("Wall Thickness", wallThickness);

        GUILayout.Space(6);
        GUILayout.Label("End Door Openings", EditorStyles.boldLabel);
        doorNorth = EditorGUILayout.Toggle("Door on North (+Z) End", doorNorth);
        doorSouth = EditorGUILayout.Toggle("Door on South (-Z) End", doorSouth);

        doorWidth = EditorGUILayout.FloatField("Door Width", doorWidth);
        doorHeight = EditorGUILayout.FloatField("Door Height", doorHeight);

        GUILayout.Space(8);
        if (GUILayout.Button("Create Corridor"))
            CreateCorridor();
    }

    private void CreateCorridor()
    {
        var root = new GameObject("PB_Corridor");
        root.transform.position = Vector3.zero;

        CreateHollowCorridor(
            namePrefix: "Corridor",
            center: Vector3.zero,
            size: corridorSize,
            thickness: wallThickness,
            doorNorth: doorNorth,
            doorSouth: doorSouth,
            doorWidth: doorWidth,
            doorHeight: doorHeight,
            parent: root.transform
        );

        Selection.activeGameObject = root;
        SceneView.lastActiveSceneView?.FrameSelected();
    }

    private static void CreateHollowCorridor(
        string namePrefix,
        Vector3 center,
        Vector3 size,
        float thickness,
        bool doorNorth,
        bool doorSouth,
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

        // Floor & ceiling (positioned so inner surfaces align with corridor boundaries)
        CreatePBBox($"{namePrefix}_Floor",
            new Vector3(0f, -halfY - thickness * 0.5f, 0f),
            new Vector3(size.x, thickness, size.z),
            container.transform);

        CreatePBBox($"{namePrefix}_Ceiling",
            new Vector3(0f, +halfY + thickness * 0.5f, 0f),
            new Vector3(size.x, thickness, size.z),
            container.transform);

        // Side walls (East/West, no doors here) span FULL Z depth
        CreatePBBox($"{namePrefix}_Wall_West",
            new Vector3(-halfX + thickness * 0.5f, 0f, 0f),
            new Vector3(thickness, size.y, size.z),
            container.transform);

        CreatePBBox($"{namePrefix}_Wall_East",
            new Vector3(+halfX - thickness * 0.5f, 0f, 0f),
            new Vector3(thickness, size.y, size.z),
            container.transform);

        // End walls (South = -Z, North = +Z) span REDUCED X width (to fit between East/West walls)
        float reducedWidth = size.x - 2f * thickness;
        Vector3 southCenter = new Vector3(0f, 0f, -halfZ + thickness * 0.5f);
        Vector3 northCenter = new Vector3(0f, 0f, +halfZ - thickness * 0.5f);

        if (doorSouth)
            CreateWallWithDoorGap($"{namePrefix}_Wall_South", container.transform,
                wallCenterLocal: southCenter,
                span: reducedWidth, height: size.y, thickness: thickness,
                holeWidth: doorWidth, holeHeight: doorHeight,
                wallAxisIsXSpan: true);
        else
            CreatePBBox($"{namePrefix}_Wall_South", southCenter, new Vector3(reducedWidth, size.y, thickness), container.transform);

        if (doorNorth)
            CreateWallWithDoorGap($"{namePrefix}_Wall_North", container.transform,
                wallCenterLocal: northCenter,
                span: reducedWidth, height: size.y, thickness: thickness,
                holeWidth: doorWidth, holeHeight: doorHeight,
                wallAxisIsXSpan: true);
        else
            CreatePBBox($"{namePrefix}_Wall_North", northCenter, new Vector3(reducedWidth, size.y, thickness), container.transform);
    }

    /// <summary>
    /// Same opening method as your Hub/Room scripts: left + right + top segments around a void.
    /// Door reaches the floor.
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

        float leftWidth = holeLeft + halfSpan;
        float rightWidth = halfSpan - holeRight;

        float topHeight = height - holeHeight;
        float doorTopY = -halfH + holeHeight;
        float lintelCenterY = doorTopY + topHeight * 0.5f;

        Vector3 SpanOffset(float s, float y) => wallAxisIsXSpan ? new Vector3(s, y, 0f) : new Vector3(0f, y, s);

        if (leftWidth > 0.001f)
        {
            float centerS = -halfSpan + leftWidth * 0.5f;
            Vector3 segSize = wallAxisIsXSpan ? new Vector3(leftWidth, height, thickness) : new Vector3(thickness, height, leftWidth);
            CreatePBBox($"{wallName}_Left", wallCenterLocal + SpanOffset(centerS, 0f), segSize, parent);
        }

        if (rightWidth > 0.001f)
        {
            float centerS = halfSpan - rightWidth * 0.5f;
            Vector3 segSize = wallAxisIsXSpan ? new Vector3(rightWidth, height, thickness) : new Vector3(thickness, height, rightWidth);
            CreatePBBox($"{wallName}_Right", wallCenterLocal + SpanOffset(centerS, 0f), segSize, parent);
        }

        if (topHeight > 0.001f)
        {
            Vector3 lintelSize = wallAxisIsXSpan ? new Vector3(holeWidth, topHeight, thickness) : new Vector3(thickness, topHeight, holeWidth);
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