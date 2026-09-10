using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using FishNet.Managing;
using NUnit.Framework;
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
    public IEnumerator DisconnectAndReconnect_ReusesManagerAndReplacesMarkers()
    {
        string serverPath = Path.GetFullPath(Path.Combine(Application.dataPath,
            "../../../OctOpus-backend/Builds/WindowsServer/OctOpusServer.exe"));
        Assert.That(File.Exists(serverPath), Is.True, "Build the companion Windows server first.");
        string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../TestResults"));
        Directory.CreateDirectory(output);
        string log = Path.Combine(output, "reconnect-server-" + Guid.NewGuid().ToString("N") + ".log");
        var server = Process.Start(new ProcessStartInfo(serverPath,
            "-batchmode -nographics -logFile \"" + log + "\"")
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
            yield return null;
            var manager = UnityEngine.Object.FindFirstObjectByType<NetworkManager>();
            client.Connect();
            deadline = Time.realtimeSinceStartup + 15f;
            while (!manager.ClientManager.Connection.IsAuthenticated && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(manager.ClientManager.Connection.IsAuthenticated, Is.True);
            yield return null;
            int firstId = manager.ClientManager.Connection.ClientId;
            Assert.That(GameObject.Find("Player " + firstId), Is.Not.Null);

            client.Disconnect();
            deadline = Time.realtimeSinceStartup + 10f;
            while (manager.IsClientStarted && Time.realtimeSinceStartup < deadline)
                yield return null;
            yield return null;
            yield return null;
            Assert.That(manager.IsClientStarted, Is.False);
            Assert.That(GameObject.Find("Player " + firstId), Is.Null);

            client.Connect();
            deadline = Time.realtimeSinceStartup + 15f;
            while (!manager.ClientManager.Connection.IsAuthenticated && Time.realtimeSinceStartup < deadline)
                yield return null;
            yield return null;
            Assert.That(manager.ClientManager.Connection.IsAuthenticated, Is.True);
            Assert.That(UnityEngine.Object.FindFirstObjectByType<NetworkManager>(), Is.SameAs(manager));
            Assert.That(GameObject.Find("Player " + manager.ClientManager.Connection.ClientId), Is.Not.Null);
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
