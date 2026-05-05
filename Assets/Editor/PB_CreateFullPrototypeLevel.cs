#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// One-click builder for a playable janitor prototype slice.
/// Creates: player + camera, hub + two rooms + corridor, doors with point-based movement,
/// station/AI managers, room controllers/storage, shift UI, evaluation UI, and one cleaning task.
/// </summary>
public class PB_CreateFullPrototypeLevel : EditorWindow
{
    [Header("Layout")]
    [SerializeField] private Vector3 hubCenter = Vector3.zero;
    [SerializeField] private Vector3 hubSize = new Vector3(18f, 4f, 14f);
    [SerializeField] private Vector3 roomSize = new Vector3(10f, 4f, 10f);
    [SerializeField] private Vector3 powerRoomSize = new Vector3(8f, 4f, 8f);
    [SerializeField] private Vector3 oxygenRoomSize = new Vector3(12f, 4f, 12f);
    [SerializeField] private float roomOffsetX = 24f;
    [SerializeField] private float corridorWidth = 3f;
    [SerializeField] private float wallThickness = 0.25f;

    [Header("Doors")]
    [SerializeField] private float doorWidth = 1.8f;
    [SerializeField] private float doorHeight = 2.4f;
    [SerializeField] private float doorDepth = 0.25f;
    [SerializeField] private float doorSpeed = 2.5f;

    [Header("Gameplay")]
    [SerializeField] private float shiftDurationSeconds = 300f;
    [SerializeField] private float roomReqAmount = 2f;
    [SerializeField] private float roomMaxAmount = 100f;

    [Header("Optional material")]
    [SerializeField] private Material wallMaterial;
    [Header("UI Template")]
    [SerializeField] private string uiTemplateScenePath = "Assets/Scenes/PrototypeScene 1.unity";
    [Header("Room Layout Tuning")]
    [SerializeField] private float roomPcWallInset = 1.6f;
    [SerializeField] private float roomPcZOffset = -2.1f;
    [SerializeField] private float roomPcTiltDegrees = 42f;
    [SerializeField] private float roomPuzzleWallInset = 2.6f;
    [SerializeField] private float roomPuzzleZOffset = 2.0f;
    [SerializeField] private float roomPuzzleHeight = 0.55f;

    [MenuItem("Tools/Level Gen/Prototype/Create Full Prototype Level")]
    public static void Open() => GetWindow<PB_CreateFullPrototypeLevel>("Full Prototype Level");

