#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.Shapes;

/// <summary>
/// Hollow hub + one corridor arm per entry in <see cref="corridorDoorWalls"/>.
/// Hub walls get a door opening if at least one entry uses that side (unique sides only — duplicate entries overlap).
/// </summary>
public class PB_CreateHubWithCorridors : EditorWindow
{
    public enum WallSide { North, South, East, West } // North=+Z, South=-Z, East=+X, West=-X

    private Vector3 hubSize = new Vector3(14f, 3f, 10f);
    private float hubWallThickness = 0.25f;

    /// <summary>One corridor per element; hub openings are the set of distinct <see cref="WallSide"/> values.</summary>
    private readonly List<WallSide> corridorDoorWalls = new List<WallSide> { WallSide.North, WallSide.East };

    private float doorWidth = 1.6f;
    private float doorHeight = 2.2f;

    private Vector3 corridorSize = new Vector3(4f, 3f, 8f); // X = width, Y = height, Z = length (runs along local ±Z)
    private float corridorWallThickness = 0.25f;

    private bool corridorOpenTowardHub = true;  // opening on corridor South (-Z) end
    private bool corridorOpenFarEnd = false;   // opening on corridor North (+Z) end

    public Material wallMaterial;

    [MenuItem("Tools/Level Gen/Simple/Create Hub + Corridors (ProBuilder)")]
    public static void Open() => GetWindow<PB_CreateHubWithCorridors>("Hub + Corridors");

    private void OnGUI()
    {
        GUILayout.Label("Hub + Corridors", EditorStyles.boldLabel);

        GUILayout.Label("Hub", EditorStyles.boldLabel);
        hubSize = EditorGUILayout.Vector3Field("Hub Size (X,Y,Z)", hubSize);
        hubWallThickness = EditorGUILayout.FloatField("Hub Wall Thickness", hubWallThickness);

        GUILayout.Space(4);
        GUILayout.Label("Doors / corridor arms (one corridor per row)", EditorStyles.boldLabel);
        doorWidth = EditorGUILayout.FloatField("Door Width", doorWidth);
        doorHeight = EditorGUILayout.FloatField("Door Height", doorHeight);

        for (int i = 0; i < corridorDoorWalls.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"#{i + 1}", GUILayout.Width(28));
            corridorDoorWalls[i] = (WallSide)EditorGUILayout.EnumPopup(corridorDoorWalls[i]);
            EditorGUI.BeginDisabledGroup(corridorDoorWalls.Count <= 1);
            if (GUILayout.Button("−", GUILayout.Width(24)))
            {
                corridorDoorWalls.RemoveAt(i);
                i--;
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
        }

        if (GUILayout.Button("Add corridor / door"))
            corridorDoorWalls.Add(WallSide.North);

        GUILayout.Space(4);
        GUILayout.Label("Corridors (shared size)", EditorStyles.boldLabel);
        corridorSize = EditorGUILayout.Vector3Field("Corridor Size (X,Y,Z)", corridorSize);
        corridorWallThickness = EditorGUILayout.FloatField("Corridor Wall Thickness", corridorWallThickness);
        corridorOpenTowardHub = EditorGUILayout.Toggle("Open toward hub (−Z end)", corridorOpenTowardHub);
        corridorOpenFarEnd = EditorGUILayout.Toggle("Open far end (+Z end)", corridorOpenFarEnd);

        GUILayout.Space(4);
        wallMaterial = EditorGUILayout.ObjectField("Wall Material", wallMaterial, typeof(Material), false) as Material;

        GUILayout.Space(8);
        if (GUILayout.Button("Create Hub + Corridors"))
            CreateEverything();

        if (corridorDoorWalls.Count == 0)
            EditorGUILayout.HelpBox("Add at least one door row before creating.", MessageType.Warning);
        else if (corridorDoorWalls.GroupBy(w => w).Any(g => g.Count() > 1))
            EditorGUILayout.HelpBox("The same wall appears more than once: the hub still has only one opening per wall, but multiple corridors will use the same arm transform and overlap.", MessageType.Warning);
        if (Mathf.Abs(hubSize.y - corridorSize.y) > 0.01f)
            EditorGUILayout.HelpBox("Hub Y and corridor Y differ: floor/heights may not line up.", MessageType.Info);
    }

