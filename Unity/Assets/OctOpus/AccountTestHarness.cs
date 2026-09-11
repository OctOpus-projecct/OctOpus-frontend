using System;
using System.Collections;
using System.IO;
using System.Linq;
using FishNet.Transporting;
using OctOpus.Shared;
using UnityEngine;

// Explicit integration harness; all changes go through normal authenticated RPCs.
public sealed class AccountTestHarness : MonoBehaviour
{
    private bool failed;
    private NetworkPlayer owner;
    private ClientBootstrap client;
    private string stopFile;
    private bool stopRequested;
    private bool gracefulReported;
    private void Awake()
    {
        client = GetComponent<ClientBootstrap>();
        stopFile = Environment.GetEnvironmentVariable("OCTOPUS_TEST_STOP_FILE");
    }
    private void Update()
    {
        if (string.IsNullOrEmpty(stopFile) || gracefulReported) return;
        if (!stopRequested && File.Exists(stopFile))
        {
            stopRequested = true;
            StopAllCoroutines();
            client.Disconnect();
        }
        if (stopRequested && client.ConnectionState == LocalConnectionState.Stopped &&
            !FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None).Any(p => p.IsOwner))
        {
            gracefulReported = true;
            Debug.Log("[OctOpus] AccountStage=GracefulDisconnected");
        }
    }
    private IEnumerator Start()
    {
        var args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, "-octopus-account-case");
        string scenario = index >= 0 && index + 1 < args.Length ? args[index + 1] : "restore";
        int expected = int.Parse(Environment.GetEnvironmentVariable("OCTOPUS_EXPECTED_WOOD") ?? "0");
        if (scenario == "reject")
        {
            yield return Wait(() => client.LoginFailed && client.ConnectionState == LocalConnectionState.Stopped, "Rejected");
            if (FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None).Any(p => p.IsOwner)) Fail("Rejected login spawned an owner");
        }
        else
        {
            yield return Wait(() => FindOwner(expected), "InitialWood=" + expected);
            if (failed) yield break;
            if (scenario == "earn")
            {
                NetworkTree tree = null;
                yield return Wait(() => (tree = FindObjectsByType<NetworkTree>(FindObjectsSortMode.None)
                    .FirstOrDefault(t => Vector3.Distance(t.transform.position, WorldLayout.TreePositions[0]) < .01f)) != null, "Tree");
                if (failed) yield break;
                owner.RequestWork(tree.NetworkObject);
                yield return Wait(() => owner.Activity == PlayerActivity.Working, "Working");
                if (failed) yield break;
                for (int hit = 0; hit < 5; hit++)
                {
                    yield return new WaitForSecondsRealtime(.7f);
                    yield return Wait(() => owner.Stamina >= 25, "Stamina");
                    if (failed) yield break;
                    uint before = owner.StrikeSequence;
                    owner.RequestStrike();
                    yield return Wait(() => owner.StrikeSequence == before + 1, "Strike");
                    if (failed) yield break;
                }
                yield return Wait(() => owner.Inventory.WoodCount == expected + 12, "RewardSaved");
                if (failed) yield break;
                client.Disconnect();
                yield return Wait(() => client.ConnectionState == LocalConnectionState.Stopped &&
                    !FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None).Any(p => p.IsOwner), "Disconnected");
                if (failed) yield break;
                yield return new WaitForSecondsRealtime(1);
                client.Connect();
                yield return Wait(() => FindOwner(expected + 12), "ReconnectRestored");
            }
        }
        if (!failed) Debug.Log("[OctOpus] AccountTest=Passed");
    }
    private bool FindOwner(int wood)
    {
        owner = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None).FirstOrDefault(p => p.IsOwner);
        return owner != null && owner.Inventory.CapacityUnits > 0 && owner.Inventory.WoodCount == wood;
    }
    private IEnumerator Wait(Func<bool> predicate, string stage)
    {
        float deadline = Time.realtimeSinceStartup + 35;
        while (!predicate() && Time.realtimeSinceStartup < deadline) yield return null;
        if (!predicate()) Fail("Timeout: " + stage);
        else Debug.Log("[OctOpus] AccountStage=" + stage);
    }
    private void Fail(string reason) { failed = true; Debug.LogError("[OctOpus] AccountTest=Failed: " + reason); }
}
