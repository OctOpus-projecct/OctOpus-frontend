using OctOpus.Shared;
using UnityEngine;

// Keeps the shared hit colliders; meshes and stump are client-only presentation.
public sealed class TreeModelView : MonoBehaviour
{
    private NetworkTree tree;
    private GameObject standing,stump;
    private Renderer[] originals;
    private Collider[] hitColliders;
    private bool[] originalVisibility,originalCollision;
    private float lastHealth,shakeAt=float.NegativeInfinity;
    private void Start()
    {
        tree=GetComponent<NetworkTree>();
        var prefab=Resources.Load<GameObject>("Village/Tree");
        var stumpPrefab=Resources.Load<GameObject>("Village/Stump");
        if(prefab==null || stumpPrefab==null)return;
        originals=GetComponentsInChildren<Renderer>(true);
        originalVisibility=new bool[originals.Length];
        for(int i=0;i<originals.Length;i++){originalVisibility[i]=originals[i].enabled;originals[i].enabled=false;}
        hitColliders=GetComponentsInChildren<Collider>(true);
        originalCollision=new bool[hitColliders.Length];
        for(int i=0;i<hitColliders.Length;i++)originalCollision[i]=hitColliders[i].enabled;
        standing=Instantiate(prefab,transform,false);stump=Instantiate(stumpPrefab,transform,false);
        lastHealth=tree.Health;Refresh();
    }
    private void Update(){if(standing!=null)Refresh();}
    private void Refresh()
    {
        if(tree.Health<lastHealth)shakeAt=Time.unscaledTime+WoodcuttingPose.ImpactTime;
        else if(tree.Health>lastHealth)shakeAt=float.NegativeInfinity;
        lastHealth=tree.Health;
        bool showStump=tree.IsDepleted && Time.unscaledTime>=shakeAt;
        standing.SetActive(!showStump);stump.SetActive(showStump);
        for(int i=0;i<hitColliders.Length;i++)hitColliders[i].enabled=originalCollision[i] && !tree.IsDepleted;
        float elapsed=Time.unscaledTime-shakeAt;
        standing.transform.localRotation=Quaternion.Euler(0,0,elapsed>=0 && elapsed<.4f?Mathf.Sin(elapsed*45)*(1-elapsed/.4f)*2:0);
    }
    private void OnDestroy()
    {
        if(originals!=null)for(int i=0;i<originals.Length;i++)if(originals[i]!=null)originals[i].enabled=originalVisibility[i];
        if(hitColliders!=null)for(int i=0;i<hitColliders.Length;i++)if(hitColliders[i]!=null)hitColliders[i].enabled=originalCollision[i];
        if(standing!=null)Destroy(standing);if(stump!=null)Destroy(stump);
    }
}
