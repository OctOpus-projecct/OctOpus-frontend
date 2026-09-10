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
    private Collider ground;
    private MaterialPropertyBlock playerColor;
    private float nextPositionLog;
    private Vector2 playerListScroll;
    private static readonly string[] ButtonNames = { "Left", "Right", "Middle" };
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private void Awake()
    {
        movementInput = new MovementInput();
        playerColor = new MaterialPropertyBlock();
    }

    private void Start()
    {
        Application.runInBackground = true;
        string configuredAddress = Environment.GetEnvironmentVariable("OCTOPUS_SERVER_ADDRESS");
        if (!string.IsNullOrWhiteSpace(configuredAddress)) address = configuredAddress.Trim();
        manager = NetworkFactory.Create();
        manager.ClientManager.OnClientConnectionState += OnConnectionState;
        ground = GameObject.Find("Test Ground")?.GetComponent<Collider>();
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-octopus-movement-test") >= 0)
            gameObject.AddComponent<MovementTestHarness>();
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-octopus-connect") >= 0)
            Connect();
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
        Debug.Log("[OctOpus] Client=" + state);
    }

    private void OnApplicationFocus(bool focused)
    {
        movementInput?.UpdateFocus(focused, Time.frameCount);
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
            var renderer = player.GetComponent<Renderer>();
            if (renderer == null) continue;
            renderer.GetPropertyBlock(playerColor);
            playerColor.SetColor(ColorId, player.IsOwner
                ? new Color(0.2f, 0.85f, 0.6f) : new Color(0.85f, 0.6f, 0.25f));
            renderer.SetPropertyBlock(playerColor);
        }
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
        var owner = players.FirstOrDefault(player => player.IsOwner);
        var camera = Camera.main;
        if (owner == null || camera == null || ground == null) return;
        if (ground.Raycast(camera.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 1000f))
            owner.RequestMove(hit.point);
    }

    private void OnGUI()
    {
        if (manager == null) return;
        GUILayout.BeginArea(MovementInput.PanelRect, GUI.skin.box);
        GUILayout.Label("OctOpus | Click movement prototype");
        GUILayout.Label("Server address (UDP " + NetworkDefaults.Port + ")");
        GUI.enabled = state == LocalConnectionState.Stopped;
        address = GUILayout.TextField(address, 253);
        if (GUILayout.Button("Connect")) Connect();
        GUI.enabled = state == LocalConnectionState.Started || state == LocalConnectionState.Starting;
        if (GUILayout.Button("Disconnect")) Disconnect();
        GUI.enabled = true;
        GUILayout.Label("Status: " + state);
        GUILayout.Label("Players: " + roster.Length + " / " + NetworkDefaults.MaximumPlayers);
        GUILayout.Label("Move mouse button (saved)");
        int selected = GUILayout.Toolbar(movementInput.Button, ButtonNames);
        if (selected != movementInput.Button) movementInput.SetButton(selected);
        GUILayout.Label("You: green | Others: orange");
        GUILayout.Label("Positions confirmed by server:");
        playerListScroll = GUILayout.BeginScrollView(playerListScroll, GUILayout.MaxHeight(180));
        foreach (NetworkPlayer player in players)
        {
            Vector3 position = player.ServerPosition;
            GUILayout.Label(string.Format(CultureInfo.InvariantCulture, "Player {0}{1}  ({2:F1}, {3:F1})",
                player.OwnerId, player.IsOwner ? " (You)" : "", position.x, position.z));
        }
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void OnDestroy()
    {
        if (manager == null) return;
        manager.ClientManager.OnClientConnectionState -= OnConnectionState;
        manager.ClientManager.StopConnection();
        Destroy(manager.gameObject);
    }
}
