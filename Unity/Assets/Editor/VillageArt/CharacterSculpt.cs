using UnityEngine;

public static class CharacterSculpt
{
    public static readonly Vector3 GripCenter=new Vector3(0,-.06f,.055f);
    public static readonly Quaternion ToolRotation=Quaternion.Euler(180,0,-65);
    public const float ToolScale=.78f, ToolGripY=.10f, GripClearance=.035f;
    public static void Face(ArtMesh art,Transform parent)
    {
        var sculpt=new OrganicSculpt(.045f);
        sculpt.Ellipsoid(Vector3.zero,new Vector3(.38f,.355f,.335f));
        sculpt.Ellipsoid(new Vector3(0,-.12f,.035f),new Vector3(.30f,.22f,.29f));
        sculpt.Ellipsoid(new Vector3(0,-.065f,.322f),new Vector3(.059f,.046f,.072f));
        foreach(int sign in new[]{-1,1})sculpt.Ellipsoid(new Vector3(sign*.357f,-.02f,-.018f),new Vector3(.086f,.108f,.075f));
        var face=sculpt.Bake(art,"Sculpted face",parent,new Bounds(Vector3.zero,new Vector3(1.05f,.87f,.98f)),.011f,Color.white,p=>{
            float blush=0;
            foreach(int sign in new[]{-1,1})blush+=Mathf.Exp(-((p.x-sign*.215f)*(p.x-sign*.215f)/.003f+(p.y+.085f)*(p.y+.085f)/.0015f))*Mathf.Clamp01((p.z-.19f)*12);
            return Color.Lerp(VillageColors.Skin,new Color(.90f,.43f,.32f),blush*.45f);
        });
        var material=new Material(Shader.Find("OctOpus/Painted Skin")){name="Painted skin",color=Color.white};art.Assets.Add(material);face.GetComponent<Renderer>().sharedMaterial=material;
    }
    public static void Hair(ArtMesh art,Transform parent)
    {
        var sculpt=new OrganicSculpt(.042f);
        sculpt.Ellipsoid(new Vector3(0,.18f,-.085f),new Vector3(.40f,.31f,.355f));
        for(int i=0;i<7;i++)
        {
            float a=i*Mathf.PI*2/7;
            Lock(sculpt,new Vector3(.03f,.39f,-.085f),new Vector3(Mathf.Sin(a)*.36f,.40f,Mathf.Cos(a)*.30f-.07f),new Vector3(Mathf.Sin(a)*.36f,.045f,Mathf.Cos(a)*.30f-.08f),.078f);
        }
        for(int i=0;i<4;i++)
        {
            float x=-.24f+i*.15f;
            Lock(sculpt,new Vector3(x+.09f,.32f,.19f),new Vector3(x+.06f,.30f,.345f),new Vector3(x-.05f,.075f+(i%2)*.055f,.285f),.085f);
        }
        Lock(sculpt,new Vector3(-.1f,.34f,-.07f),new Vector3(.02f,.51f,-.06f),new Vector3(.18f,.40f,.02f),.055f);
        sculpt.Bake(art,"Continuous sculpted hair",parent,new Bounds(new Vector3(0,.20f,-.04f),new Vector3(1.02f,.86f,1.0f)),.013f,new Color(.29f,.17f,.10f));
    }
    private static void Lock(OrganicSculpt s,Vector3 a,Vector3 c,Vector3 b,float radius)
    {
        Vector3 previous=a;float oldRadius=.04f;
        for(int i=1;i<=8;i++)
        {
            float t=i/8f;var p=(1-t)*(1-t)*a+2*(1-t)*t*c+t*t*b;
            float r=Mathf.Lerp(.04f,.013f,t)+Mathf.Sin(t*Mathf.PI)*radius;
            s.Capsule(previous,p,oldRadius,r);previous=p;oldRadius=r;
        }
    }
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
