using System.Collections;
using System.Linq;
using OctOpus.Shared;
using UnityEngine;

// Enabled only by -octopus-movement-test for the local two-client integration script.
public sealed class MovementTestHarness : MonoBehaviour
{
    private bool failed;

    private IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 25f;
        NetworkPlayer owner = null;
        NetworkPlayer other = null;
        while (Time.realtimeSinceStartup < deadline)
        {
            var players = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
            owner = players.FirstOrDefault(player => player.IsOwner);
            other = players.FirstOrDefault(player => !player.IsOwner);
            if (owner != null && other != null) break;
            yield return null;
        }
        if (owner == null || other == null) { Fail("Two players did not spawn"); yield break; }

        Vector3 firstTarget = new Vector3(3f, 1f, 2f);
        Vector3 startingPosition = owner.ServerPosition;
        float startingDistance = Vector3.Distance(startingPosition, firstTarget);
        Debug.Log("[OctOpus] MovementStage=InitialRequest");
        owner.RequestMove(new Vector3(3f, 0f, 2f));
        deadline = Time.realtimeSinceStartup + 2f;
        yield return new WaitForSecondsRealtime(0.25f);
        while (owner != null && Time.realtimeSinceStartup < deadline &&
            Vector3.Distance(owner.ServerPosition, firstTarget) >= startingDistance - 0.1f)
            yield return null;
        if (owner == null) { Fail("Owner disappeared"); yield break; }
        if (!(Vector3.Distance(owner.ServerPosition, firstTarget) < startingDistance - 0.1f))
        { Fail("First request did not move toward its destination"); yield break; }
        if (Near(owner.ServerPosition, firstTarget))
        { Fail("First request already arrived before destination replacement"); yield break; }
        Debug.Log("[OctOpus] MovementStage=InitialProgress");
        Debug.Log("[OctOpus] MovementStage=ReplaceDestination");
        owner.RequestMove(new Vector3(-3f, 0f, 2f));
        yield return WaitForArrival(owner, new Vector3(-3f, 1f, 2f));
        if (failed) yield break;

        Debug.Log("[OctOpus] MovementStage=InvalidRequests");
        Vector3 stopped = owner.ServerPosition;
        owner.RequestMove(new Vector3(float.NaN, 0f, 0f));
        owner.RequestMove(new Vector3(1000f, 0f, 1000f));
        yield return new WaitForSecondsRealtime(0.75f);
        if (owner == null || !Near(owner.ServerPosition, stopped))
        { Fail("Invalid destination changed position"); yield break; }

        if (other == null) { Fail("Observer disappeared"); yield break; }
        Vector3 otherStopped = other.ServerPosition;
        Debug.Log("[OctOpus] MovementStage=ForeignOwnership");
        other.RequestMove(new Vector3(0f, 0f, -3f));
        yield return new WaitForSecondsRealtime(0.75f);
        if (other == null || !Near(other.ServerPosition, otherStopped))
        { Fail("Foreign ownership request moved observer"); yield break; }

        if (owner == null) { Fail("Owner disappeared"); yield break; }
        Debug.Log("[OctOpus] MovementStage=Recovery");
        owner.RequestMove(new Vector3(3f, 0f, -2f));
        yield return WaitForArrival(owner, new Vector3(3f, 1f, -2f));
        if (!failed) Debug.Log("[OctOpus] MovementTest=Passed");
    }

    private IEnumerator WaitForArrival(NetworkPlayer player, Vector3 destination)
    {
        float deadline = Time.realtimeSinceStartup + 8f;
        while (player != null && Time.realtimeSinceStartup < deadline)
        {
            if (Near(player.ServerPosition, destination) && Near(player.transform.position, destination))
                yield break;
            yield return null;
        }
        Fail("Did not reach " + destination);
    }

    private static bool Near(Vector3 actual, Vector3 expected)
    {
        // NaN distances compare false, so non-finite server state cannot pass.
        return Vector3.Distance(actual, expected) < 0.02f;
    }

    private void Fail(string reason)
    {
        failed = true;
        Debug.LogError("[OctOpus] MovementTest=Failed: " + reason);
    }
}
