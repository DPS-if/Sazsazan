using Godot;
using System;

public partial class CharacterBody2d : CharacterBody2D
{
	// Mudei de 'const' para '[Export]' para que possamos mudá-la
	// e também para que ela apareça no Inspetor do Godot.
	[Export]
	public float Speed = 200.0f;
	[Export]
	public float SprintMultiplier = 2.0f; // Multiplicador da corrida
	[Export]
	public float JumpVelocity = -350.0f;

	// Pega a gravidade das configurações do projeto
	public float Gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;

		// 1. Adiciona a gravidade (se não estiver no chão)
		if (!IsOnFloor())
		{
			// Usamos a gravidade das configurações do projeto
			velocity.Y += Gravity * (float)delta;
		}

		// 2. Handle Jump (Pulo)
		// (Você pode querer mudar "ui_accept" para sua própria ação "pular")
		if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
		{
			velocity.Y = JumpVelocity;
		}

		// 3. Handle Corrida (Sprint) 💡
		float currentSpeed = Speed; // Começa com a velocidade normal
		
		// Verifica se a ação "ui_corrida" (Shift) está SENDO PRESSIONADA
		if (Input.IsActionPressed("ui_corrida"))
		{
			// Multiplica a velocidade normal pelo multiplicador
			currentSpeed = Speed * SprintMultiplier;
		}

		// 4. Handle Movimento Esquerda/Direita
		// Usamos GetAxis para jogos de plataforma (retorna -1, 0, ou 1)
		float horizontalDirection = Input.GetAxis("ui_left", "ui_right");

		if (horizontalDirection != 0)
		{
			// Aplica a velocidade (normal OU de corrida)
			velocity.X = horizontalDirection * currentSpeed;
		}
		else
		{
			// Desaceleração (atrito)
			// Usamos a velocidade base para desacelerar
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
		}

		// 5. Aplica a velocidade e move o personagem
		Velocity = velocity;
		MoveAndSlide();
	}
}
