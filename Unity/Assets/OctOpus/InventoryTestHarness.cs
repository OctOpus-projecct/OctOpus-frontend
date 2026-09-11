using System;
using System.Collections;
using System.Linq;
using OctOpus.Shared;
using UnityEngine;

// Opt-in real clients send requests only through their owned player; no inventory mutation RPC.
public sealed class InventoryTestHarness : MonoBehaviour
{
    private NetworkPlayer owner;
    private NetworkPlayer other;
    private NetworkTree tree;
    private NetworkTree nextTree;
    private bool failed;
    private bool observer;
    private bool overweight;
    private float started;
    private int initialWood;

    private IEnumerator Start()
    {
        started = Time.realtimeSinceStartup;
        var args = Environment.GetCommandLineArgs();
        observer = Array.IndexOf(args, "-octopus-inventory-observer") >= 0;
        overweight = Array.IndexOf(args, "-octopus-inventory-overweight") >= 0;
        initialWood = overweight ? 94 : 0;
        yield return Wait(() =>
        {
            owner = Players().FirstOrDefault(player => player.IsOwner);
            var trees = FindObjectsByType<NetworkTree>(FindObjectsSortMode.None);
            tree = trees.FirstOrDefault(item => Vector3.Distance(item.transform.position, WorldLayout.TreePositions[0]) < .01f);
            nextTree = trees.FirstOrDefault(item => Vector3.Distance(item.transform.position, WorldLayout.TreePositions[1]) < .01f);
            return owner != null && tree != null && nextTree != null && InventoryMatches(observer ? 0 : initialWood);
        }, 30, "InitialInventory");
        if (failed) yield break;
        LogInventory("Initial");
        yield return Wait(() =>
        {
            other = Players().FirstOrDefault(player => !player.IsOwner);
            return other != null && Players().Length == 2;
        }, 30, "TwoPlayers");
        if (failed) yield break;
        if (observer) yield return Observer();
        else yield return Worker();
        if (!failed) Debug.Log("[OctOpus] InventoryTest=Passed");
    }

    private IEnumerator Worker()
    {
        if (!FullFree(tree) || !FullFree(nextTree)) { Fail("Fresh trees required"); yield break; }
        owner.RequestWork(tree.NetworkObject);
        yield return Wait(() => Working(tree), 15, "Working");
        if (failed) yield break;
        owner.RequestStrike();
        yield return Wait(() => tree.Health == 80 && owner.StrikeSequence == 1, 3, "FirstHit");
        if (failed) yield break;
        yield return new WaitForSecondsRealtime(.4f);
        if (!InventoryMatches(initialWood)) { Fail("Partial work paid wood"); yield break; }
        Stage("PartialNoReward");
        yield return Wait(() => other.LastStrikeResult == StrikeResult.NotWorking &&
            other.LastWorkResult == WorkResult.Busy, 15, "ObserverRejectionsObserved");
        if (failed) yield break;
        owner.RequestMove(owner.ServerPosition);
        yield return Wait(() => FullFree(tree) && owner.Activity == PlayerActivity.Idle &&
            owner.LastWorkResult == WorkResult.Cancelled, 3, "Cancelled");
        if (failed) yield break;
        yield return new WaitForSecondsRealtime(.4f);
        if (!InventoryMatches(initialWood)) { Fail("Cancelled work paid wood"); yield break; }
        Stage("CancellationNoReward");
        owner.RequestWork(tree.NetworkObject);
        yield return Wait(() => Working(tree), 5, "Reacquired");
        if (failed) yield break;
        while (!tree.IsDepleted && !failed)
        {
            yield return new WaitForSecondsRealtime(.7f);
            yield return Wait(() => owner.Stamina >= 26, 10, "StaminaReady");
            if (failed) yield break;
            uint sequence = owner.StrikeSequence;
            float health = tree.Health;
            owner.RequestStrike();
            yield return Wait(() => owner.StrikeSequence == sequence + 1 && tree.Health == health - 20,
                3, "DamageReplicated");
            if (failed) yield break;
            if (tree.Health == 0)
                yield return Wait(() => tree.IsDepleted, 3, "DepletionReplicated");
        }
        if (failed) yield break;
        int rewardWood = initialWood + InventoryRules.WoodReward;
        yield return Wait(() => InventoryMatches(rewardWood) && owner.Activity == PlayerActivity.Idle &&
            tree.IsDepleted && tree.Health == 0 && tree.WorkerId == -1, 3, "FullRewardOnce");
        if (failed) yield break;
        LogInventory("Reward");
        yield return Wait(() => other.LastWorkResult == WorkResult.Depleted, 5, "ObserverDepletionAcknowledged");
        if (failed) yield break;
        uint finalSequence = owner.StrikeSequence;
        owner.RequestStrike();
        owner.RequestStrike();
        yield return Wait(() => owner.LastStrikeResult == StrikeResult.NotWorking, 3, "LateStrikesRejected");
        if (failed) yield break;
        yield return new WaitForSecondsRealtime(.5f);
        if (!InventoryMatches(rewardWood) || owner.StrikeSequence != finalSequence)
        { Fail("Duplicate or late strike paid another reward"); yield break; }
        Stage("NoDuplicateReward");
        Vector3 beforeWork = owner.ServerPosition;
        owner.RequestWork(nextTree.NetworkObject);
        if (overweight)
        {
            yield return Wait(() => owner.LastWorkResult == WorkResult.WeightRestricted, 3, "WeightRestricted");
            if (failed) yield break;
            yield return new WaitForSecondsRealtime(.5f);
            if (owner.Activity != PlayerActivity.Idle || !FullFree(nextTree) ||
                Vector3.Distance(beforeWork, owner.ServerPosition) > .03f || !InventoryMatches(rewardWood))
            { Fail("Restricted work changed movement, tree or inventory"); yield break; }
            Stage("RestrictedWorkPreservedState");
        }
        else
        {
            yield return Wait(() => Working(nextTree), 15, "NormalNextWorkAllowed");
            if (failed) yield break;
            owner.RequestMove(owner.ServerPosition);
            yield return Wait(() => owner.Activity == PlayerActivity.Idle && FullFree(nextTree), 3, "NextWorkCancelled");
            if (failed) yield break;
        }
        // Eight units toward the opposite half of the map keeps the straight segment in bounds.
        Vector3 origin = owner.ServerPosition;
        Vector3 destination = origin + Vector3.right * (origin.x >= 0 ? -8 : 8);
        owner.RequestMove(new Vector3(destination.x, 0, destination.z));
        yield return Wait(() => Vector3.Distance(owner.ServerPosition, origin) >= .2f, 3, "MovementStarted");
        if (failed) yield break;
        Vector3 sample = owner.ServerPosition;
        float sampleTime = Time.realtimeSinceStartup;
        yield return new WaitForSecondsRealtime(1);
        float elapsed = Time.realtimeSinceStartup - sampleTime;
        float speed = Vector3.Distance(owner.ServerPosition, sample) / elapsed;
        Debug.Log(FormattableString.Invariant($"[OctOpus] InventorySpeed={speed:F3};Seconds={elapsed:F3};Overweight={overweight}"));
        if (overweight ? speed < 1.3f || speed > 2.7f : speed < 3.1f || speed > 4.9f)
        { Fail("Replicated server movement speed outside expected range"); yield break; }
        Stage("MovementSpeedVerified");
        yield return Wait(() => Vector3.Distance(owner.ServerPosition, destination) <= .03f, 10, "MovementArrived");
        if (failed) yield break;
        if (!InventoryMatches(rewardWood)) { Fail("Movement changed inventory"); yield break; }
        Stage("FinalInventoryPreserved");
    }

