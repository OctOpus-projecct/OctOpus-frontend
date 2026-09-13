using UnityEngine;

// Deterministic editor-built village scenery. Geometry has no gameplay components.
public static class EnvironmentModels
{
    private static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
    private static Color Shade(Color color, float value) => new Color(color.r * value, color.g * value, color.b * value);

    public static ArtMesh Tree()
    {
        var a = new ArtMesh("Tree"); var p = a.Root.transform;
        a.Cylinder("TaperedTrunk", p, V(0, .86f, 0), V(.68f, 1.72f, .64f), VillageColors.Wood, .64f, 9);
        for (int i = 0; i < 7; i++)
        {
            float angle=i*Mathf.PI*2/7;
            var outward=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
            SculptedParts.Curve(a,"Flowing root "+i,p,outward*.12f+Vector3.up*.66f,outward*.22f+Vector3.up*.10f,outward*.65f+Vector3.up*.025f,.16f,.025f,VillageColors.Wood);
        }
        a.Beam("LeftBranch", p, V(-.06f, .92f, 0), V(-.65f, 2.14f, .02f), .29f, VillageColors.Wood);
        a.Beam("RightBranch", p, V(.05f, 1.05f, 0), V(.65f, 2.27f, .12f), .26f, VillageColors.Wood);
        a.Beam("BackBranch", p, V(0, 1.15f, .03f), V(.05f, 2.24f, .62f), .23f, VillageColors.Tan);
        var canopy = a.Group("Canopy", p, Vector3.zero);
        a.Ball("Crown", canopy, V(-.08f, 2.95f, .08f), V(1.43f, 1.3f, 1.38f), VillageColors.LightLeaf, true);
        a.Ball("LeftCrown", canopy, V(-.71f, 2.43f, .04f), V(1.28f, 1.37f, 1.23f), Shade(VillageColors.Leaf,.88f), true);
        a.Ball("RightCrown", canopy, V(.68f, 2.5f, .1f), V(1.22f, 1.23f, 1.28f), VillageColors.Leaf, true);
        a.Ball("FrontCrown", canopy, V(-.23f, 2.23f, -.54f), V(1.25f, 1.24f, 1.17f), VillageColors.LightLeaf, true);
        a.Ball("LowRightCrown", canopy, V(.57f, 2.04f, .4f), V(1.18f, 1.06f, 1.23f), Shade(VillageColors.Leaf,.88f), true);
        a.Ball("RearCrown", canopy, V(-.37f, 2.41f, .66f), V(1.27f, 1.19f, 1.13f), VillageColors.Leaf, true);
        Grass(a, p, V(-.47f, 0, -.28f), .7f);
        Grass(a, p, V(.43f, 0, .12f), .55f);
        return a;
    }

    public static ArtMesh Stump()
    {
        var a = new ArtMesh("Stump"); var p = a.Root.transform;
        a.Cylinder("Bark", p, V(0, .165f, 0), V(.7f, .33f, .7f), VillageColors.Wood, .84f, 11);
        for(int i=0;i<7;i++)
        {
            float angle=i*Mathf.PI*2/7;
            var outward=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
            SculptedParts.Curve(a,"Stump root "+i,p,outward*.20f+Vector3.up*.25f,outward*.29f+Vector3.up*.08f,outward*.49f+Vector3.up*.018f,.11f,.018f,VillageColors.Wood);
        }
        Rings(a, p, V(0, .337f, 0), .56f, Quaternion.identity);
        Grass(a, p, V(.35f, 0, .23f), .42f);
        return a;
    }

    public static ArtMesh Wood()
    {
        var a = new ArtMesh("Wood"); var p = a.Root.transform;
        Log(a, p, V(-.115f, .105f, 0), .9f, .21f, 0);
        Log(a, p, V(.115f, .105f, .03f), .9f, .21f, 1);
        Log(a, p, V(0, .28f, -.015f), .9f, .21f, 2);
        return a;
    }

