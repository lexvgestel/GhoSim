using System.Collections.Generic;
using System.IO;
using System.Linq;
using MyBox;
using UnityEngine;
using Util;

[ExecuteAlways]
public class LoadMatch : MonoBehaviour
{
    [SerializeField] private GameObject[] fieldPrefab;

    [Header("Multiplayer")]
    [SerializeField] private bool enableSecondPlayer = false;

    [Header("Robot Selection - Season")] [SerializeField]
    private InspectorDropdown robotSeasonSelected;

    [Header("Player 1")]
    [SerializeField] private bool useCustomSpawnPointP1;
    [SerializeField] private Transform spawnPointP1;
    [SerializeField] private InspectorDropdown robotSelectedP1;
    [SerializeField] private Cameras viewP1;
    [SerializeField] private bool allianceP1IsRed = false;
    [SerializeField] private bool invertSteeringP1 = false;
    [SerializeField] private string playerNumberP1 = "Player1";
    [SerializeField] private Rect viewportP1 = new Rect(0f, 0f, 0.5f, 1f);
    [ConditionalField(true, nameof(isDriverStationP1))] [SerializeField]
    private StationNum stationNumberP1;
    [ConditionalField(true, nameof(isDriverStationP1))] [SerializeField]
    private TrackingType trackingTypeP1;

    [Header("Player 2")]
    [ConditionalField(nameof(enableSecondPlayer))] [SerializeField]
    private Transform spawnPointP2;
    [ConditionalField(nameof(enableSecondPlayer))] [SerializeField]
    private InspectorDropdown robotSelectedP2;
    [ConditionalField(nameof(enableSecondPlayer))] [SerializeField]
    private Cameras viewP2;
    [ConditionalField(nameof(enableSecondPlayer))] [SerializeField]
    private bool allianceP2IsRed = true;
    [ConditionalField(nameof(enableSecondPlayer))] [SerializeField]
    private bool invertSteeringP2 = false;
    [ConditionalField(nameof(enableSecondPlayer))] [SerializeField]
    private string playerNumberP2 = "Player2";
    [ConditionalField(nameof(enableSecondPlayer))] [SerializeField]
    private Rect viewportP2 = new Rect(0.5f, 0f, 0.5f, 1f);
    [ConditionalField(true, nameof(isDriverStationP2))] [SerializeField]
    private StationNum stationNumberP2 = StationNum.Two;
    [ConditionalField(true, nameof(isDriverStationP2))] [SerializeField]
    private TrackingType trackingTypeP2;

    private readonly int[] selectedRobotIndex = new int[2];
    private readonly string[] selectedName = new string[2];
    private int selectedSeasonIndex;
    private string selectedSeasonName;
    private readonly List<GameObject> availableRobots = new List<GameObject>();
    private readonly List<string> availableSeasons = new List<string>();

    private bool isDriverStationP1() => viewP1 == Cameras.DriverStation;
    private bool isDriverStationP2() => viewP2 == Cameras.DriverStation;

    private GameObject _fieldHolder;
    private readonly GameObject[] _activeRobots = new GameObject[2];
    private readonly GameObject[] _spawnedCameras = new GameObject[2];
    private GameObject _activeCam;

    private FMS fms;

    // Small helpers so the rest of the class can loop over both players
    // instead of duplicating every method.
    private InspectorDropdown RobotDropdown(int i) => i == 0 ? robotSelectedP1 : robotSelectedP2;
    private Transform SpawnPoint(int i) => i == 0 ? spawnPointP1 : spawnPointP2;
    private Cameras View(int i) => i == 0 ? viewP1 : viewP2;
    private string PlayerNumber(int i) => i == 0 ? playerNumberP1 : playerNumberP2;
    private Rect Viewport(int i) => i == 0 ? viewportP1 : viewportP2;
    private StationNum StationNumber(int i) => i == 0 ? stationNumberP1 : stationNumberP2;
    private bool IsRed(int i) => i == 0 ? allianceP1IsRed : allianceP2IsRed;
    private bool InvertSteering(int i) => i == 0 ? invertSteeringP1 : invertSteeringP2;
    private int PlayerCount => enableSecondPlayer ? 2 : 1;

    private void OnEnable()
    {
        if (Application.isPlaying) return; // dropdown lists are only needed in the editor
        RefreshDropdownLists();
    }

    // Editor only: keeps the inspector dropdowns up to date while NOT playing.
    // This does file-system reads and Resources.LoadAll, so it must never run
    // every frame in play mode or in a build.
    private void LateUpdate()
    {
        if (Application.isPlaying) return;

        CheckSeasons();
        robotSeasonSelected.canBeSelected = availableSeasons;
        robotSeasonSelected.selectedIndex = selectedSeasonIndex;
        robotSeasonSelected.selectedName = selectedSeasonName;
        CheckRobots();

        var names = availableRobots.Select(x => x.name).ToList();

        robotSelectedP1.canBeSelected = names;
        robotSelectedP1.selectedIndex = selectedRobotIndex[0];
        robotSelectedP1.selectedName = selectedName[0];

        robotSelectedP2.canBeSelected = names;
        robotSelectedP2.selectedIndex = selectedRobotIndex[1];
        robotSelectedP2.selectedName = selectedName[1];
    }

