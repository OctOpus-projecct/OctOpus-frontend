using System;
using System.Globalization;
using System.Linq;
using FishNet.Managing;
using FishNet.Transporting;
using OctOpus.Shared;
using UnityEngine;

public sealed class ClientBootstrap : MonoBehaviour
{
    private NetworkManager manager;
    private string address = NetworkDefaults.LocalAddress;
    private LocalConnectionState state = LocalConnectionState.Stopped;
    private string lastRoster;
    private int[] roster = Array.Empty<int>();
    private NetworkPlayer[] players = Array.Empty<NetworkPlayer>();
    private MovementInput movementInput;
    private StrikeInput strikeInput;
    private bool rebindingStrike;
    private bool uiHasKeyboardFocus;
    private bool clearGuiFocus;
    private Collider ground;
    private MaterialPropertyBlock playerColor;
    private float nextPositionLog;
    private Vector2 panelScroll;
    private NetworkTree selectedTree;
    private static readonly string[] ButtonNames = { "Left", "Right", "Middle" };
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private void Awake()
    {
        movementInput = new MovementInput();
        strikeInput = new StrikeInput();
        playerColor = new MaterialPropertyBlock();
    }

    private void Start()
    {
        Application.runInBackground = true;
        string configuredAddress = Environment.GetEnvironmentVariable("OCTOPUS_SERVER_ADDRESS");
        if (!string.IsNullOrWhiteSpace(configuredAddress)) address = configuredAddress.Trim();
        manager = NetworkFactory.Create();
        manager.ClientManager.OnClientConnectionState += OnConnectionState;
        var mapView = GetComponent<PrototypeMapView>() ?? gameObject.AddComponent<PrototypeMapView>();
        mapView.EnsureBuilt(GameObject.Find("Test Ground"), Camera.main);
        ground = GameObject.Find("Test Ground")?.GetComponent<Collider>();
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-octopus-movement-test") >= 0)
            gameObject.AddComponent<MovementTestHarness>();
        if (Environment.GetCommandLineArgs().Any(argument => argument == "-octopus-tree-test" ||
                argument == "-octopus-tree-contender" || argument == "-octopus-tree-observer"))
            gameObject.AddComponent<TreeTestHarness>();
        if (Environment.GetCommandLineArgs().Any(argument => argument == "-octopus-strike-test" ||
                argument == "-octopus-strike-observer"))
            gameObject.AddComponent<StrikeTestHarness>();
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-octopus-connect") >= 0)
            Connect();
        if (Environment.GetCommandLineArgs().Any(argument => argument == "-octopus-map-test" || argument == "-octopus-map-partner"))
            gameObject.AddComponent<MapTestHarness>();
        if (Environment.GetCommandLineArgs().Any(argument => argument == "-octopus-inventory-test" || argument == "-octopus-inventory-observer"))
            gameObject.AddComponent<InventoryTestHarness>();
    }

    public void Connect()
    {
        string target = address.Trim();
        if (target.Length == 0 || state != LocalConnectionState.Stopped) return;
        manager.ClientManager.StartConnection(target, NetworkDefaults.Port);
    }

    public void Disconnect() { manager.ClientManager.StopConnection(); }

    private void OnConnectionState(ClientConnectionStateArgs args)
    {
        state = args.ConnectionState;
        if (state == LocalConnectionState.Stopped) selectedTree = null;
        Debug.Log("[OctOpus] Client=" + state);
    }

    private void OnApplicationFocus(bool focused)
    {
        movementInput?.UpdateFocus(focused, Time.frameCount);
        strikeInput?.UpdateFocus(Time.frameCount);
        if (!focused) rebindingStrike = false;
    }

