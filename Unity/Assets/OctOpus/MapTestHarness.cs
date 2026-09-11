using System;
using System.Collections;
using System.Linq;
using OctOpus.Shared;
using UnityEngine;

// Opt-in real-client probe. Every RPC is sent through this client's owned player.
public sealed class MapTestHarness : MonoBehaviour
{
    private NetworkPlayer owner;
    private NetworkPlayer other;
    private NetworkTree[] trees;
    private int[] identities;
    private bool failed;
    private float started;

    private IEnumerator Start()
    {
        started = Time.realtimeSinceStartup;
        yield return Wait(() =>
        {
            owner = Players().FirstOrDefault(player => player.IsOwner);
            var spawned = FindObjectsByType<NetworkTree>(FindObjectsSortMode.None);
            if (owner == null || spawned.Length != WorldLayout.TreePositions.Count) return false;
            trees = WorldLayout.TreePositions.Select(position => spawned.FirstOrDefault(tree =>
                Vector3.Distance(tree.transform.position, position) < .01f)).ToArray();
            return trees.All(tree => tree != null && tree.NetworkObject.IsSpawned && tree.MaximumHealth == 100) &&
                trees.Distinct().Count() == trees.Length &&
                Vector3.Distance(owner.ServerPosition,
                    WorldLayout.PlayerSpawn(owner.OwnerId % NetworkDefaults.MaximumPlayers)) < .03f;
        }, 30, "LayoutAndSpawn");
        if (failed) yield break;
        identities = trees.Select(tree => tree.NetworkObject.ObjectId).ToArray();
        Debug.Log("[OctOpus] MapLayout=" + string.Join("|", trees.Select(tree =>
            FormattableString.Invariant($"{tree.NetworkObject.ObjectId}:{tree.transform.position.x:F2},{tree.transform.position.y:F2},{tree.transform.position.z:F2}"))));
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-octopus-map-partner") >= 0)
            yield return Partner();
        else
            yield return Lead();
        if (!failed) Debug.Log("[OctOpus] MapTest=Passed");
    }

    private IEnumerator Lead()
    {
        if (!trees.All(FullFree)) { Fail("Fresh default trees required"); yield break; }
        var destination = new Vector3(-11, 1, 10);
        owner.RequestMove(new Vector3(-11, 0, 10));
        yield return Wait(() => Vector3.Distance(owner.ServerPosition, destination) <= .03f,
            15, "BoundaryInteriorReached");
        if (failed) yield break;
        owner.RequestMove(new Vector3(WorldLayout.HalfExtent, 0, 10));
        yield return new WaitForSecondsRealtime(.5f);
        if (Vector3.Distance(owner.ServerPosition, destination) > .03f)
        { Fail("Out-of-bound destination moved player"); yield break; }
        Stage("BoundaryOutsideRejected");
        owner.RequestWork(trees[0].NetworkObject);
        yield return Wait(() => Working(owner, trees[0]), 15, "LeadWorking");
        if (failed) yield break;
        yield return Wait(() =>
        {
            other = Players().FirstOrDefault(player => !player.IsOwner && player.OwnerId == trees[1].WorkerId);
            return other != null && Working(other, trees[1]);
        }, 25, "PartnerWorkingObserved");
        if (failed) yield break;
        owner.RequestStrike();
        yield return Wait(BothDamaged, 5, "IndependentDamageObserved");
        if (failed) yield break;
        // Keep this state visible across independent player/tree SyncVar deliveries.
        yield return new WaitForSecondsRealtime(1);
        owner.RequestMove(owner.ServerPosition);
        yield return Wait(() => FullFree(trees[0]) && owner.Activity == PlayerActivity.Idle &&
            owner.LastWorkResult == WorkResult.Cancelled && Working(other, trees[1]) && trees[1].Health == 80,
            3, "IndependentCancellationObserved");
        if (failed) yield break;
        yield return Wait(() => trees[1].IsDepleted && trees[1].Health == 0 && trees[1].WorkerId == -1 &&
            FullFree(trees[0]), 25, "IndependentDepletionObserved");
        if (failed) yield break;
        owner.RequestWork(trees[0].NetworkObject);
        yield return Wait(() => Working(owner, trees[0]), 5, "LeadReacquired");
        if (failed) yield break;
        owner.RequestStrike();
        yield return Wait(() => Working(owner, trees[0]) && trees[0].Health == 80 && owner.StrikeSequence == 2,
            3, "LeadProgressRestored");
        if (failed) yield break;
        yield return Wait(() => FullFree(trees[1]) && Working(owner, trees[0]) && trees[0].Health == 80,
            12, "IndependentRespawnObserved");
        if (failed) yield break;
        VerifyIdentity();
    }

