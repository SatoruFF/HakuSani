using Godot;

namespace HakuSani;

public enum HomeLook { Night, Sunny, Past, Future }

/// <summary>The cats' room: bed cushion under a window, bowls, toy, shelf. Self-lit on the given layer.</summary>
public static class Home
{
    public static Node3D Build(Node parent, Vector3 origin, uint layers, HomeLook look, bool collide = true, bool frontWall = false)
    {
        var root = new Node3D { Position = origin, Name = "Home" };
        parent.AddChild(root);
        void Bx(Vector3 p, Vector3 s, Material m, bool col = false, float rotY = 0)
        {
            Node3D n = col && collide ? new StaticBody3D { CollisionLayer = L.PhysPast | L.PhysFuture, CollisionMask = 0 } : new Node3D();
            n.Position = p;
            n.RotationDegrees = new Vector3(0, rotY, 0);
            if (n is StaticBody3D) n.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = s } });
            n.AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = s }, MaterialOverride = m, Layers = layers });
            root.AddChild(n);
        }

        bool dusty = look == HomeLook.Future;
        var wall = dusty ? Mats.Flat(new Color(0.42f, 0.44f, 0.5f)) : Mats.Flat(new Color(0.55f, 0.6f, 0.72f));
        var floor = dusty ? Mats.DustFloor : Mats.Flat(new Color(0.55f, 0.38f, 0.26f), 0.6f);
        float W = 6, D = 5, H = 3;
        Bx(new Vector3(0, -0.1f, 0), new Vector3(W, 0.2f, D), floor, true);
        Bx(new Vector3(0, H / 2, -D / 2), new Vector3(W, H, 0.2f), wall, true);           // back wall (window)
        Bx(new Vector3(-W / 2, H / 2, 0), new Vector3(0.2f, H, D), wall, true);
        Bx(new Vector3(W / 2, H / 2, 0), new Vector3(0.2f, H, D), wall, true);
        Bx(new Vector3(0, H, 0), new Vector3(W, 0.2f, D), wall);
        if (frontWall)
        {
            Bx(new Vector3(-1.75f, H / 2, D / 2), new Vector3(2.5f, H, 0.2f), wall, true);
            Bx(new Vector3(1.75f, H / 2, D / 2), new Vector3(2.5f, H, 0.2f), wall, true);
            Bx(new Vector3(0, 2.5f, D / 2), new Vector3(1f, 1f, 0.2f), wall, true);
            var pivot = new Node3D { Name = "DoorPivot", Position = new Vector3(-0.5f, 0, D / 2) };
            root.AddChild(pivot);
            var door = dusty ? Mats.Flat(new Color(0.45f, 0.42f, 0.38f)) : Mats.Flat(new Color(0.85f, 0.82f, 0.75f));
            pivot.AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(1f, 2f, 0.08f) }, MaterialOverride = door, Layers = layers, Position = new Vector3(0.5f, 1f, 0) });
            pivot.AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(0.1f, 0.05f, 0.14f) }, MaterialOverride = Mats.Metal, Layers = layers, Position = new Vector3(0.85f, 1f, 0) });
        }

        // window (frame + glass glow)
        var sky = look switch
        {
            HomeLook.Night => Mats.Emissive(new Color(0.12f, 0.2f, 0.45f), 1.2f),
            HomeLook.Future => Mats.Emissive(new Color(0.25f, 0.35f, 0.6f), 1.5f),
            _ => Mats.Emissive(new Color(1f, 0.9f, 0.7f), 2.2f),
        };
        Bx(new Vector3(0, 1.7f, -D / 2 + 0.12f), new Vector3(2.2f, 1.5f, 0.04f), sky);
        var frame = Mats.Flat(new Color(0.9f, 0.88f, 0.84f));
        Bx(new Vector3(0, 1.7f, -D / 2 + 0.15f), new Vector3(0.06f, 1.5f, 0.06f), frame);
        Bx(new Vector3(0, 1.7f, -D / 2 + 0.15f), new Vector3(2.2f, 0.06f, 0.06f), frame);
        Bx(new Vector3(0, 0.92f, -D / 2 + 0.25f), new Vector3(2.5f, 0.06f, 0.35f), frame);
        if (look == HomeLook.Night)
        {
            var moon = new MeshInstance3D { Mesh = new SphereMesh { Radius = 0.18f, Height = 0.36f }, MaterialOverride = Mats.Emissive(new Color(0.9f, 0.95f, 1f), 4f), Layers = layers, Position = new Vector3(0.55f, 2.05f, -D / 2 + 0.08f) };
            root.AddChild(moon);
        }

        // curtains
        var curtain = Mats.Fabric(dusty ? new Color(0.35f, 0.37f, 0.42f) : new Color(0.72f, 0.78f, 0.92f));
        Bx(new Vector3(-1.35f, 1.6f, -D / 2 + 0.2f), new Vector3(0.4f, 2f, 0.06f), curtain);
        Bx(new Vector3(1.35f, 1.6f, -D / 2 + 0.2f), new Vector3(0.4f, 2f, 0.06f), curtain);

        // bed cushion (where the cats sleep) — at origin-ish
        var cushion = Mats.Fabric(dusty ? new Color(0.5f, 0.48f, 0.46f) : new Color(0.92f, 0.9f, 0.86f));
        var bed = new MeshInstance3D
        {
            Mesh = new CylinderMesh { TopRadius = 0.62f, BottomRadius = 0.7f, Height = 0.14f, RadialSegments = 28 },
            MaterialOverride = cushion, Layers = layers, Position = new Vector3(0, 0.07f, -1.2f), Scale = new Vector3(1.3f, 1, 1),
        };
        root.AddChild(bed);
        var rim = new MeshInstance3D
        {
            Mesh = new TorusMesh { InnerRadius = 0.55f, OuterRadius = 0.75f, Rings = 24, RingSegments = 12 },
            MaterialOverride = cushion, Layers = layers, Position = new Vector3(0, 0.14f, -1.2f), Scale = new Vector3(1.3f, 1, 1),
        };
        root.AddChild(rim);

        // bowls
        var bowl = Mats.Flat(new Color(0.9f, 0.45f, 0.6f), 0.4f);
        var bowl2 = Mats.Flat(new Color(0.35f, 0.55f, 0.95f), 0.4f);
        foreach (var (x, m) in new[] { (1.9f, bowl), (2.25f, bowl2) })
            root.AddChild(new MeshInstance3D { Mesh = new CylinderMesh { TopRadius = 0.13f, BottomRadius = 0.09f, Height = 0.07f }, MaterialOverride = m, Layers = layers, Position = new Vector3(x, 0.035f, 0.6f) });

        // toy mouse with tail
        if (!dusty)
        {
            var toy = Mats.Flat(new Color(0.85f, 0.75f, 0.6f));
            root.AddChild(new MeshInstance3D { Mesh = new SphereMesh { Radius = 0.06f, Height = 0.1f }, MaterialOverride = toy, Layers = layers, Position = new Vector3(-1.3f, 0.05f, 0.4f), Scale = new Vector3(1, 0.8f, 1.5f) });
        }

        // shelf (in the far future only the screw marks remain)
        if (!dusty)
        {
            Bx(new Vector3(-2.2f, 1.8f, -D / 2 + 0.25f), new Vector3(1.2f, 0.05f, 0.3f), Mats.WoodPlank);
            Bx(new Vector3(-2.4f, 1.95f, -D / 2 + 0.25f), new Vector3(0.18f, 0.25f, 0.18f), Mats.Flat(new Color(0.5f, 0.7f, 0.5f)));
            Bx(new Vector3(-2.0f, 1.92f, -D / 2 + 0.25f), new Vector3(0.3f, 0.2f, 0.2f), Mats.Flat(new Color(0.75f, 0.35f, 0.3f)));
        }
        else
        {
            foreach (var x in new[] { -2.7f, -1.7f })
                Bx(new Vector3(x, 1.78f, -D / 2 + 0.11f), new Vector3(0.04f, 0.04f, 0.02f), Mats.Flat(new Color(0.15f, 0.15f, 0.15f)));
        }

        // lights
        uint mask = layers;
        Color key = look switch
        {
            HomeLook.Night => new Color(0.55f, 0.65f, 1f),
            HomeLook.Future => new Color(0.5f, 0.6f, 0.9f),
            _ => new Color(1f, 0.85f, 0.6f),
        };
        float e = look == HomeLook.Night ? 1.2f : look == HomeLook.Future ? 0.9f : 2.2f;
        var spot = new SpotLight3D
        {
            LightColor = key, LightEnergy = e * 3, SpotRange = 8, SpotAngle = 40, ShadowEnabled = true,
            LightCullMask = mask, ShadowCasterMask = mask,
            Position = new Vector3(0.4f, 2.4f, -D / 2 + 0.5f),
        };
        root.AddChild(spot);
        spot.RotationDegrees = new Vector3(-62, 0, 0);
        root.AddChild(new OmniLight3D
        {
            LightColor = look == HomeLook.Night ? new Color(1f, 0.7f, 0.45f) : key,
            LightEnergy = look == HomeLook.Night ? 0.6f : 0.8f, OmniRange = 7, LightCullMask = mask,
            Position = new Vector3(-2.2f, 1.2f, 1.2f),
        });
        if (look == HomeLook.Night)
        {
            root.AddChild(new OmniLight3D
            {
                LightColor = new Color(1f, 0.72f, 0.45f), LightEnergy = 2.2f, OmniRange = 4.5f, LightCullMask = mask,
                ShadowEnabled = true, Position = new Vector3(-2.3f, 1.0f, -1.3f),
            });
            // small bedside lamp
            Bx(new Vector3(-2.5f, 0.3f, -1.6f), new Vector3(0.5f, 0.6f, 0.5f), Mats.WoodPlank);
            root.AddChild(new MeshInstance3D { Mesh = new CylinderMesh { TopRadius = 0.08f, BottomRadius = 0.16f, Height = 0.22f }, MaterialOverride = Mats.Emissive(new Color(1f, 0.75f, 0.45f), 2.5f), Layers = layers, Position = new Vector3(-2.5f, 0.75f, -1.6f) });
        }
        return root;
    }
}