    private void OnGUI()
    {
        GUILayout.Label("Full Prototype Level Builder", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        hubCenter = EditorGUILayout.Vector3Field("Hub Center", hubCenter);
        hubSize = EditorGUILayout.Vector3Field("Hub Size", hubSize);
        roomSize = EditorGUILayout.Vector3Field("Room Size", roomSize);
        roomOffsetX = EditorGUILayout.FloatField("Room Offset X", roomOffsetX);
        corridorWidth = EditorGUILayout.FloatField("Corridor Width", corridorWidth);
        wallThickness = EditorGUILayout.FloatField("Wall Thickness", wallThickness);

        EditorGUILayout.Space(6);
        doorWidth = EditorGUILayout.FloatField("Door Width", doorWidth);
        doorHeight = EditorGUILayout.FloatField("Door Height", doorHeight);
        doorDepth = EditorGUILayout.FloatField("Door Depth", doorDepth);
        doorSpeed = EditorGUILayout.FloatField("Door Speed", doorSpeed);

        EditorGUILayout.Space(6);
        shiftDurationSeconds = EditorGUILayout.FloatField("Shift Duration (sec)", shiftDurationSeconds);
        roomReqAmount = EditorGUILayout.FloatField("Room Req Amount", roomReqAmount);
        roomMaxAmount = EditorGUILayout.FloatField("Room Max Amount", roomMaxAmount);

        EditorGUILayout.Space(6);
        wallMaterial = EditorGUILayout.ObjectField("Wall Material", wallMaterial, typeof(Material), false) as Material;

        EditorGUILayout.Space(8);
        EditorGUILayout.HelpBox("Creates a new hierarchy root named Generated_PrototypeLevel. Existing scene objects are untouched.", MessageType.Info);

        if (GUILayout.Button("Create Full Prototype Level", GUILayout.Height(32)))
            CreateFullLevel();
    }

    private void CreateFullLevel()
    {
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Create Full Prototype Level");
        int group = Undo.GetCurrentGroup();

        GameObject root = new GameObject("Generated_PrototypeLevel");
        Undo.RegisterCreatedObjectUndo(root, "Create root");

        EnsureEventSystem();

        // === World layout ===
        var worldRoot = NewChild(root.transform, "World");
        var hub = CreateRoomShell(worldRoot, "Hub", hubCenter, hubSize, wallThickness, openEast: true, openWest: true);

        Vector3 powerCenter = hubCenter + Vector3.right * roomOffsetX;
        Vector3 oxyCenter = hubCenter + Vector3.left * roomOffsetX;

        var powerRoom = CreateRoomShell(worldRoot, "Room-Power", powerCenter, powerRoomSize, wallThickness, openWest: true);
        var oxyRoom = CreateRoomShell(worldRoot, "Room-Oxygen", oxyCenter, oxygenRoomSize, wallThickness, openEast: true);

        float hubHalfX = hubSize.x * 0.5f;
        float powerHalfX = powerRoomSize.x * 0.5f;
        float oxygenHalfX = oxygenRoomSize.x * 0.5f;

        float hubDoorXRight = hubCenter.x + hubHalfX;
        float roomDoorXRight = powerCenter.x - powerHalfX;
        float rightGap = Mathf.Max(4f, Mathf.Abs(roomDoorXRight - hubDoorXRight));
        float rightMid = (hubDoorXRight + roomDoorXRight) * 0.5f;
        CreateCorridor(worldRoot, "Corridor-Power", new Vector3(rightMid, hubCenter.y, hubCenter.z), corridorWidth, hubSize.y, rightGap);

        float hubDoorXLeft = hubCenter.x - hubHalfX;
        float roomDoorXLeft = oxyCenter.x + oxygenHalfX;
        float leftGap = Mathf.Max(4f, Mathf.Abs(roomDoorXLeft - hubDoorXLeft));
        float leftMid = (hubDoorXLeft + roomDoorXLeft) * 0.5f;
        CreateCorridor(worldRoot, "Corridor-Oxygen", new Vector3(leftMid, hubCenter.y, hubCenter.z), corridorWidth, hubSize.y, leftGap);

        // Doors at both ends of each corridor
        Door powerDoor = CreateSlidingDoor(worldRoot, "Door-Power-Hub", new Vector3(hubDoorXRight, hubCenter.y, hubCenter.z));
        CreateSlidingDoor(worldRoot, "Door-Power-Room", new Vector3(roomDoorXRight, hubCenter.y, hubCenter.z));
        Door oxygenDoor = CreateSlidingDoor(worldRoot, "Door-Oxygen-Hub", new Vector3(hubDoorXLeft, hubCenter.y, hubCenter.z));
        CreateSlidingDoor(worldRoot, "Door-Oxygen-Room", new Vector3(roomDoorXLeft, hubCenter.y, hubCenter.z));
        AddLightFixtures(worldRoot, hubCenter, hubSize, powerCenter, powerRoomSize, oxyCenter, oxygenRoomSize);

        // === Core gameplay root ===
        GameObject mainRoom = new GameObject("MainRoom");
        Undo.RegisterCreatedObjectUndo(mainRoom, "Create MainRoom");
        mainRoom.transform.SetParent(root.transform, false);
        mainRoom.transform.position = hubCenter;

        var ws = mainRoom.AddComponent<WorkStation>();
        var sm = mainRoom.AddComponent<StationManager>();
        var gc = mainRoom.AddComponent<GeneralConsumption>();
        var trigger = mainRoom.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(hubSize.x + roomOffsetX + Mathf.Max(powerRoomSize.x, oxygenRoomSize.x), hubSize.y + 2f, hubSize.z + 6f);
        var rb = mainRoom.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // AI Manager
        var aiGo = NewChild(mainRoom.transform, "AIManager");
        var ai = aiGo.gameObject.AddComponent<AIManager>();

        // Storages
        Storage powerStorage = CreateStorage(mainRoom.transform, "PowerStorage", roomMaxAmount, roomReqAmount);
        Storage oxyStorage = CreateStorage(mainRoom.transform, "OxygenStorage", roomMaxAmount, roomReqAmount);

        // Room controllers
        RoomController powerRC = CreateRoomController(powerRoom.gameObject, powerRoomSize, RoomController.RoomType.Power, powerStorage, powerDoor);
        RoomController oxyRC = CreateRoomController(oxyRoom.gameObject, oxygenRoomSize, RoomController.RoomType.Oxygen, oxyStorage, oxygenDoor);
        CreateRoomConsolesAndPuzzles(mainRoom.transform, powerRoom.transform, powerRoomSize, oxyRoom.transform, oxygenRoomSize, powerRC, oxyRC,
            out Transform mainPc, out Transform powerPc, out Transform oxygenPc);

        // Player + camera
        Transform player = EnsurePlayer(root.transform, hubCenter + new Vector3(0f, 1f, -2f));

        // Manual task
        CreateCleaningTask(mainRoom.transform, new Vector3(0f, -1.75f, 2.8f));

        // UI
        BuildUI(root.transform, out GameObject uiRoot, out GameObject homePanel, out GameObject storePanel, out Button startBtn, out Button endShiftBtn,
            out TextMeshProUGUI shiftTimerText, out ShiftEvaluationUI evaluationUI, out Button continueBtn, out TextMeshProUGUI taskHintText);
        CreatePcInteractionRig(root.transform, player, uiRoot, mainPc, powerPc, oxygenPc);

        // Configure CleaningTask hint reference if found
        var clean = root.GetComponentInChildren<CleaningTask>(true);
        if (clean != null)
        {
            var soClean = new SerializedObject(clean);
            soClean.FindProperty("taskHintText").objectReferenceValue = taskHintText;
            soClean.ApplyModifiedPropertiesWithoutUndo();
        }

        // Optional terminal script
        ShiftTerminalUI terminal = homePanel.GetComponent<ShiftTerminalUI>();
        if (terminal == null)
            terminal = homePanel.AddComponent<ShiftTerminalUI>();
        {
            var soTerminal = new SerializedObject(terminal);
            soTerminal.FindProperty("acceptShiftButton").objectReferenceValue = startBtn;
            soTerminal.ApplyModifiedPropertiesWithoutUndo();
        }

        // Wire StationManager serialized refs
        WireStationManager(sm, ws, ai, evaluationUI, powerStorage, oxyStorage, new[] { powerRC, oxyRC }, uiRoot.transform, homePanel, storePanel, startBtn, endShiftBtn, shiftTimerText, continueBtn);

        // Configure GeneralConsumption
        {
            var soGc = new SerializedObject(gc);
            soGc.FindProperty("usePassiveO2").boolValue = true;
            soGc.FindProperty("usePassivePower").boolValue = true;
            soGc.FindProperty("breatheDrain").floatValue = 0.05f;
            soGc.FindProperty("powerDrain").floatValue = 0.05f;
            soGc.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = root;
        SceneView.lastActiveSceneView?.FrameSelected();
        Undo.CollapseUndoOperations(group);
        Debug.Log("[PB_CreateFullPrototypeLevel] Done. Created Generated_PrototypeLevel with core systems wired.");
    }

    private void WireStationManager(StationManager sm, WorkStation ws, AIManager ai, ShiftEvaluationUI evalUI, Storage powerStorage, Storage oxyStorage,
        RoomController[] rooms, Transform uiRoot, GameObject homePanel, GameObject storePanel, Button startBtn, Button endShiftBtn, TextMeshProUGUI shiftTimerText, Button continueBtn)
    {
        var so = new SerializedObject(sm);
        so.FindProperty("workStation").objectReferenceValue = ws;
        so.FindProperty("rooms").arraySize = rooms.Length;
        for (int i = 0; i < rooms.Length; i++)
            so.FindProperty("rooms").GetArrayElementAtIndex(i).objectReferenceValue = rooms[i];
        so.FindProperty("aiManager").objectReferenceValue = ai;
        so.FindProperty("powerStorage").objectReferenceValue = powerStorage;
        so.FindProperty("oxygenStorage").objectReferenceValue = oxyStorage;
        so.FindProperty("evaluationUI").objectReferenceValue = evalUI;
        so.FindProperty("homeUI").objectReferenceValue = homePanel;
        so.FindProperty("storeUI").objectReferenceValue = storePanel;
        so.FindProperty("startBtnUI").objectReferenceValue = startBtn;
        so.FindProperty("endShiftButton").objectReferenceValue = endShiftBtn;
        so.FindProperty("shiftTimerText").objectReferenceValue = shiftTimerText;
        so.FindProperty("viewBtn").objectReferenceValue = FindNamedInChildren<Button>(uiRoot, "ViewBtn");
        so.FindProperty("workingIcon").objectReferenceValue = FindNamedInChildren<Transform>(uiRoot, "WorkingIcon")?.gameObject;
        so.FindProperty("scoreUI").objectReferenceValue = FindNamedInChildren<TextMeshProUGUI>(uiRoot, "ScoreUI");
        so.FindProperty("powerTextUI").objectReferenceValue = FindNamedInChildren<TextMeshProUGUI>(uiRoot, "PowerTextUI");
        so.FindProperty("oxygenTextUI").objectReferenceValue = FindNamedInChildren<TextMeshProUGUI>(uiRoot, "OxygenTextUI");
        so.FindProperty("workstationLvlMain").objectReferenceValue = FindNamedInChildren<TextMeshProUGUI>(uiRoot, "WorkstationLvlMain");
        so.FindProperty("workstationCurrproductionMain").objectReferenceValue = FindNamedInChildren<TextMeshProUGUI>(uiRoot, "WorkstationCurrproductionMain");
        so.FindProperty("powerSlider").objectReferenceValue = FindNamedInChildren<Slider>(uiRoot, "PowerSlider");
        so.FindProperty("oxygenSlider").objectReferenceValue = FindNamedInChildren<Slider>(uiRoot, "OxygenSlider");
        so.FindProperty("powerStorageLvl").objectReferenceValue = FindNamedInChildren<TextMeshProUGUI>(uiRoot, "PowerStorageLvl");
        so.FindProperty("powerCurrAmount").objectReferenceValue = FindNamedInChildren<TextMeshProUGUI>(uiRoot, "PowerCurrAmount");
        so.FindProperty("powerNextLvlAmount").objectReferenceValue = FindNamedInChildren<TextMeshProUGUI>(uiRoot, "PowerNextLvlAmount");
        so.FindProperty("powerUpgradeCost").objectReferenceValue = FindNamedInChildren<TextMeshProUGUI>(uiRoot, "PowerUpgradeCost");
        so.FindProperty("oxygenStorageLvl").objectReferenceValue = FindNamedInChildren<TextMeshProUGUI>(uiRoot, "OxygenStorageLvl");
        so.FindProperty("oxygenCurrAmount").objectReferenceValue = FindNamedInChildren<TextMeshProUGUI>(uiRoot, "OxygenCurrAmount");
        so.FindProperty("oxygenNextLvlAmount").objectReferenceValue = FindNamedInChildren<TextMeshProUGUI>(uiRoot, "OxygenNextLvlAmount");
        so.FindProperty("oxygenUpgradeCost").objectReferenceValue = FindNamedInChildren<TextMeshProUGUI>(uiRoot, "OxygenUpgradeCost");
        so.FindProperty("workstationLvl").objectReferenceValue = FindNamedInChildren<TextMeshProUGUI>(uiRoot, "WorkstationLvl");
        so.FindProperty("workstationCurrproduction").objectReferenceValue = FindNamedInChildren<TextMeshProUGUI>(uiRoot, "WorkstationCurrproduction");
        so.FindProperty("workstationNextLvlproduction").objectReferenceValue = FindNamedInChildren<TextMeshProUGUI>(uiRoot, "WorkstationNextLvlproduction");
        so.FindProperty("workStationUpgradeCost").objectReferenceValue = FindNamedInChildren<TextMeshProUGUI>(uiRoot, "WorkStationUpgradeCost");
        so.FindProperty("healCostText").objectReferenceValue = FindNamedInChildren<TextMeshProUGUI>(uiRoot, "HealCostText");
        so.FindProperty("maskLvl").objectReferenceValue = FindNamedInChildren<TextMeshProUGUI>(uiRoot, "MaskLvl");
        so.FindProperty("maskCostText").objectReferenceValue = FindNamedInChildren<TextMeshProUGUI>(uiRoot, "MaskCostText");
        so.FindProperty("timeInRooms").objectReferenceValue = FindNamedInChildren<TextMeshProUGUI>(uiRoot, "TimeInRooms");
        so.FindProperty("oxygenBaloonCost").objectReferenceValue = FindNamedInChildren<TextMeshProUGUI>(uiRoot, "OxygenBaloonCost");
        so.FindProperty("oxygenLvl").objectReferenceValue = FindNamedInChildren<TextMeshProUGUI>(uiRoot, "OxygenLvl");
        so.FindProperty("shiftDuration").floatValue = shiftDurationSeconds;
        so.ApplyModifiedPropertiesWithoutUndo();

        // Start/End button wiring
        startBtn.onClick.RemoveAllListeners();
        startBtn.onClick.AddListener(sm.StartShift);
        endShiftBtn.onClick.RemoveAllListeners();
        endShiftBtn.onClick.AddListener(sm.EndShift);

        continueBtn.onClick.RemoveAllListeners();
        continueBtn.onClick.AddListener(sm.ContinueToNextShift);
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null)
            return;

        var go = new GameObject("EventSystem");
        Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
    }

    private Storage CreateStorage(Transform parent, string name, float maxAmount, float reqAmount)
    {
        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create Storage");
        go.transform.SetParent(parent, false);
        var storage = go.AddComponent<Storage>();
        storage.level = 1;
        storage.maxAmount = maxAmount;
        storage.reqAmount = reqAmount;
        storage.baseCost = 200f;
        return storage;
    }

    private RoomController CreateRoomController(GameObject roomObj, Vector3 roomBounds, RoomController.RoomType type, Storage storage, Door door)
    {
        var rc = roomObj.GetComponent<RoomController>();
        if (rc == null) rc = roomObj.AddComponent<RoomController>();
        rc.roomType = type;
        rc.myTank = storage;
        rc.door = door;

        var col = roomObj.GetComponent<BoxCollider>();
        if (col == null) col = roomObj.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(roomBounds.x - 1f, roomBounds.y, roomBounds.z - 1f);
        return rc;
    }

    private Transform EnsurePlayer(Transform parent, Vector3 position)
    {
        var existing = Object.FindAnyObjectByType<PlayerMovement>();
        if (existing != null)
            return existing.transform;

        var player = new GameObject("Player");
        Undo.RegisterCreatedObjectUndo(player, "Create Player");
        player.transform.SetParent(parent, false);
        player.transform.position = position;
        player.tag = "Player";
        player.AddComponent<CapsuleCollider>().height = 1.8f;
        var rb = player.AddComponent<Rigidbody>();
        rb.freezeRotation = true;

        player.AddComponent<PlayerMovement>();
        player.AddComponent<PlayerInteraction>();
        player.AddComponent<PlayerHealth>();
        player.AddComponent<PlayerOxygen>();
        player.AddComponent<Mask>();
        player.AddComponent<PlayerTaskActor>();

        var camPivot = NewChild(player.transform, "CameraPivot");
        camPivot.localPosition = new Vector3(0f, 0.75f, 0f);
        var camGo = NewChild(camPivot, "PlayerCamera");
        camGo.gameObject.tag = "PlayerCamera";
        camGo.gameObject.AddComponent<Camera>();
        camGo.gameObject.AddComponent<AudioListener>();
        camGo.gameObject.AddComponent<PlayerCamera>();

        return player.transform;
    }

    private CleaningTask CreateCleaningTask(Transform parent, Vector3 localPosition)
    {
        var go = new GameObject("CleaningTask_01");
        Undo.RegisterCreatedObjectUndo(go, "Create CleaningTask");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;

        var visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Undo.RegisterCreatedObjectUndo(visual, "Create task visual");
        visual.transform.SetParent(go.transform, false);
        visual.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
        visual.transform.localPosition = new Vector3(0f, 0.45f, 0f);

        var top = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Undo.RegisterCreatedObjectUndo(top, "Create task topper");
        top.transform.SetParent(go.transform, false);
        top.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
        top.transform.localPosition = new Vector3(0f, 1.2f, 0f);

        var col = go.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.center = new Vector3(0f, 0.8f, 0f);
        col.size = new Vector3(2.2f, 1.8f, 2.2f);

        var task = go.AddComponent<CleaningTask>();
        var soTask = new SerializedObject(task);
        soTask.FindProperty("uniqueTaskId").stringValue = "cleaning_task_01";
        soTask.FindProperty("progressPerSecond").floatValue = 0.35f;
        soTask.FindProperty("operationalThreshold").floatValue = 0.75f;
        soTask.ApplyModifiedPropertiesWithoutUndo();
        return task;
    }

    private void CreateRoomConsolesAndPuzzles(Transform mainRoom, Transform powerRoom, Vector3 powerSize, Transform oxygenRoom, Vector3 oxygenSize,
        RoomController powerRC, RoomController oxygenRC, out Transform mainPc, out Transform powerPc, out Transform oxygenPc)
    {
        Transform runtimePrefabs = NewChild(mainRoom, "_RuntimePuzzlePrefabs");
        runtimePrefabs.localPosition = new Vector3(0f, -200f, 0f);

        // Scene-1 style "main PC" in front of player path.
        float floorY = -hubSize.y * 0.5f;
        mainPc = NewChild(mainRoom, "PC");
        mainPc.localPosition = new Vector3(0f, floorY, hubSize.z * 0.5f - 2.5f);
        mainPc.localRotation = Quaternion.Euler(0f, 90f, 0f);
        CreateCube(mainPc, "Desk", Vector3.zero, new Vector3(1.8f, 1.0f, 0.8f));
        CreateCube(mainPc, "Screen", new Vector3(0f, 0.7f, -0.25f), new Vector3(1.2f, 0.8f, 0.08f));
        CreatePcInteractionPad(mainPc, "UseZone_MainPC");
        AttachMainPcScreenUI(mainPc);

        float powerFloorY = -powerSize.y * 0.5f;
        float oxygenFloorY = -oxygenSize.y * 0.5f;

        // Power room: PC on east wall (opposite the west door), switch board on north wall.
        float powerPcX = powerSize.x * 0.5f - Mathf.Max(0.8f, roomPcWallInset);
        float powerBoardZ = powerSize.z * 0.5f - Mathf.Max(1.0f, roomPuzzleWallInset);
        powerPc = CreateRoomPcRig(powerRoom, "ScreenStationUI-Power", new Vector3(powerPcX, powerFloorY, -0.6f), 90f, roomPcTiltDegrees);

        // Wire simple world-space room UI so RoomController has visible timer/fill feedback in generated scenes.
        CreatePcInteractionPad(powerPc, "UseZone_PowerPC");
        AttachRoomStatusUI(powerRC, powerPc, "Power Room PC");

        // Power puzzle board (fuse puzzle)
        Transform powerPuzzle = NewChild(powerRoom, "PowerPuzzleBoard");
        powerPuzzle.localPosition = new Vector3(0f, powerFloorY + roomPuzzleHeight, powerBoardZ);
        powerPuzzle.localRotation = Quaternion.Euler(0f, 180f, 0f);
        CreateCube(powerPuzzle, "Board", Vector3.zero, new Vector3(2.2f, 1.7f, 0.16f));

        Transform switchContainer = NewChild(powerPuzzle, "SwitchContainer");
        GameObject switchPrefab = CreateFuseSwitchPrefab(runtimePrefabs);
        var fuseBoard = powerPuzzle.gameObject.AddComponent<FuseBoard>();
        var soFuse = new SerializedObject(fuseBoard);
        soFuse.FindProperty("powerRoomController").objectReferenceValue = powerRC;
        soFuse.FindProperty("switchPrefab").objectReferenceValue = switchPrefab;
        soFuse.FindProperty("switchContainer").objectReferenceValue = switchContainer;
        soFuse.FindProperty("switchForwardOffset").floatValue = 0.12f;
        soFuse.FindProperty("switchSpacing").vector2Value = new Vector2(0.42f, 0.38f);
        soFuse.ApplyModifiedPropertiesWithoutUndo();

        // Oxygen room: large-area puzzle dominates center, PC on west wall (opposite east door).
        float oxygenPcX = -oxygenSize.x * 0.5f + Mathf.Max(0.8f, roomPcWallInset);
        oxygenPc = CreateRoomPcRig(oxygenRoom, "ScreenStationUI-Oxygen", new Vector3(oxygenPcX, oxygenFloorY, -0.6f), -90f, roomPcTiltDegrees);
        CreatePcInteractionPad(oxygenPc, "UseZone_OxygenPC");
        AttachRoomStatusUI(oxygenRC, oxygenPc, "Oxygen Room PC");

        // Oxygen puzzle station occupies most room area.
        Transform oxygenPuzzle = NewChild(oxygenRoom, "OxygenPuzzleStation");
        oxygenPuzzle.localPosition = new Vector3(0f, oxygenFloorY + roomPuzzleHeight, 0.9f);
        oxygenPuzzle.localRotation = Quaternion.identity;
        CreateCube(oxygenPuzzle, "PuzzleFrame", new Vector3(0f, 0f, -1.4f), new Vector3(2.2f, 1.7f, 0.16f));

        Transform tankContainer = NewChild(oxygenPuzzle, "TankContainer");
        GameObject oxygenTankPrefab = CreateOxygenTankPrefab(runtimePrefabs);

        BoxCollider spawnZone = NewChild(oxygenPuzzle, "SpawnZone").gameObject.AddComponent<BoxCollider>();
        spawnZone.center = new Vector3(0f, 0.65f, 0.2f);
        spawnZone.size = new Vector3(Mathf.Max(3.5f, oxygenSize.x - 2.2f), 1.0f, Mathf.Max(3.2f, oxygenSize.z - 2.8f));

        GameObject disposalZoneGo = CreateCube(oxygenPuzzle, "DisposalZone", new Vector3(0f, 0.45f, -1.0f), new Vector3(1.1f, 0.6f, 0.45f));
        var disposalRenderer = disposalZoneGo.GetComponent<MeshRenderer>();
        var disposalZone = disposalZoneGo.GetComponent<BoxCollider>();
        disposalZone.isTrigger = true;

        var oxygenPuzzleScript = oxygenPuzzle.gameObject.AddComponent<OxygenPuzzle>();
        var soOxy = new SerializedObject(oxygenPuzzleScript);
        soOxy.FindProperty("oxygenRoomController").objectReferenceValue = oxygenRC;
        soOxy.FindProperty("oxygenTankPrefab").objectReferenceValue = oxygenTankPrefab;
        soOxy.FindProperty("tankContainer").objectReferenceValue = tankContainer;
        soOxy.FindProperty("spawnZone").objectReferenceValue = spawnZone;
        soOxy.FindProperty("disposalZone").objectReferenceValue = disposalZone;
        soOxy.FindProperty("disposalZoneRenderer").objectReferenceValue = disposalRenderer;
        soOxy.ApplyModifiedPropertiesWithoutUndo();
    }

    private Transform CreateRoomPcRig(Transform room, string name, Vector3 localPosition, float yaw, float pitch)
    {
        Transform pc = NewChild(room, name);
        pc.localPosition = localPosition;
        pc.localRotation = Quaternion.Euler(Mathf.Clamp(pitch, 15f, 75f), yaw, 0f);
        CreateCube(pc, "ConsoleBody", Vector3.zero, new Vector3(1.6f, 0.9f, 0.7f));
        CreateCube(pc, "ConsoleScreen", new Vector3(0f, 0.52f, -0.25f), new Vector3(1.2f, 0.7f, 0.08f));
        return pc;
    }

    private void AttachRoomStatusUI(RoomController roomController, Transform pcRoot, string title)
    {
        var canvasGo = new GameObject("RoomPC-Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGo, "Create RoomPC canvas");
        canvasGo.transform.SetParent(pcRoot, false);
        canvasGo.transform.localPosition = new Vector3(0f, 0.55f, 0.28f);
        canvasGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        canvasGo.transform.localScale = Vector3.one * 0.0022f;

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var canvasRt = (RectTransform)canvasGo.transform;
        canvasRt.sizeDelta = new Vector2(900f, 500f);

        Text titleText = CreateLegacyText(canvasGo.transform, "Title", title, new Vector2(0, 200), new Vector2(840, 60), TextAnchor.MiddleCenter);
        Text timerText = CreateLegacyText(canvasGo.transform, "TimerText", "--:--", new Vector2(-280, 110), new Vector2(260, 50), TextAnchor.MiddleCenter);
        Text amountText = CreateLegacyText(canvasGo.transform, "AmountText", "0%", new Vector2(0, 110), new Vector2(200, 50), TextAnchor.MiddleCenter);
        Text levelText = CreateLegacyText(canvasGo.transform, "LevelText", "Lvl: 1", new Vector2(230, 110), new Vector2(220, 50), TextAnchor.MiddleCenter);
        Text alertText = CreateLegacyText(canvasGo.transform, "AlertText", "No Errors, can fill storage.", new Vector2(0, -20), new Vector2(820, 120), TextAnchor.MiddleCenter);
        Button fillButton = CreateLegacyButton(canvasGo.transform, "FillStorageButton", "Fill Storage", new Vector2(0, -170), new Vector2(280, 70));

        fillButton.onClick.RemoveAllListeners();
        fillButton.onClick.AddListener(roomController.FillStorage);

        roomController.timerText = timerText;
        roomController.ammountPerc = amountText;
        roomController.storageLvlText = levelText;
        roomController.alertMsg = alertText;
        roomController.fillStorage = fillButton;

        var alertLight = NewChild(pcRoot, "AlertLight").gameObject.AddComponent<Light>();
        alertLight.type = LightType.Point;
        alertLight.range = 2f;
        alertLight.intensity = 3f;
        alertLight.color = Color.red;
        alertLight.enabled = false;
        alertLight.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        roomController.myAlertLight = alertLight;
    }

    private void AttachMainPcScreenUI(Transform pcRoot)
    {
        var canvasGo = new GameObject("MainRoomPC-UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGo, "Create MainRoom PC UI");
        canvasGo.transform.SetParent(pcRoot, false);
        canvasGo.transform.localPosition = new Vector3(0f, 0.58f, 0.28f);
        canvasGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        canvasGo.transform.localScale = Vector3.one * 0.0024f;

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rt = (RectTransform)canvasGo.transform;
        rt.sizeDelta = new Vector2(980f, 560f);

        var bg = new GameObject("PanelBG", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(canvasGo.transform, false);
        var bgRt = (RectTransform)bg.transform;
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.14f, 0.85f);

        CreateLegacyText(canvasGo.transform, "MainTitle", "MAIN STATION TERMINAL", new Vector2(0f, 220f), new Vector2(900f, 60f), TextAnchor.MiddleCenter);
        CreateLegacyText(canvasGo.transform, "MainHint", "Use PLAYER UI monitor to start shift / upgrades", new Vector2(0f, 170f), new Vector2(900f, 46f), TextAnchor.MiddleCenter);
        CreateLegacyText(canvasGo.transform, "MainBody", "Approach any room PC to solve active room tasks.\nPower room: fix fuse board\nOxygen room: move tanks into disposal zone", new Vector2(0f, 40f), new Vector2(880f, 200f), TextAnchor.UpperCenter);
        CreateLegacyText(canvasGo.transform, "MainFooter", "ESC: return from PC camera", new Vector2(0f, -220f), new Vector2(900f, 42f), TextAnchor.MiddleCenter);
    }

    private void CreatePcInteractionPad(Transform pcRoot, string name)
    {
        if (pcRoot == null) return;
        var pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(pad, "Create PC interaction pad");
        pad.name = name;
        pad.transform.SetParent(pcRoot, false);
        pad.transform.localPosition = new Vector3(0f, -0.45f, 0.95f);
        pad.transform.localScale = new Vector3(1.35f, 0.02f, 0.9f);

        var mr = pad.GetComponent<MeshRenderer>();
        mr.sharedMaterial = null;
        mr.enabled = false;
    }

    private static Text CreateLegacyText(Transform parent, string name, string text, Vector2 anchoredPos, Vector2 size, TextAnchor anchor)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        var t = go.GetComponent<Text>();
        t.text = text;
        t.alignment = anchor;
        t.color = Color.white;
        t.fontSize = 26;
        Font builtinFont = null;
        try
        {
            builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        catch
        {
            builtinFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        t.font = builtinFont;
        return t;
    }

    private static Button CreateLegacyButton(Transform parent, string name, string label, Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        go.GetComponent<Image>().color = new Color(0.2f, 0.45f, 0.65f, 0.85f);

        CreateLegacyText(go.transform, "Text", label, Vector2.zero, size, TextAnchor.MiddleCenter);
        return go.GetComponent<Button>();
    }

    private static GameObject CreateFuseSwitchPrefab(Transform parent)
    {
        var prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        prefab.name = "FuseSwitchPrefab_Runtime";
        prefab.transform.SetParent(parent, false);
        prefab.transform.localPosition = Vector3.zero;
        prefab.transform.localScale = new Vector3(0.16f, 0.16f, 0.08f);
        if (prefab.GetComponent<FuseSwitch>() == null)
            prefab.AddComponent<FuseSwitch>();
        return prefab;
    }

    private static GameObject CreateOxygenTankPrefab(Transform parent)
    {
        var prefab = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        prefab.name = "OxygenTankPrefab_Runtime";
        prefab.transform.SetParent(parent, false);
        prefab.transform.localPosition = new Vector3(0.6f, 0f, 0f);
        prefab.transform.localScale = new Vector3(0.3f, 0.6f, 0.3f);
        var rb = prefab.GetComponent<Rigidbody>();
        if (rb == null) rb = prefab.AddComponent<Rigidbody>();
        rb.mass = 2f;
        if (prefab.GetComponent<OxygenTank>() == null)
            prefab.AddComponent<OxygenTank>();
        return prefab;
    }

    private void CreatePcInteractionRig(Transform root, Transform player, GameObject playerUi, Transform mainPc, Transform powerPc, Transform oxygenPc)
    {
        if (player == null || playerUi == null)
            return;

        Camera playerCam = FindByNameContains<Camera>(player, "PlayerCamera");
        if (playerCam == null)
            playerCam = player.GetComponentInChildren<Camera>(true);
        if (playerCam == null)
            return;

        var screenCamGo = new GameObject("CameraPC");
        Undo.RegisterCreatedObjectUndo(screenCamGo, "Create PC camera");
        screenCamGo.transform.SetParent(root, false);
        var screenCam = screenCamGo.AddComponent<Camera>();
        screenCam.fieldOfView = 60f;
        screenCam.clearFlags = CameraClearFlags.Skybox;
        screenCamGo.SetActive(false);

        var prompt = CreateLegacyText(playerUi.transform, "goToPCScreenText", "Press E to use PC", new Vector2(0f, -220f), new Vector2(420f, 52f), TextAnchor.MiddleCenter);
        prompt.gameObject.SetActive(false);

        Transform[] camPos = new Transform[6];
        CreatePcViewPoints(mainPc, "PCView_Main", camPos, 0);
        CreatePcViewPoints(powerPc, "PCView_Power", camPos, 2);
        CreatePcViewPoints(oxygenPc, "PCView_Oxygen", camPos, 4);

        var managerGo = new GameObject("CamerasManager");
        Undo.RegisterCreatedObjectUndo(managerGo, "Create CamerasManager");
        managerGo.transform.SetParent(root, false);
        var cm = managerGo.AddComponent<CamerasManager>();
        cm.playerCamera = playerCam;
        cm.screenCamera = screenCam;
        cm.player = player;
        cm.playerUI = playerUi;
        cm.goToPCScreenText = prompt.gameObject;
        cm.camPos = camPos;
        cm.checkDistance = 2.4f;

        if (cm.playerAudioSource == null)
            cm.playerAudioSource = managerGo.AddComponent<AudioSource>();
    }

    private static void CreatePcViewPoints(Transform pcRoot, string nameBase, Transform[] targetArray, int startIndex)
    {
        if (pcRoot == null || targetArray == null || startIndex + 1 >= targetArray.Length) return;

        Vector3 focus = new Vector3(0f, 0.55f, 0.2f);
        Transform left = NewChild(pcRoot, nameBase + "_Left");
        left.localPosition = new Vector3(-0.42f, 0.95f, 1.15f);
        left.rotation = Quaternion.LookRotation((pcRoot.TransformPoint(focus) - left.position).normalized, Vector3.up);

        Transform right = NewChild(pcRoot, nameBase + "_Right");
        right.localPosition = new Vector3(0.42f, 0.95f, 1.15f);
        right.rotation = Quaternion.LookRotation((pcRoot.TransformPoint(focus) - right.position).normalized, Vector3.up);

        targetArray[startIndex] = left;
        targetArray[startIndex + 1] = right;
    }

    private Door CreateSlidingDoor(Transform parent, string name, Vector3 center)
    {
        var root = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(root, "Create Door");
        root.transform.SetParent(parent, false);
        root.transform.position = center;

        var mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(mesh, "Create Door mesh");
        mesh.transform.SetParent(root.transform, false);
        mesh.transform.localScale = new Vector3(doorDepth, doorHeight, doorWidth);
        if (wallMaterial != null)
            mesh.GetComponent<MeshRenderer>().sharedMaterial = wallMaterial;
        Object.DestroyImmediate(mesh.GetComponent<BoxCollider>());

        var col = root.AddComponent<BoxCollider>();
        col.size = new Vector3(doorDepth, doorHeight, doorWidth);

        var closed = NewChild(root.transform, "ClosedPoint");
        closed.position = root.transform.position;
        var open = NewChild(root.transform, "OpenPoint");
        open.position = root.transform.position + Vector3.up * (doorHeight + 0.3f);

        var door = root.AddComponent<Door>();
        door.doorSpeed = doorSpeed;

        // Set private serialized point refs
        var so = new SerializedObject(door);
        so.FindProperty("closedPoint").objectReferenceValue = closed;
        so.FindProperty("openPoint").objectReferenceValue = open;
        so.FindProperty("autoCalculateOpenPoint").boolValue = false;
        so.ApplyModifiedPropertiesWithoutUndo();

        return door;
    }

    private void AddLightFixtures(Transform worldRoot, Vector3 hubC, Vector3 hubS, Vector3 powerC, Vector3 powerS, Vector3 oxygenC, Vector3 oxygenS)
    {
        // Hub lights
        CreateCeilingLightFixture(worldRoot, "Light_Hub_Center", new Vector3(hubC.x, hubC.y + hubS.y * 0.5f - 0.25f, hubC.z), 1.2f, 10f);
        CreateCeilingLightFixture(worldRoot, "Light_Hub_Front", new Vector3(hubC.x, hubC.y + hubS.y * 0.5f - 0.25f, hubC.z + hubS.z * 0.28f), 0.95f, 8f);
        CreateCeilingLightFixture(worldRoot, "Light_Hub_Back", new Vector3(hubC.x, hubC.y + hubS.y * 0.5f - 0.25f, hubC.z - hubS.z * 0.28f), 0.95f, 8f);

        // Power room lights
        CreateCeilingLightFixture(worldRoot, "Light_Power_A", new Vector3(powerC.x, powerC.y + powerS.y * 0.5f - 0.25f, powerC.z), 0.95f, 8f);
        CreateCeilingLightFixture(worldRoot, "Light_Power_B", new Vector3(powerC.x, powerC.y + powerS.y * 0.5f - 0.25f, powerC.z - powerS.z * 0.22f), 0.85f, 7f);

        // Oxygen room lights
        CreateCeilingLightFixture(worldRoot, "Light_Oxygen_A", new Vector3(oxygenC.x, oxygenC.y + oxygenS.y * 0.5f - 0.25f, oxygenC.z), 1.05f, 10f);
        CreateCeilingLightFixture(worldRoot, "Light_Oxygen_B", new Vector3(oxygenC.x, oxygenC.y + oxygenS.y * 0.5f - 0.25f, oxygenC.z + oxygenS.z * 0.24f), 0.95f, 9f);
        CreateCeilingLightFixture(worldRoot, "Light_Oxygen_C", new Vector3(oxygenC.x, oxygenC.y + oxygenS.y * 0.5f - 0.25f, oxygenC.z - oxygenS.z * 0.24f), 0.95f, 9f);
    }

    private void CreateCeilingLightFixture(Transform parent, string name, Vector3 worldPos, float intensity, float range)
    {
        var root = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(root, "Create light fixture");
        root.transform.SetParent(parent, true);
        root.transform.position = worldPos;
        TrySetTag(root, "RoomLight");

        // Physical fixture shell
        var shell = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(shell, "Create light shell");
        shell.name = "FixtureMesh";
        shell.transform.SetParent(root.transform, false);
        shell.transform.localScale = new Vector3(0.65f, 0.1f, 0.65f);
        shell.transform.localPosition = Vector3.zero;
        if (wallMaterial != null)
            shell.GetComponent<MeshRenderer>().sharedMaterial = wallMaterial;
        Object.DestroyImmediate(shell.GetComponent<BoxCollider>());

        // Light source slightly below fixture
        var lightGo = new GameObject("PointLight");
        Undo.RegisterCreatedObjectUndo(lightGo, "Create point light");
        lightGo.transform.SetParent(root.transform, false);
        lightGo.transform.localPosition = new Vector3(0f, -0.15f, 0f);
        var l = lightGo.AddComponent<Light>();
        l.type = LightType.Point;
        l.range = range;
        l.intensity = intensity;
        l.color = new Color(1f, 0.96f, 0.86f, 1f);
        l.shadows = LightShadows.None;
    }

    private static void TrySetTag(GameObject go, string tagName)
    {
        if (go == null || string.IsNullOrEmpty(tagName)) return;
        try
        {
            go.tag = tagName;
        }
        catch
        {
            // If the project doesn't define this tag yet, keep default Untagged.
        }
    }

    private void BuildUI(Transform parent, out GameObject uiRoot, out GameObject homePanel, out GameObject storePanel,
        out Button startBtn, out Button endShiftBtn, out TextMeshProUGUI shiftTimerText,
        out ShiftEvaluationUI evaluationUI, out Button continueBtn, out TextMeshProUGUI taskHintText)
    {
        if (TryCloneUITemplate(parent, out uiRoot, out homePanel, out storePanel, out startBtn, out endShiftBtn, out shiftTimerText, out evaluationUI, out continueBtn, out taskHintText))
            return;

        uiRoot = new GameObject("PlayerUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(uiRoot, "Create PlayerUI");
        uiRoot.transform.SetParent(parent, false);
        var canvas = uiRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = uiRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        homePanel = CreatePanel(uiRoot.transform, "HomePanel", true);
        storePanel = CreatePanel(uiRoot.transform, "StorePanel", false);

        // Top bar
        var topBar = new GameObject("PlayerStats", typeof(RectTransform), typeof(Image));
        topBar.transform.SetParent(uiRoot.transform, false);
        var topRt = (RectTransform)topBar.transform;
        topRt.anchorMin = new Vector2(0f, 1f);
        topRt.anchorMax = new Vector2(1f, 1f);
        topRt.pivot = new Vector2(0.5f, 1f);
        topRt.sizeDelta = new Vector2(0, 130);

        startBtn = CreateButton(topBar.transform, "StartBtn", "Accept Shift", new Vector2(-80, -24), new Vector2(160, 40));
        endShiftBtn = CreateButton(topBar.transform, "EndShiftButton", "End Shift", new Vector2(100, -24), new Vector2(160, 40));
        endShiftBtn.gameObject.SetActive(false);
        shiftTimerText = CreateTMP(topBar.transform, "ShiftTimerText", "No Active Shift", new Vector2(280, -24), new Vector2(280, 40), TextAlignmentOptions.Left);

        taskHintText = CreateTMP(topBar.transform, "TaskHintText", "", new Vector2(600, -24), new Vector2(500, 40), TextAlignmentOptions.Left);
        CreateButton(topBar.transform, "ViewBtn", "View", new Vector2(-260, -24), new Vector2(140, 40));
        var workingIcon = new GameObject("WorkingIcon", typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(workingIcon, "Create WorkingIcon");
        workingIcon.transform.SetParent(topBar.transform, false);
        ((RectTransform)workingIcon.transform).anchorMin = ((RectTransform)workingIcon.transform).anchorMax = new Vector2(0.5f, 0.5f);
        ((RectTransform)workingIcon.transform).anchoredPosition = new Vector2(780, -24);
        ((RectTransform)workingIcon.transform).sizeDelta = new Vector2(24, 24);
        CreateTMP(topBar.transform, "ScoreUI", "Score: 0", new Vector2(-470, -24), new Vector2(220, 40), TextAlignmentOptions.Left);
        CreateTMP(topBar.transform, "PowerTextUI", "Power: --", new Vector2(-690, -24), new Vector2(220, 40), TextAlignmentOptions.Left);
        CreateTMP(topBar.transform, "OxygenTextUI", "Oxygen: --", new Vector2(-900, -24), new Vector2(220, 40), TextAlignmentOptions.Left);
        CreateTMP(topBar.transform, "WorkstationLvlMain", "WS Lvl: 1", new Vector2(1000, -24), new Vector2(180, 40), TextAlignmentOptions.Left);
        CreateTMP(topBar.transform, "WorkstationCurrproductionMain", "Prod: --", new Vector2(1170, -24), new Vector2(180, 40), TextAlignmentOptions.Left);
        CreateSlider(topBar.transform, "PowerSlider", new Vector2(-620, -64), new Vector2(220, 18));
        CreateSlider(topBar.transform, "OxygenSlider", new Vector2(-860, -64), new Vector2(220, 18));

        CreateTMP(storePanel.transform, "PowerStorageLvl", "Power Storage Lvl", new Vector2(-720, 380), new Vector2(340, 36), TextAlignmentOptions.Left);
        CreateTMP(storePanel.transform, "PowerCurrAmount", "Power Current", new Vector2(-720, 340), new Vector2(340, 36), TextAlignmentOptions.Left);
        CreateTMP(storePanel.transform, "PowerNextLvlAmount", "Power Next", new Vector2(-720, 300), new Vector2(340, 36), TextAlignmentOptions.Left);
        CreateTMP(storePanel.transform, "PowerUpgradeCost", "Power Cost", new Vector2(-720, 260), new Vector2(340, 36), TextAlignmentOptions.Left);
        CreateTMP(storePanel.transform, "OxygenStorageLvl", "Oxygen Storage Lvl", new Vector2(-720, 200), new Vector2(340, 36), TextAlignmentOptions.Left);
        CreateTMP(storePanel.transform, "OxygenCurrAmount", "Oxygen Current", new Vector2(-720, 160), new Vector2(340, 36), TextAlignmentOptions.Left);
        CreateTMP(storePanel.transform, "OxygenNextLvlAmount", "Oxygen Next", new Vector2(-720, 120), new Vector2(340, 36), TextAlignmentOptions.Left);
        CreateTMP(storePanel.transform, "OxygenUpgradeCost", "Oxygen Cost", new Vector2(-720, 80), new Vector2(340, 36), TextAlignmentOptions.Left);
        CreateTMP(storePanel.transform, "WorkstationLvl", "Workstation Lvl", new Vector2(-720, 20), new Vector2(340, 36), TextAlignmentOptions.Left);
        CreateTMP(storePanel.transform, "WorkstationCurrproduction", "Workstation Current", new Vector2(-720, -20), new Vector2(340, 36), TextAlignmentOptions.Left);
        CreateTMP(storePanel.transform, "WorkstationNextLvlproduction", "Workstation Next", new Vector2(-720, -60), new Vector2(340, 36), TextAlignmentOptions.Left);
        CreateTMP(storePanel.transform, "WorkStationUpgradeCost", "Workstation Cost", new Vector2(-720, -100), new Vector2(340, 36), TextAlignmentOptions.Left);
        CreateTMP(storePanel.transform, "HealCostText", "Heal Cost", new Vector2(720, 380), new Vector2(340, 36), TextAlignmentOptions.Left);
        CreateTMP(storePanel.transform, "MaskLvl", "Mask Lvl", new Vector2(720, 340), new Vector2(340, 36), TextAlignmentOptions.Left);
        CreateTMP(storePanel.transform, "MaskCostText", "Mask Cost", new Vector2(720, 300), new Vector2(340, 36), TextAlignmentOptions.Left);
        CreateTMP(storePanel.transform, "TimeInRooms", "Time In Rooms", new Vector2(720, 260), new Vector2(340, 36), TextAlignmentOptions.Left);
        CreateTMP(storePanel.transform, "OxygenBaloonCost", "Oxygen Balloon Cost", new Vector2(720, 220), new Vector2(340, 36), TextAlignmentOptions.Left);
        CreateTMP(storePanel.transform, "OxygenLvl", "Oxygen Lvl", new Vector2(720, 180), new Vector2(340, 36), TextAlignmentOptions.Left);

        // Evaluation panel
        var evalPanel = CreatePanel(uiRoot.transform, "ShiftEvaluationPanel (Panel)", false);
        continueBtn = CreateButton(evalPanel.transform, "ContinueButton", "Continue", new Vector2(0, -220), new Vector2(220, 50));
        var cls = CreateTMP(evalPanel.transform, "ClassificationText", "CLASSIFICATION", new Vector2(0, 180), new Vector2(900, 60), TextAlignmentOptions.Center);
        var rpt = CreateTMP(evalPanel.transform, "ReportText", "REPORT", new Vector2(0, 40), new Vector2(1000, 220), TextAlignmentOptions.TopLeft);
        var obs = CreateTMP(evalPanel.transform, "ObservationsText", "OBSERVATIONS", new Vector2(0, -120), new Vector2(1000, 120), TextAlignmentOptions.TopLeft);

        evaluationUI = evalPanel.AddComponent<ShiftEvaluationUI>();
        var so = new SerializedObject(evaluationUI);
        so.FindProperty("canvas").objectReferenceValue = canvas;
        so.FindProperty("evaluationPanel").objectReferenceValue = evalPanel;
        so.FindProperty("classificationText").objectReferenceValue = cls;
        so.FindProperty("reportText").objectReferenceValue = rpt;
        so.FindProperty("observationsText").objectReferenceValue = obs;
        so.FindProperty("continueButton").objectReferenceValue = continueBtn;
        so.FindProperty("buttonText").objectReferenceValue = continueBtn.GetComponentInChildren<TextMeshProUGUI>();
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private bool TryCloneUITemplate(Transform parent, out GameObject uiRoot, out GameObject homePanel, out GameObject storePanel,
        out Button startBtn, out Button endShiftBtn, out TextMeshProUGUI shiftTimerText,
        out ShiftEvaluationUI evaluationUI, out Button continueBtn, out TextMeshProUGUI taskHintText)
    {
        uiRoot = null;
        homePanel = null;
        storePanel = null;
        startBtn = null;
        endShiftBtn = null;
        shiftTimerText = null;
        evaluationUI = null;
        continueBtn = null;
        taskHintText = null;

        if (string.IsNullOrWhiteSpace(uiTemplateScenePath))
            return false;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(uiTemplateScenePath) == null)
            return false;

        Scene templateScene = default;
        try
        {
            templateScene = EditorSceneManager.OpenScene(uiTemplateScenePath, OpenSceneMode.Additive);
            GameObject templateUiRoot = null;
            StationManager templateSm = null;
            var roots = templateScene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (templateUiRoot == null && roots[i].name == "PlayerUI")
                    templateUiRoot = roots[i];
            }
            if (templateUiRoot != null)
                templateSm = Object.FindAnyObjectByType<StationManager>();
            if (templateUiRoot == null)
                return false;

            uiRoot = Instantiate(templateUiRoot, parent, false);
            uiRoot.name = "PlayerUI";
            Undo.RegisterCreatedObjectUndo(uiRoot, "Clone UI template");

            if (templateSm != null)
            {
                homePanel = MapTemplateGameObjectRef(templateUiRoot.transform, uiRoot.transform, templateSm, "homeUI");
                storePanel = MapTemplateGameObjectRef(templateUiRoot.transform, uiRoot.transform, templateSm, "storeUI");
                startBtn = MapTemplateComponentRef<Button>(templateUiRoot.transform, uiRoot.transform, templateSm, "startBtnUI");
                endShiftBtn = MapTemplateComponentRef<Button>(templateUiRoot.transform, uiRoot.transform, templateSm, "endShiftButton");
                shiftTimerText = MapTemplateComponentRef<TextMeshProUGUI>(templateUiRoot.transform, uiRoot.transform, templateSm, "shiftTimerText");
                evaluationUI = MapTemplateComponentRef<ShiftEvaluationUI>(templateUiRoot.transform, uiRoot.transform, templateSm, "evaluationUI");
            }

            if (homePanel == null)
                homePanel = FindNamedInChildren<Transform>(uiRoot.transform, "HomePanel")?.gameObject;
            if (storePanel == null)
                storePanel = FindNamedInChildren<Transform>(uiRoot.transform, "StorePanel")?.gameObject;
            if (startBtn == null)
                startBtn = FindByNameContains<Button>(uiRoot.transform, "StartBtn");
            if (endShiftBtn == null)
                endShiftBtn = FindByNameContains<Button>(uiRoot.transform, "EndShiftButton");
            if (shiftTimerText == null)
                shiftTimerText = FindByNameContains<TextMeshProUGUI>(uiRoot.transform, "ShiftTimerText");
            continueBtn = FindByNameContains<Button>(uiRoot.transform, "ContinueButton");
            taskHintText = FindByNameContains<TextMeshProUGUI>(uiRoot.transform, "TaskHint");
            if (evaluationUI == null)
                evaluationUI = uiRoot.GetComponentInChildren<ShiftEvaluationUI>(true);

            if (evaluationUI == null)
                evaluationUI = uiRoot.GetComponentInChildren<ShiftEvaluationUI>(true);
            if (continueBtn == null && evaluationUI != null)
                continueBtn = evaluationUI.GetComponentInChildren<Button>(true);
            if (taskHintText == null)
                taskHintText = FindByNameContains<TextMeshProUGUI>(uiRoot.transform, "TaskHint");

            bool ok = homePanel != null && storePanel != null && startBtn != null && endShiftBtn != null && shiftTimerText != null;
            if (!ok)
            {
                Debug.LogWarning("[PB_CreateFullPrototypeLevel] UI template clone found PlayerUI but missed required refs; using fallback UI builder.");
                if (uiRoot != null)
                    Undo.DestroyObjectImmediate(uiRoot);
                uiRoot = null;
            }
            return ok;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[PB_CreateFullPrototypeLevel] Failed to clone UI template scene; using fallback UI builder.\n" + ex.Message);
            if (uiRoot != null)
                Undo.DestroyObjectImmediate(uiRoot);
            uiRoot = null;
            return false;
        }
        finally
        {
            if (templateScene.IsValid())
                EditorSceneManager.CloseScene(templateScene, true);
        }
    }

    private static GameObject CreatePanel(Transform parent, string name, bool active)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(go, "Create panel");
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var image = go.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0f);
        go.SetActive(active);
        return go;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        Undo.RegisterCreatedObjectUndo(go, "Create button");
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        var btn = go.GetComponent<Button>();

        CreateTMP(go.transform, "Text", label, Vector2.zero, size, TextAlignmentOptions.Center);
        return btn;
    }

    private static TextMeshProUGUI CreateTMP(Transform parent, string name, string text, Vector2 anchoredPos, Vector2 size, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        Undo.RegisterCreatedObjectUndo(go, "Create TMP");
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 24;
        tmp.alignment = align;
        tmp.color = Color.white;
        return tmp;
    }

    private static Slider CreateSlider(Transform parent, string name, Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Slider));
        Undo.RegisterCreatedObjectUndo(go, "Create slider");
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(go.transform, false);
        var bgRt = (RectTransform)background.transform;
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(go.transform, false);
        var faRt = (RectTransform)fillArea.transform;
        faRt.anchorMin = Vector2.zero;
        faRt.anchorMax = Vector2.one;
        faRt.offsetMin = new Vector2(5f, 5f);
        faRt.offsetMax = new Vector2(-5f, -5f);

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        var fillRt = (RectTransform)fill.transform;
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;

        var slider = go.GetComponent<Slider>();
        slider.fillRect = fillRt;
        slider.targetGraphic = fill.GetComponent<Image>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        return slider;
    }

    private GameObject CreateRoomShell(Transform parent, string name, Vector3 center, Vector3 size, float thickness,
        bool openEast = false, bool openWest = false, bool openNorth = false, bool openSouth = false)
    {
        var container = NewChild(parent, name).gameObject;
        container.transform.position = center;

        CreateCube(container.transform, "Floor", new Vector3(0, -size.y * 0.5f - thickness * 0.5f, 0), new Vector3(size.x, thickness, size.z));
        CreateCube(container.transform, "Ceiling", new Vector3(0, +size.y * 0.5f + thickness * 0.5f, 0), new Vector3(size.x, thickness, size.z));
        CreateWallWithOptionalGap(container.transform, "Wall_West", new Vector3(-size.x * 0.5f + thickness * 0.5f, 0, 0), true, size, thickness, openWest);
        CreateWallWithOptionalGap(container.transform, "Wall_East", new Vector3(+size.x * 0.5f - thickness * 0.5f, 0, 0), true, size, thickness, openEast);
        CreateWallWithOptionalGap(container.transform, "Wall_South", new Vector3(0, 0, -size.z * 0.5f + thickness * 0.5f), false, size, thickness, openSouth);
        CreateWallWithOptionalGap(container.transform, "Wall_North", new Vector3(0, 0, +size.z * 0.5f - thickness * 0.5f), false, size, thickness, openNorth);

        return container;
    }

    private void CreateWallWithOptionalGap(Transform parent, string name, Vector3 center, bool xWall, Vector3 roomSize, float thickness, bool withGap)
    {
        if (!withGap)
        {
            if (xWall)
                CreateCube(parent, name, center, new Vector3(thickness, roomSize.y, roomSize.z));
            else
                CreateCube(parent, name, center, new Vector3(roomSize.x - 2f * thickness, roomSize.y, thickness));
            return;
        }

        float span = xWall ? roomSize.z : (roomSize.x - 2f * thickness);
        float halfRemain = Mathf.Max(0.5f, (span - doorWidth) * 0.5f);
        float offset = (doorWidth + halfRemain) * 0.5f;

        if (xWall)
        {
            CreateCube(parent, name + "_A", center + new Vector3(0f, 0f, -offset), new Vector3(thickness, roomSize.y, halfRemain));
            CreateCube(parent, name + "_B", center + new Vector3(0f, 0f, +offset), new Vector3(thickness, roomSize.y, halfRemain));
        }
        else
        {
            CreateCube(parent, name + "_A", center + new Vector3(-offset, 0f, 0f), new Vector3(halfRemain, roomSize.y, thickness));
            CreateCube(parent, name + "_B", center + new Vector3(+offset, 0f, 0f), new Vector3(halfRemain, roomSize.y, thickness));
        }
    }

    private GameObject CreateCorridor(Transform parent, string name, Vector3 center, float width, float height, float length)
    {
        var container = NewChild(parent, name).gameObject;
        container.transform.position = center;

        CreateCube(container.transform, "Floor", new Vector3(0, -height * 0.5f - wallThickness * 0.5f, 0), new Vector3(length, wallThickness, width));
        CreateCube(container.transform, "Ceiling", new Vector3(0, +height * 0.5f + wallThickness * 0.5f, 0), new Vector3(length, wallThickness, width));
        CreateCube(container.transform, "Wall_Left", new Vector3(0, 0, -width * 0.5f + wallThickness * 0.5f), new Vector3(length, height, wallThickness));
        CreateCube(container.transform, "Wall_Right", new Vector3(0, 0, +width * 0.5f - wallThickness * 0.5f), new Vector3(length, height, wallThickness));
        return container;
    }

    private GameObject CreateCube(Transform parent, string name, Vector3 localPos, Vector3 localScale)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(go, "Create cube");
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = localScale;
        if (wallMaterial != null)
            go.GetComponent<MeshRenderer>().sharedMaterial = wallMaterial;
        return go;
    }

