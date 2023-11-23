using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class CharacterBody2D : Godot.CharacterBody2D
{
	public const uint MaxJumps = 2;
	public const float Speed = 30000.0f;
	public const float JumpVelocity = -600.0f;
	private List<PhysicsContext> _recordedPhysicsContexts = new List<PhysicsContext>();
	private Vector2 _positionAtLastRollback = Vector2.Zero;
	private State _prevState = new State(
		Velocity: Vector2.Zero,
		JumpsLeft: MaxJumps
	);
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
		var input = CollectPlayerInput();
		var state = CalculateState(
			delta: fDelta,
			input: input,
			prevState: _prevState
		);

		RecordPhysicsContext(fDelta, input, _prevState);

		_prevState = state;

		Velocity = state.Velocity;
		MoveAndSlide();

		if(Input.IsActionJustPressed("rollback"))
			RollbackMovement();
		
	}
	
	private void RollbackMovement() {
		GlobalPosition = _positionAtLastRollback;

		foreach(var ctx in _recordedPhysicsContexts)
		{
			var state = CalculateState(ctx.Delta, ctx.Input, ctx.State);
			Velocity = state.Velocity;
			MoveAndSlide();
		}

		_prevState = _recordedPhysicsContexts.Last().State;
		_recordedPhysicsContexts = new List<PhysicsContext>();
		_positionAtLastRollback = GlobalPosition;
	}

	private State CalculateState(float delta, PlayerInput input, State prevState) {
		var direction = input.Direction;
		var velocity = prevState.Velocity;
		var jumpsLeft = prevState.JumpsLeft;

		if(IsOnFloor())
		{
			jumpsLeft = MaxJumps;
			velocity.Y = gravity * delta;
		} else {
			// Add the gravity.
			velocity.Y += gravity * delta;
		}

		if (input.Jump && jumpsLeft > 0)
		{
			velocity.Y = JumpVelocity;
			jumpsLeft -= 1;
		}

		// Get the input direction and handle the movement/deceleration.
		// As good practice, you should replace UI actions with custom gameplay actions.
		if (direction != Vector2.Zero)
		{
			velocity.X = direction.X * Speed * delta;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
		}

		return new State(
			Velocity: velocity,
			JumpsLeft: jumpsLeft
		);
	}

	private void RecordPhysicsContext(float delta, PlayerInput input, State state) {
		var ctx = new PhysicsContext(
			Delta: delta,
			Input: input,
			State: state
		);

		_recordedPhysicsContexts.Add(ctx);
	}

	private PlayerInput CollectPlayerInput() => new PlayerInput(
		Direction: Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down"),
		Jump: Input.IsActionJustPressed("ui_accept")
	);
}

public record PhysicsContext(
	float Delta,
	PlayerInput Input,
	State State
);

public record State(
	Vector2 Velocity,
	uint JumpsLeft
);

public record PlayerInput (
	Vector2 Direction,
	bool Jump
);