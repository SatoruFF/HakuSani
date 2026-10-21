# Blender headless build: decimate source cat, add collar, rig, animate, export haku.glb / sani.glb
# Run: Blender -b --factory-startup -P tools/build_cats.py
import bpy, math, os, sys
from mathutils import Vector, Quaternion, Matrix
from mathutils.bvhtree import BVHTree

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "generalContext/models/source/model.glb")
OUT = os.path.join(ROOT, "assets/cats")
S = 0.25  # source units -> meters

CATS = {
    "haku": (1.0, 0.36, 0.62),   # pink collar
    "sani": (0.20, 0.45, 1.0),   # blue collar
}

def f(x, y, z):
    """source coords -> final coords (head toward +Y, feet on z=0, scaled)"""
    return Vector((-x * S, -y * S, (z + 0.84) * S))

def build(name, collar_rgb):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=SRC)
    body = [o for o in bpy.data.objects if o.type == 'MESH'][0]
    body.name = "CatBody"
    bpy.context.view_layer.objects.active = body
    body.select_set(True)
    dec = body.modifiers.new("dec", 'DECIMATE'); dec.ratio = 0.08
    bpy.ops.object.modifier_apply(modifier="dec")
    body.matrix_world = Matrix.Translation((0, 0, 0.84 * S)) @ Matrix.Rotation(math.pi, 4, 'Z') @ Matrix.Scale(S, 4)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    bpy.ops.object.shade_smooth()

    # ---------- armature ----------
    arm_data = bpy.data.armatures.new("CatRig")
    arm = bpy.data.objects.new("CatRig", arm_data)
    bpy.context.scene.collection.objects.link(arm)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode='EDIT')
    eb = arm_data.edit_bones
    def bone(n, h, t, parent=None, connect=False, deform=True):
        b = eb.new(n); b.head = f(*h); b.tail = f(*t); b.roll = 0
        if parent: b.parent = eb[parent]; b.use_connect = connect
        b.use_deform = deform
        return b
    bone("root", (0, 0, -0.84), (0, -0.3, -0.84), deform=False)
    bone("hips", (0, 0.42, -0.15), (0, 0.05, -0.15), "root")
    bone("spine", (0, 0.05, -0.15), (0, -0.28, -0.1), "hips", True)
    bone("chest", (0, -0.28, -0.1), (0, -0.42, 0.04), "spine", True)
    bone("neck", (0, -0.42, 0.04), (0, -0.55, 0.22), "chest", True)
    bone("head", (0, -0.55, 0.22), (0, -0.75, 0.7), "neck", True)
    tail = [(0, 0.6, -0.12), (0, 0.75, -0.11), (0, 0.84, -0.02), (0, 0.86, 0.17), (0, 0.86, 0.33), (0, 0.92, 0.48)]
    for i in range(5):
        bone(f"tail{i+1}", tail[i], tail[i+1], "hips" if i == 0 else f"tail{i}", i > 0)
    for side, sx in (("L", 1), ("R", -1)):
        x = 0.155 * sx
        bone(f"fleg1.{side}", (x, -0.45, -0.18), (x, -0.42, -0.5), "chest")
        bone(f"fleg2.{side}", (x, -0.42, -0.5), (x, -0.42, -0.73), f"fleg1.{side}", True)
        bone(f"fpaw.{side}", (x, -0.42, -0.73), (x * 1.05, -0.56, -0.8), f"fleg2.{side}", True)
        x = 0.175 * sx
        bone(f"rleg1.{side}", (x, 0.36, -0.18), (x, 0.42, -0.45), "hips")
        bone(f"rleg2.{side}", (x, 0.42, -0.45), (x, 0.52, -0.68), f"rleg1.{side}", True)
        bone(f"rpaw.{side}", (x, 0.52, -0.68), (x * 1.1, 0.38, -0.8), f"rleg2.{side}", True)
    bpy.ops.object.mode_set(mode='OBJECT')

    # ---------- skin ----------
    bpy.ops.object.select_all(action='DESELECT')
    body.select_set(True); arm.select_set(True)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.parent_set(type='ARMATURE_NAME')
    skin(body, arm)

    # ---------- collar (ray-cast ring around neck) ----------
    bvh = BVHTree.FromObject(body, bpy.context.evaluated_depsgraph_get())
    c = f(0, -0.40, 0.0)
    n = (f(0, -0.62, 0.32) - f(0, -0.28, -0.12)).normalized()
    u = Vector((1, 0, 0)); v = n.cross(u).normalized()
    pts = []
    for i in range(48):
        a = 2 * math.pi * i / 48
        d = (u * math.cos(a) + v * math.sin(a)).normalized()
        hit, nrm, idx, dist = bvh.ray_cast(c, d, 1.0)
        pts.append((hit if hit else c + d * 0.08) + d * 0.006)
    # smooth radii to avoid fur spikes
    for _ in range(3):
        pts = [(pts[i - 1] + pts[i] * 2 + pts[(i + 1) % len(pts)]) / 4 for i in range(len(pts))]
    cu = bpy.data.curves.new("collar", 'CURVE'); cu.dimensions = '3D'
    sp = cu.splines.new('POLY'); sp.points.add(len(pts) - 1)
    for p, q in zip(sp.points, pts): p.co = (q.x, q.y, q.z, 1)
    sp.use_cyclic_u = True
    cu.bevel_depth = 0.0075; cu.bevel_resolution = 2
    col = bpy.data.objects.new("Collar", cu); bpy.context.scene.collection.objects.link(col)
    m = bpy.data.materials.new("Collar"); m.use_nodes = True
    bsdf = m.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = (*collar_rgb, 1); bsdf.inputs["Roughness"].default_value = 0.45
    col.data.materials.append(m)
    # bell/tag at lowest point
    low = min(pts, key=lambda p: p.z - p.y * 0.3)
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.012, location=low + Vector((0, 0.006, -0.008)), segments=16, ring_count=8)
    bell = bpy.context.active_object; bell.name = "Bell"
    gm = bpy.data.materials.new("Bell"); gm.use_nodes = True
    gb = gm.node_tree.nodes["Principled BSDF"]
    gb.inputs["Base Color"].default_value = (1.0, 0.78, 0.25, 1); gb.inputs["Metallic"].default_value = 1; gb.inputs["Roughness"].default_value = 0.3
    bell.data.materials.append(gm)
    bpy.ops.object.select_all(action='DESELECT')
    col.select_set(True); bpy.context.view_layer.objects.active = col
    bpy.ops.object.convert(target='MESH')
    col = bpy.context.active_object
    bell.select_set(True); bpy.ops.object.join(); col = bpy.context.active_object
    bpy.ops.object.shade_smooth()
    vg = col.vertex_groups.new(name="neck"); vg.add([v.index for v in col.data.vertices], 1.0, 'REPLACE')
    col.parent = arm
    mod = col.modifiers.new("Armature", 'ARMATURE'); mod.object = arm

    animate(arm)
    os.makedirs(OUT, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ROOT, "tools/blend", f"{name}.blend"))
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.gltf(filepath=os.path.join(OUT, f"{name}.glb"), export_format='GLB',
                              export_animation_mode='ACTIONS', export_apply=False, export_yup=True)
    return arm

