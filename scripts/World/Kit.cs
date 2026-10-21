using System.Collections.Generic;
using Godot;

namespace HakuSani;

/// <summary>
/// Quaternius modular kits (Medieval Village + Downtown City, CC0).
/// Pieces are loaded per era (render layer) with an optional per-era colour tint.
/// </summary>
public static class Kit
{
    public static readonly Color PastTint = new(1.0f, 0.93f, 0.82f);    // warm, sun-faded
    public static readonly Color RuinTint = new(0.82f, 0.86f, 0.74f);   // mossy, dirty
    public static readonly Color FutureTint = new(0.92f, 0.95f, 1.0f);  // clean, cool

    static readonly Dictionary<(ulong, Color), Material> TintCache = new();
    static readonly Dictionary<string, PackedScene> SceneCache = new();

    /// <summary>Box collider fitted to the node's meshes, in its own (rotated/scaled) space.</summary>
    public static StaticBody3D AddBoundsCollider(Node3D n, Era era, float shrink = 0.92f, float minHeight = 0.05f)
    {
        var inv = n.GlobalTransform.AffineInverse();
        Aabb? box = null;
        void Walk(Node x)
        {
            if (x is MeshInstance3D mi && mi.Mesh != null)
            {
                var b = (inv * mi.GlobalTransform) * mi.Mesh.GetAabb();
                box = box.HasValue ? box.Value.Merge(b) : b;
            }
            foreach (var c in x.GetChildren()) Walk(c);
        }
        Walk(n);
        if (!box.HasValue || box.Value.Size.Y < minHeight) return null;
        var bb = box.Value;
        var body = new StaticBody3D { CollisionLayer = L.Phys(era), CollisionMask = 0 };
        body.AddChild(new CollisionShape3D
        {
            Shape = new BoxShape3D { Size = new Vector3(bb.Size.X * shrink, bb.Size.Y, bb.Size.Z * shrink) },
            Position = bb.GetCenter(),
        });
        n.AddChild(body);
        return body;
    }

    public static Node3D Load(Node parent, Era era, string path, Vector3 pos, float rotY = 0, float scale = 1, Color? tint = null, bool shadows = true)
    {
        if (!SceneCache.TryGetValue(path, out var scene))
            SceneCache[path] = scene = GD.Load<PackedScene>($"res://assets/{path}.gltf");
        var n = scene.Instantiate<Node3D>();
        n.Position = pos;
        n.RotationDegrees = new Vector3(0, rotY, 0);
        n.Scale = Vector3.One * scale;
        parent.AddChild(n);
        Apply(n, L.Vis(era), tint, shadows);
        return n;
    }

    public static Node3D Med(Node parent, Era era, string name, Vector3 pos, float rotY = 0, float scale = 1, Color? tint = null, bool collide = false)
    {
        var n = Load(parent, era, "medieval/" + name, pos, rotY, scale, tint ?? (era == Era.Past ? PastTint : FutureTint));
        if (collide) AddBoundsCollider(n, era);
        return n;
    }

    public static Node3D City(Node parent, Era era, string name, Vector3 pos, float rotY = 0, float scale = 1, Color? tint = null, bool collide = false)
    {
        var n = Load(parent, era, "city/" + name, pos, rotY, scale, tint ?? FutureTint);
        if (collide) AddBoundsCollider(n, era);
        return n;
    }

    static void Apply(Node n, uint layers, Color? tint, bool shadows)
    {
        if (n is MeshInstance3D mi)
        {
            mi.Layers = layers;
            if (!shadows) mi.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
            if (tint.HasValue && mi.Mesh != null)
                for (int s = 0; s < mi.Mesh.GetSurfaceCount(); s++)
                {
                    var m = mi.GetActiveMaterial(s);
                    if (m is BaseMaterial3D bm) mi.SetSurfaceOverrideMaterial(s, Tinted(bm, tint.Value));
                }
        }
        foreach (var c in n.GetChildren()) Apply(c, layers, tint, shadows);
    }

    static Material Tinted(BaseMaterial3D src, Color tint)
    {
        var key = (src.GetInstanceId(), tint);
        if (TintCache.TryGetValue(key, out var m)) return m;
        var d = (BaseMaterial3D)src.Duplicate();
        d.AlbedoColor = src.AlbedoColor * tint;
        TintCache[key] = d;
        return d;
    }