    public static ArtMesh Cottage()
    {
        var a = new ArtMesh("Cottage"); var p = a.Root.transform;
        a.Box("StoneFoundation", p, V(0, .16f, 0), V(3.1f, .32f, 2.42f), VillageColors.Stone);
        a.Box("CreamPlaster", p, V(0, 1.2f, 0), V(2.94f, 1.9f, 2.26f), VillageColors.Cream);
        a.Extrude("PlasterGable", p, V(0, 2.11f, 0), new[] {new Vector2(-1.46f, 0), new Vector2(1.46f, 0), new Vector2(0, 1.04f)}, 2.22f, VillageColors.Cream);
        for (int x = -1; x <= 1; x += 2)
        for (int z = -1; z <= 1; z += 2)
            a.Box("CornerPost", p, V(x * 1.43f, 1.25f, z * 1.08f), V(.19f, 2.12f, .19f), VillageColors.Wood);
        a.Box("FrontCrossbeam", p, V(0, 2.05f, -1.17f), V(2.94f, .18f, .18f), VillageColors.Tan);
        a.Box("GablePost", p, V(0, 2.57f, -1.17f), V(.19f, .97f, .18f), VillageColors.Tan);
        Roof(a, p, 1.7f, 2.14f, 3.3f, 2.8f);
        var arch = new[] {new Vector2(-.42f, 0), new Vector2(.42f, 0), new Vector2(.42f, .88f), new Vector2(.31f, 1.1f), new Vector2(0, 1.24f), new Vector2(-.31f, 1.1f), new Vector2(-.42f, .88f)};
        a.Extrude("DoorFrame", p, V(-.52f, .28f, -1.23f), arch, .15f, VillageColors.Wood);
        var door = a.Extrude("ArchedDoor", p, V(-.52f, .3f, -1.325f), arch, .055f, VillageColors.Tan);
        door.transform.localScale = V(.83f, .92f, 1);
        for (int i = -1; i <= 1; i++) a.Box("DoorPlankGroove", p, V(-.52f + i * .18f, .76f, -1.359f), V(.017f, .84f, .013f), VillageColors.Wood);
        a.Ball("DoorHandle", p, V(-.28f, .77f, -1.4f), V(.075f, .075f, .065f), VillageColors.Steel);
        a.Box("DoorStep", p, V(-.52f, .085f, -1.47f), V(1.03f, .17f, .54f), Shade(VillageColors.Stone, 1.12f));
        Window(a, p, V(.77f, 1.22f, -1.24f), false);
        Window(a, p, V(1.52f, 1.24f, .18f), true);
        a.Box("FlowerBox", p, V(.77f, .82f, -1.42f), V(.74f, .2f, .29f), VillageColors.Tan);
        for (int i = 0; i < 3; i++) Flower(a, p, V(.54f + .22f * i, .9f, -1.43f), .52f, i == 1);
        a.Box("Chimney", p, V(.99f, 2.97f, .68f), V(.48f, 1.12f, .48f), VillageColors.Stone);
        for (int row = 0; row < 4; row++)
        {
            a.Box("ChimneyMortar", p, V(.99f, 2.68f + row * .21f, .431f), V(.47f, .025f, .012f), Shade(VillageColors.Stone, .75f));
            a.Box("ChimneyJoint", p, V(.9f + (row % 2) * .17f, 2.77f + row * .21f, .43f), V(.021f, .17f, .012f), Shade(VillageColors.Stone, .75f));
        }
        a.Box("ChimneyCap", p, V(.99f, 3.5f, .68f), V(.61f, .2f, .61f), Shade(VillageColors.Stone, 1.12f));
        Grass(a, p, V(1.4f, 0, -.95f), .75f);
        return a;
    }