    private void CreateEverything()
    {
        if (corridorDoorWalls.Count == 0)
        {
            Debug.LogWarning("PB_CreateHubWithCorridors: no door rows — nothing created.");
            return;
        }

        var root = new GameObject("PB_HubWithCorridors");
        root.transform.position = Vector3.zero;

        var hubHolder = new GameObject("Hub");
        hubHolder.transform.SetParent(root.transform, false);

        CreateHollowHubWithDoors(
            namePrefix: "Hub",
            center: Vector3.zero,
            size: hubSize,
            thickness: hubWallThickness,
            doorWalls: corridorDoorWalls,
            doorWidth: doorWidth,
            doorHeight: doorHeight,
            parent: hubHolder.transform,
            wallMaterial: wallMaterial);

        for (int i = 0; i < corridorDoorWalls.Count; i++)
            SpawnCorridorArm(root.transform, corridorDoorWalls[i], $"Corridor_{i + 1}_{corridorDoorWalls[i]}");

        Selection.activeGameObject = root;
        SceneView.lastActiveSceneView?.FrameSelected();
    }

    private void SpawnCorridorArm(Transform root, WallSide doorWall, string armObjectName)
    {
        var arm = new GameObject(armObjectName);
        arm.transform.SetParent(root, false);

        float halfAlong = HubHalfExtentAlongDoorWall(doorWall, hubSize);
        float halfLen = corridorSize.z * 0.5f;
        Vector3 outward = OutwardFromHub(doorWall);
        arm.transform.localPosition = outward * (halfAlong + halfLen);
        arm.transform.localRotation = ArmRotationForDoorWall(doorWall);

        bool doorSouth = corridorOpenTowardHub;
        bool doorNorth = corridorOpenFarEnd;

        CreateHollowCorridor(
            namePrefix: armObjectName,
            center: Vector3.zero,
            size: corridorSize,
            thickness: corridorWallThickness,
            doorNorth: doorNorth,
            doorSouth: doorSouth,
            doorWidth: doorWidth,
            doorHeight: doorHeight,
            parent: arm.transform,
            wallMaterial: wallMaterial);
    }

    private static float HubHalfExtentAlongDoorWall(WallSide side, Vector3 hubSize)
    {
        return (side == WallSide.North || side == WallSide.South)
            ? hubSize.z * 0.5f
            : hubSize.x * 0.5f;
    }

    private static Vector3 OutwardFromHub(WallSide side)
    {
        switch (side)
        {
            case WallSide.North: return Vector3.forward;
            case WallSide.South: return Vector3.back;
            case WallSide.East: return Vector3.right;
            case WallSide.West: return Vector3.left;
            default: return Vector3.forward;
        }
    }

    /// <summary>Corridor length runs along local +Z after this rotation (South = −Z sits on hub side).</summary>
    private static Quaternion ArmRotationForDoorWall(WallSide side)
    {
        float y = side switch
        {
            WallSide.North => 0f,
            WallSide.East => 90f,
            WallSide.South => 180f,
            WallSide.West => 270f,
            _ => 0f
        };
        return Quaternion.Euler(0f, y, 0f);
    }

