using System;
using System.Collections;
using System.Linq;
using OctOpus.Shared;
using UnityEngine;

// Opt-in probe using two real clients, each sending RPCs only on its own player.
public sealed class StrikeTestHarness : MonoBehaviour
{
    private NetworkPlayer owner;
    private NetworkTree tree;
    private bool failed;
    private float started;
    private float nextLog;

    private IEnumerator Start()
    {
        started = Time.realtimeSinceStartup;
        yield return Wait(() =>
        {
            owner = Players().FirstOrDefault(player => player.IsOwner);
            tree = FindObjectsByType<NetworkTree>(FindObjectsSortMode.None)
                .OrderBy(item => item.NetworkObject.ObjectId).FirstOrDefault();
            return owner != null && tree != null;
        }, 30, "Spawned");
        if (failed) yield break;
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-octopus-strike-observer") >= 0)
            yield return Observer();
        else
            yield return Worker();
        if (!failed) Debug.Log("[OctOpus] StrikeTest=Passed");
    }

    private IEnumerator Worker()
    {
        int objectId = tree.NetworkObject.ObjectId;
        Vector3 originalPosition = tree.transform.position;
        if (tree.MaximumHealth != 100 || owner.MaximumStamina != 100 || !FullFree())
        { Fail("Fresh server with default 100 HP/stamina required"); yield break; }
        owner.RequestWork(tree.NetworkObject);
        yield return Wait(Working, 15, "Working");
        if (failed) yield break;
        owner.RequestStrike();
        owner.RequestStrike();
        yield return Wait(() => owner.StrikeSequence == 1 && tree.Health == 80 &&
            owner.LastStrikeResult == StrikeResult.Cooldown && owner.Stamina >= 75 && owner.Stamina < 85,
            3, "DuplicateCooldown");
        if (failed) yield break;
        yield return Wait(() => Players().Any(player => !player.IsOwner &&
            player.LastStrikeResult == StrikeResult.NotWorking), 20, "ObserverRejectionObserved");
        if (failed) yield break;
        if (tree.Health != 80 || owner.StrikeSequence != 1)
        { Fail("Observer strike changed worker progress"); yield break; }

        // Refresh the low stamina snapshot before cancellation, regardless of observer startup latency.
        yield return new WaitForSecondsRealtime(.7f);
        owner.RequestStrike();
        yield return Wait(() => owner.StrikeSequence == 2 && tree.Health == 60, 3, "SecondHit");
        if (failed) yield break;
        float beforeCancel = owner.Stamina;
        owner.RequestMove(owner.ServerPosition);
        yield return Wait(() => owner.Activity == PlayerActivity.Idle &&
            owner.LastWorkResult == WorkResult.Cancelled && FullFree(), 3, "CancelRestoredHealth");
        if (failed) yield break;
        if (owner.Stamina >= owner.MaximumStamina || owner.Stamina > beforeCancel + 10)
        { Fail("Cancellation refunded stamina"); yield break; }
        float idleStart = owner.Stamina;
        yield return Wait(() => owner.Stamina >= idleStart + 5, 3, "IdleRegeneration");
        if (failed) yield break;
        owner.RequestWork(tree.NetworkObject);
        yield return Wait(Working, 5, "Restarted");
        if (failed) yield break;

        bool blocked = false;
        while (!tree.IsDepleted && !failed)
        {
            yield return new WaitForSecondsRealtime(.75f);
            uint sequence = owner.StrikeSequence;
            float health = tree.Health;
            bool previousInsufficient = owner.LastStrikeResult == StrikeResult.InsufficientStamina;
            owner.RequestStrike();
            yield return Wait(() => owner.StrikeSequence > sequence ||
                (!previousInsufficient && owner.LastStrikeResult == StrikeResult.InsufficientStamina),
                3, "StrikeResolved");
            if (failed) yield break;
            if (owner.StrikeSequence == sequence)
            {
                // Wait for independent tree/player SyncVars to converge before checking rejection.
                yield return new WaitForSecondsRealtime(.3f);
                if (owner.StrikeSequence != sequence || tree.Health != health)
                { Fail("Rejected hit changed HP or accepted sequence"); yield break; }
                blocked = true;
                Stage("InsufficientStaminaPreservedProgress");
                yield return Wait(() => owner.Stamina >= 26, 10, "WorkingRegeneration");
                if (failed) yield break;
            }
            else
            {
                yield return Wait(() => tree.Health == health - 20, 2, "DamageReplicated");
                if (failed) yield break;
                if (tree.Health == 0)
                {
                    yield return Wait(() => tree.IsDepleted, 2, "DepletionReplicated");
                    if (failed) yield break;
                }
            }
        }
        if (!blocked) { Fail("Expected stamina rejection before depletion"); yield break; }
        yield return Wait(() => tree.IsDepleted && tree.Health == 0 && tree.WorkerId == -1 &&
            owner.Activity == PlayerActivity.Idle && owner.LastWorkResult == WorkResult.Depleted,
            3, "Depleted");
        if (failed) yield break;
        yield return Wait(() => Players().Any(player => !player.IsOwner &&
            player.LastWorkResult == WorkResult.Depleted), 5, "ObserverDepletedRejectionObserved");
        if (failed) yield break;
        uint depletedSequence = owner.StrikeSequence;
        owner.RequestStrike();
        owner.RequestWork(tree.NetworkObject);
        yield return Wait(() => owner.LastWorkResult == WorkResult.Depleted &&
            owner.LastStrikeResult != StrikeResult.Hit, 3, "DepletedRequestsRejected");
        if (failed) yield break;
        yield return new WaitForSecondsRealtime(.3f);
        if (tree.Health != 0 || !tree.IsDepleted || owner.StrikeSequence != depletedSequence)
        { Fail("Depleted requests changed tree or accepted sequence"); yield break; }
        yield return Wait(FullFree, 12, "Respawned");
        if (failed) yield break;
        if (tree.NetworkObject.ObjectId != objectId || tree.transform.position != originalPosition ||
            FindObjectsByType<NetworkTree>(FindObjectsSortMode.None).Length != 1)
        { Fail("Respawn replaced, moved or duplicated the tree"); yield break; }
        Stage("RespawnIdentityPreserved");
        // Leave the free respawn visible for the observer before starting the next work cycle.
        yield return new WaitForSecondsRealtime(.5f);

        owner.RequestWork(tree.NetworkObject);
        yield return Wait(Working, 5, "TimeoutWorkStarted");
        if (failed) yield break;
        owner.RequestStrike();
        yield return Wait(() => owner.StrikeSequence == depletedSequence + 1 && tree.Health == 80,
            3, "TimeoutInitialDamage");
        if (failed) yield break;
        // Idle/work regeneration could naturally fill stamina over 30 seconds. Damage again near
        // expiry so that an erroneous timeout refund remains observable on the real client.
        yield return Wait(() => Working() && tree.SecondsRemaining <= 2 && tree.SecondsRemaining > .5f,
            32, "NearTimeout");
        if (failed) yield break;
        owner.RequestStrike();
        yield return Wait(() => owner.StrikeSequence == depletedSequence + 2 && tree.Health == 60,
            1.5f, "TimeoutFinalDamage");
        if (failed) yield break;
        yield return Wait(() => owner.Activity == PlayerActivity.Idle &&
            owner.LastWorkResult == WorkResult.TimedOut && FullFree(), 5, "TimeoutRestoredHealth");
        if (failed) yield break;
        if (owner.Stamina >= 95) { Fail("Timeout unexpectedly refilled stamina"); yield break; }
        Stage("TimeoutPreservedStamina");
    }

