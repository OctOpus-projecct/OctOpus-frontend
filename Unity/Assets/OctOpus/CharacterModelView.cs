using OctOpus.Shared;
using UnityEngine;

// Presentation follows replicated position and accepted strikes; never changes network state.
public sealed class CharacterModelView : MonoBehaviour
{
    private NetworkPlayer player;
    private GameObject model;
    private Transform body, leftLeg, rightLeg, leftArm, rightArm;
    private Renderer placeholder;
    private bool placeholderWasEnabled;
    private Vector3 previousPosition;
    private uint lastStrike;
    private float swingAt=float.NegativeInfinity, step;
    private void Start()
    {
        player=GetComponent<NetworkPlayer>();
        var prefab=Resources.Load<GameObject>("Village/Player");
        if(prefab==null)return;
        placeholder=GetComponent<Renderer>();
        if(placeholder!=null){placeholderWasEnabled=placeholder.enabled;placeholder.enabled=false;}
        model=Instantiate(prefab,transform,false);
        model.transform.localPosition=Vector3.down;
        body=model.transform.Find("Body");
        leftLeg=body.Find("LegL");rightLeg=body.Find("LegR");
        leftArm=body.Find("ArmL");rightArm=body.Find("ArmR");
        previousPosition=transform.position;lastStrike=player.StrikeSequence;
    }
    private void Update()
    {
        if(model==null || player==null)return;
        Vector3 delta=transform.position-previousPosition;delta.y=0;
        previousPosition=transform.position;
        bool walking=delta.sqrMagnitude>.000001f;
        if(walking)
        {
            step+=delta.magnitude*7;
            model.transform.rotation=Quaternion.Slerp(model.transform.rotation,Quaternion.LookRotation(delta),Time.deltaTime*15);
        }
        if(player.Activity==PlayerActivity.Working)
            foreach(var tree in FindObjectsByType<NetworkTree>(FindObjectsSortMode.None))
                if(tree.WorkerId==player.OwnerId)
                {var direction=tree.transform.position-transform.position;direction.y=0;if(direction.sqrMagnitude>.01f)model.transform.rotation=Quaternion.LookRotation(direction);break;}
        if(lastStrike!=player.StrikeSequence){lastStrike=player.StrikeSequence;swingAt=Time.unscaledTime;}
        float elapsed=Time.unscaledTime-swingAt;
        float swing=elapsed<.34f?Mathf.Sin(elapsed/.34f*Mathf.PI):0;
        float stride=walking?Mathf.Sin(step)*23:0;
        leftLeg.localRotation=Quaternion.Euler(stride,0,0);
        rightLeg.localRotation=Quaternion.Euler(-stride,0,0);
        leftArm.localRotation=Quaternion.Euler(-stride*.65f-swing*12,0,-11);
        rightArm.localRotation=Quaternion.Euler(stride*.65f-swing*115,0,11);
        body.localPosition=Vector3.up*(walking?Mathf.Abs(Mathf.Sin(step))*.035f:Mathf.Sin(Time.time*2)*.008f);
        body.localRotation=Quaternion.Euler(swing*8,0,0);
    }
    private void OnDestroy()
    {
        if(placeholder!=null)placeholder.enabled=placeholderWasEnabled;
        if(model!=null)Destroy(model);
    }
}