    private void RefreshDropdownLists()
    {
        CheckSeasons();
        robotSeasonSelected.canBeSelected = availableSeasons;
        CheckRobots();
        var names = availableRobots.Select(x => x.name).ToList();
        robotSelectedP1.canBeSelected = names;
        robotSelectedP2.canBeSelected = names;
    }

    private void ReadSelections()
    {
        for (int i = 0; i < 2; i++)
        {
            selectedName[i] = RobotDropdown(i).selectedName;
            selectedRobotIndex[i] = RobotDropdown(i).selectedIndex;
        }
        selectedSeasonIndex = robotSeasonSelected.selectedIndex;
        selectedSeasonName = robotSeasonSelected.selectedName;
    }

    private void Start()
    {
        ReadSelections();
        ResetField(); // ResetField refreshes the robot list itself
    }

    private void Update()
    {
        ReadSelections();

        if (Application.isPlaying) return; // everything below is edit-mode only

        if (RobotLoaded())
        {
            DeleteRobots();
        }

        if (!CheckField())
        {
            DestroyField();
            LoadField();
        }

        CheckRobots();
    }

    private void LoadField()
    {
        _fieldHolder = new GameObject
        {
            name = "FieldHolder",
            transform = { position = Vector3.zero, rotation = Quaternion.identity, parent = transform },
        };
        Instantiate(fieldPrefab[0], Vector3.zero, Quaternion.identity, _fieldHolder.transform);
    }

    private bool CheckField()
    {
        if (transform.childCount == 0)
        {
            return false;
        }

        return _fieldHolder != null && _fieldHolder.transform.Find(fieldPrefab[0].name + "(Clone)");
    }

    private void DestroyField()
    {
        var holder = transform.Find("FieldHolder");
        if (holder)
        {
            _fieldHolder = holder.gameObject;
            DestroyImmediate(_fieldHolder);
        }
    }

    public TrackingType GetTrackingType()
    {
        return trackingTypeP1;
    }

    public TrackingType GetTrackingType(int playerIndex)
    {
        return playerIndex == 0 ? trackingTypeP1 : trackingTypeP2;
    }

    public void ResetField()
    {
        if (!Application.isPlaying) CheckRobots(); // in play mode only the chosen robot is loaded (see LoadRobotPrefab)
        DestroyField();
        LoadField();
        for (int i = 0; i < PlayerCount; i++)
        {
            SpawnRobot(i);
            addCamera(i);
        }
        Utils.resetParentCache();
        if (fms)
        {
            fms.Restart();
        }
    }

    public void setFMS(FMS fms)
    {
        this.fms = fms;
    }

    public GameObject getFieldHolder()
    {
        return _fieldHolder;
    }

    private void SpawnRobot(int playerIndex)
    {
        GameObject robotToSpawn = LoadRobotPrefab(playerIndex);
        if (robotToSpawn != null)
        {
            Transform spawnLocation;
            if (playerIndex == 0)
            {
                spawnLocation = useCustomSpawnPointP1 ? spawnPointP1 :
                                 fms != null ? fms.defaultSpawn :
                                 spawnPointP1;
            }
            else
            {
                // Player 2 always uses its own explicit spawn point so it
                // doesn't land on top of player 1. Falls back to an offset
                // from player 1's spot (with a warning) if none was set.
                spawnLocation = spawnPointP2;
                if (spawnLocation == null)
                {
                    Debug.LogWarning("LoadMatch: spawnPointP2 is not set - offsetting from player 1's spawn point. " +
                                      "Assign a dedicated Transform for player 2 to avoid this.");
                    var fallback = new GameObject("P2SpawnFallback").transform;
                    var p1Spawn = useCustomSpawnPointP1 ? spawnPointP1 : (fms != null ? fms.defaultSpawn : spawnPointP1);
                    fallback.position = p1Spawn.position + new Vector3(3f, 0f, 0f);
                    fallback.rotation = p1Spawn.rotation;
                    spawnLocation = fallback;
                }
            }

            var activeRobot = Instantiate(robotToSpawn, spawnLocation.position, spawnLocation.rotation, _fieldHolder.transform);
            _activeRobots[playerIndex] = activeRobot;

            var frame = activeRobot.GetComponent<BuildFrame>();
            frame.playerNumber = PlayerNumber(playerIndex);
            var controller = frame.GetSwerveController();
            if (controller)
            {
                controller.isRed = IsRed(playerIndex);
                controller.reversed = InvertSteering(playerIndex);

                switch (View(playerIndex))
                {
                    case Cameras.FirstPerson:
                    case Cameras.FirstPersonReversed:
                        controller.fieldCentric = false;
                        break;
                    case Cameras.ThirdPerson:
                    case Cameras.ReversedThirdPerson:
                    case Cameras.DriverStation:
                        controller.fieldCentric = true;
                        break;
                }
            }
        }
    }