    /// <summary>World-space AABB of all meshes under a node.</summary>
    public static Aabb Bounds(Node3D n)
    {
        Aabb? box = null;
        void Walk(Node x)
        {
            if (x is MeshInstance3D mi && mi.Mesh != null)
            {
                var b = mi.GlobalTransform * mi.Mesh.GetAabb();
                box = box.HasValue ? box.Value.Merge(b) : b;
            }
            foreach (var c in x.GetChildren()) Walk(c);
        }
        Walk(n);
        return box ?? new Aabb(n.GlobalPosition, Vector3.Zero);
    }

    static readonly int[,] Roofs = { { 4, 4 }, { 4, 6 }, { 4, 8 }, { 6, 6 }, { 6, 8 }, { 8, 8 }, { 8, 10 } };

    /// <summary>
    /// Modular medieval house on a 2 m grid. Front (door) faces local +Z.
    /// intact: tiled roof, glazed windows, warm-lit interiors (if lit).
    /// ruined: no roof, gaps in the top storey, vines, mossy tint.
    /// </summary>
    public static Node3D House(Node parent, Era era, Vector3 pos, int nx, int nz, int floors, float rotY, bool ruined, bool lit, int seed = 1)
    {
        var root = new Node3D { Position = pos, RotationDegrees = new Vector3(0, rotY, 0) };
        parent.AddChild(root);
        var rng = new RandomNumberGenerator { Seed = (ulong)(seed * 7919 + nx * 31 + nz) };
        float W = nx * 2, D = nz * 2, FH = 3.12f;
        var tint = ruined ? RuinTint : era == Era.Past ? PastTint : FutureTint;
        var glow = Mats.WindowLit;

        // collision follows the wall modules (so open doorways of ruins really are open)
        var body = new StaticBody3D { CollisionLayer = L.Phys(era), CollisionMask = 0 };
        root.AddChild(body);
        void WallCollider(Vector3 p, float rot, bool openDoor)
        {
            float H = floors * FH;
            var r = Mathf.DegToRad(rot);
            var outward = new Vector3(Mathf.Sin(r), 0, Mathf.Cos(r));
            var right = new Vector3(Mathf.Cos(r), 0, -Mathf.Sin(r));
            var c = p - outward * 0.11f;
            void Part(Vector3 centre, Vector3 size) => body.AddChild(new CollisionShape3D
            {
                Shape = new BoxShape3D { Size = size },
                Transform = new Transform3D(new Basis(Vector3.Up, r), centre),
            });
            if (!openDoor) { Part(c + Vector3.Up * H / 2, new Vector3(2.0f, H, 0.4f)); return; }
            Part(c + right * 0.79f + Vector3.Up * H / 2, new Vector3(0.42f, H, 0.4f));
            Part(c - right * 0.79f + Vector3.Up * H / 2, new Vector3(0.42f, H, 0.4f));
            Part(c + Vector3.Up * (2.3f + (H - 2.3f) / 2), new Vector3(2.0f, H - 2.3f, 0.4f));
        }

        void Side(int count, float half, float rot, System.Func<int, Vector3> at, bool front)
        {
            for (int f = 0; f < floors; f++)
                for (int i = 0; i < count; i++)
                {
                    bool top = f == floors - 1;
                    if (ruined && top && floors > 1 && rng.Randf() < 0.45f) continue;
                    bool door = front && f == 0 && i == count / 2;
                    bool window = !door && (f > 0 || (i + (front ? 1 : 0)) % 2 == 0);
                    string piece;
                    if (f == 0) piece = door ? (ruined ? "Wall_UnevenBrick_Door_Round" : "Wall_Plaster_Door_Round")
                                      : window ? (ruined ? "Wall_UnevenBrick_Window_Wide_Round" : "Wall_Plaster_Window_Wide_Round")
                                      : (ruined ? "Wall_UnevenBrick_Straight" : "Wall_Plaster_Straight_Base");
                    else piece = window ? "Wall_Plaster_Window_Thin_Round" : "Wall_Plaster_WoodGrid";
                    var p = at(i) + Vector3.Up * f * FH;
                    Med(root, era, piece, p, rot, 1, tint);
                    if (f == 0) WallCollider(p, rot, door && ruined);
                    var outward = new Vector3(Mathf.Sin(Mathf.DegToRad(rot)), 0, Mathf.Cos(Mathf.DegToRad(rot)));
                    if (door && !ruined)
                        Med(root, era, "Door_1_Round", p - outward * 0.12f + new Vector3(Mathf.Cos(Mathf.DegToRad(rot)), 0, -Mathf.Sin(Mathf.DegToRad(rot))) * -0.55f, rot, 1, tint);
                    if (window && !ruined)
                    {
                        Med(root, era, f == 0 ? "Window_Wide_Round1" : "Window_Thin_Round1", p, rot, 1, tint);
                        if (lit)
                        {
                            var pane = new MeshInstance3D
                            {
                                Mesh = new QuadMesh { Size = new Vector2(1.5f, 1.9f) }, MaterialOverride = glow, Layers = L.Vis(era),
                                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                            };
                            root.AddChild(pane);
                            pane.Position = p + Vector3.Up * 1.85f - outward * 0.4f;
                            pane.RotationDegrees = new Vector3(0, rot, 0);
                        }
                    }
                    if (ruined && rng.Randf() < 0.35f)
                        Med(root, era, rng.Randf() < 0.5f ? "Prop_Vine4" : "Prop_Vine1", p + outward * 0.12f + Vector3.Up * (FH - 0.3f), rot, 1.2f, tint);
                }
        }

        // walls: piece local +Z faces outward
        Side(nx, D / 2, 0, i => new Vector3(-W / 2 + 1 + i * 2, 0, D / 2 - 0.1f), true);
        Side(nx, D / 2, 180, i => new Vector3(W / 2 - 1 - i * 2, 0, -D / 2 + 0.1f), false);
        Side(nz, W / 2, 90, i => new Vector3(W / 2 - 0.1f, 0, D / 2 - 1 - i * 2), false);
        Side(nz, W / 2, -90, i => new Vector3(-W / 2 + 0.1f, 0, -D / 2 + 1 + i * 2), false);
        foreach (var (cx, cz) in new[] { (1, 1), (1, -1), (-1, 1), (-1, -1) })
            for (int f = 0; f < floors; f++)
                Med(root, era, ruined ? "Corner_Exterior_Brick" : "Corner_Exterior_Wood", new Vector3(cx * (W / 2 - 0.05f), f * FH, cz * (D / 2 - 0.05f)), cx > 0 ? (cz > 0 ? 0 : 90) : (cz > 0 ? -90 : 180), 1, tint);

        if (!ruined)
        {
            // pick the tiled roof that fits the footprint (either orientation)
            string roof = null; float roofRot = 0;
            for (int i = 0; i < Roofs.GetLength(0); i++)
            {
                if (Roofs[i, 0] == W && Roofs[i, 1] == D) { roof = $"Roof_RoundTiles_{Roofs[i, 0]}x{Roofs[i, 1]}"; roofRot = 0; break; }
                if (Roofs[i, 0] == D && Roofs[i, 1] == W) { roof = $"Roof_RoundTiles_{Roofs[i, 0]}x{Roofs[i, 1]}"; roofRot = 90; }
            }
            if (roof != null)
            {
                var r = Med(root, era, roof, new Vector3(0, floors * FH, 0), roofRot, 1, tint);
                // recentre: some roof pieces have their pivot on an edge
                var b = Bounds(r);
                var centre = root.ToLocal(b.GetCenter());
                r.Position += new Vector3(-centre.X, 0, -centre.Z);
                if (rng.Randf() < 0.7f) Med(root, era, "Prop_Chimney", new Vector3(W / 4, floors * FH + 0.6f, -D / 4), 0, 1, tint);
            }
            else
                B.Box(root, era, new Vector3(0, floors * FH + 0.15f, 0), new Vector3(W + 0.4f, 0.3f, D + 0.4f), era == Era.Past ? Mats.RoofOld : Mats.RoofNew, 0, false);
        }
        else if (rng.Randf() < 0.6f)
        {
            // a tree growing out of the roofless shell
            B.Prop(root, era, "CommonTree_" + rng.RandiRange(1, 5), new Vector3(rng.RandfRange(-W / 4, W / 4), 0, rng.RandfRange(-D / 4, D / 4)), rng.RandfRange(0, 360), 0.8f);
        }
        return root;
    }
}
