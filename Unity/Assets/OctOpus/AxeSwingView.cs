using OctOpus.Shared;
using UnityEngine;

// Placeholder visual driven only by the server's accepted strike sequence.
public sealed class AxeSwingView : MonoBehaviour
{
    private NetworkPlayer player;
    private Transform pivot;
    private uint lastSequence;
    private float swingStarted = float.NegativeInfinity;
    private void Start()
    {
        player = GetComponent<NetworkPlayer>();
        lastSequence = player.StrikeSequence;
        pivot = new GameObject("Prototype Axe").transform;
        pivot.SetParent(transform, false);
        pivot.localPosition = new Vector3(.55f, .45f, .2f);
        CreatePart("Handle", new Vector3(0, .35f, 0), new Vector3(.08f, .7f, .08f), new Color(.4f, .22f, .1f));
        CreatePart("Blade", new Vector3(.14f, .68f, 0), new Vector3(.3f, .24f, .1f), new Color(.7f, .75f, .8f));
    }
    private void CreatePart(string label, Vector3 position, Vector3 scale, Color color)
    {
        var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = label;
        part.transform.SetParent(pivot, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        Destroy(part.GetComponent<Collider>());
        var properties = new MaterialPropertyBlock();
        properties.SetColor("_Color", color);
        part.GetComponent<Renderer>().SetPropertyBlock(properties);
    }
    private void Update()
    {
        if (player == null || pivot == null) return;
        if (lastSequence != player.StrikeSequence)
        {
            lastSequence = player.StrikeSequence;
            swingStarted = Time.unscaledTime;
        }
        float elapsed = Time.unscaledTime - swingStarted;
        float angle = elapsed < .3f ? Mathf.Sin(elapsed / .3f * Mathf.PI) * 95f : 0f;
        pivot.localRotation = Quaternion.Euler(angle, 0, -20);
    }
}
