using System.Threading.Tasks;
using Godot;

// Automated walkthroughs used by `-- --chapter=N --script=solve` to smoke-test puzzle logic.
namespace HakuSani.Levels;

public partial class Chapter1
{
    protected override async Task DebugSolve()
    {
        Shot("start");
        await DPut(Haku, new Vector3(0.5f, 0, -1.5f), Mathf.Pi / 2);
        await DPut(Sani, new Vector3(0.5f, 0, -1.5f), Mathf.Pi / 2);
        Haku.Cam.Pitch = Sani.Cam.Pitch = -0.1f;
        await Wait(1f);
        Shot("houses");
        await DPut(Haku, new Vector3(0, 0, -17.5f), Mathf.Pi * 0.75f);
        await DPut(Sani, new Vector3(0, 0, -11.5f), -Mathf.Pi * 0.6f);
        await Wait(1f);
        Shot("yardB");
        await DPut(Haku, new Vector3(0, 0, -1.0f), 0);
        await DUse(Haku, new Vector3(0, 0, -1.5f), "свечения");
        await Wait(1f);
        Shot("ghost");
        await DPut(Haku, new Vector3(0, 0, -7.2f), 0);
        await DPut(Sani, new Vector3(0.4f, 0, -7.0f), 0);
        for (int i = 0; i < 3; i++) await DUse(Haku, new Vector3(0, 0.3f, -7.7f), "доску");
        await Wait(3.5f);
        Shot("door_fallen");
        // crate: push it three times toward the ruined wall
        await DPut(Haku, new Vector3(2.5f, 0, -10.5f), 0);
        for (int i = 0; i < 3; i++) { await DUse(Haku, new Vector3(2.5f, 0.4f, -11.4f - i), "ящик"); await Wait(0.3f); Haku.Teleport(new Vector3(2.5f, 0, -10.5f - i - 1), 0); }
        await DPut(Haku, new Vector3(2.5f, 0, -13.5f), 0);
        Shot("crate");
        await DPut(Haku, new Vector3(1.6f, 0, -15.6f), Mathf.Pi);
        await DUse(Haku, new Vector3(1.6f, 0.3f, -16.2f), "столбик");
        await Wait(4f);
        Shot("gate_open");
        await DPut(Sani, new Vector3(0, 0, -16.5f), 0);
        await DPut(Haku, new Vector3(-0.4f, 0, -19.5f), 0);
        await DPut(Sani, new Vector3(0.4f, 0, -19.5f), 0);
    }
}

public partial class Chapter2
{
    protected override async Task DebugSolve()
    {
        Shot("start");
        // walk Sani along the maze path to the blockage (footprints appear as she "steps")
        int k = System.Math.Max(2, _path.Count / 3);
        Sani.Teleport(CellCenter(0, Cols / 2) + new Vector3(0, 0, 2), 0);
        for (int i = 0; i <= k; i++)
        {
            var a = CellCenter(_path[i].r, _path[i].c);
            var t = Sani.CreateTween();
            t.TweenProperty(Sani, "global_position", a, 0.45f);
            await ToSignal(t, Tween.SignalName.Finished);
        }
        await Wait(0.5f);
        Haku.Teleport(CellCenter(_path[1].r, _path[1].c), 0);
        await Wait(0.5f);
        Shot("prints_in_fog");
        await DUse(Haku, _blockPos, "колонну");
        await Wait(3f);
        Shot("pillar");
        var end = new Vector3(0, 0, Z0 - Rows * Cell - 2.2f);
        await DPut(Sani, end + new Vector3(0.6f, 0, 0), 0);
        await DPut(Haku, end + new Vector3(0, 0, -0.1f), 0);
        await DUse(Haku, end, "игрушку");
        await Wait(2.5f);
        Shot("vision");
    }
}

public partial class Chapter3
{
    protected override async Task DebugSolve()
    {
        Shot("start");
        await DPut(Haku, new Vector3(2.2f, 0, -4.5f), Mathf.Pi);
        await DUse(Haku, new Vector3(2.2f, 0.3f, -5.1f), "механизм");
        await DPut(Sani, new Vector3(3, -2.5f, -10f), 0);
        await DPut(Haku, new Vector3(0, 0.1f, -7.6f), 0);
        await Wait(2.5f);
        Shot("raft");
        await DPut(Haku, new Vector3(-4, 0, -14.5f), 0);
        for (int i = 0; i < 2; i++) { await DUse(Haku, new Vector3(-4, 0.4f, -15.5f - i), "ящик"); Haku.Teleport(new Vector3(-4, 0, -15.5f - i), 0); await Wait(0.3f); }
        await Wait(3.5f);
        Shot("gate");
        await DPut(Sani, new Vector3(1.5f, 2.3f, -30.8f), Mathf.Pi / 2);
        await DUse(Sani, new Vector3(0, 2.5f, -31.3f), "рычаг");
        await Wait(2.5f);
        Shot("lever");
        await DPut(Haku, new Vector3(0, 0, -28.4f), 0);
        await Wait(0.5f);
        Shot("tower_inside");
        await DUse(Haku, new Vector3(0, 0.4f, -29.3f), "механизм");
    }
}

public partial class Chapter4
{
    protected override async Task DebugSolve()
    {
        Shot("start");
        await DPut(Sani, new Vector3(0.5f, 0, -6.5f), 0);
        await DPut(Haku, new Vector3(0, 0, -6.3f), 0);
        for (int i = 0; i < 3; i++) await DUse(Haku, new Vector3(-1.07f + i * 1.07f, 0.4f, -6.9f), "завал");
        await Wait(2.5f);
        Shot("rubble");
        await DPut(Sani, new Vector3(0.9f, 0.05f, -12f), 0);
        await Wait(1f);
        Shot("gateP_open");
        await DPut(Haku, new Vector3(-0.9f, 0.05f, -16f), 0);
        await DPut(Sani, new Vector3(0.6f, 0, -20f), 0);
        await Wait(1f);
        Shot("both_through");
        await DPut(Haku, new Vector3(-0.3f, 0, -27f), 0);
        await DPut(Sani, new Vector3(0.3f, 0, -27f), 0);
        await DUse(Haku, new Vector3(0, 0.4f, -27.5f), "двери");
        await DUse(Sani, new Vector3(0, 0.4f, -27.5f), "двери");
    }
}
