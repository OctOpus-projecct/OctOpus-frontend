using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FishNet.Managing;
using FishNet.Transporting;
using NUnit.Framework;
using OctOpus.Shared;
using UnityEngine;
using UnityEngine.TestTools;

public class ClientReconnectTests
{
    private static bool ServerReady(string log)
    {
        if (!File.Exists(log)) return false;
        using (var stream = new FileStream(log, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var reader = new StreamReader(stream))
            return reader.ReadToEnd().Contains("[OctOpus] Server=Started");
    }

    [UnityTest]
    public IEnumerator DisconnectAndReconnect_ReusesManagerAndReplacesPlayerAndTreeInstances()
    {
        string serverPath = Path.GetFullPath(Path.Combine(Application.dataPath,
            "../../../OctOpus-backend/Builds/WindowsServer/OctOpusServer.exe"));
        Assert.That(File.Exists(serverPath), Is.True, "Build the companion Windows server first.");
        string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../TestResults"));
        Directory.CreateDirectory(output);
        string log = Path.Combine(output, "reconnect-server-" + Guid.NewGuid().ToString("N") + ".log");
        var server = Process.Start(new ProcessStartInfo(serverPath,
            "-batchmode -nographics -octopus-anonymous-regression -logFile \"" + log + "\"")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        });
        GameObject root = null;
        try
        {
            float deadline = Time.realtimeSinceStartup + 20f;
            while (!ServerReady(log)
                && Time.realtimeSinceStartup < deadline && !server.HasExited)
                yield return new WaitForSecondsRealtime(0.1f);
            Assert.That(!server.HasExited && ServerReady(log), Is.True, "Server did not start.");

            root = new GameObject("Client under test");
            var client = root.AddComponent<ClientBootstrap>();
            client.UseAnonymousRegression = true;
            yield return null;
            var manager = UnityEngine.Object.FindFirstObjectByType<NetworkManager>();
            client.Connect();
            deadline = Time.realtimeSinceStartup + 15f;
            while (!manager.ClientManager.Connection.IsAuthenticated && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(manager.ClientManager.Connection.IsAuthenticated, Is.True);
            yield return null;
            int firstId = manager.ClientManager.Connection.ClientId;
            deadline = Time.realtimeSinceStartup + 10f;
            while (UnityEngine.Object.FindFirstObjectByType<NetworkPlayer>() == null && Time.realtimeSinceStartup < deadline)
                yield return null;
            var firstPlayer = UnityEngine.Object.FindFirstObjectByType<NetworkPlayer>();
            Assert.That(firstPlayer, Is.Not.Null, "Authenticated connection must spawn a network player.");
            Assert.That(firstPlayer.IsOwner, Is.True);
            Assert.That(firstPlayer.OwnerId, Is.EqualTo(firstId));
            int firstInstance = firstPlayer.GetInstanceID();
            deadline = Time.realtimeSinceStartup + 10f;
            while (UnityEngine.Object.FindObjectsByType<NetworkTree>(FindObjectsSortMode.None).Length != WorldLayout.TreePositions.Count && Time.realtimeSinceStartup < deadline)
                yield return null;
            var firstTrees = UnityEngine.Object.FindObjectsByType<NetworkTree>(FindObjectsSortMode.None);
            Assert.That(firstTrees.Length, Is.EqualTo(WorldLayout.TreePositions.Count), "Initial join must spawn the world tree set.");
            var firstTreeInstances = firstTrees.ToDictionary(tree => tree.NetworkObject.ObjectId, tree => tree.GetInstanceID());
            firstPlayer.RequestMove(new Vector3(3f, 0f, 2f));
            deadline = Time.realtimeSinceStartup + 8f;
            while (Vector3.Distance(firstPlayer.ServerPosition, new Vector3(3f, 1f, 2f)) > 0.02f && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(Vector3.Distance(firstPlayer.ServerPosition, new Vector3(3f, 1f, 2f)), Is.LessThan(0.02f));

            client.Disconnect();
            deadline = Time.realtimeSinceStartup + 10f;
            while (manager.TransportManager.Transport.GetConnectionState(false) != LocalConnectionState.Stopped && Time.realtimeSinceStartup < deadline)
                yield return null;
            yield return null;
            yield return null;
            Assert.That(manager.TransportManager.Transport.GetConnectionState(false), Is.EqualTo(LocalConnectionState.Stopped));
            Assert.That(UnityEngine.Object.FindFirstObjectByType<NetworkPlayer>(), Is.Null);
            deadline = Time.realtimeSinceStartup + 5f;
            while (UnityEngine.Object.FindObjectsByType<NetworkTree>(FindObjectsSortMode.None).Length != 0 &&
                   Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(UnityEngine.Object.FindObjectsByType<NetworkTree>(FindObjectsSortMode.None).Length,
                Is.Zero, "Disconnect must remove the client's tree instance.");

            client.Connect();
            deadline = Time.realtimeSinceStartup + 15f;
            while (!manager.ClientManager.Connection.IsAuthenticated && Time.realtimeSinceStartup < deadline)
                yield return null;
            yield return null;
            Assert.That(manager.ClientManager.Connection.IsAuthenticated, Is.True);
            Assert.That(UnityEngine.Object.FindFirstObjectByType<NetworkManager>(), Is.SameAs(manager));
            deadline = Time.realtimeSinceStartup + 10f;
            while (UnityEngine.Object.FindFirstObjectByType<NetworkPlayer>() == null && Time.realtimeSinceStartup < deadline)
                yield return null;
            var secondPlayer = UnityEngine.Object.FindFirstObjectByType<NetworkPlayer>();
            Assert.That(secondPlayer, Is.Not.Null);
            Assert.That(secondPlayer.IsOwner, Is.True);
            Assert.That(secondPlayer.OwnerId, Is.EqualTo(manager.ClientManager.Connection.ClientId));
            Assert.That(secondPlayer.GetInstanceID(), Is.Not.EqualTo(firstInstance));
            Assert.That(UnityEngine.Object.FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            deadline = Time.realtimeSinceStartup + 10f;
            while (UnityEngine.Object.FindObjectsByType<NetworkTree>(FindObjectsSortMode.None).Length != WorldLayout.TreePositions.Count && Time.realtimeSinceStartup < deadline)
                yield return null;
            var secondTrees = UnityEngine.Object.FindObjectsByType<NetworkTree>(FindObjectsSortMode.None);
            Assert.That(secondTrees.Length, Is.EqualTo(WorldLayout.TreePositions.Count), "Reconnect must spawn the world tree set without duplicates.");
            foreach (var tree in secondTrees)
            {
                Assert.That(firstTreeInstances.ContainsKey(tree.NetworkObject.ObjectId), Is.True,
                    "The same server trees must survive the client's disconnect.");
                Assert.That(tree.GetInstanceID(), Is.Not.EqualTo(firstTreeInstances[tree.NetworkObject.ObjectId]),
                    "Reconnect must create fresh client tree instances.");
            }
        }
        finally
        {
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
            if (server != null)
            {
                if (!server.HasExited) { server.Kill(); server.WaitForExit(); }
                server.Dispose();
            }
        }
        yield return null;
    }
}