    private static void CreateHollowHubWithDoors(
        string namePrefix,
        Vector3 center,
        Vector3 size,
        float thickness,
        IReadOnlyList<WallSide> doorWalls,
        float doorWidth,
        float doorHeight,
        Transform parent,
        Material wallMaterial)
    {
        var container = new GameObject(namePrefix);
        container.transform.SetParent(parent, false);
        container.transform.localPosition = center;

        float halfX = size.x * 0.5f;
        float halfY = size.y * 0.5f;
        float halfZ = size.z * 0.5f;

        var doorSet = new HashSet<WallSide>(doorWalls);
        bool doorNorth = doorSet.Contains(WallSide.North);
        bool doorSouth = doorSet.Contains(WallSide.South);
        bool doorEast = doorSet.Contains(WallSide.East);
        bool doorWest = doorSet.Contains(WallSide.West);

        CreatePBBox($"{namePrefix}_Floor", new Vector3(0f, -halfY - thickness * 0.5f, 0f), new Vector3(size.x, thickness, size.z), container.transform, wallMaterial);
        CreatePBBox($"{namePrefix}_Ceiling", new Vector3(0f, +halfY + thickness * 0.5f, 0f), new Vector3(size.x, thickness, size.z), container.transform, wallMaterial);

        if (doorWest)
            CreateWallWithDoorGap($"{namePrefix}_Wall_West", container.transform,
                wallCenterLocal: new Vector3(-halfX + thickness * 0.5f, 0f, 0f),
                span: size.z, height: size.y, thickness: thickness,
                holeWidth: doorWidth, holeHeight: doorHeight,
                wallAxisIsXSpan: false, wallMaterial: wallMaterial);
        else
            CreatePBBox($"{namePrefix}_Wall_West", new Vector3(-halfX + thickness * 0.5f, 0f, 0f), new Vector3(thickness, size.y, size.z), container.transform, wallMaterial);

        if (doorEast)
            CreateWallWithDoorGap($"{namePrefix}_Wall_East", container.transform,
                wallCenterLocal: new Vector3(+halfX - thickness * 0.5f, 0f, 0f),
                span: size.z, height: size.y, thickness: thickness,
                holeWidth: doorWidth, holeHeight: doorHeight,
                wallAxisIsXSpan: false, wallMaterial: wallMaterial);
        else
            CreatePBBox($"{namePrefix}_Wall_East", new Vector3(+halfX - thickness * 0.5f, 0f, 0f), new Vector3(thickness, size.y, size.z), container.transform, wallMaterial);

        float reducedWidth = size.x - 2f * thickness;

        if (doorSouth)
            CreateWallWithDoorGap($"{namePrefix}_Wall_South", container.transform,
                wallCenterLocal: new Vector3(0f, 0f, -halfZ + thickness * 0.5f),
                span: reducedWidth, height: size.y, thickness: thickness,
                holeWidth: doorWidth, holeHeight: doorHeight,
                wallAxisIsXSpan: true, wallMaterial: wallMaterial);
        else
            CreatePBBox($"{namePrefix}_Wall_South", new Vector3(0f, 0f, -halfZ + thickness * 0.5f), new Vector3(reducedWidth, size.y, thickness), container.transform, wallMaterial);

        if (doorNorth)
            CreateWallWithDoorGap($"{namePrefix}_Wall_North", container.transform,
                wallCenterLocal: new Vector3(0f, 0f, +halfZ - thickness * 0.5f),
                span: reducedWidth, height: size.y, thickness: thickness,
                holeWidth: doorWidth, holeHeight: doorHeight,
                wallAxisIsXSpan: true, wallMaterial: wallMaterial);
        else
            CreatePBBox($"{namePrefix}_Wall_North", new Vector3(0f, 0f, +halfZ - thickness * 0.5f), new Vector3(reducedWidth, size.y, thickness), container.transform, wallMaterial);
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
        Transform parent,
        Material wallMaterial)
    {
        var container = new GameObject(namePrefix);
        container.transform.SetParent(parent, false);
        container.transform.localPosition = center;

        float halfX = size.x * 0.5f;
        float halfY = size.y * 0.5f;
        float halfZ = size.z * 0.5f;

        CreatePBBox($"{namePrefix}_Floor",
            new Vector3(0f, -halfY - thickness * 0.5f, 0f),
            new Vector3(size.x, thickness, size.z),
            container.transform, wallMaterial);

        CreatePBBox($"{namePrefix}_Ceiling",
            new Vector3(0f, +halfY + thickness * 0.5f, 0f),
            new Vector3(size.x, thickness, size.z),
            container.transform, wallMaterial);

        CreatePBBox($"{namePrefix}_Wall_West",
            new Vector3(-halfX + thickness * 0.5f, 0f, 0f),
            new Vector3(thickness, size.y, size.z),
            container.transform, wallMaterial);

        CreatePBBox($"{namePrefix}_Wall_East",
            new Vector3(+halfX - thickness * 0.5f, 0f, 0f),
            new Vector3(thickness, size.y, size.z),
            container.transform, wallMaterial);

        float reducedWidth = size.x - 2f * thickness;
        Vector3 southCenter = new Vector3(0f, 0f, -halfZ + thickness * 0.5f);
        Vector3 northCenter = new Vector3(0f, 0f, +halfZ - thickness * 0.5f);

        if (doorSouth)
            CreateWallWithDoorGap($"{namePrefix}_Wall_South", container.transform,
                wallCenterLocal: southCenter,
                span: reducedWidth, height: size.y, thickness: thickness,
                holeWidth: doorWidth, holeHeight: doorHeight,
                wallAxisIsXSpan: true, wallMaterial: wallMaterial);
        else
            CreatePBBox($"{namePrefix}_Wall_South", southCenter, new Vector3(reducedWidth, size.y, thickness), container.transform, wallMaterial);

        if (doorNorth)
            CreateWallWithDoorGap($"{namePrefix}_Wall_North", container.transform,
                wallCenterLocal: northCenter,
                span: reducedWidth, height: size.y, thickness: thickness,
                holeWidth: doorWidth, holeHeight: doorHeight,
                wallAxisIsXSpan: true, wallMaterial: wallMaterial);
        else
            CreatePBBox($"{namePrefix}_Wall_North", northCenter, new Vector3(reducedWidth, size.y, thickness), container.transform, wallMaterial);
    }