# ---------- custom skin weights ----------
def seg_dist(p, a, b):
    ab = b - a; t = max(0.0, min(1.0, (p - a).dot(ab) / ab.length_squared))
    return (p - (a + ab * t)).length

def skin(body, arm):
    bones = [b for b in arm.data.bones if b.use_deform]
    segs = {b.name: (b.head_local.copy(), b.tail_local.copy()) for b in bones}
    groups = {b.name: body.vertex_groups.get(b.name) or body.vertex_groups.new(name=b.name) for b in bones}
    for v in body.data.vertices:
        p = v.co
        sx, sy, sz = -p.x / S, -p.y / S, p.z / S - 0.84   # back to source coords for region masks
        allowed = []
        for n in segs:
            if n.startswith("tail"):
                if not (sy > 0.6 and sz > -0.25): continue
            elif n[1:4] == "leg" or n[1:4] == "paw":
                side = 1 if n.endswith(".L") else -1
                front = n.startswith("f")
                if sx * side < 0.03 or sz > -0.2: continue
                if front and sy > -0.2: continue
                if not front and sy < 0.15: continue
            elif sy > 0.6 and sz > -0.25:
                if n != "hips": continue
            allowed.append(n)
        ws = sorted(((1.0 / (seg_dist(p, *segs[n]) + 0.004) ** 4, n) for n in allowed), reverse=True)[:3]
        tot = sum(w for w, _ in ws)
        for w, n in ws:
            groups[n].add([v.index], w / tot, 'REPLACE')