    private IEnumerator Observer()
    {
        yield return Wait(() => other.StrikeSequence == 1 && tree.Health == 80 && tree.WorkerId == other.OwnerId,
            20, "PartialWorkObserved");
        if (failed) yield break;
        if (!PrivateInventory()) { Fail("Observer received another player's inventory"); yield break; }
        owner.RequestWork(tree.NetworkObject);
        yield return Wait(() => owner.LastWorkResult == WorkResult.Busy && owner.Activity == PlayerActivity.Idle,
            15, "BusyRejected");
        if (failed) yield break;
        owner.RequestStrike();
        yield return Wait(() => owner.LastStrikeResult == StrikeResult.NotWorking && owner.StrikeSequence == 0,
            3, "NonWorkerStrikeRejected");
        if (failed) yield break;
        yield return Wait(() => tree.IsDepleted && tree.Health == 0 && other.Activity == PlayerActivity.Idle,
            45, "DepletionObserved");
        if (failed) yield break;
        yield return new WaitForSecondsRealtime(.5f);
        if (!PrivateInventory() || !InventoryMatches(0)) { Fail("Owner-only reward leaked to observer"); yield break; }
        Stage("ObserverNoReward");
        Stage("OwnerInventoryPrivate");
        owner.RequestWork(tree.NetworkObject);
        yield return Wait(() => owner.LastWorkResult == WorkResult.Depleted && owner.Activity == PlayerActivity.Idle,
            3, "DepletedRequestRejected");
        // The process stays connected after Passed so the worker can finish its movement checks.
    }

    private bool InventoryMatches(int wood)
    {
        var inventory = owner.Inventory;
        long weight = (long)wood * InventoryRules.WoodUnitWeight;
        return inventory.WoodCount == wood && inventory.WeightUnits == weight &&
            inventory.CapacityUnits == InventoryRules.Capacity &&
            inventory.CanGather == (weight * 100 < (long)InventoryRules.Capacity * 95) &&
            inventory.IsOverweight == (weight > InventoryRules.Capacity);
    }
    private bool PrivateInventory() => other.Inventory.WoodCount == 0 && other.Inventory.WeightUnits == 0 &&
        other.Inventory.CapacityUnits == 0;
    private bool Working(NetworkTree target) => owner.Activity == PlayerActivity.Working &&
        target.WorkerId == owner.OwnerId && target.SecondsRemaining > 0;
    private static bool FullFree(NetworkTree target) => !target.IsDepleted && target.Health == target.MaximumHealth &&
        target.WorkerId == -1 && target.SecondsRemaining == 0;
    private static NetworkPlayer[] Players() => FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
    private IEnumerator Wait(Func<bool> predicate, float seconds, string stage)
    {
        float deadline = Mathf.Min(Time.realtimeSinceStartup + seconds, started + 100);
        while (!failed && Time.realtimeSinceStartup < deadline)
        {
            if (predicate()) { Stage(stage); yield break; }
            yield return null;
        }
        if (!failed) Fail("Timed out: " + stage);
    }
    private void Update()
    {
        if (!failed && observer && other != null && !PrivateInventory())
            Fail("Other player's inventory became visible");
    }
    private void LogInventory(string phase) => Debug.Log(FormattableString.Invariant(
        $"[OctOpus] InventorySnapshot={phase};Wood={owner.Inventory.WoodCount};Weight={owner.Inventory.WeightUnits};Capacity={owner.Inventory.CapacityUnits};CanGather={owner.Inventory.CanGather};Overweight={owner.Inventory.IsOverweight}"));
    private static void Stage(string stage) => Debug.Log("[OctOpus] InventoryStage=" + stage);
    private void Fail(string reason) { failed = true; Debug.LogError("[OctOpus] InventoryTest=Failed: " + reason); }
}
