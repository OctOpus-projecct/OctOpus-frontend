using System;
using System.Collections.Generic;
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
    private readonly Dictionary<int, GameObject> markers = new Dictionary<int, GameObject>();
    private string lastRoster;
    private int[] roster = Array.Empty<int>();

    private void Start()
    {
        Application.runInBackground = true;
        manager = NetworkFactory.Create();
        manager.ClientManager.OnClientConnectionState += OnConnectionState;
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-octopus-connect") >= 0)
            Connect();
    }

    public void Connect()
    {
        string target = address.Trim();
        if (target.Length == 0 || state != LocalConnectionState.Stopped) return;
        manager.ClientManager.StartConnection(target, NetworkDefaults.Port);
    }

    public void Disconnect()
    {
        manager.ClientManager.StopConnection();
    }

    private void OnConnectionState(ClientConnectionStateArgs args)
    {
        state = args.ConnectionState;
        Debug.Log("[OctOpus] Client=" + state);
    }

    private void Update()
    {
        if (manager == null) return;
        roster = manager.ClientManager.Connection.IsAuthenticated
            ? manager.ClientManager.Clients.Keys.OrderBy(id => id).ToArray()
            : Array.Empty<int>();
        string current = string.Join(",", roster);
        if (current == lastRoster) return;
        lastRoster = current;
        Debug.Log("[OctOpus] Roster=" + current);
        foreach (int id in markers.Keys.Except(roster).ToArray())
        {
            Destroy(markers[id]);
            markers.Remove(id);
        }
        for (int index = 0; index < roster.Length; index++)
        {
            int id = roster[index];
            if (!markers.TryGetValue(id, out GameObject marker))
            {
                marker = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                marker.name = "Player " + id;
                markers.Add(id, marker);
            }
            marker.transform.position = new Vector3((index - (roster.Length - 1) * 0.5f) * 2f, 1f, 0f);
        }
    }

    private void OnGUI()
    {
        if (manager == null) return;
        GUILayout.BeginArea(new Rect(20, 20, 340, 480), GUI.skin.box);
        GUILayout.Label("OctOpus | Connection prototype");
        GUILayout.Label("Server address (UDP " + NetworkDefaults.Port + ")");
        GUI.enabled = state == LocalConnectionState.Stopped;
        address = GUILayout.TextField(address, 253);
        if (GUILayout.Button("Connect")) Connect();
        GUI.enabled = state == LocalConnectionState.Started || state == LocalConnectionState.Starting;
        if (GUILayout.Button("Disconnect")) Disconnect();
        GUI.enabled = true;
        GUILayout.Label("Status: " + state);
        GUILayout.Label("Players: " + roster.Length + " / " + NetworkDefaults.MaximumPlayers);
        foreach (int id in roster)
            GUILayout.Label("Player " + id + (id == manager.ClientManager.Connection.ClientId ? " (You)" : ""));
        GUILayout.EndArea();
    }

    private void OnDestroy()
    {
        if (manager != null)
        {
            manager.ClientManager.OnClientConnectionState -= OnConnectionState;
            manager.ClientManager.StopConnection();
            Destroy(manager.gameObject);
        }
        foreach (GameObject marker in markers.Values) Destroy(marker);
        markers.Clear();
    }
}
