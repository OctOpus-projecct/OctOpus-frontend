using System;
using System.Collections;
using System.IO;
using System.Linq;
using OctOpus.Shared;
using UnityEngine;

// Opt-in integration probe; never attached during normal play.
public sealed class TreeTestHarness : MonoBehaviour
{
    private NetworkPlayer owner;
    private NetworkTree tree;
    private bool failed;
    private float nextLog;

    private IEnumerator Start()
    {
        yield return Wait(() =>
        {
            owner = Players().FirstOrDefault(player => player.IsOwner);
            tree = FindObjectsByType<NetworkTree>(FindObjectsSortMode.None)
                .OrderBy(item => item.NetworkObject.ObjectId).FirstOrDefault();
            return owner != null && tree != null;
        }, 30, "Spawned");
        if (failed) yield break;
        string[] args = Environment.GetCommandLineArgs();
        if (Array.IndexOf(args, "-octopus-tree-observer") >= 0)
        {
            yield return Wait(() => tree.WorkerId >= 0 && tree.WorkerId != owner.OwnerId &&
                tree.SecondsRemaining > 0 && Players().Any(player => player.OwnerId == tree.WorkerId &&
                player.Activity == PlayerActivity.Working), 20, "LateJoinObserved");
            if (failed) yield break;
            yield return new WaitForSecondsRealtime(1);
            if (FindObjectsByType<NetworkTree>(FindObjectsSortMode.None).Length != WorldLayout.TreePositions.Count)
            { Fail("Late join must observe the complete world tree set"); yield break; }
            Stage("WorldTreesObserved");
        }
        else if (Array.IndexOf(args, "-octopus-tree-contender") >= 0)
            yield return Contender();
        else
            yield return Worker(args);
        if (!failed) Debug.Log("[OctOpus] TreeTest=Passed");
    }

    private IEnumerator Worker(string[] args)
    {
        if (tree.WorkerId >= 0 || TreeInteractionRules.Distance(owner.ServerPosition, tree.transform.position) <= TreeInteractionRules.Range)
        { Fail("Initial tree must be free and outside interaction range"); yield break; }
        owner.RequestWork(tree.NetworkObject);
        yield return Wait(() => owner.Activity == PlayerActivity.Approaching && tree.WorkerId == -1 &&
            TreeInteractionRules.Distance(owner.ServerPosition, tree.transform.position) > TreeInteractionRules.Range,
            5, "ApproachingUnclaimed");
        if (failed) yield break;
        yield return Wait(Working, 15, "Working");
        if (failed) yield break;
        // Position and activity SyncVars may arrive on different updates. Require convergence
        // while still working; this client probe does not assert atomic server-start snapshots.
        yield return Wait(() => Working() && TreeInteractionRules.Distance(owner.ServerPosition,
            tree.transform.position) <= TreeInteractionRules.Range + 0.01f, 2, "ArrivedInRange");
        if (failed) yield break;
        yield return Wait(() => Players().Any(player => !player.IsOwner && player.LastWorkResult == WorkResult.Busy),
            20, "ContenderBusyObserved");
        if (failed) yield break;
        yield return new WaitForSecondsRealtime(2);
        float before = tree.SecondsRemaining;
        owner.RequestWork(tree.NetworkObject);
        owner.RequestWork(tree.NetworkObject);
        yield return new WaitForSecondsRealtime(1);
        if (!Working() || !(tree.SecondsRemaining < before - 0.4f))
        { Fail("Repeated request extended or disrupted work"); yield break; }
        Stage("RepeatedRequestPreservedDeadline");
        yield return Wait(() => owner.Activity == PlayerActivity.Idle && owner.LastWorkResult == WorkResult.TimedOut &&
            tree.WorkerId == -1 && tree.SecondsRemaining == 0, 35, "TimedOut");
        if (failed) yield break;
        owner.RequestWork(tree.NetworkObject);
        yield return Wait(Working, 5, "Restarted");
        if (failed) yield break;
        Vector3 destination = new Vector3(-3, 0, -3);
        owner.RequestMove(destination);
        yield return Wait(() => owner.Activity == PlayerActivity.Idle && owner.LastWorkResult == WorkResult.Cancelled &&
            tree.WorkerId == -1, 5, "MoveCancelled");
        if (failed) yield break;
        yield return Wait(() => Vector3.Distance(owner.ServerPosition, new Vector3(-3, 1, -3)) < 0.03f,
            15, "CancelMoveArrived");
        if (failed) yield break;
        owner.RequestWork(tree.NetworkObject);
        yield return Wait(Working, 15, "ReadyForDisconnect");
        if (failed) yield break;
        owner.RequestStrike();
        yield return Wait(() => tree.Health < tree.MaximumHealth && owner.StrikeSequence > 0,
            3, "DamagedBeforeDisconnect");
        if (failed) yield break;
        int markerIndex = Array.IndexOf(args, "-octopus-tree-disconnect-marker");
        if (markerIndex < 0 || markerIndex + 1 >= args.Length)
        { Fail("Disconnect marker argument missing"); yield break; }
        string marker = args[markerIndex + 1];
        yield return Wait(() => File.Exists(marker), 10, "DisconnectSignal");
        if (failed) yield break;
        GetComponent<ClientBootstrap>().Disconnect();
        Stage("Disconnected");
    }

    private IEnumerator Contender()
    {
        yield return Wait(() => tree.WorkerId >= 0 && tree.WorkerId != owner.OwnerId, 20, "WorkerObserved");
        if (failed) yield break;
        int originalWorker = tree.WorkerId;
        owner.RequestWork(tree.NetworkObject);
        yield return Wait(() => owner.LastWorkResult == WorkResult.Busy && owner.Activity == PlayerActivity.Idle &&
            tree.WorkerId == originalWorker, 15, "Busy");
        if (failed) yield break;
        yield return Wait(() => !Players().Any(player => player.OwnerId == originalWorker) && tree.WorkerId == -1 &&
            tree.Health == tree.MaximumHealth,
            110, "DisconnectedWorkerReleased");
        if (failed) yield break;
        owner.RequestWork(tree.NetworkObject);
        yield return Wait(Working, 10, "Reclaimed");
    }

    private bool Working() => owner != null && tree != null && owner.Activity == PlayerActivity.Working &&
        owner.LastWorkResult == WorkResult.Started && tree.WorkerId == owner.OwnerId && tree.SecondsRemaining > 0;
    private static NetworkPlayer[] Players() => FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
    private IEnumerator Wait(Func<bool> predicate, float seconds, string stage)
    {
        float deadline = Time.realtimeSinceStartup + seconds;
        while (Time.realtimeSinceStartup < deadline)
        {
            if (predicate()) { Stage(stage); yield break; }
            yield return null;
        }
        Fail("Timed out: " + stage);
    }
    private void Update()
    {
        if (tree == null || owner == null || Time.realtimeSinceStartup < nextLog) return;
        nextLog = Time.realtimeSinceStartup + 0.5f;
        Debug.Log(FormattableString.Invariant($"[OctOpus] TreeState={tree.NetworkObject.ObjectId};Worker={tree.WorkerId};Remaining={tree.SecondsRemaining:F2};Owner={owner.OwnerId};Activity={owner.Activity};Result={owner.LastWorkResult};Distance={TreeInteractionRules.Distance(owner.ServerPosition, tree.transform.position):F3}"));
    }
    private static void Stage(string stage) => Debug.Log("[OctOpus] TreeStage=" + stage);
    private void Fail(string reason) { failed = true; Debug.LogError("[OctOpus] TreeTest=Failed: " + reason); }
}
