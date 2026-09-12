using UnityEngine;

public static class CharacterSculpt
{
    public static readonly Vector3 GripCenter=new Vector3(0,-.06f,.055f);
    public static readonly Quaternion ToolRotation=Quaternion.Euler(180,0,-65);
    public const float ToolScale=.78f, ToolGripY=.10f, GripClearance=.035f;
    public static void Face(ArtMesh art,Transform parent)
    {
        var sculpt=new OrganicSculpt(.030f);
        sculpt.Ellipsoid(Vector3.zero,new Vector3(.37f,.34f,.31f));
        sculpt.Ellipsoid(new Vector3(0,-.12f,0),new Vector3(.27f,.21f,.27f));
        sculpt.Ellipsoid(new Vector3(0,-.055f,.300f),new Vector3(.027f,.022f,.040f));
        foreach(int sign in new[]{-1,1})sculpt.Ellipsoid(new Vector3(sign*.351f,-.015f,-.018f),new Vector3(.067f,.089f,.055f));
        var face=sculpt.Bake(art,"Sculpted face",parent,new Bounds(Vector3.zero,new Vector3(1.05f,.87f,.98f)),.011f,Color.white,p=>{
            float blush=0;
            foreach(int sign in new[]{-1,1})blush+=Mathf.Exp(-((p.x-sign*.215f)*(p.x-sign*.215f)/.003f+(p.y+.085f)*(p.y+.085f)/.0015f))*Mathf.Clamp01((p.z-.19f)*12);
            return Color.Lerp(VillageColors.Skin,new Color(.90f,.43f,.32f),blush*.25f);
        });
        var material=new Material(Shader.Find("OctOpus/Painted Skin")){name="Painted skin",color=Color.white};art.Assets.Add(material);face.GetComponent<Renderer>().sharedMaterial=material;
    }
    public static void Hair(ArtMesh art,Transform parent)=>HairSurface.Build(art,parent);
    public static void Hand(ArtMesh art,Transform parent,bool gripping,int side)
    {
        var s=new OrganicSculpt(.012f);
        s.Ellipsoid(new Vector3(0,.08f,0),new Vector3(.082f,.145f,.08f));
        s.Ellipsoid(new Vector3(0,-.035f,-.008f),new Vector3(.079f,.080f,.053f));
        if(gripping)
        {
            Vector3 axis=ToolRotation*Vector3.up,radial=Vector3.forward,across=Vector3.Cross(axis,radial).normalized;
            for(int finger=0;finger<3;finger++)
            {
                var center=GripCenter+axis*((finger-1)*.044f);
                Vector3 previous=center-radial*.05f;
                for(int j=1;j<=12;j++)
                {
                    float angle=Mathf.Lerp(-Mathf.PI/2,Mathf.PI*1.05f,j/12f);
                    var next=center+(across*Mathf.Cos(angle)+radial*Mathf.Sin(angle))*.055f;
                    s.Capsule(previous,next,.020f,.020f);previous=next;
                }
            }
            s.Capsule(new Vector3(-.065f,-.035f,-.004f),GripCenter+axis*.067f+Vector3.forward*.023f,.031f,.025f);
            s.Exclusion=p=>Vector3.ProjectOnPlane(p-GripCenter,axis).magnitude-GripClearance;
        }
        else
        {
            for(int finger=0;finger<4;finger++)
            {
                float x=(finger-1.5f)*.033f;
                s.Capsule(new Vector3(x,-.07f,-.007f),new Vector3(x,-.12f+(Mathf.Abs(finger-1.5f)*.013f),.012f),.021f,.018f);
            }
            s.Capsule(new Vector3(-side*.061f,-.022f,.005f),new Vector3(-side*.077f,-.073f,.04f),.029f,.023f);
        }
        s.Bake(art,gripping?"Sculpted gripping hand":"Sculpted relaxed hand",parent,new Bounds(new Vector3(0,.025f,0),new Vector3(.35f,.48f,.32f)),.0055f,VillageColors.Skin);
    }
}
