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
            var hand=b.Group("Hand",arm,new Vector3(0,-.47f,.015f));
            CharacterSculpt.Hand(b,hand,!villager && s==1,s);
            if(!villager && s==1)
            {
                var tool=b.Group("HeldAxe",hand,CharacterSculpt.GripCenter-CharacterSculpt.ToolRotation*(Vector3.up*CharacterSculpt.ToolGripY*CharacterSculpt.ToolScale));
                tool.localRotation=CharacterSculpt.ToolRotation;tool.localScale=Vector3.one*CharacterSculpt.ToolScale;MakeAxe(b,tool);
                var grip=b.Group("GripAxis",hand,CharacterSculpt.GripCenter);grip.localRotation=CharacterSculpt.ToolRotation;
            }
        }
        var head=b.Group("Head",body,new Vector3(0,1.60f,0));head.localScale=Vector3.one*1.18f;
        CharacterSculpt.Face(b,head);
        foreach(int s in new[]{-1,1})
        {
            b.Ball("Eye",head,new Vector3(s*.125f,.005f,.296f),new Vector3(.072f,.112f,.027f),new Color(.12f,.095f,.055f));
            b.Ball("Eye highlight",head,new Vector3(s*.125f-.012f,.032f,.311f),Vector3.one*.017f,VillageColors.Cream);
            var brow=b.Ball("Eyebrow",head,new Vector3(s*.125f,.113f,.282f),new Vector3(.090f,.026f,.017f),VillageColors.Hair);
            brow.transform.localRotation=Quaternion.Euler(0,0,-s*5);
        }
        SculptedParts.Curve(b,"Gentle smile",head,new Vector3(-.052f,-.135f,.286f),new Vector3(0,-.167f,.289f),new Vector3(.052f,-.135f,.286f),.004f,.004f,new Color(.35f,.19f,.11f));
        CharacterSculpt.Hair(b,head);
        if(villager)
        {
            CharacterSurface.Form(b,"Draped leather apron",body,new[]{new Vector4(.55f,.37f,.233f,.006f),new Vector4(.60f,.40f,.245f,.006f),new Vector4(.75f,.38f,.246f,.006f),new Vector4(.9f,.36f,.25f,.006f),new Vector4(1.05f,.30f,.248f,.006f),new Vector4(1.14f,.25f,.21f,.006f)},new Color(.40f,.29f,.16f),.95f);
            CharacterSurface.Form(b,"Apron pocket",body,new[]{new Vector4(.91f,.34f,.26f,.014f),new Vector4(.99f,.34f,.26f,.014f),new Vector4(1.055f,.34f,.26f,.014f)},VillageColors.Tan,.32f);
            foreach(int sign in new[]{-1,1})
                SculptedParts.Curve(b,"Apron shoulder strap",body,new Vector3(sign*.21f,1.08f,.215f),new Vector3(sign*.23f,1.32f,.14f),new Vector3(sign*.23f,1.15f,-.17f),.028f,.028f,VillageColors.Tan);
            CharacterSurface.Form(b,"Sculpted beard",head,new[]{new Vector4(-.40f,.025f,.045f,.12f),new Vector4(-.35f,.18f,.16f,.12f),new Vector4(-.26f,.29f,.205f,.09f),new Vector4(-.18f,.30f,.18f,.085f)},new Color(.29f,.21f,.15f));
            foreach(int s in new[]{-1,1})
                SculptedParts.Curve(b,"Tapered moustache",head,new Vector3(s*.018f,-.096f,.304f),new Vector3(s*.10f,-.085f,.310f),new Vector3(s*.155f,-.132f,.265f),.019f,.005f,new Color(.34f,.25f,.17f),20,.009f);
        }
        else MakeBackpack(b,body,new Vector3(0,1.0f,-.30f));
        HairLocks.Build(b,head);
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
        for(int i=0;i<5;i++){var wrap=b.Cylinder("Green grip wrap",root,new Vector3(0,.045f+i*.025f,0),new Vector3(.079f,.034f,.079f),VillageColors.Sage,1);wrap.transform.localRotation=Quaternion.Euler(0,0,i%2==0?8:-8);}
        b.Extrude("Forged axe blade",root,new Vector3(0,.60f,0),new[]{new Vector2(-.07f,-.075f),new Vector2(.24f,-.19f),new Vector2(.29f,.12f),new Vector2(.04f,.16f),new Vector2(-.07f,.085f)},.065f,VillageColors.Steel);
        b.Extrude("Cutting edge",root,new Vector3(0,.60f,0),new[]{new Vector2(.24f,-.19f),new Vector2(.275f,-.18f),new Vector2(.32f,.12f),new Vector2(.29f,.12f)},.068f,new Color(.72f,.77f,.73f));
    }
}