    private IEnumerator Partner()
    {
        yield return Wait(() =>
        {
            other = Players().FirstOrDefault(player => !player.IsOwner && player.OwnerId == trees[0].WorkerId);
            return other != null && Working(other, trees[0]) && FullFree(trees[1]);
        }, 10, "LeadWorkingObserved");
        if (failed) yield break;
        owner.RequestWork(trees[1].NetworkObject);
        yield return Wait(() => Working(owner, trees[1]), 15, "PartnerWorking");
        if (failed) yield break;
        owner.RequestStrike();
        yield return Wait(BothDamaged, 5, "IndependentDamageObserved");
        if (failed) yield break;
        yield return Wait(() => FullFree(trees[0]) && other.Activity == PlayerActivity.Idle &&
            other.LastWorkResult == WorkResult.Cancelled && Working(owner, trees[1]) && trees[1].Health == 80,
            5, "IndependentCancellationObserved");
        if (failed) yield break;
        // Allow the lead to observe cancellation before further damage changes this snapshot.
        yield return new WaitForSecondsRealtime(1);
        while (!trees[1].IsDepleted && !failed)
        {
            yield return new WaitForSecondsRealtime(.7f);
            yield return Wait(() => owner.Stamina >= 26, 10, "StrikeStaminaReady");
            if (failed) yield break;
            uint sequence = owner.StrikeSequence;
            float health = trees[1].Health;
            owner.RequestStrike();
            yield return Wait(() => owner.StrikeSequence == sequence + 1 && trees[1].Health == health - 20,
                3, "PartnerDamageReplicated");
            if (failed) yield break;
            if (trees[1].Health == 0)
                yield return Wait(() => trees[1].IsDepleted, 3, "DepletionReplicated");
        }
        if (failed) yield break;
        yield return Wait(() => trees[1].IsDepleted && trees[1].Health == 0 && trees[1].WorkerId == -1 &&
            owner.Activity == PlayerActivity.Idle, 3, "IndependentDepletionObserved");
        if (failed) yield break;
        yield return Wait(() => Working(other, trees[0]) && trees[0].Health == 80 && other.StrikeSequence == 2,
            5, "LeadProgressRestored");
        if (failed) yield break;
        yield return Wait(() => FullFree(trees[1]) && Working(other, trees[0]) && trees[0].Health == 80,
            12, "IndependentRespawnObserved");
        if (failed) yield break;
        VerifyIdentity();
    }

    private bool BothDamaged() => other != null && trees[0].WorkerId != trees[1].WorkerId &&
        trees[0].WorkerId >= 0 && trees[1].WorkerId >= 0 && owner.Activity == PlayerActivity.Working &&
        other.Activity == PlayerActivity.Working && owner.StrikeSequence == 1 && other.StrikeSequence == 1 &&
        trees[0].Health == 80 && trees[1].Health == 80 && !trees[0].IsDepleted && !trees[1].IsDepleted;

    private void VerifyIdentity()
    {
        var spawned = FindObjectsByType<NetworkTree>(FindObjectsSortMode.None);
        if (spawned.Length != trees.Length || trees.Where((tree, index) => tree == null ||
            !tree.NetworkObject.IsSpawned || tree.NetworkObject.ObjectId != identities[index] ||
            Vector3.Distance(tree.transform.position, WorldLayout.TreePositions[index]) > .01f).Any() ||
            trees.Skip(2).Any(tree => !FullFree(tree)))
        { Fail("Respawn changed tree identity/layout/count or unrelated tree state"); return; }
        Stage("AllTreeIdentitiesPreserved");
    }

    private static bool Working(NetworkPlayer player, NetworkTree tree) => player != null &&
        player.Activity == PlayerActivity.Working && tree.WorkerId == player.OwnerId && tree.SecondsRemaining > 0;
    private static bool FullFree(NetworkTree tree) => !tree.IsDepleted && tree.Health == tree.MaximumHealth &&
        tree.WorkerId == -1 && tree.SecondsRemaining == 0;
    private static NetworkPlayer[] Players() => FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
    private IEnumerator Wait(Func<bool> predicate, float seconds, string stage)
    {
        float deadline = Mathf.Min(Time.realtimeSinceStartup + seconds, started + 160);
        while (!failed && Time.realtimeSinceStartup < deadline)
        {
            if (predicate()) { Stage(stage); yield break; }
            yield return null;
        }
        if (!failed) Fail("Timed out: " + stage);
    }
    private static void Stage(string stage) => Debug.Log("[OctOpus] MapStage=" + stage);
    private void Fail(string reason) { failed = true; Debug.LogError("[OctOpus] MapTest=Failed: " + reason); }
}
