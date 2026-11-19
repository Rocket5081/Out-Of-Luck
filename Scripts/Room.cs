using Godot;
using System.Collections.Generic;
using System.Linq;

public enum RoomType { Start, Combat, Item, Boss }
public enum Dir { N, S, E, W }

public partial class Room : Node3D
{
    [Export] public RoomType Type;
    [Export] public Vector3 BoundsSize = new(100, 20, 100);   // size of the room incl. halls
    [Export] public Vector3 BoundsOffset = Vector3.Zero;   // local center of that box

    public HashSet<Dir> Used = new();

    public Marker3D GetExit(Dir d) => FindChild($"Exit_{d}", recursive: true, owned: false) as Marker3D;

    public IEnumerable<Dir> AllExits() =>
        System.Enum.GetValues<Dir>().Where(d => GetExit(d) != null);

    public List<Dir> FreeExits() => AllExits().Where(d => !Used.Contains(d)).ToList();

    public Aabb WorldBounds() =>
        new Aabb(GlobalPosition + BoundsOffset - BoundsSize / 2f, BoundsSize);
}