    private IEnumerator Observer()
    {
        NetworkPlayer worker = null;
        yield return Wait(() =>
        {
            worker = Players().FirstOrDefault(player => !player.IsOwner && player.OwnerId == tree.WorkerId);
            return worker != null && worker.StrikeSequence == 1 && tree.Health == 80 &&
                worker.Stamina > 0 && worker.Stamina <= worker.MaximumStamina;
        }, 15, "LateJoinDamageObserved");
        if (failed) yield break;
        owner.RequestStrike();
        yield return Wait(() => owner.LastStrikeResult == StrikeResult.NotWorking &&
            owner.StrikeSequence == 0 && owner.Activity == PlayerActivity.Idle,
            3, "NonWorkerStrikeRejected");
        if (failed) yield break;
        // Worker sees this result and resumes; do not require its HP to remain frozen afterwards.
        yield return Wait(() => tree.IsDepleted && tree.Health == 0 && worker.Activity == PlayerActivity.Idle,
            60, "ObserverDepletionObserved");
        if (failed) yield break;
        owner.RequestWork(tree.NetworkObject);
        yield return Wait(() => owner.LastWorkResult == WorkResult.Depleted &&
            owner.Activity == PlayerActivity.Idle && owner.StrikeSequence == 0 && tree.Health == 0,
            3, "ObserverDepletedRequestRejected");
        if (failed) yield break;
        yield return Wait(FullFree, 12, "ObserverRespawnObserved");
    }

    private bool Working() => owner.Activity == PlayerActivity.Working && tree.WorkerId == owner.OwnerId &&
        tree.SecondsRemaining > 0;
    private bool FullFree() => !tree.IsDepleted && tree.Health == tree.MaximumHealth &&
        tree.WorkerId == -1 && tree.SecondsRemaining == 0;
    private static NetworkPlayer[] Players() => FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
    private IEnumerator Wait(Func<bool> predicate, float seconds, string stage)
    {
        float deadline = Mathf.Min(Time.realtimeSinceStartup + seconds, started + 180);
        while (!failed && Time.realtimeSinceStartup < deadline)
        {
            if (predicate()) { Stage(stage); yield break; }
            yield return null;
        }
        if (!failed) Fail("Timed out: " + stage);
    }
    private void Update()
    {
        if (owner == null || tree == null || Time.realtimeSinceStartup < nextLog) return;
        nextLog = Time.realtimeSinceStartup + .5f;
        Debug.Log(FormattableString.Invariant($"[OctOpus] StrikeState={tree.NetworkObject.ObjectId};HP={tree.Health};Depleted={tree.IsDepleted};Respawn={tree.RespawnRemaining:F2};Remaining={tree.SecondsRemaining:F2};Owner={owner.OwnerId};Stamina={owner.Stamina:F2};Sequence={owner.StrikeSequence};Result={owner.LastStrikeResult};Work={owner.LastWorkResult}"));
    }
    private static void Stage(string stage) => Debug.Log("[OctOpus] StrikeStage=" + stage);
    private void Fail(string reason) { failed = true; Debug.LogError("[OctOpus] StrikeTest=Failed: " + reason); }
}
