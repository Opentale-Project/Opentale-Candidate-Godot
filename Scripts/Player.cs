using Godot;
using System;

public partial class Player : CharacterBody3D
{
	// Called when the node enters the scene tree for the first time.

	[Export] public float speed = 4.9f;
	[Export] public float sprintMultiplier = 1.9f;
	[Export] public float sneakMultiplier = 0.5f;
	[Export] public float jumpVelocity = 9.4f;
	[Export] public float GravityScale = 28f;
	//[Export] public float GravityScale = 0f;
	[Export] public bool InfiniteMode = false;
	[Export] public int breakInterval = 10;
	[Export] public int breakStartRepeatDelay = 20;
	[Export] public int placeInterval = 10;
	[Export] public int placeStartRepeatDelay = 20;
	private float currentSpeed = 0f;
	private float breakCooldown = 0;
	private float placeCooldown = 0;
	private Camera3D cam;
	private Vector2 look;
	private bool camLock = false;
	[Export] public World world;
	[Export] public float interactionDistance = 6f;
	[Export] public Hotbar hotbar;
	public override void _Ready()
	{
		cam = GetNode<Camera3D>("Camera3D");
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_cancel")) 
		{
			Input.MouseMode = Input.MouseModeEnum.Visible;
			camLock = true;
		}
		if (@event is InputEventMouseButton mbe && mbe.Pressed)
		{
			Input.MouseMode = Input.MouseModeEnum.Captured;
			camLock = false;
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;
		
		Vector2 input = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");

		Vector3 direction = (Transform.Basis.Z * input.Y + Transform.Basis.X * input.X);
		direction.Y = 0f;
		direction = direction.Normalized();
		
		//if (Input.IsActionJustPressed("crouch")) cam.GlobalPosition.y = cam.GlobalPosition.y - 1f;
		//if (Input.IsActionJustReleased("crouch")) cam.GlobalPosition.y = cam.GlobalPosition.y + 1f;
		
		// === Get Modifiers
		//GD.Print(Input.Get("sprint"));
		if(Input.IsActionPressed("sprint") && Input.IsActionPressed("move_forward")) currentSpeed = speed * sprintMultiplier;
		else if (Input.IsActionPressed("crouch")) currentSpeed = speed * sneakMultiplier;
		else currentSpeed = speed;
		
		// === Apply Result
		velocity.X = direction.X * currentSpeed;
		velocity.Z = direction.Z * currentSpeed;
		
		if(!IsOnFloor()) velocity.Y -= InfiniteMode ? 0.0f : GravityScale * (float)delta;
		
		if (Input.IsActionPressed("jump") && IsOnFloor() && !InfiniteMode) velocity.Y = jumpVelocity;

		Velocity = velocity;
		MoveAndSlide();

		if(!camLock)
		{
			Vector2 mouseDelta = Input.GetLastMouseVelocity() * 0.00015f;
			look += new Vector2(-mouseDelta.Y, -mouseDelta.X);
			
			look.X = Mathf.Clamp(look.X, -1.5708f, 1.5708f);

			cam.Rotation = new Vector3(look.X, 0, 0);
			Rotation = new Vector3(0, look.Y, 0);
			
			HandleBlockInteraction();
		}
	}

	private void HandleBlockInteraction()
	{
		var space = GetWorld3D().DirectSpaceState;

		var from = cam.GlobalPosition + cam.GlobalTransform.Basis.Z * -0.1f;
		var to = from + cam.GlobalTransform.Basis.Z * -interactionDistance;

		var result = new PhysicsRayQueryParameters3D()
		{
			From = from,
			To = to,
			CollideWithAreas = false,
			CollideWithBodies = true,
		};

		var hit = space.IntersectRay(result);
		
		// === Iterate BreakCooldown
		if (breakCooldown > 0) breakCooldown -=1;

		if (hit.Count > 0)
		{
			Vector3 pos = (Vector3)hit["position"];
			Vector3 normal = ((Vector3)hit["normal"]).Normalized();

			// compute exact block grid coordinates
			Vector3 breakTarget = pos - normal * 0.0001f;
			Vector3 placeTarget = pos + normal * 0.5f;

			//IsActionJustPressed to avoid holding break
			if (Input.IsActionPressed("break"))
			{
				if (breakCooldown <= 0)
				{
					// on new repeat cycle, start with some delay
					// this way the player is holding the button down
					// to choose to break on repeat, rather than
					// activating repeat on accident
					
					if (breakCooldown == -2048) breakCooldown = breakInterval + breakStartRepeatDelay;
					else breakCooldown = breakInterval;
					BreakBlock(breakTarget);
				}
			} else {
				breakCooldown = -2048;	//mark as ready for next repeat
			}
				

			if (Input.IsActionJustPressed("interact"))
				PlaceBlock(placeTarget, (byte)hotbar.SelectedItem.InternalID);
		}
	}

	private void BreakBlock(Vector3 pos)
	{
		world.BreakBlock(pos);
	}

	private void PlaceBlock(Vector3 pos, byte block)
	{
		world.PlaceBlock(pos, block);
	}
}