# ---------- animation ----------
# Legs are driven by IK: paws plant on the ground while the body moves over them, which removes
# foot sliding. Gait speeds (m/s) are exported to the game as WALK_SPEED / RUN_SPEED (see Cat.cs).
FPS = 60
X, Yax, Z = (1, 0, 0), (0, 1, 0), (0, 0, 1)
LEGS = {   # key: (lower leg bone that owns the IK, paw bone, is_front)
    "FL": ("fleg2.L", "fpaw.L", True), "FR": ("fleg2.R", "fpaw.R", True),
    "HL": ("rleg2.L", "rpaw.L", False), "HR": ("rleg2.R", "rpaw.R", False),
}
WALK = dict(frames=20, L=0.13, duty=0.62, h=0.03, crouch=-0.018,
            phase={"HL": 0.0, "FL": 0.25, "HR": 0.5, "FR": 0.75})
RUN = dict(frames=14, L=0.36, duty=0.34, h=0.05, crouch=-0.012,
           phase={"HL": 0.0, "HR": 0.1, "FL": 0.45, "FR": 0.55})


def wq(pb, axis, deg):
    """rotation about an armature-space axis, converted to bone-local quaternion"""
    r = pb.bone.matrix_local.to_quaternion()
    return r.inverted() @ Quaternion(Vector(axis), math.radians(deg)) @ r


def setup_ik(arm):
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode='EDIT')
    eb = arm.data.edit_bones
    for k, (chain, paw, front) in LEGS.items():
        p = eb[paw]
        t = eb.new("ik." + k)
        t.head, t.tail, t.roll = p.head.copy(), p.tail.copy(), p.roll
        t.use_deform = False
        knee = eb[chain].head.copy()
        pole = eb.new("pole." + k)
        pole.head = knee + Vector((0, -0.15 if front else 0.15, 0))
        pole.tail = pole.head + Vector((0, 0, 0.02))
        pole.use_deform = False
        pole.parent = eb["chest"] if front else eb["hips"]
    bpy.ops.object.mode_set(mode='POSE')
    for k, (chain, paw, front) in LEGS.items():
        pb = arm.pose.bones[chain]
        c = pb.constraints.new('IK'); c.name = "IK"
        c.target, c.subtarget, c.chain_count = arm, "ik." + k, 2
        c.pole_target, c.pole_subtarget = arm, "pole." + k
        cr = arm.pose.bones[paw].constraints.new('COPY_ROTATION'); cr.name = "PawRot"
        cr.target, cr.subtarget = arm, "ik." + k
        # pick the pole angle that keeps the rest pose and bends the joint the anatomical way
        ik = arm.pose.bones["ik." + k]
        upper = pb.parent
        want = Vector((0, -1 if front else 1, 0))          # elbow goes back, hind knee forward
        best = None
        for deg in range(-180, 180, 5):
            c.pole_angle = math.radians(deg)
            ik.location = (0, 0, 0)
            bpy.context.view_layer.update()
            rest_err = sum(abs(v) for row in (pb.matrix.to_3x3() - pb.bone.matrix_local.to_3x3()) for v in row) + \
                       sum(abs(v) for row in (upper.matrix.to_3x3() - upper.bone.matrix_local.to_3x3()) for v in row)
            ik.location = ik.bone.matrix_local.to_3x3().inverted() @ Vector((0, 0, 0.03))
            bpy.context.view_layer.update()
            joint = upper.tail
            straight = (upper.head + pb.tail) / 2
            disp = joint - straight
            score = rest_err * 2 - disp.dot(want) * 50 + abs(disp.x) * 80
            if best is None or score < best[0]:
                best = (score, deg)
        c.pole_angle = math.radians(best[1])
        ik.location = (0, 0, 0)
        print("POLE", k, best[1])
    bpy.ops.object.mode_set(mode='OBJECT')


def new_action(arm, name):
    act = bpy.data.actions.new(name); act.use_fake_user = True
    arm.animation_data_create(); arm.animation_data.action = act
    return act


