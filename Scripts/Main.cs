using Godot;
using System;
using VoxelEngine.Scripts;

public partial class Main : Node3D
{
	public override void _Ready()
	{
		var chunk = new Chunk();
		AddChild(chunk);
	}
}