    public static ArtMesh Workshop()
    {
        var a = new ArtMesh("Workshop"); var p = a.Root.transform;
        a.Box("Floor", p, V(0, .065f, 0), V(3.12f, .13f, 2.5f), Shade(VillageColors.Tan, .8f));
        a.Box("BackWall", p, V(0, 1.05f, 1.08f), V(2.93f, 1.95f, .14f), VillageColors.Tan);
        for (int i = 0; i < 8; i++) a.Box("BackPlankJoint", p, V(-1.28f + i * .36f, 1.05f, .999f), V(.023f, 1.91f, .018f), VillageColors.Wood);
        for (int x = -1; x <= 1; x += 2)
        for (int z = -1; z <= 1; z += 2)
        {
            a.Box("TimberPost", p, V(x * 1.4f, 1.12f, z * 1.04f), V(.2f, 2.22f, .2f), VillageColors.Wood);
            a.Beam("DiagonalBrace", p, V(x * 1.4f, 1.58f, z * 1.04f), V(x * .92f, 2.11f, z * 1.04f), .14f, VillageColors.Tan);
        }
        a.Box("FrontHeader", p, V(0, 2.11f, -1.04f), V(2.98f, .2f, .2f), VillageColors.Wood);
        Roof(a, p, 1.75f, 2.18f, 3.17f, 2.85f);
        a.Beam("GableKingPost", p, V(0, 2.14f, -1.05f), V(0, 3.05f, -1.05f), .15f, VillageColors.Tan);
        a.Box("WorkbenchTop", p, V(.35f, .91f, .65f), V(1.8f, .16f, .65f), VillageColors.Wood);
        for (int x = -1; x <= 1; x += 2) a.Box("BenchLeg", p, V(.35f + x * .7f, .46f, .62f), V(.14f, .85f, .49f), VillageColors.Tan);
        a.Box("BenchShelf", p, V(.35f, .28f, .65f), V(1.64f, .1f, .55f), VillageColors.Wood);
        a.Box("ToolRack", p, V(.45f, 1.68f, .93f), V(1.49f, .13f, .12f), VillageColors.Wood);
        for (int i = 0; i < 3; i++)
        {
            float x = -.02f + i * .43f;
            a.Beam("ToolHandle", p, V(x, 1.19f, .83f), V(x, 1.71f, .83f), .07f, VillageColors.Wood);
            a.Box(i == 1 ? "MalletHead" : "SteelToolHead", p, V(x, 1.62f, .8f), V(.26f, .14f, .13f), i == 1 ? VillageColors.Tan : VillageColors.Steel);
        }
        for (int i = 0; i < 4; i++) Log(a, p, V(-1.02f + (i % 2) * .24f, .24f + (i / 2) * .21f, .55f), .74f, .22f, i);
        a.Box("Crate", p, V(.98f, .29f, -.45f), V(.55f, .46f, .47f), VillageColors.Tan);
        for (int i = 0; i < 2; i++) a.Box("CrateBand", p, V(.98f, .16f + i * .25f, -.695f), V(.57f, .06f, .025f), VillageColors.Wood);
        Grass(a, p, V(-1.48f, 0, -.87f), .8f);
        return a;
    }

    public static ArtMesh Lantern()
    {
        var a = new ArtMesh("Lantern"); var p = a.Root.transform;
        a.Box("Post", p, V(.2f, 1.1f, 0), V(.2f, 2.2f, .22f), VillageColors.Tan);
        a.Box("PostCap", p, V(.2f, 2.13f, 0), V(.26f, .14f, .27f), VillageColors.Wood);
        a.Beam("HangingArm", p, V(.24f, 2.0f, 0), V(-.55f, 2.0f, 0), .14f, VillageColors.Wood);
        a.Beam("ArmBrace", p, V(.2f, 1.65f, 0), V(-.14f, 2.0f, 0), .095f, VillageColors.Gold);
        a.Beam("Hanger", p, V(-.46f, 1.97f, 0), V(-.46f, 1.8f, 0), .045f, Shade(VillageColors.Steel, .6f));
        a.Cylinder("LanternRoof", p, V(-.46f, 1.74f, 0), V(.45f, .17f, .45f), Shade(VillageColors.Steel, .65f), .45f, 6);
        a.Cylinder("LanternBase", p, V(-.46f, 1.16f, 0), V(.37f, .085f, .37f), Shade(VillageColors.Steel, .65f), 1, 6);
        for (int i = 0; i < 4; i++)
        {
            float x = -.46f + (i % 2 == 0 ? -.135f : .135f); float z = i < 2 ? -.135f : .135f;
            a.Beam("MetalFrame", p, V(x, 1.18f, z), V(x, 1.67f, z), .025f, Shade(VillageColors.Steel, .7f));
        }
        a.Cylinder("CrystalUpper", p, V(-.46f, 1.51f, 0), V(.22f, .25f, .22f), VillageColors.Glow, .05f, 5);
        var lower = a.Cylinder("CrystalLower", p, V(-.46f, 1.30f, 0), V(.22f, .17f, .22f), Shade(VillageColors.Glow, .9f), .05f, 5);
        lower.transform.localRotation = Quaternion.Euler(180, 0, 0);
        Grass(a, p, V(.27f, 0, -.13f), 1);
        a.Ball("FootStone", p, V(-.14f, .1f, .08f), V(.29f, .2f, .24f), VillageColors.Stone, true);
        return a;
    }