def pose(arm, frame, rots=None, root_dz=0.0, feet=None, flex=None, ik=1.0):
    """Key one frame. rots: {bone: [(axis, deg)]} in armature space. feet: {leg: Vector offset of the paw
    target from rest}. flex: {leg: deg} paw curl. ik: 1/0 or {leg: influence} (0 = pure FK)."""
    rots = dict(rots or {})
    for k, deg in (flex or {}).items():
        rots.setdefault("ik." + k, []).append((X, deg))
    for pb in arm.pose.bones:
        q = Quaternion()
        for axis, deg in rots.get(pb.name, []):
            q = wq(pb, axis, deg) @ q
        pb.rotation_mode = 'QUATERNION'
        pb.rotation_quaternion = q
        pb.keyframe_insert("rotation_quaternion", frame=frame)
        if pb.name == "root":
            pb.location = pb.bone.matrix_local.to_3x3().inverted() @ Vector((0, 0, root_dz))
            pb.keyframe_insert("location", frame=frame)
        elif pb.name.startswith("ik."):
            k = pb.name[3:]
            off = (feet or {}).get(k, Vector((0, 0, 0)))
            pb.location = pb.bone.matrix_local.to_3x3().inverted() @ Vector(off)
            pb.keyframe_insert("location", frame=frame)
    for k, (chain, paw, front) in LEGS.items():
        inf = ik.get(k, 1.0) if isinstance(ik, dict) else ik
        for bn, cn in ((chain, "IK"), (paw, "PawRot")):
            con = arm.pose.bones[bn].constraints[cn]
            con.influence = inf
            con.keyframe_insert("influence", frame=frame)


def foot(phase, duty, L, h):
    """Paw path relative to its neutral spot: stance slides back (body moves over it), swing arcs forward."""
    p = phase % 1.0
    if p < duty:
        s = p / duty
        return Vector((0, L / 2 - s * L, 0)), 0.0
    s = (p - duty) / (1 - duty)
    ease = 0.5 - 0.5 * math.cos(math.pi * s)
    return Vector((0, -L / 2 + L * ease, h * math.sin(math.pi * s))), math.sin(math.pi * s)


def tail(t, amp, lift=0.0, lag=0.7):
    return {f"tail{i+1}": [(Z, amp * math.sin(t - i * lag)), (X, lift)] for i in range(5)}


def merge(*ds):
    out = {}
    for d in ds:
        for k, v in d.items(): out.setdefault(k, []).extend(v)
    return out


def gait(arm, name, g, body):
    new_action(arm, name)
    n = g["frames"]
    for fr in range(n + 1):
        t = fr / n
        feet, flex = {}, {}
        for k, ph in g["phase"].items():
            front = LEGS[k][2]
            off, sw = foot(t + ph, g["duty"], g["L"], g["h"] * (1 if front else 0.85))
            feet[k] = off
            flex[k] = -55 * sw if front else -40 * sw
        rots, dz = body(t)
        pose(arm, fr + 1, rots, g["crouch"] + dz, feet, flex)


def walk_body(t):
    w = 2 * math.pi * t
    rots = merge({
        "hips": [(Yax, 3.5 * math.sin(w)), (Z, 2.5 * math.sin(w))],
        "chest": [(Yax, -3 * math.sin(w + 1.6)), (Z, -2 * math.sin(w + 1.6))],
        "neck": [(X, 2 * math.cos(2 * w))],
        "head": [(Yax, 2.5 * math.sin(w + 1.6)), (X, -2.5 * math.cos(2 * w)), (Z, 1.5 * math.sin(w))],
    }, tail(w, 9, -4))
    return rots, -0.004 * (0.5 + 0.5 * math.cos(2 * w))


def run_body(t):
    w = 2 * math.pi * t
    flexion = math.sin(w - 0.6)
    rots = merge({
        "spine": [(X, 11 * flexion)],
        "hips": [(X, -7 * flexion)],
        "chest": [(X, -6 * flexion)],
        "neck": [(X, 4 * flexion)],
        "head": [(X, -6 * flexion)],
    }, tail(w, 5, 18 + 6 * flexion, 0.5))
    return rots, 0.012 * max(0.0, math.sin(w - 2.2)) - 0.006 * max(0.0, -math.sin(w - 2.2))


