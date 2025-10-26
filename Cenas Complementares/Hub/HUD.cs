using Godot;

public partial class HUD : CanvasLayer
{
	private TextureProgressBar _healthBar;
	private TextureProgressBar _staminaBar;

	public override void _Ready()
	{
		_healthBar = GetNode<TextureProgressBar>("HealthBar");
		
		// Verifique se o nome "staminaBar" está correto aqui
		_staminaBar = GetNode<TextureProgressBar>("StaminaBar"); 
	}

	// Esta é a função que recebe o sinal da Erika
	public void OnPlayerStatsChanged(float currentHealth, float maxHealth, float currentStamina, float maxStamina)
	{
		
		// Atualiza a vida
		_healthBar.MaxValue = maxHealth;
		_healthBar.Value = currentHealth;
		
		// --- A SOLUÇÃO ESTÁ AQUI ---
		// Garante que o valor máximo da barra é o mesmo da estamina máxima da Erika
		_staminaBar.MaxValue = maxStamina; 
		
		// Atualiza o valor atual da barra
		_staminaBar.Value = currentStamina;
	}
}
