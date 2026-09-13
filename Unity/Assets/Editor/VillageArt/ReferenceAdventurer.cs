using UnityEngine;

// Authored from the hat-and-coat character in the user's selected ArtStation video.
public static class ReferenceAdventurer
{
    private static readonly Color Coat=new Color(.065f,.095f,.135f),Leather=new Color(.29f,.17f,.095f),
        Linen=new Color(.77f,.73f,.61f),Scarf=new Color(.46f,.095f,.085f),Metal=new Color(.57f,.53f,.43f),
        Hair=new Color(.38f,.29f,.18f),Skin=new Color(.96f,.77f,.65f);
    public static ArtMesh Build()
    {
        var art=new ArtMesh("Player");var body=art.Group("Body",art.Root.transform,Vector3.zero);
        CharacterSurface.Form(art,"Linen shirt",body,new[]{new Vector4(.65f,.25f,.19f,0),new Vector4(.78f,.29f,.21f,0),new Vector4(.98f,.275f,.20f,0),new Vector4(1.15f,.29f,.19f,0),new Vector4(1.24f,.16f,.13f,0)},Linen);
        ReferenceGarments.Coat(art,body,Coat);
        foreach(int side in new[]{-1,1}){Leg(art,body,side);Arm(art,body,side);}
        // Cloth creases have tapered ends and follow the wrapped torso.
        for(int i=0;i<3;i++)SculptedParts.Curve(art,"Shirt fold",body,new Vector3(-.19f,.84f+i*.085f,.17f),new Vector3(-.02f,.80f+i*.085f,.224f),new Vector3(.19f,.85f+i*.085f,.17f),.004f,.002f,Linen*.80f);
        CharacterSurface.Form(art,"Waist belt",body,new[]{new Vector4(.73f,.298f,.236f,0),new Vector4(.78f,.298f,.238f,0),new Vector4(.83f,.292f,.232f,0)},Leather);
        Buckle(art,body,new Vector3(0,.783f,.247f),.14f,.102f);
        foreach(int side in new[]{-1,1})
        {
            var pouch=art.Group("Belt pouch",body,new Vector3(side*.245f,.66f,.235f));pouch.localRotation=Quaternion.Euler(0,side*17,side*-9);
            art.SoftBox("Stitched leather pouch",pouch,Vector3.zero,new Vector3(.16f,.20f,.10f),Leather);
            art.SoftBox("Pouch flap",pouch,new Vector3(0,.057f,.06f),new Vector3(.163f,.095f,.026f),Leather*1.22f);
            art.Beam("Pouch keeper",pouch,new Vector3(0,.08f,.078f),new Vector3(0,-.055f,.073f),.025f,Leather*.65f);
            art.Ball("Pouch rivet",pouch,new Vector3(0,.006f,.085f),Vector3.one*.019f,Metal);
        }
        ReferenceGarments.Patch(art,"Diagonal shoulder belt",body,(t,u)=>{
            float x=Mathf.Lerp(-.255f,.25f,t)+(u-.5f)*.077f;
            return new Vector3(x,Mathf.Lerp(.85f,1.255f,t),.24f+.014f*Mathf.Sin(t*Mathf.PI));
        },Vector3.back*.018f,Leather*1.35f,24,6);
        var shoulderBuckle=art.Group("Shoulder buckle",body,new Vector3(.045f,1.09f,.264f));shoulderBuckle.localRotation=Quaternion.Euler(0,0,-49);
        Buckle(art,shoulderBuckle,Vector3.zero,.094f,.074f);
        CharacterSurface.Form(art,"Wrapped crimson scarf",body,new[]{new Vector4(1.14f,.17f,.16f,.04f),new Vector4(1.20f,.28f,.21f,.02f),new Vector4(1.29f,.235f,.18f,0),new Vector4(1.34f,.145f,.135f,0)},Scarf);
        for(int i=0;i<3;i++)SculptedParts.Curve(art,"Scarf layered fold",body,new Vector3(-.23f,1.25f+i*.025f,.095f),new Vector3(0,1.10f+i*.047f,.30f),new Vector3(.23f,1.26f+i*.02f,.09f),.012f,.007f,Scarf*(.72f+i*.12f));
        ReferenceGarments.Patch(art,"Scarf tail",body,(t,u)=>new Vector3(-.17f-t*.16f+(u-.5f)*(.12f-t*.035f),1.25f-t*.28f,-.20f-.08f*Mathf.Sin(t*Mathf.PI)),Vector3.back*.014f,Scarf,20,8);
        var head=art.Group("Head",body,new Vector3(0,1.61f,0));head.localScale=Vector3.one*1.14f;
        Face(art,head);HairStyle(art,head);ReferenceGarments.Hat(art,head);SeatHairUnderHat(head);
        WoodcuttingPose.Apply(art.Root.transform,float.PositiveInfinity,false);
        return art;
    }
    private static void Leg(ArtMesh art,Transform body,int side)
    {
        var leg=art.Group(side<0?"LegL":"LegR",body,new Vector3(side*.185f,.67f,0));
        CharacterSurface.Form(art,"Gathered linen trousers",leg,new[]{new Vector4(-.32f,.11f,.105f,0),new Vector4(-.28f,.15f,.135f,0),new Vector4(-.20f,.178f,.15f,0),new Vector4(-.07f,.164f,.155f,0),new Vector4(.045f,.13f,.13f,0)},Linen);
        foreach(float y in new[]{-.22f,-.27f})SculptedParts.Curve(art,"Trouser fold",leg,new Vector3(-.12f,y+.025f,.09f),new Vector3(.01f,y-.025f,.165f),new Vector3(.12f,y+.01f,.09f),.006f,.003f,Linen*.74f);
        CharacterSurface.Form(art,"Traveler boot",leg,new[]{new Vector4(-.657f,.09f,.15f,.064f),new Vector4(-.63f,.14f,.197f,.06f),new Vector4(-.57f,.143f,.20f,.065f),new Vector4(-.51f,.12f,.15f,.035f),new Vector4(-.40f,.11f,.112f,0),new Vector4(-.29f,.126f,.123f,0)},Leather);
        CharacterSurface.Form(art,"Boot sole",leg,new[]{new Vector4(-.67f,.10f,.16f,.062f),new Vector4(-.651f,.141f,.198f,.063f),new Vector4(-.63f,.14f,.197f,.06f)},Leather*.55f);
        CharacterSurface.Form(art,"Boot broad strap",leg,new[]{new Vector4(-.435f,.123f,.129f,0),new Vector4(-.38f,.127f,.131f,0),new Vector4(-.33f,.129f,.132f,0)},Leather*1.45f);
        Buckle(art,leg,new Vector3(side*.082f,-.383f,.112f),.069f,.089f);
        for(int i=0;i<3;i++)SculptedParts.Curve(art,"Boot wrapped seam",leg,new Vector3(-.10f,-.49f-i*.037f,.12f),new Vector3(0,-.51f-i*.034f,.211f),new Vector3(.10f,-.485f-i*.035f,.12f),.006f,.006f,Leather*1.6f);
    }
    private static void Arm(ArtMesh art,Transform body,int side)
    {
        var arm=art.Group(side<0?"ArmL":"ArmR",body,new Vector3(side*.32f,1.19f,0));arm.localRotation=Quaternion.Euler(0,0,side*14);
        CharacterSurface.Form(art,"Coat sleeve",arm,new[]{new Vector4(-.29f,.096f,.103f,0),new Vector4(-.24f,.14f,.139f,0),new Vector4(-.13f,.143f,.143f,0),new Vector4(.005f,.12f,.12f,0),new Vector4(.046f,.038f,.047f,0)},Coat);
        var forearm=art.Group("Forearm",arm,Vector3.down*WoodcuttingPose.UpperArmLength);
        CharacterSurface.Form(art,"Broad turned cuff",forearm,new[]{new Vector4(-.05f,.095f,.10f,0),new Vector4(-.025f,.125f,.131f,0),new Vector4(.03f,.131f,.134f,0),new Vector4(.06f,.12f,.123f,0)},Linen);
        CharacterSurface.Form(art,"Cuff edge seam",forearm,new[]{new Vector4(-.044f,.107f,.113f,0),new Vector4(-.028f,.124f,.131f,0)},Leather*1.30f);
        CharacterSurface.Form(art,"Shaped forearm",forearm,new[]{new Vector4(-WoodcuttingPose.ForearmLength,.062f,.062f,0),new Vector4(-.18f,.074f,.071f,0),new Vector4(-.07f,.086f,.081f,0),new Vector4(.02f,.072f,.073f,0)},Skin);
        var hand=art.Group("Hand",arm,new Vector3(0,-.54f,0));CharacterSculpt.Hand(art,hand,side==1,side,false);
        if(side<0)
        {
            var support=art.Group("SupportGrip",hand,Vector3.zero);
            CharacterSculpt.Hand(art,support,true,side,false);support.gameObject.SetActive(false);
        }
        var grip=art.Group("GripAxis",hand,CharacterSculpt.GripCenter);grip.localRotation=CharacterSculpt.ToolRotation;
        var wrist=art.Group("Wrist wrap",forearm,Vector3.down*WoodcuttingPose.ForearmLength);
        CharacterSurface.Form(art,"Leather wrist wrap",wrist,new[]{new Vector4(.007f,.070f,.067f,0),new Vector4(.052f,.075f,.075f,0),new Vector4(.09f,.08f,.077f,0)},Leather);
        art.Beam("Wrist keeper",wrist,new Vector3(-.026f,.083f,.08f),new Vector3(.03f,.018f,.069f),.023f,Leather*1.50f);
        if(side==1)
        {
            var tool=art.Group("HeldAxe",hand,CharacterSculpt.GripCenter-CharacterSculpt.ToolRotation*(Vector3.up*CharacterSculpt.ToolGripY*CharacterSculpt.ToolScale));
            tool.localRotation=CharacterSculpt.ToolRotation;tool.localScale=Vector3.one*CharacterSculpt.ToolScale;CharacterModels.MakeAxe(art,tool);
        }
    }
    private static void Buckle(ArtMesh art,Transform parent,Vector3 center,float width,float height)
    {
        art.SoftBox("Buckle leather backing",parent,center,new Vector3(width,height,.027f),Leather*.62f);
        Vector3 a=center+new Vector3(-width*.5f,-height*.5f,.02f),b=center+new Vector3(width*.5f,-height*.5f,.02f),c=center+new Vector3(width*.5f,height*.5f,.02f),d=center+new Vector3(-width*.5f,height*.5f,.02f);
        art.Beam("Buckle frame",parent,a,b,.012f,Metal);art.Beam("Buckle frame",parent,b,c,.012f,Metal);art.Beam("Buckle frame",parent,c,d,.012f,Metal);art.Beam("Buckle frame",parent,d,a,.012f,Metal);
        art.Beam("Buckle pin",parent,center+Vector3.forward*.027f,center+new Vector3(width*.42f,0,.027f),.009f,Metal);
    }
    private static void Face(ArtMesh art,Transform head)
    {
        var sculpt=new OrganicSculpt(.020f);
        sculpt.Ellipsoid(Vector3.zero,new Vector3(.355f,.31f,.275f));
        sculpt.Ellipsoid(new Vector3(0,-.115f,-.008f),new Vector3(.25f,.185f,.235f));
        sculpt.Ellipsoid(new Vector3(0,-.065f,.270f),new Vector3(.010f,.009f,.011f));
        foreach(int side in new[]{-1,1})sculpt.Ellipsoid(new Vector3(side*.337f,-.008f,-.023f),new Vector3(.061f,.083f,.046f));
        var face=sculpt.Bake(art,"Sculpted face",head,new Bounds(Vector3.zero,new Vector3(.95f,.78f,.84f)),.010f,Color.white,p=>{
            float blush=0;foreach(int side in new[]{-1,1})blush+=Mathf.Exp(-Mathf.Pow((p.x-side*.235f)/.06f,2)-Mathf.Pow((p.y+.065f)/.035f,2))*Mathf.Clamp01((p.z-.13f)*10);
            return Color.Lerp(Skin,new Color(.94f,.48f,.42f),blush*.25f);
        });
        var material=new Material(Shader.Find("OctOpus/Adventurer Face")){name="Adventurer skin",color=Color.white};art.Assets.Add(material);face.GetComponent<Renderer>().sharedMaterial=material;
    }
    private static void SeatHairUnderHat(Transform head)
    {
        var hat=head.Find("Wide brim hat");
        foreach(var part in head.GetComponentsInChildren<MeshFilter>())
        {
            if(part.transform.name!="Continuous sculpted hair" && !part.transform.IsChildOf(head.Find("Layered hair locks")))continue;
            var mesh=part.sharedMesh;var vertices=mesh.vertices;
            for(int i=0;i<vertices.Length;i++)
            {
                var p=hat.InverseTransformPoint(part.transform.TransformPoint(vertices[i]));
                float radius=Mathf.Sqrt(p.x*p.x/(.34f*.34f)+p.z*p.z/(.29f*.29f));
                if(radius<=.96f)continue;
                float underside=.402f-.035f*Mathf.Max(0,p.z/.56f);
                if(p.y<=underside)continue;
                p.y=Mathf.Lerp(p.y,underside,Mathf.SmoothStep(0,1,(radius-.96f)/.05f));
                vertices[i]=part.transform.InverseTransformPoint(hat.TransformPoint(p));
            }
            mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateBounds();
        }
    }
    private static void HairStyle(ArtMesh art,Transform head)
    {
        HairSurface.Build(art,head);
        var baseMesh=head.Find("Continuous sculpted hair").GetComponent<MeshFilter>().sharedMesh;
        var baseColors=baseMesh.colors;for(int i=0;i<baseColors.Length;i++)baseColors[i]=Hair*.82f;baseMesh.colors=baseColors;
        var root=art.Group("Layered hair locks",head,Vector3.zero);
        for(int i=0;i<12;i++)
        {
            float angle=.90f+i*(Mathf.PI*2-1.8f)/11;
            var radial=new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle));var tangent=new Vector3(Mathf.Cos(angle),0,-Mathf.Sin(angle));
            float variation=.012f*Mathf.Sin(i*2.4f);
            HairLocks.Lock(art,root,"Upper hair layer",new Vector3(.02f,.42f,-.04f),radial*.30f+Vector3.up*.40f,radial*.36f+Vector3.up*(.13f+variation)+tangent*.05f,.084f,.028f,i,Hair,radial*.39f+Vector3.up*.24f+tangent*.09f);
            HairLocks.Lock(art,root,"Swept side hair",radial*.22f+Vector3.up*.29f,radial*.42f+Vector3.up*.25f,radial*.36f+Vector3.up*(.018f+variation)+tangent*.085f,.076f,.027f,i,Hair,radial*.43f+Vector3.up*.055f+tangent*.13f);
            HairLocks.Lock(art,root,"Fine side tip",radial*.28f+Vector3.up*.13f,radial*.40f+Vector3.up*.10f,radial*.34f-Vector3.up*(.055f-variation)+tangent*.07f,.046f,.020f,i+1,Hair,radial*.415f-Vector3.up*.015f+tangent*.115f);
        }
        for(int side=-1;side<=1;side+=2)for(int i=0;i<4;i++)
        {
            float offset=i*.035f,asymmetry=side<0?.012f:0;
            HairLocks.Lock(art,root,"Soft parted fringe",new Vector3(-.045f+side*offset,.40f-i*.01f,.06f),new Vector3(side*(.17f+offset),.34f-i*.030f,.35f),new Vector3(side*(.275f+offset*.6f),.14f-i*.028f+asymmetry,.265f),.058f-i*.004f,.024f,i,Hair,new Vector3(side*(.34f+offset*.4f),.20f-i*.023f,.335f));
        }
        HairLocks.Lock(art,root,"Loose central fringe",new Vector3(-.09f,.405f,.10f),new Vector3(.055f,.345f,.365f),new Vector3(-.03f,.175f,.325f),.065f,.023f,2,Hair,new Vector3(.09f,.24f,.375f));
    }
}