    public static ArtMesh Signpost()
    {
        var a = new ArtMesh("Signpost"); var p = a.Root.transform;
        a.Box("Post", p, V(0, .6f, .03f), V(.15f, 1.2f, .16f), VillageColors.Wood);
        var arrow = new[] { new Vector2(-.5f, -.105f), new Vector2(.36f, -.105f), new Vector2(.5f, 0), new Vector2(.36f, .105f), new Vector2(-.5f, .105f) };
        a.Extrude("UpperArrow", p, V(.02f, .98f, -.1f), arrow, .11f, VillageColors.Tan);
        var lower = a.Extrude("LowerArrow", p, V(-.09f, .64f, -.105f), arrow, .11f, VillageColors.Gold);
        lower.transform.localRotation = Quaternion.Euler(0, 0, 180);
        for (int i = 0; i < 2; i++) a.Ball("IronPeg", p, V(0, .64f + i * .34f, -.17f), V(.05f, .05f, .04f), Shade(VillageColors.Steel, .6f));
        Grass(a, p, V(.17f, 0, .05f), .65f);
        return a;
    }

    public static ArtMesh Details()
    {
        var a = new ArtMesh("Details"); var p = a.Root.transform;
        a.Ball("LargeFacetedRock", p, V(-.2f, .23f, .08f), V(.51f, .46f, .47f), VillageColors.Stone, true);
        a.Ball("SmallFacetedRock", p, V(-.42f, .09f, -.1f), V(.24f, .18f, .25f), Shade(VillageColors.Stone, 1.12f), true);
        a.Ball("Pebble", p, V(.01f, .055f, -.23f), V(.19f, .11f, .15f), Shade(VillageColors.Stone, .92f), true);
        Grass(a, p, V(.3f, 0, .13f), .85f);
        Grass(a, p, V(.05f, 0, -.19f), .5f);
        Flower(a, p, V(.12f, 0, -.12f), .8f, false);
        Flower(a, p, V(.34f, 0, -.08f), .63f, true);
        Flower(a, p, V(.22f, 0, .07f), .94f, false);
        return a;
    }

    private static void Roof(ArtMesh a, Transform p, float halfWidth, float eave, float peak, float depth)
    {
        float roofRise = peak - eave;
        a.Extrude("LeftRoofDeck", p, V(0, eave, 0), new[] {new Vector2(-halfWidth, -.12f), new Vector2(0, roofRise - .12f), new Vector2(0, roofRise), new Vector2(-halfWidth, 0)}, depth, VillageColors.DarkGreen);
        a.Extrude("RightRoofDeck", p, V(0, eave, 0), new[] {new Vector2(0, roofRise - .12f), new Vector2(halfWidth, -.12f), new Vector2(halfWidth, 0), new Vector2(0, roofRise)}, depth, VillageColors.DarkGreen);
        float rise = peak - eave; float length = Mathf.Sqrt(halfWidth * halfWidth + rise * rise);
        for (int side = -1; side <= 1; side += 2)
        {
            a.Beam("FrontRoofTrim", p, V(side * halfWidth, eave, -depth * .51f), V(0, peak, -depth * .51f), .16f, VillageColors.Tan);
            for (int row = 0; row < 4; row++)
            for (int col = 0; col < 5; col++)
            {
                float t = (row + .5f) / 4;
                var tile = a.Box("SageRoofTile", p, V(side * halfWidth * t, peak - rise * t + .055f, (col - 2) * depth / 5), V(length / 4 + .045f, .11f, depth / 5 - .025f), Shade(VillageColors.Sage, .94f + ((row * 3 + col + (side + 1)) % 4) * .045f));
                tile.transform.localRotation = Quaternion.Euler(0, 0, -side * Mathf.Atan2(rise, halfWidth) * Mathf.Rad2Deg);
            }
        }
        a.Box("RidgeCap", p, V(0, peak + .045f, 0), V(.19f, .15f, depth + .04f), VillageColors.Sage);
    }