    private static void CreateWallWithDoorGap(
        string wallName,
        Transform parent,
        Vector3 wallCenterLocal,
        float span,
        float height,
        float thickness,
        float holeWidth,
        float holeHeight,
        bool wallAxisIsXSpan,
        Material wallMaterial)
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
            Vector3 segSize = wallAxisIsXSpan
                ? new Vector3(leftWidth, height, thickness)
                : new Vector3(thickness, height, leftWidth);

            CreatePBBox($"{wallName}_Left", wallCenterLocal + SpanOffset(centerS, 0f), segSize, parent, wallMaterial);
        }

        if (rightWidth > 0.001f)
        {
            float centerS = halfSpan - rightWidth * 0.5f;
            Vector3 segSize = wallAxisIsXSpan
                ? new Vector3(rightWidth, height, thickness)
                : new Vector3(thickness, height, rightWidth);

            CreatePBBox($"{wallName}_Right", wallCenterLocal + SpanOffset(centerS, 0f), segSize, parent, wallMaterial);
        }

        if (topHeight > 0.001f)
        {
            Vector3 lintelSize = wallAxisIsXSpan
                ? new Vector3(holeWidth, topHeight, thickness)
                : new Vector3(thickness, topHeight, holeWidth);

            CreatePBBox($"{wallName}_Top", wallCenterLocal + SpanOffset(0f, lintelCenterY), lintelSize, parent, wallMaterial);
        }
    }

    private static GameObject CreatePBBox(string name, Vector3 localPos, Vector3 localSize, Transform parent, Material wallMaterial)
    {
        ProBuilderMesh pb = ShapeGenerator.GenerateCube(PivotLocation.Center, localSize);
        pb.gameObject.name = name;
        pb.transform.SetParent(parent, false);
        pb.transform.localPosition = localPos;

        if (!pb.gameObject.TryGetComponent<MeshCollider>(out _))
            pb.gameObject.AddComponent<MeshCollider>();

        pb.ToMesh();
        pb.Refresh();

        if (wallMaterial != null)
        {
            MeshRenderer mr = pb.gameObject.GetComponent<MeshRenderer>();
            mr.sharedMaterial = wallMaterial;
        }

        return pb.gameObject;
    }
}
#endif