    // Loads only the selected robot prefab instead of every robot in the season
    // (Resources.LoadAll was the main cause of the slow match start).
    private GameObject LoadRobotPrefab(int playerIndex)
    {
        if (!string.IsNullOrEmpty(selectedName[playerIndex]))
        {
            var prefab = Resources.Load<GameObject>("Robots/" + selectedSeasonName + "/" + selectedName[playerIndex]);
            if (prefab != null) return prefab;
        }

        // Fallback: old behaviour (loads the whole season and picks by index)
        CheckRobots();
        if (availableRobots.Count > 0 &&
            selectedRobotIndex[playerIndex] >= 0 &&
            selectedRobotIndex[playerIndex] < availableRobots.Count)
        {
            return availableRobots[selectedRobotIndex[playerIndex]];
        }

        return null;
    }

    private bool RobotLoaded()
    {
        return _activeRobots[0] != null || _activeRobots[1] != null;
    }

    public GameObject GetRobotLoaded()
    {
        return _activeRobots[0];
    }

    public GameObject GetRobotLoaded(int playerIndex)
    {
        return _activeRobots[playerIndex];
    }

    private void DeleteRobots()
    {
        for (int i = 0; i < 2; i++)
        {
            if (_spawnedCameras[i] != null) DestroyImmediate(_spawnedCameras[i]);
            if (_activeRobots[i] != null) DestroyImmediate(_activeRobots[i]);
            _spawnedCameras[i] = null;
            _activeRobots[i] = null;
        }
    }

    private void addCamera(int playerIndex)
    {
        string objectToLoad = "Cameras/" + View(playerIndex);
        _activeCam = Resources.Load(objectToLoad) as GameObject;

        var parent = _activeRobots[playerIndex];
        var spawnRotation = SpawnPoint(playerIndex).gameObject;
        if (fms)
        {
            var stationArray = IsRed(playerIndex) ? fms.redStationCams : fms.blueStationCams;
            parent = View(playerIndex) == Cameras.DriverStation
                ? stationArray[(int)StationNumber(playerIndex)]
                : _activeRobots[playerIndex];
            spawnRotation = View(playerIndex) == Cameras.DriverStation
                ? stationArray[(int)StationNumber(playerIndex)]
                : SpawnPoint(playerIndex).gameObject;
        }

        var spawnedCamera = Instantiate(_activeCam, Vector3.zero, spawnRotation.transform.rotation, parent.transform);
        spawnedCamera.transform.localPosition = Vector3.zero;
        _spawnedCameras[playerIndex] = spawnedCamera;

        // Driver Station cameras carry a LookAtRobot component - tell it
        // which player it belongs to so it tracks the right robot instead
        // of always defaulting to player 1.
        var lookAtRobot = spawnedCamera.GetComponentInChildren<LookAtRobot>();
        if (lookAtRobot != null)
        {
            lookAtRobot.playerIndex = playerIndex;
        }

        // Split-screen: give this player's camera its slice of the screen.
        var cam = spawnedCamera.GetComponentInChildren<Camera>();
        if (cam != null)
        {
            cam.rect = PlayerCount > 1 ? Viewport(playerIndex) : new Rect(0f, 0f, 1f, 1f);
        }

        // Only one AudioListener may be active at a time - keep player 1's,
        // disable any others to avoid Unity's "2 audio listeners" warning
        // and doubled-up audio.
        var listener = spawnedCamera.GetComponentInChildren<AudioListener>();
        if (listener != null)
        {
            listener.enabled = playerIndex == 0;
        }
    }

    public void CheckSeasons()
    {
        string resourcesPath = Path.Combine(Application.dataPath, "Resources", "Robots");

        availableSeasons.Clear();

        if (Directory.Exists(resourcesPath))
        {
            string[] rawFolderPaths = Directory.GetDirectories(resourcesPath);

            foreach (string path in rawFolderPaths)
            {
                availableSeasons.Add(Path.GetFileName(path));
            }
        }

        if (selectedSeasonIndex >= availableSeasons.Count)
        {
            selectedSeasonIndex = availableSeasons.Count > 0 ? availableSeasons.Count - 1 : 0;
        }
    }

    public void CheckRobots()
    {
        string path = "Robots/" + selectedSeasonName;
        GameObject[] loadedRobots = Resources.LoadAll<GameObject>(path);

        availableRobots.Clear();
        availableRobots.AddRange(loadedRobots);

        for (int i = 0; i < 2; i++)
        {
            if (selectedRobotIndex[i] >= availableRobots.Count)
            {
                selectedRobotIndex[i] = availableRobots.Count > 0 ? availableRobots.Count - 1 : 0;
            }
        }
    }
}