def animate(arm):
    bpy.context.scene.render.fps = FPS
    setup_ik(arm)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode='POSE')

    # Idle: breathing, looking around, a lazy tail and a little weight shift
    new_action(arm, "Idle")
    n = 300
    for fr in range(0, n + 1, 6):
        t = fr / n
        w = 2 * math.pi * t
        look = math.sin(w) * 0.7 + math.sin(3 * w) * 0.3
        rots = merge({
            "chest": [(X, 1.5 * math.sin(5 * w))],
            "spine": [(X, -1 * math.sin(5 * w))],
            "hips": [(Yax, 2 * math.sin(w))],
            "neck": [(Z, 8 * look)],
            "head": [(Z, 10 * look), (Yax, 6 * math.sin(2 * w)), (X, 3 * math.sin(2 * w + 1))],
        }, tail(3 * w, 14, -6))
        pose(arm, fr + 1, rots, -0.006 - 0.002 * math.sin(5 * w))

    gait(arm, "Walk", WALK, walk_body)
    gait(arm, "Run", RUN, run_body)

    # Jump: crouch → launch (legs extend) → tuck
    new_action(arm, "Jump")
    V = lambda y, z: Vector((0, y, z))
    pose(arm, 1, {}, -0.006)
    pose(arm, 6, {"spine": [(X, -6)], "head": [(X, -8)], **tail(0, 0, 10)}, -0.04,
         {k: V(0, 0) for k in LEGS})
    pose(arm, 11, {"spine": [(X, 8)], "head": [(X, 6)], **tail(0, 0, -15)}, 0.0,
         {"FL": V(0.06, 0.02), "FR": V(0.06, 0.02), "HL": V(-0.07, -0.01), "HR": V(-0.07, -0.01)},
         {"HL": -40, "HR": -40})
    pose(arm, 22, {"spine": [(X, -4)], **tail(0, 0, 10)}, 0.0,
         {"FL": V(0.02, 0.05), "FR": V(0.02, 0.05), "HL": V(0.0, 0.05), "HR": V(0.0, 0.05)},
         {"FL": -50, "FR": -50, "HL": -30, "HR": -30})

    # Fall: legs reach down and forward, tail up for balance
    new_action(arm, "Fall")
    for fr in (1, 21, 41):
        s = math.sin(fr * 0.157)
        pose(arm, fr, merge({"head": [(X, -8)], "spine": [(X, -3)]}, tail(fr * 0.157, 14, 18)), 0.0,
             {"FL": V(0.05, 0.0), "FR": V(0.05 + 0.01 * s, 0.0), "HL": V(-0.03, 0.01), "HR": V(-0.03, 0.01)},
             {"FL": -15, "FR": -15})

    # Sleep (sphinx loaf) — pure FK
    new_action(arm, "Sleep")
    for fr in range(0, 241, 20):
        t = 2 * math.pi * fr / 240
        fold = {f"fleg1.{s}": [(X, 82)] for s in "LR"}
        fold.update({f"fleg2.{s}": [(X, -10)] for s in "LR"})
        fold.update({f"fpaw.{s}": [(X, -20)] for s in "LR"})
        fold.update({f"rleg1.{s}": [(X, 55)] for s in "LR"})
        fold.update({f"rleg2.{s}": [(X, -125)] for s in "LR"})
        fold.update({f"rpaw.{s}": [(X, 70)] for s in "LR"})
        fold["head"] = [(X, 18 + 1.5 * math.sin(t)), (Z, 20)]
        fold["neck"] = [(X, 10)]
        fold["chest"] = [(X, 2 * math.sin(t))]
        fold.update({f"tail{i+1}": [(X, -30 if i < 2 else -5), (Z, 32 + 3 * math.sin(t))] for i in range(5)})
        pose(arm, fr + 1, fold, -0.088, ik=0.0)

    # Paw: right front swipes forward (FK on that leg only)
    new_action(arm, "Paw")
    for fr, a, b in [(1, 0, 0), (9, 75, -40), (15, 90, -10), (22, 25, -60), (34, 0, 0)]:
        pose(arm, fr, {"fleg1.R": [(X, a)], "fleg2.R": [(X, b)], "fpaw.R": [(X, -b * 0.5)],
                       "head": [(X, -6 if a else 0)], "spine": [(X, 4 if a else 0)], "chest": [(Yax, -4 if a else 0)]},
             -0.008, ik={"FR": 0.0, "FL": 1.0, "HL": 1.0, "HR": 1.0})

    bpy.ops.object.mode_set(mode='OBJECT')
    arm.animation_data.action = bpy.data.actions["Idle"]
    walk = WALK["L"] / (WALK["frames"] / FPS)
    run = RUN["L"] / (RUN["frames"] / FPS)
    print(f"GAIT_SPEEDS walk={walk:.3f} run={run:.3f}")

if __name__ == "__main__":
    for n, rgb in CATS.items():
        build(n, rgb)
    print("BUILD_OK")
