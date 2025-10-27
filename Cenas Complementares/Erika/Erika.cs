using Godot;
using System;

public partial class Erika : CharacterBody2D
{
	// --- Variáveis de Movimento ---
	[Export]
	public float Speed = 200.0f;
	[Export]
	public float SprintMultiplier = 2.0f;
	[Export]
	public float JumpVelocity = -350.0f;
	
	// --- NOVAS VARIÁVEIS PARA O PULO DUPLO ---
	[Export]
	public float DoubleJumpVelocityMultiplier = 1.2f; // Pulo Duplo é 20% mais alto que o primeiro
	[Export]
	public float DoubleJumpStaminaCost = 25.0f;       // Custo de estamina do Pulo Duplo
	
	// 1 = Pulo disponível, 0 = Pulo Duplo disponível, -1 = Nenhum pulo disponível.
	public int ndepulo = 1; 

	//salve salve safadão, dps do quick time event que ela aprende o pulo duplo, essa variavel tem q ser true se n ela n pula duas vez igual tu sentado nessa cadeira ai, que Deus te abençoe.
	public bool sabepular2 = false;

	// --- Variáveis de Stats ---
	[Export]
	public float MaxHealth = 100.0f;
	[Export]
	public float MaxStamina = 100.0f;
	[Export]
	public float StaminaDrainRate = 20.0f; 
	[Export]
	public float StaminaRegenRate = 15.0f; 
	[Export]
	public float StaminaRegenDelay = 2.0f; // O delay de 2s quando acaba
	
	public float CurrentHealth { get; private set; }
	public float CurrentStamina { get; private set; }
	
	private float _staminaRegenTimer = 0.0f; // Timer interno
	
	// --- SINAL PARA AVISAR O HUD ---
	[Signal]
	public delegate void StatsChangedEventHandler(float currentHealth, float maxHealth, float currentStamina, float maxStamina);
	
	public float Gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

	public override void _Ready()
	{
		CurrentHealth = MaxHealth;
		CurrentStamina = MaxStamina;
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;

		// 1. Gravidade
		if (!IsOnFloor())
		{
			velocity.Y += Gravity * (float)delta;
		}

		// 2. Reset do Pulo (CORRIGIDO: Veio para antes do Input)
		// Se o personagem ESTÁ no chão, ele SEMPRE tem o pulo 1 disponível.
		if (IsOnFloor())
		{
			// (ndepulo = 1) significa Pulo Normal disponível
			// (ndepulo = 0) significa Pulo Duplo disponível
			// (ndepulo = -1) significa Sem pulos
			ndepulo = 1;
		}

		// 3. Pulo e Pulo Duplo (LÓGICA CORRIGIDA)
		if (Input.IsActionJustPressed("ui_accept"))
		{
			// Pulo Normal (só acontece se ndepulo == 1, que foi setado acima)
			if (ndepulo == 1 && IsOnFloor()) 
			{
				ndepulo = 0; // Prepara para o pulo duplo
				velocity.Y = JumpVelocity;
			}
			// Pulo Duplo
			// Só executa se:
			// a) ndepulo for 0 (Pulo Duplo disponível)
			// b) O delay de regeneração de estamina não estiver ativo (_staminaRegenTimer <= 0)
			// c) Tiver estamina suficiente (CurrentStamina >= DoubleJumpStaminaCost)
			else if (sabepular2 && ((ndepulo == 0 && _staminaRegenTimer <= 0 && CurrentStamina >= DoubleJumpStaminaCost) || (!IsOnFloor() && _staminaRegenTimer <= 0 && CurrentStamina >= DoubleJumpStaminaCost && ndepulo != -1)))
			{
				ndepulo = -1; // Desativa o pulo duplo
				
				// Pulo mais alto
				velocity.Y = JumpVelocity * DoubleJumpVelocityMultiplier; 
				
				// Gasta Estamina
				CurrentStamina -= DoubleJumpStaminaCost;
				
				// Trava no 0 se ficou negativo
				CurrentStamina = Mathf.Max(CurrentStamina, 0); 
				
				// (OPCIONAL: Ativar o delay se o pulo duplo zerar a estamina)
				if (CurrentStamina <= 0)
				{
					_staminaRegenTimer = StaminaRegenDelay;
				}
			}
		}
		
		// --- (LÓGICA DE ESTAMINA E CORRIDA TOTALMENTE ATUALIZADA) ---
		
		bool wantsToSprint = Input.IsActionPressed("ui_corrida");
		float currentSpeed = Speed; // Começa com a velocidade NORMAL

		// 4. Contar o timer de delay (se estiver ativo)
		if (_staminaRegenTimer > 0)
		{
			_staminaRegenTimer -= (float)delta;
		}

		// 5. Lógica de Corrida (Gastar Estamina)
		// O jogador PODE tentar correr? (Se o delay de 2s não estiver ativo)
		bool canSprint = _staminaRegenTimer <= 0;

		if (wantsToSprint && canSprint)
		{
			// Calcula o gasto ANTES de verificar
			float staminaToDrain = StaminaDrainRate * (float)delta;

			// Verifica se temos estamina SUFICIENTE para este frame
			if (CurrentStamina > staminaToDrain)
			{
				// Sim, temos. Corra.
				currentSpeed = Speed * SprintMultiplier;
				CurrentStamina -= staminaToDrain;
			}
			else
			{
				// Não temos estamina suficiente.
				// Define a estamina como 0 e ATIVA o delay de 2s
				CurrentStamina = 0;
				_staminaRegenTimer = StaminaRegenDelay;
				// Nota: currentSpeed continua sendo a 'Speed' normal. O personagem para de correr.
			}
		}
		else
		{
			// 6. Lógica de Regeneração
			// REGENERA se:
			// A. O timer de delay acabou (<= 0)
			// B. A estamina não está cheia
			// C. O JOGADOR NÃO ESTÁ SEGURANDO O BOTÃO DE CORRIDA (!wantsToSprint)
			if (_staminaRegenTimer <= 0 && CurrentStamina < MaxStamina && !wantsToSprint)
			{
				CurrentStamina += StaminaRegenRate * (float)delta;
				CurrentStamina = Mathf.Min(CurrentStamina, MaxStamina); // Trava no máximo
			}
		}
		
		// 7. Handle Movimento Esquerda/Direita
		float horizontalDirection = Input.GetAxis("ui_left", "ui_right");

		if (horizontalDirection != 0)
		{
			// Aplica a velocidade (que será 'Speed' normal ou 'Sprint Speed')
			velocity.X = horizontalDirection * currentSpeed;
		}
		else
		{
			// Desaceleração (atrito)
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
		}

		// 8. Aplica a velocidade e move o personagem
		Velocity = velocity;
		MoveAndSlide();
		
		// 9. Emitir o Sinal (sempre no final)
		EmitSignal(SignalName.StatsChanged, CurrentHealth, MaxHealth, CurrentStamina, MaxStamina);
	}
}
