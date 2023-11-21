using Godot;
using System;

public partial class CharacterBody2D : Godot.CharacterBody2D
{
	public const float Speed = 30000.0f;
	public const float JumpVelocity = -600.0f;

	// Get the gravity from the project settings to be synced with RigidBody nodes.
	public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

	public override void _PhysicsProcess(double delta)
	{
		var fDelta = (float)delta;
		var input = CollectPlayerInput();
		var physicsContext = new PhysicsContext(
			Delta: fDelta,
			Input: input
		);

		ApplyMovement(physicsContext);
	}

	private void ApplyMovement(PhysicsContext ctx) {
		var direction = ctx.Input.Direction;
		var fDelta = ctx.Delta;
		var velocity = Velocity;
		// Add the gravity.
		if (!IsOnFloor())
			velocity.Y += gravity * fDelta;

		// Handle Jump.
		if (ctx.Input.Jump && IsOnFloor())
			velocity.Y = JumpVelocity;

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

	private PlayerInput CollectPlayerInput() => new PlayerInput(
		Direction: Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down"),
		Jump: Input.IsActionJustPressed("ui_accept")
	);
}

public record PhysicsContext(
	float Delta,
	PlayerInput Input
);

public record PlayerInput (
	Vector2 Direction,
	bool Jump
);