using Godot;
using System; // Necessário para Mathf

public partial class HUD : CanvasLayer
{
	// --- Referências para AMBAS as barras animadas ---
	private AnimatedSprite2D _healthBarAnimated; 
	private AnimatedSprite2D _staminaBarAnimated; 

	public override void _Ready()
	{
		// --- Pega o nó da Vida ---
		// (IMPORTANTE: Mude "HealthBarAnimated" para o nome EXATO do seu nó)
		_healthBarAnimated = GetNode<AnimatedSprite2D>("HealthBarAnimated");
		
		// (IMPORTANTE: Mude "level_vida" para o nome da animação da VIDA)
		_healthBarAnimated.Animation = "level_vida"; // Exemplo de nome
		_healthBarAnimated.Stop();
		
		// --- Pega o nó da Estamina ---
		// (IMPORTANTE: Mude "StaminaBarAnimated" para o nome EXATO do seu nó)
		_staminaBarAnimated = GetNode<AnimatedSprite2D>("StaminaBarAnimated");
		
		// (IMPORTANTE: Mude "level_estamina" para o nome da animação da ESTAMINA)
		_staminaBarAnimated.Animation = "level_estamina"; // Exemplo de nome
		_staminaBarAnimated.Stop(); 
	}

	// Sinal da Erika (roda a cada frame)
	public void OnPlayerStatsChanged(float currentHealth, float maxHealth, float currentStamina, float maxStamina)
	{
		// --- (NOVA LÓGICA DE VIDA - GENÉRICA) ---
		UpdateHealthBar(currentHealth, maxHealth);
		
		// --- (LÓGICA DE ESTAMINA - 36 FRAMES) ---
		UpdateStaminaBar(currentStamina, maxStamina);
	}
	
	// --- FUNÇÃO PARA ATUALIZAR A VIDA ---
	private void UpdateHealthBar(float currentHealth, float maxHealth)
	{
		// Funciona com qualquer valor de vida máxima:
		// Frame 0 = cheio, último frame = vazio.
		if (maxHealth <= 0)
		{
			return;
		}

		float percent = Mathf.Clamp(currentHealth / maxHealth, 0f, 1f);
		int totalFrames = _healthBarAnimated.SpriteFrames.GetFrameCount(_healthBarAnimated.Animation);

		// Teto: garante que qualquer vida parcial "apague" o próximo segmento
		int frameIndex = (int)Math.Ceiling((1.0f - percent) * (totalFrames - 1));
		_healthBarAnimated.Frame = Mathf.Clamp(frameIndex, 0, totalFrames - 1);
	}
	
	// --- FUNÇÃO PARA ATUALIZAR A ESTAMINA ---
	private void UpdateStaminaBar(float currentValue, float maxValue)
	{
		// 1. Calcula a porcentagem (0.0 a 1.0)
		float percent = currentValue / maxValue;

		// 2. Pega o número total de frames da animação de estamina
		int totalFrames = _staminaBarAnimated.SpriteFrames.GetFrameCount(_staminaBarAnimated.Animation);

		// 3. Converte a porcentagem para o índice do frame (ex: 0 a 35)
		int frameIndex = (int)Mathf.Round(percent * (totalFrames - 1));
		
		// 4. INVERTE o índice (porque Frame 0 = Cheio, Último Frame = Vazio)
		int invertedFrameIndex = (totalFrames - 1) - frameIndex;
		
		// 5. Garante que o frame não saia dos limites (segurança)
		invertedFrameIndex = Mathf.Clamp(invertedFrameIndex, 0, totalFrames - 1);

		// 6. Define o frame da estamina!
		_staminaBarAnimated.Frame = invertedFrameIndex;
	}
}
