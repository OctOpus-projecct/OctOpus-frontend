using UnityEngine;

public static class CharacterModels
{
    public static ArtMesh Player()=>Person(false);
    public static ArtMesh Villager()=>Person(true);
    public static ArtMesh Axe(){var b=new ArtMesh("Axe");MakeAxe(b,b.Root.transform);return b;}
    public static ArtMesh Backpack(){var b=new ArtMesh("Backpack");MakeBackpack(b,b.Root.transform,Vector3.zero);return b;}
    private static ArtMesh Person(bool villager)
    {
        var b=new ArtMesh(villager?"Villager":"Player");var root=b.Root.transform;
        float width=villager?1.30f:1;
        var body=b.Group("Body",root,Vector3.zero);
        Color cloth=villager?new Color(.58f,.27f,.15f):VillageColors.Sage;
        CharacterSurface.Form(b,"Tailored tunic",body,new[]{
            new Vector4(.55f,.29f*width,.185f,0),new Vector4(.59f,.33f*width,.22f,0),
            new Vector4(.74f,.30f*width,.22f,0),new Vector4(.87f,.29f*width,.22f,0),
            new Vector4(1.03f,.32f*width,.235f,0),new Vector4(1.15f,.30f*width,.205f,0),
            new Vector4(1.23f,.19f*width,.15f,0),new Vector4(1.25f,.095f,.095f,0)},cloth);
        CharacterSurface.Form(b,"Fitted belt",body,new[]{new Vector4(.74f,.303f*width,.228f,0),new Vector4(.78f,.30f*width,.232f,0),new Vector4(.82f,.296f*width,.228f,0)},VillageColors.Wood);
        b.SoftBox("Buckle gold",body,new Vector3(0,.78f,.245f),new Vector3(.115f,.095f,.028f),VillageColors.Gold);
        b.SoftBox("Buckle inset",body,new Vector3(0,.78f,.263f),new Vector3(.074f,.056f,.009f),VillageColors.Wood);
        b.Ball("Neck",body,new Vector3(0,1.27f,0),new Vector3(.20f,.24f,.20f),VillageColors.Skin);
        for(int sign=-1;sign<=1;sign+=2)
        {
            var collar=b.Ball("Folded collar",body,new Vector3(sign*.09f,1.205f,.15f),new Vector3(.12f,.14f,.055f),VillageColors.Cream);
            collar.transform.localRotation=Quaternion.Euler(-25,0,sign*28);
        }
        for(int i=0;i<3;i++)b.Ball("Tunic button",body,new Vector3(0,1.10f-i*.105f,.232f),Vector3.one*.03f,VillageColors.Gold);
        foreach(int s in new[]{-1,1})
        {
            var leg=b.Group(s<0?"LegL":"LegR",body,new Vector3(s*.18f,.61f,0));
            CharacterSurface.Form(b,"Soft trousers",leg,new[]{new Vector4(-.38f,.09f,.09f,0),new Vector4(-.28f,.12f,.12f,0),new Vector4(-.12f,.125f,.125f,0),new Vector4(.04f,.13f,.13f,0)},VillageColors.DarkGreen);
            CharacterSurface.Form(b,"Round leather boot",leg,new[]{new Vector4(-.596f,.105f,.16f,.065f),new Vector4(-.565f,.145f,.205f,.07f),new Vector4(-.50f,.147f,.21f,.07f),new Vector4(-.44f,.13f,.16f,.035f),new Vector4(-.37f,.112f,.12f,0),new Vector4(-.265f,.128f,.128f,0)},VillageColors.Wood);
            CharacterSurface.Form(b,"Boot folded rim",leg,new[]{new Vector4(-.30f,.126f,.126f,0),new Vector4(-.272f,.14f,.14f,0),new Vector4(-.25f,.126f,.126f,0)},new Color(.34f,.215f,.125f));
            CharacterSurface.Form(b,"Leather sole",leg,new[]{new Vector4(-.609f,.10f,.15f,.07f),new Vector4(-.596f,.143f,.203f,.07f),new Vector4(-.579f,.145f,.205f,.07f)},new Color(.23f,.15f,.095f));
            var arm=b.Group(s<0?"ArmL":"ArmR",body,new Vector3(s*.29f*width,1.16f,0));
            arm.localRotation=Quaternion.Euler(0,0,s*14);
            CharacterSurface.Form(b,"Cloth sleeve",arm,new[]{new Vector4(-.30f,.092f,.10f,0),new Vector4(-.23f,.125f,.13f,0),new Vector4(-.10f,.14f,.145f,0),new Vector4(.015f,.10f,.11f,0),new Vector4(.05f,.035f,.045f,0)},villager?cloth:VillageColors.Cream);
            CharacterSurface.Form(b,"Rolled sleeve",arm,new[]{new Vector4(-.32f,.103f,.113f,0),new Vector4(-.28f,.133f,.14f,0),new Vector4(-.25f,.113f,.123f,0)},VillageColors.Cream);
            CharacterSurface.Form(b,"Rounded forearm",arm,new[]{new Vector4(-.47f,.074f,.08f,.012f),new Vector4(-.39f,.098f,.10f,.005f),new Vector4(-.285f,.093f,.095f,0)},VillageColors.Skin);
            var hand=b.Group("Hand",arm,new Vector3(0,-.47f,.015f));
            b.Ball("Palm",hand,new Vector3(0,-.035f,0),new Vector3(.20f,.23f,.17f),VillageColors.Skin);
            for(int finger=0;finger<3;finger++)b.Ball("Curled finger",hand,new Vector3((finger-1)*.048f,-.107f,.014f),new Vector3(.066f,.085f,.115f),VillageColors.Skin);
            b.Ball("Thumb",hand,new Vector3(-s*.075f,-.018f,.055f),new Vector3(.09f,.13f,.10f),VillageColors.Skin);
            if(!villager && s==1)
            {
                var tool=b.Group("HeldAxe",hand,new Vector3(.04f,-.08f,.09f));
                tool.localRotation=Quaternion.Euler(-16,0,-7);tool.localScale=Vector3.one*.78f;MakeAxe(b,tool);
            }
        }
        var head=b.Group("Head",body,new Vector3(0,1.60f,0));head.localScale=Vector3.one*1.18f;
        b.Ball("Face",head,Vector3.zero,new Vector3(.79f,.73f,.70f),VillageColors.Skin);
        foreach(int s in new[]{-1,1})
        {
            b.Ball("Ear",head,new Vector3(s*.375f,-.02f,0),new Vector3(.145f,.21f,.13f),VillageColors.Skin);
            b.Ball("Ear inner",head,new Vector3(s*.407f,-.025f,.043f),new Vector3(.075f,.12f,.035f),new Color(.82f,.48f,.33f));
            b.Ball("Eye",head,new Vector3(s*.145f,.015f,.331f),new Vector3(.075f,.102f,.03f),new Color(.12f,.095f,.055f));
            b.Ball("Eye highlight",head,new Vector3(s*.145f-.013f,.038f,.348f),Vector3.one*.017f,VillageColors.Cream);
            var brow=b.Ball("Eyebrow",head,new Vector3(s*.146f,.118f,.326f),new Vector3(.117f,.038f,.025f),VillageColors.Hair);
            brow.transform.localRotation=Quaternion.Euler(0,0,-s*8);
            b.Ball("Cheek",head,new Vector3(s*.233f,-.087f,.276f),new Vector3(.097f,.048f,.014f),new Color(.91f,.48f,.34f));
        }
        b.Ball("Nose",head,new Vector3(0,-.065f,.365f),new Vector3(.10f,.075f,.07f),VillageColors.Skin);
        SculptedParts.Curve(b,"Gentle smile",head,new Vector3(-.063f,-.145f,.326f),new Vector3(0,-.193f,.324f),new Vector3(.063f,-.145f,.326f),.0055f,.0055f,new Color(.35f,.19f,.11f));
        b.Ball("Hair cap",head,new Vector3(0,.17f,-.08f),new Vector3(.82f,.64f,.74f),VillageColors.Hair);
        for(int i=0;i<8;i++)
        {
            float a=i*Mathf.PI*2/8;
            var start=new Vector3(.04f,.42f,-.08f);
            var mid=new Vector3(Mathf.Sin(a)*.39f,.37f,Mathf.Cos(a)*.35f-.07f);
            var tip=new Vector3(Mathf.Sin(a)*.36f,.035f,Mathf.Cos(a)*.32f-.07f);
            SculptedParts.Curve(b,"Swept hair",head,start,mid,tip,.025f,.025f,new Color(.29f+(i%2)*.018f,.17f,.10f),20,.085f);
        }
        for(int i=0;i<5;i++)
        {
            float x=-.29f+i*.14f;
            SculptedParts.Curve(b,"Soft fringe",head,new Vector3(x+.07f,.33f,.21f),new Vector3(x+.035f,.33f,.355f),new Vector3(x-.035f,.10f+(i%2)*.065f,.30f),.022f,.008f,new Color(.31f+(i%2)*.02f,.18f,.105f),20,.070f);
        }
        SculptedParts.Curve(b,"Tousled crown",head,new Vector3(-.12f,.36f,-.09f),new Vector3(.03f,.53f,-.03f),new Vector3(.23f,.44f,.035f),.025f,.014f,new Color(.32f,.19f,.11f),20,.075f);
        if(villager)
        {
            CharacterSurface.Form(b,"Draped leather apron",body,new[]{new Vector4(.55f,.37f,.233f,.006f),new Vector4(.60f,.40f,.245f,.006f),new Vector4(.75f,.38f,.246f,.006f),new Vector4(.9f,.36f,.25f,.006f),new Vector4(1.05f,.30f,.248f,.006f),new Vector4(1.14f,.25f,.21f,.006f)},new Color(.40f,.29f,.16f),.95f);
            CharacterSurface.Form(b,"Apron pocket",body,new[]{new Vector4(.91f,.34f,.26f,.014f),new Vector4(.99f,.34f,.26f,.014f),new Vector4(1.055f,.34f,.26f,.014f)},VillageColors.Tan,.32f);
            foreach(int sign in new[]{-1,1})
                SculptedParts.Curve(b,"Apron shoulder strap",body,new Vector3(sign*.21f,1.08f,.215f),new Vector3(sign*.23f,1.32f,.14f),new Vector3(sign*.23f,1.15f,-.17f),.028f,.028f,VillageColors.Tan);
            CharacterSurface.Form(b,"Sculpted beard",head,new[]{new Vector4(-.40f,.025f,.045f,.12f),new Vector4(-.35f,.18f,.16f,.12f),new Vector4(-.26f,.29f,.205f,.09f),new Vector4(-.18f,.30f,.18f,.085f)},new Color(.29f,.21f,.15f));
            foreach(int s in new[]{-1,1})b.Ball("Moustache",head,new Vector3(s*.075f,-.096f,.382f),new Vector3(.19f,.09f,.065f),new Color(.34f,.25f,.17f));
        }
        else MakeBackpack(b,body,new Vector3(0,1.0f,-.30f));
        return b;
    }
    private static void MakeBackpack(ArtMesh b,Transform parent,Vector3 pos)
    {
        var root=b.Group("Travel pack",parent,pos);
        b.SoftBox("Leather bag",root,Vector3.zero,new Vector3(.47f,.51f,.25f),VillageColors.Tan);
        b.SoftBox("Folded flap",root,new Vector3(0,.145f,-.13f),new Vector3(.49f,.23f,.055f),new Color(.72f,.52f,.28f));
        b.SoftBox("Bag strap",root,new Vector3(0,-.03f,-.158f),new Vector3(.075f,.23f,.045f),VillageColors.Wood);
        b.SoftBox("Bag buckle",root,new Vector3(0,-.03f,-.185f),new Vector3(.115f,.085f,.025f),VillageColors.Gold);
        var roll=b.Cylinder("Rolled blanket",root,new Vector3(0,.335f,0),new Vector3(.16f,.57f,.16f),VillageColors.Sage,1);roll.transform.localRotation=Quaternion.Euler(0,0,90);
        foreach(int s in new[]{-1,1})
        {
            b.SoftBox("Blanket belt",root,new Vector3(s*.17f,.335f,0),new Vector3(.05f,.17f,.17f),VillageColors.Wood);
            SculptedParts.Curve(b,"Curved shoulder strap",parent,pos+new Vector3(s*.20f,.20f,.07f),pos+new Vector3(s*.36f,.42f,.55f),pos+new Vector3(s*.19f,-.19f,.09f),.032f,.032f,VillageColors.Tan);
        }
        b.Beam("Charm string",root,new Vector3(.25f,.1f,0),new Vector3(.28f,-.12f,0),.015f,VillageColors.Wood);
        b.Ball("Blue crystal charm",root,new Vector3(.28f,-.17f,0),new Vector3(.07f,.15f,.07f),VillageColors.Glow,true);
    }
    public static void MakeAxe(ArtMesh b,Transform root)
    {
        b.Cylinder("Wood handle",root,new Vector3(0,.27f,0),new Vector3(.065f,.76f,.065f),VillageColors.Wood,1);
        for(int i=0;i<5;i++){var wrap=b.Cylinder("Green grip wrap",root,new Vector3(0,.19f+i*.03f,0),new Vector3(.079f,.034f,.079f),VillageColors.Sage,1);wrap.transform.localRotation=Quaternion.Euler(0,0,i%2==0?8:-8);}
        b.Extrude("Forged axe blade",root,new Vector3(0,.60f,0),new[]{new Vector2(-.07f,-.075f),new Vector2(.24f,-.19f),new Vector2(.29f,.12f),new Vector2(.04f,.16f),new Vector2(-.07f,.085f)},.065f,VillageColors.Steel);
        b.Extrude("Cutting edge",root,new Vector3(0,.60f,0),new[]{new Vector2(.24f,-.19f),new Vector2(.275f,-.18f),new Vector2(.32f,.12f),new Vector2(.29f,.12f)},.068f,new Color(.72f,.77f,.73f));
    }
}