    private static void Window(ArtMesh a, Transform p, Vector3 position, bool side)
    {
        var g = a.Group("WarmWindow", p, position);
        if (side) g.localRotation = Quaternion.Euler(0, -90, 0);
        a.Box("DarkRecess", g, Vector3.zero, V(.62f, .76f, .1f), VillageColors.Wood);
        a.Box("WarmGlass", g, V(0, 0, -.06f), V(.46f, .6f, .035f), new Color(1, .73f, .32f));
        a.Box("VerticalMullion", g, V(0, 0, -.09f), V(.055f, .65f, .05f), VillageColors.Tan);
        a.Box("HorizontalMullion", g, V(0, .02f, -.09f), V(.5f, .055f, .05f), VillageColors.Tan);
        a.Box("Sill", g, V(0, -.39f, -.05f), V(.71f, .085f, .2f), VillageColors.Tan);
    }

    private static void Rings(ArtMesh a, Transform p, Vector3 position, float diameter, Quaternion rotation)
    {
        var g = a.Group("CutGrowthRings", p, position); g.localRotation = rotation;
        for (int i = 0; i < 6; i++)
            a.Cylinder("GrowthRing" + i, g, V(0, i * .0015f, 0), V(diameter * (1 - i * .15f), .004f, diameter * (1 - i * .15f)), i % 2 == 0 ? new Color(.72f,.51f,.30f) : new Color(.61f,.39f,.21f), 1, 16);
    }

    private static void Log(ArtMesh a, Transform p, Vector3 position, float length, float diameter, int index)
    {
        var g = a.Group("Log" + index, p, position);
        var bark = a.Cylinder("FacetedBark", g, Vector3.zero, V(diameter, length, diameter), Shade(VillageColors.Wood, 1 + index * .045f), .98f, 9);
        bark.transform.localRotation = Quaternion.Euler(90, 0, 0);
        Rings(a, g, V(0, 0, -length / 2 - .003f), diameter * .86f, Quaternion.Euler(-90, 0, 0));
        Rings(a, g, V(0, 0, length / 2 + .003f), diameter * .86f, Quaternion.Euler(90, 0, 0));
    }

    private static void Grass(ArtMesh a, Transform p, Vector3 position, float size)
    {
        var g = a.Group("GrassTuft", p, position);
        for (int i = 0; i < 5; i++)
        {
            float angle = i * 137.5f * Mathf.Deg2Rad;
            float height = (.25f + .045f * (i % 3)) * size;
            var leaf = a.Ball("LeafBlade", g, V(Mathf.Cos(angle) * .06f * size, height * .56f, Mathf.Sin(angle) * .06f * size), V(.1f * size, height, .065f * size), i % 2 == 0 ? VillageColors.Leaf : VillageColors.LightLeaf);
            leaf.transform.localRotation = Quaternion.Euler(Mathf.Sin(angle) * 25, 0, Mathf.Cos(angle) * 25);
        }
    }

    private static void Flower(ArtMesh a, Transform p, Vector3 position, float size, bool yellow)
    {
        var g = a.Group("Wildflower", p, position);
        a.Beam("Stem", g, Vector3.zero, V(0, .33f * size, 0), .018f * size, VillageColors.DarkLeaf);
        for (int i = 0; i < 5; i++)
        {
            float angle = i * Mathf.PI * 2 / 5;
            a.Ball("Petal", g, V(Mathf.Cos(angle) * .055f * size, (.33f + Mathf.Sin(angle) * .055f) * size, -.012f), V(.073f * size, .073f * size, .04f * size), yellow ? new Color(.98f, .73f, .23f) : new Color(.98f, .96f, .85f));
        }
        a.Ball("GoldenCenter", g, V(0, .33f * size, -.037f), V(.053f * size, .053f * size, .04f * size), VillageColors.Gold);
    }
}
