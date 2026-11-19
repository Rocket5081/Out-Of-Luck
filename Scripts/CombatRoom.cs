using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class CombatRoom : Room{
    [Export] public Area3D EnemyTrigger;
    [Export] public PackedScene Enemy;


    void OnBodyEntered(Node3D body){

    }

    void SpawnEnemies(){

    }

    void OnEnemyDied(){
        
    }
}