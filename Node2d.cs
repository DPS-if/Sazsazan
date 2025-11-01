using Godot;

public partial class Ato1 : Node2D // (Ou "Node", dependendo do seu nó raiz)
{
	private AudioStreamPlayer _musicPlayer;
	private bool _musicHasStarted = false;

	public override void _Ready()
	{
		// Pega o nó do player de música
		_musicPlayer = GetNode<AudioStreamPlayer>("MusicPlayer");
	}

	// Esta função foi conectada pelo Godot (Ação 3)
	private void _on_ato_1_music_trigger_body_entered(Node2D body)
	{
		// 1. Verifica se foi a Erika que entrou
		// 2. Verifica se a música JÁ NÃO começou (para não tocar de novo)
		if (body is Erika && !_musicHasStarted)
		{
			// Toca a música e marca que ela já começou
			PlayLevelMusic();
			_musicHasStarted = true;
			
			// (Opcional) Desativa a área para ela nunca mais ser ativada
			GetNode<Area2D>("Ato1MusicTrigger").Monitoring = false;
		}
	}

	// --- FUNÇÕES PARA A CUTSCENE CONTROLAR A MÚSICA ---

	/// <summary>
	/// Toca a música da fase (só se não estiver tocando).
	/// </summary>
	public void PlayLevelMusic()
	{
		if (!_musicPlayer.Playing)
		{
			_musicPlayer.Play();
		}
	}

	/// <summary>
	/// PARA a música da fase (para a cutscene).
	/// </summary>
	public void StopLevelMusic()
	{
		_musicPlayer.Stop();
	}
}