    private static Transform NewChild(Transform parent, string name)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create object");
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static T FindNamedInChildren<T>(Transform root, string name) where T : Component
    {
        if (root == null) return null;
        var all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].name != name) continue;
            return all[i].GetComponent<T>();
        }
        return null;
    }

    private static T FindByNameContains<T>(Transform root, string token) where T : Component
    {
        if (root == null || string.IsNullOrEmpty(token)) return null;
        var all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].name.Contains(token))
            {
                var c = all[i].GetComponent<T>();
                if (c != null) return c;
            }
        }
        return null;
    }

    private static GameObject MapTemplateGameObjectRef(Transform templateRoot, Transform clonedRoot, Object sourceComponent, string propertyName)
    {
        if (sourceComponent == null) return null;
        var so = new SerializedObject(sourceComponent);
        var prop = so.FindProperty(propertyName);
        if (prop == null || prop.objectReferenceValue == null) return null;
        var go = prop.objectReferenceValue as GameObject;
        if (go == null) return null;
        return MapToClonedGameObject(templateRoot, clonedRoot, go.transform);
    }

    private static T MapTemplateComponentRef<T>(Transform templateRoot, Transform clonedRoot, Object sourceComponent, string propertyName) where T : Component
    {
        if (sourceComponent == null) return null;
        var so = new SerializedObject(sourceComponent);
        var prop = so.FindProperty(propertyName);
        if (prop == null || prop.objectReferenceValue == null) return null;
        var comp = prop.objectReferenceValue as Component;
        if (comp == null) return null;
        var mappedGo = MapToClonedGameObject(templateRoot, clonedRoot, comp.transform);
        return mappedGo != null ? mappedGo.GetComponent<T>() : null;
    }

    private static GameObject MapToClonedGameObject(Transform templateRoot, Transform clonedRoot, Transform targetInTemplate)
    {
        if (templateRoot == null || clonedRoot == null || targetInTemplate == null) return null;
        if (!targetInTemplate.IsChildOf(templateRoot)) return null;

        string relPath = GetRelativeTransformPath(templateRoot, targetInTemplate);
        if (string.IsNullOrEmpty(relPath)) return clonedRoot.gameObject;
        var mapped = clonedRoot.Find(relPath);
        return mapped != null ? mapped.gameObject : null;
    }

    private static string GetRelativeTransformPath(Transform root, Transform leaf)
    {
        if (root == leaf) return string.Empty;
        var names = new List<string>();
        var cur = leaf;
        while (cur != null && cur != root)
        {
            names.Add(cur.name);
            cur = cur.parent;
        }
        if (cur != root) return null;
        names.Reverse();
        return string.Join("/", names);
    }
}
#endif
