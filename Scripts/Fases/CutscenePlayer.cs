using Godot;

public partial class CutscenePlayer : Control
{
	// (IMPORTANTE) Mude "res://Fase1.tscn" para o caminho real da sua fase!
	private string _nextScenePath = "res://Cenas/Fases/Ato1.tscn"; 

	private VideoStreamPlayer _videoPlayer;
	private AudioStreamPlayer _musicPlayer; // <-- (NOVA LINHA 1)
	private bool _isSkipping = false;

	public override void _Ready()
	{
		// Pega os nós
		_videoPlayer = GetNode<VideoStreamPlayer>("VideoPlayer");
		_musicPlayer = GetNode<AudioStreamPlayer>("CutsceneMusic"); // <-- (NOVA LINHA 2)
		
		// Conecta o sinal "finished" (quando o vídeo termina)
		_videoPlayer.Finished += GoToNextScene;
		
		// Permite que este Control receba foco antes de chamá-lo
		// (sem isso o Godot emite o erro "This control can't grab focus")
		FocusMode = FocusModeEnum.All;
		GrabFocus();
	}

	// Esta função é chamada para pular a cutscene
	public override void _UnhandledInput(InputEvent @event)
	{
		// Se o jogador apertar Espaço, Enter ou Esc, pula a cena
		if (@event.IsActionPressed("ui_accept") || @event.IsActionPressed("ui_cancel"))
		{
			GoToNextScene();
		}
	}

	// Esta função é chamada pelo sinal "Finished" ou pelo "_UnhandledInput"
	private void GoToNextScene()
	{
		// Se já estamos mudando de cena, não faça nada
		if (_isSkipping) return;
		_isSkipping = true;
		
		// Para o vídeo E A MÚSICA
		_videoPlayer.Stop();
		_musicPlayer.Stop(); // <-- (NOVA LINHA 3)

		// Manda carregar a próxima cena (Sua Fase 1) com transição suave
		SceneTransitions.Instance.ChangeScene(_nextScenePath);
	}
}
