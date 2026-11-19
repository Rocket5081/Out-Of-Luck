using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class DungeonGenerator : Node3D
{
    [Export] public PackedScene StartRoom;
    [Export] public PackedScene BossRoom;
    [Export] public Godot.Collections.Array<PackedScene> CombatRooms;
    [Export] public Godot.Collections.Array<PackedScene> ItemRooms;
    [Export] public PackedScene FallbackLayout;   // optional hand-built dungeon, used only if generation fails completely
    [Export] public int MainPathLength = 8;       // rooms after start, incl. boss
    [Export(PropertyHint.Range, "0,1,0.05")] public float BranchChance = 0.4f;
    [Export] public int MaxItemRooms = 4;
    [Export] public int AttemptsPerLevel = 25;
    [Export] public int Seed = 0;                 // 0 = random each run

    readonly RandomNumberGenerator rng = new();
    readonly List<Room> placed = new();
    int itemCount;

    public override void _Ready()
    {
        if (Seed == 0) rng.Randomize(); else rng.Seed = (ulong)Seed;

        ValidatePool();
        Generate();
    }

    void Generate()
    {
        // Full settings first, then progressively simpler dungeons so we always end up with something playable.
        for (int length = MainPathLength; length >= 1; length--)
        {
            float branch = length == MainPathLength ? BranchChance : 0f;

            for (int attempt = 0; attempt < AttemptsPerLevel; attempt++)
            {
                if (Build(length, branch))
                {
                    GD.Print($"[DungeonGen] Built: path length {length}, attempt {attempt + 1}");
                    return;
                }
                Clear();
            }
            GD.PushWarning($"[DungeonGen] Path length {length} failed {AttemptsPerLevel} times, trying shorter");
        }

        GD.PushError("[DungeonGen] Generation failed completely");
        if (FallbackLayout != null) AddChild(FallbackLayout.Instantiate());
    }

    bool Build(int length, float branchChance)
    {
        itemCount = 0;

        // 1. Start room at origin
        var start = StartRoom.Instantiate<Room>();
        AddChild(start);
        start.GlobalPosition = Vector3.Zero;
        placed.Add(start);

        // 2. Critical path: try every free exit of the current room before giving up
        Room current = start;
        for (int i = 0; i < length; i++)
        {
            bool last = i == length - 1;
            var pool = last ? new List<PackedScene> { BossRoom } : CombatRooms.ToList();

            Room next = null;
            var dirs = current.FreeExits().OrderBy(_ => rng.Randi()).ToList();
            foreach (var dir in dirs)
            {
                next = TryPlace(current, dir, pool, requireExtraExit: !last);
                if (next != null) break;
            }

            if (next == null)
            {
                GD.Print($"[DungeonGen] Main path stuck at step {i + 1} (room {current.Name})");
                return false;
            }
            current = next;
        }

        // 3. Branches that can keep growing (not off start or boss)
        foreach (var room in placed.ToList())
        {
            if (room.Type == RoomType.Start || room.Type == RoomType.Boss) continue;

            foreach (var dir in room.FreeExits())
            {
                if (rng.Randf() > branchChance) continue;

                bool wantItem = itemCount < MaxItemRooms && rng.Randf() < 0.5f;
                var pool = (wantItem ? ItemRooms : CombatRooms).ToList();
                var branch = TryPlace(room, dir, pool);
                if (branch != null && branch.Type == RoomType.Item) itemCount++;
            }
        }

        // 4. Close every remaining open exit with a single-exit room
        foreach (var room in placed.ToList())
        {
            foreach (var dir in room.FreeExits())
            {
                var pool = new List<PackedScene>();
                if (itemCount < MaxItemRooms) pool.AddRange(ItemRooms);
                pool.AddRange(CombatRooms);

                var deadEnd = TryPlace(room, dir, pool, deadEndOnly: true);
                if (deadEnd == null)
                {
                    GD.Print($"[DungeonGen] No dead end fits {room.Name} Exit_{dir}");
                    return false;
                }
                if (deadEnd.Type == RoomType.Item) itemCount++;
            }
        }

        return true;
    }

    Room TryPlace(Room from, Dir dir, List<PackedScene> pool,
                  bool requireExtraExit = false, bool deadEndOnly = false)
    {
        var fromExit = from.GetExit(dir);
        var needed = Opposite(dir);

        pool = pool.OrderBy(_ => rng.Randi()).ToList();

        foreach (var scene in pool)
        {
            var room = scene.Instantiate<Room>();
            AddChild(room);
            room.GlobalPosition = Vector3.Zero;

            var myExit = room.GetExit(needed);
            int exitCount = room.AllExits().Count();

            if (myExit == null
                || (requireExtraExit && exitCount < 2)
                || (deadEndOnly && exitCount != 1))
            {
                Discard(room);
                continue;
            }

            Vector3 offset = myExit.GlobalPosition - room.GlobalPosition;
            room.GlobalPosition = fromExit.GlobalPosition - offset;

            if (Overlaps(room, from))
            {
                Discard(room);
                continue;
            }

            from.Used.Add(dir);
            room.Used.Add(needed);
            placed.Add(room);
            return room;
        }

        return null;
    }

    // Warn once at startup if some open exit could never be closed
    void ValidatePool()
    {
        var all = CombatRooms.Concat(ItemRooms).ToList();

        foreach (Dir open in System.Enum.GetValues<Dir>())
        {
            var need = Opposite(open);
            bool found = false;

            foreach (var scene in all)
            {
                var r = scene.Instantiate<Room>();
                var exits = r.AllExits().ToList();
                r.Free();

                if (exits.Count == 1 && exits[0] == need) { found = true; break; }
            }

            if (!found)
                GD.PushWarning($"[DungeonGen] No room with ONLY Exit_{need}. An open Exit_{open} can never be closed.");
        }
    }

    bool Overlaps(Room candidate, Room parent)
    {
        var box = candidate.WorldBounds().Grow(-0.1f);
        foreach (var p in placed)
        {
            if (p == parent) continue;
            if (p.WorldBounds().Grow(-0.1f).Intersects(box)) return true;
        }
        return false;
    }

    void Discard(Node n) { RemoveChild(n); n.QueueFree(); }

    void Clear()
    {
        foreach (var r in placed) { RemoveChild(r); r.QueueFree(); }
        placed.Clear();
    }

    static Dir Opposite(Dir d) => d switch
    {
        Dir.N => Dir.S, Dir.S => Dir.N, Dir.E => Dir.W, _ => Dir.E
    };
}