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
        b.SoftBox("Tunic",body,new Vector3(0,.91f,0),new Vector3(.67f*width,.76f,.40f),villager?new Color(.58f,.27f,.15f):VillageColors.Sage);
        b.SoftBox("Tunic hem",body,new Vector3(0,.65f,0),new Vector3(.63f*width,.18f,.38f),villager?VillageColors.Tan:VillageColors.Sage);
        b.SoftBox("Leather belt",body,new Vector3(0,.77f,.01f),new Vector3(.66f*width,.092f,.41f),VillageColors.Wood);
        b.SoftBox("Buckle gold",body,new Vector3(0,.77f,.229f),new Vector3(.14f,.12f,.035f),VillageColors.Gold);
        b.SoftBox("Buckle inset",body,new Vector3(0,.77f,.252f),new Vector3(.087f,.066f,.015f),VillageColors.Wood);
        b.Cylinder("Neck",body,new Vector3(0,1.30f,0),new Vector3(.19f,.20f,.19f),VillageColors.Skin,1);
        // Cream collar flaps and tiny wooden buttons provide an authored silhouette from above.
        for(int s=-1;s<=1;s+=2)
        {
            var collar=b.Ball("Collar",body,new Vector3(s*.088f,1.205f,.17f),new Vector3(.13f,.20f,.04f),VillageColors.Cream);
            collar.transform.localRotation=Quaternion.Euler(0,0,s*25);
        }
        for(int i=0;i<3;i++)b.Ball("Tunic button",body,new Vector3(0,1.13f-i*.1f,.205f),Vector3.one*.035f,VillageColors.Gold);
        foreach(int s in new[]{-1,1})
        {
            var leg=b.Group(s<0?"LegL":"LegR",body,new Vector3(s*.18f,.61f,0));
            b.SoftBox("Trousers",leg,new Vector3(0,-.19f,0),new Vector3(.25f,.43f,.26f),VillageColors.DarkGreen);
            b.SoftBox("Boot cuff",leg,new Vector3(0,-.34f,0),new Vector3(.29f,.13f,.30f),VillageColors.Wood);
            b.SoftBox("Boot",leg,new Vector3(0,-.49f,.065f),new Vector3(.30f,.27f,.43f),VillageColors.Wood);
            b.SoftBox("Sole",leg,new Vector3(0,-.588f,.065f),new Vector3(.29f,.035f,.40f),new Color(.22f,.15f,.10f));
            var arm=b.Group(s<0?"ArmL":"ArmR",body,new Vector3(s*.36f*width,1.16f,0));
            arm.localRotation=Quaternion.Euler(0,0,s*11);
            b.Ball("Cream sleeve",arm,new Vector3(0,-.185f,0),new Vector3(.24f,.40f,.27f),VillageColors.Cream);
            b.Cylinder("Sleeve cuff",arm,new Vector3(0,-.34f,0),new Vector3(.25f,.10f,.25f),new Color(.98f,.91f,.74f),1);
            b.Ball("Forearm",arm,new Vector3(0,-.415f,0),new Vector3(.19f,.20f,.20f),VillageColors.Skin);
            var hand=b.Group("Hand",arm,new Vector3(0,-.49f,.01f));
            b.Ball("Palm",hand,Vector3.zero,new Vector3(.21f,.22f,.19f),VillageColors.Skin);
            b.Ball("Thumb",hand,new Vector3(-s*.08f,.005f,.055f),new Vector3(.09f,.13f,.1f),VillageColors.Skin);
            if(!villager && s==1)
            {
                var tool=b.Group("HeldAxe",hand,new Vector3(.04f,-.08f,.09f));
                tool.localRotation=Quaternion.Euler(-16,0,-7);tool.localScale=Vector3.one*.78f;MakeAxe(b,tool);
            }
        }
        var head=b.Group("Head",body,new Vector3(0,1.62f,0));head.localScale=Vector3.one*1.10f;
        b.Ball("Face",head,Vector3.zero,new Vector3(.79f,.73f,.70f),VillageColors.Skin);
        foreach(int s in new[]{-1,1})
        {
            b.Ball("Ear",head,new Vector3(s*.375f,-.02f,0),new Vector3(.145f,.21f,.13f),VillageColors.Skin);
            b.Ball("Ear inner",head,new Vector3(s*.407f,-.025f,.043f),new Vector3(.075f,.12f,.035f),new Color(.82f,.48f,.33f));
            b.Ball("Eye",head,new Vector3(s*.145f,.015f,.331f),new Vector3(.075f,.102f,.03f),new Color(.12f,.095f,.055f));
            b.Ball("Eye highlight",head,new Vector3(s*.145f-.013f,.038f,.348f),Vector3.one*.017f,VillageColors.Cream);
            var brow=b.Ball("Eyebrow",head,new Vector3(s*.146f,.118f,.326f),new Vector3(.117f,.038f,.025f),VillageColors.Hair);
            brow.transform.localRotation=Quaternion.Euler(0,0,-s*8);
            b.Ball("Cheek",head,new Vector3(s*.233f,-.087f,.296f),new Vector3(.097f,.048f,.014f),new Color(.91f,.48f,.34f));
        }
        b.Ball("Nose",head,new Vector3(0,-.065f,.365f),new Vector3(.10f,.075f,.07f),VillageColors.Skin);
        for(int i=0;i<7;i++)
        {
            float x=(i-3)*.016f;
            b.Ball("Smile",head,new Vector3(x,-.166f+Mathf.Abs(x)*.45f,.306f),Vector3.one*.018f,new Color(.35f,.19f,.11f));
        }
        b.Ball("Hair cap",head,new Vector3(0,.17f,-.08f),new Vector3(.82f,.64f,.74f),VillageColors.Hair);
        for(int i=0;i<8;i++)
        {
            float a=i*Mathf.PI*2/8;
            var start=new Vector3(.04f,.42f,-.08f);
            var mid=new Vector3(Mathf.Sin(a)*.39f,.37f,Mathf.Cos(a)*.35f-.07f);
            var tip=new Vector3(Mathf.Sin(a)*.36f,.035f,Mathf.Cos(a)*.32f-.07f);
            SculptedParts.Curve(b,"Swept hair",head,start,mid,tip,.115f,.035f,new Color(.29f+(i%2)*.018f,.17f,.10f));
        }
        for(int i=0;i<3;i++)
            SculptedParts.Curve(b,"Soft fringe",head,new Vector3(.13f+i*.07f,.37f,.20f),new Vector3(-.20f+i*.16f,.34f,.43f),new Vector3(-.27f+i*.18f,.10f,.32f),.115f,.025f,new Color(.32f,.19f,.11f));
        if(villager)
        {
            b.SoftBox("Work apron",body,new Vector3(0,.92f,.218f),new Vector3(.54f,.71f,.065f),new Color(.37f,.27f,.14f));
            b.SoftBox("Apron pocket",body,new Vector3(0,1.03f,.263f),new Vector3(.22f,.16f,.025f),VillageColors.Tan);
            foreach(int s in new[]{-1,1})b.Beam("Apron shoulder strap",body,new Vector3(s*.21f,1.27f,.13f),new Vector3(s*.21f,1.10f,.26f),.05f,VillageColors.Tan);
            b.Ball("Rounded beard",head,new Vector3(0,-.245f,.16f),new Vector3(.64f,.35f,.43f),new Color(.28f,.20f,.145f));
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