    private void Update()
    {
        if (manager == null) return;
        roster = manager.ClientManager.Connection.IsAuthenticated
            ? manager.ClientManager.Clients.Keys.OrderBy(id => id).ToArray()
            : Array.Empty<int>();
        string current = string.Join(",", roster);
        if (current != lastRoster)
        {
            lastRoster = current;
            Debug.Log("[OctOpus] Roster=" + current);
        }
        players = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None)
            .OrderBy(player => player.OwnerId).ToArray();
        foreach (NetworkPlayer player in players)
        {
            if (player.GetComponent<AxeSwingView>() == null) player.gameObject.AddComponent<AxeSwingView>();
            var renderer = player.GetComponent<Renderer>();
            if (renderer == null) continue;
            renderer.GetPropertyBlock(playerColor);
            playerColor.SetColor(ColorId, player.IsOwner
                ? new Color(0.2f, 0.85f, 0.6f) : new Color(0.85f, 0.6f, 0.25f));
            renderer.SetPropertyBlock(playerColor);
        }
        foreach (NetworkTree renderedTree in FindObjectsByType<NetworkTree>(FindObjectsSortMode.None))
        {
            foreach (var renderer in renderedTree.GetComponentsInChildren<Renderer>())
            {
                renderer.enabled = !renderedTree.IsDepleted;
                renderer.GetPropertyBlock(playerColor);
                playerColor.SetColor(ColorId, renderer.name == "Trunk" ? new Color(.4f, .25f, .12f) :
                    renderedTree.WorkerId < 0 ? new Color(.2f, .6f, .25f) : new Color(.7f, .6f, .15f));
                renderer.SetPropertyBlock(playerColor);
            }
            foreach (var collider in renderedTree.GetComponentsInChildren<Collider>()) collider.enabled = !renderedTree.IsDepleted;
        }
        var owner = players.FirstOrDefault(player => player.IsOwner);
        if (owner != null && owner.Activity == PlayerActivity.Working && Input.GetKeyDown(strikeInput.Key) &&
            strikeInput.CanStrike(Application.isFocused, Time.frameCount, uiHasKeyboardFocus, rebindingStrike) &&
            movementInput.CanMove(Input.mousePosition, Screen.height, Application.isFocused, Time.frameCount))
            owner.RequestStrike();
        if (Time.unscaledTime >= nextPositionLog)
        {
            nextPositionLog = Time.unscaledTime + 0.5f;
            foreach (NetworkPlayer player in players)
            {
                Vector3 position = player.ServerPosition;
                Debug.Log(string.Format(CultureInfo.InvariantCulture,
                    "[OctOpus] Position={0}:{1:F3},{2:F3},{3:F3}",
                    player.OwnerId, position.x, position.y, position.z));
                Vector3 rendered = player.transform.position;
                Debug.Log(string.Format(CultureInfo.InvariantCulture,
                    "[OctOpus] Render={0}:{1:F3},{2:F3},{3:F3}",
                    player.OwnerId, rendered.x, rendered.y, rendered.z));
            }
        }
        if (!Input.GetMouseButtonDown(movementInput.Button) ||
            !movementInput.CanMove(Input.mousePosition, Screen.height, Application.isFocused, Time.frameCount))
            return;
        var camera = Camera.main;
        if (owner == null || camera == null) return;
        clearGuiFocus = true;
        uiHasKeyboardFocus = false;
        if (rebindingStrike) return;
        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit = default;
        bool hitGround = ground != null && ground.Raycast(ray, out hit, 1000f);
        NetworkTree tree = FindTree(ray, hitGround ? hit.distance : 1000f);
        if (tree != null)
        {
            selectedTree = tree;
            owner.RequestWork(tree.NetworkObject);
        }
        else if (hitGround)
            owner.RequestMove(hit.point);
    }

    public static NetworkTree FindTree(Ray ray, float maxDistance)
    {
        NetworkTree nearest = null;
        float nearestDistance = float.PositiveInfinity;
        foreach (RaycastHit hit in Physics.RaycastAll(ray, maxDistance, Physics.DefaultRaycastLayers,
                     QueryTriggerInteraction.Ignore))
        {
            NetworkTree tree = hit.collider.GetComponentInParent<NetworkTree>();
            if (tree == null || hit.distance >= nearestDistance) continue;
            nearest = tree;
            nearestDistance = hit.distance;
        }
        return nearest;
    }

    private void OnGUI()
    {
        if (manager == null) return;
        if (clearGuiFocus) { GUI.FocusControl(null); clearGuiFocus = false; }
        if (rebindingStrike && Event.current.type == EventType.KeyDown)
        {
            var key = Event.current.keyCode;
            if (key == KeyCode.Escape || StrikeInput.IsBindable(key))
            {
                if (key != KeyCode.Escape) strikeInput.SetKey(key, Time.frameCount);
                else strikeInput.UpdateFocus(Time.frameCount);
                rebindingStrike = false;
                GUI.FocusControl(null);
                Event.current.Use();
            }
        }
        GUILayout.BeginArea(MovementInput.PanelRect, GUI.skin.box);
        GUILayout.Label("OctOpus | Life test map");
        panelScroll = GUILayout.BeginScrollView(panelScroll);
        GUILayout.Label("Server address (UDP " + NetworkDefaults.Port + ")");
        GUI.enabled = state == LocalConnectionState.Stopped;
        GUI.SetNextControlName("ServerAddress");
        address = GUILayout.TextField(address, 253);
        if (GUILayout.Button("Connect")) Connect();
        GUI.enabled = state == LocalConnectionState.Started || state == LocalConnectionState.Starting;
        if (GUILayout.Button("Disconnect")) Disconnect();
        GUI.enabled = true;
        GUILayout.Label("Status: " + state);
        GUILayout.Label("Players: " + roster.Length + " / " + NetworkDefaults.MaximumPlayers);
        GUILayout.Label("Move / select tree mouse button (saved)");
        int selected = GUILayout.Toolbar(movementInput.Button, ButtonNames);
        if (selected != movementInput.Button) movementInput.SetButton(selected);
        if (GUILayout.Button(rebindingStrike ? "Press keyboard key (Esc cancels)" : "Strike key: " + strikeInput.Key + " (change)"))
        {
            rebindingStrike = !rebindingStrike;
            strikeInput.UpdateFocus(Time.frameCount);
        }
        GUILayout.Label("You: green | Others: orange");
        GUILayout.Label("Spawn clearing / Guide marker / Logging grove");
        GUILayout.Label("Guide marks a future tutorial location.");
        GUILayout.Label("Click tree: approach and request work.");
        GUILayout.Label("Click ground: cancel work and move.");
        GUILayout.Label("Selection is local; work state is from server.");
        NetworkPlayer owner = players.FirstOrDefault(player => player.IsOwner);
        DrawInventory(owner);
        GUILayout.Label("Your activity: " + (owner == null ? "-" : owner.Activity.ToString()));
        GUILayout.Label("Last server result: " + (owner == null ? "-" : owner.LastWorkResult.ToString()));
        GUILayout.Label(owner == null ? "Stamina: -" : string.Format(CultureInfo.InvariantCulture,
            "Stamina: {0:F1} / {1:F0} | Strike: {2}", owner.Stamina, owner.MaximumStamina, owner.LastStrikeResult));
        if (selectedTree == null)
            GUILayout.Label("Selected tree: none");
        else
        {
            GUILayout.Label("Selected tree: " + selectedTree.NetworkObject.ObjectId);
            GUILayout.Label("Worker: " + (selectedTree.WorkerId < 0 ? "Free" :
                "Player " + selectedTree.WorkerId +
                (owner != null && owner.OwnerId == selectedTree.WorkerId ? " (You)" : "")));
            GUILayout.Label(string.Format(CultureInfo.InvariantCulture,
                "Server time remaining: {0:F1}s", selectedTree.SecondsRemaining));
            GUILayout.Label(string.Format(CultureInfo.InvariantCulture, "Tree HP: {0:F0} / {1:F0}",
                selectedTree.Health, selectedTree.MaximumHealth));
            if (selectedTree.IsDepleted) GUILayout.Label(string.Format(CultureInfo.InvariantCulture,
                "Tree depleted - returns in {0:F1}s", selectedTree.RespawnRemaining));
        }
        GUILayout.Label("Press " + strikeInput.Key + " once per strike; wait when stamina is low.");
        GUILayout.Label("HP 0: server grants wood once to the worker; tree returns after 10s.");
        GUILayout.Label("Positions confirmed by server:");
        foreach (NetworkPlayer player in players)
        {
            Vector3 position = player.ServerPosition;
            GUILayout.Label(string.Format(CultureInfo.InvariantCulture, "Player {0}{1}  ({2:F1}, {3:F1}) | {4}",
                player.OwnerId, player.IsOwner ? " (You)" : "", position.x, position.z, player.Activity));
        }
        GUILayout.EndScrollView();
        GUILayout.EndArea();
        uiHasKeyboardFocus = GUIUtility.keyboardControl != 0;
    }

    private void DrawInventory(NetworkPlayer owner)
    {
        GUILayout.Label("Your inventory | Server confirmed");
        GUILayout.Label("Session only: inventory resets on disconnect.");
        if (state == LocalConnectionState.Stopped || state == LocalConnectionState.Stopping)
        {
            GUILayout.Label("Inventory: not connected");
            return;
        }
        if (state != LocalConnectionState.Started || owner == null || owner.Inventory.CapacityUnits <= 0)
        {
            GUILayout.Label("Inventory: waiting for server...");
            return;
        }

        InventorySnapshot inventory = owner.Inventory;
        GUILayout.Label("Wood: " + inventory.WoodCount.ToString(CultureInfo.InvariantCulture));
        GUILayout.Label(string.Format(CultureInfo.InvariantCulture, "Weight: {0:0.###} / {1:0.###}",
            (decimal)inventory.WeightUnits / InventoryRules.WeightUnitsPerUnit,
            (decimal)inventory.CapacityUnits / InventoryRules.WeightUnitsPerUnit));
        GUILayout.Label(inventory.CanGather
            ? "Gathering: allowed"
            : "Gathering: blocked (weight at least 95%)");
        GUILayout.Label(inventory.IsOverweight
            ? "Movement: slowed (weight over 100%)"
            : "Movement: normal speed");
    }

    private void OnDestroy()
    {
        if (manager == null) return;
        manager.ClientManager.OnClientConnectionState -= OnConnectionState;
        manager.ClientManager.StopConnection();
        Destroy(manager.gameObject);
    }
}
