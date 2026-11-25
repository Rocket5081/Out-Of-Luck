using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class CombatRoom : Room{
    [Export] public Area3D EnemyTrigger;
    [Export] public PackedScene Enemy;

    //Enemy Spawn formula
    [Export] public float BaseSpawn = 3f;
    [Export] public float ScalingFactor = 1.4f;
    [Export] public float DampeningExponent = 0.7f;
    [Export] public float Variance = 0.10f;

    private bool entered = false;

    void OnBodyEntered(Node3D body){
        if(entered || body is not Player){
            return;
        }

        GameState.NumOfRooms++;

        entered = true;
        SpawnEnemies();
    }

    void SpawnEnemies(){

        float scaledEnemies = BaseSpawn + ScalingFactor * Mathf.Pow(GameState.NumOfRooms, DampeningExponent);

        //+ or - 20% to add variance between rooms
        float randVar = (float)GD.RandRange(-Variance, Variance);

        float total = Mathf.Max(1, Mathf.RoundToInt(scaledEnemies * (1.0f + randVar)));

        GD.Print("Room Entered");
        GD.Print("Number Rooms Entered: ",  GameState.NumOfRooms);
        GD.Print("Spawned ", total, " enemies.");

    }

    void OnEnemyDied(){
        //spawn upgrade for player
    }
}