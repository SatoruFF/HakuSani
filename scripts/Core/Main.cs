using Godot;

namespace HakuSani;

/// <summary>Boot scene: hands control to the Game autoload.</summary>
public partial class Main : Node
{
    public override void _Ready() => Callable.From(() => Game.I.ShowMenu()).CallDeferred();
}
