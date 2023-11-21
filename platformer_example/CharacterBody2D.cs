using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class CharacterBody2D : Godot.CharacterBody2D
{
	public const uint MaxJumps = 1;
	public const float Speed = 30000.0f;
	public const float JumpVelocity = -600.0f;
	private uint _jumpsLeft = 1;
	private List<PhysicsContext> _recordedPhysicsContexts = new List<PhysicsContext>();
	private Vector2 _positionAtLastRollback = new Vector2();
	// Get the gravity from the project settings to be synced with RigidBody nodes.
	public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

    public override void _Ready()
    {
        base._Ready();
		_positionAtLastRollback = GlobalPosition;
    }
    public override void _PhysicsProcess(double delta)
	{
		var fDelta = (float)delta;
		var ctx = RecordPhysicsContext(fDelta);

		ApplyMovement(ctx);

		if(Input.IsActionJustPressed("rollback"))
			RollbackMovement();
	}
	
	private void RollbackMovement() {
		GlobalPosition = _positionAtLastRollback;

		var firstCtx = _recordedPhysicsContexts.First();
		_jumpsLeft = firstCtx.JumpsLeft;
		Velocity = firstCtx.Velocity;

		foreach(var ctx in _recordedPhysicsContexts)
		{
			ApplyMovement(ctx);
		}
		_recordedPhysicsContexts = new List<PhysicsContext>();
		_positionAtLastRollback = GlobalPosition;
	}

	private void ApplyMovement(PhysicsContext ctx) {
		var direction = ctx.Input.Direction;
		var fDelta = ctx.Delta;
		var velocity = Velocity;

		// Add the gravity.
		velocity.Y += gravity * fDelta;

		// Handle Jump.

		if(IsOnFloor())
		{
			_jumpsLeft = MaxJumps;
		}

		if (ctx.Input.Jump && ctx.JumpsLeft > 0)
		{
			velocity.Y = JumpVelocity;
			_jumpsLeft -= 1;
		}

		// Get the input direction and handle the movement/deceleration.
		// As good practice, you should replace UI actions with custom gameplay actions.
		if (direction != Vector2.Zero)
		{
			velocity.X = direction.X * Speed * fDelta;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
		}

		Velocity = velocity;
		MoveAndSlide();
	}

	private PhysicsContext RecordPhysicsContext(float delta) {
		var ctx = new PhysicsContext(
			Delta: delta,
			Velocity: Velocity,
			JumpsLeft: _jumpsLeft,
			Input: CollectPlayerInput()
		);

		_recordedPhysicsContexts.Add(ctx);

		return ctx;
	}

	private PlayerInput CollectPlayerInput() => new PlayerInput(
		Direction: Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down"),
		Jump: Input.IsActionJustPressed("ui_accept")
	);
}

public record PhysicsContext(
	float Delta,
	Vector2 Velocity,
	uint JumpsLeft,
	PlayerInput Input
);

public record PlayerInput (
	Vector2 Direction,
	bool Jump
);