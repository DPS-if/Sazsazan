using Godot;

public partial class CutscenePlayer : Control
{
	// (IMPORTANTE) Mude "res://SuaFase1.tscn" para o caminho real da sua fase!
	private string _nextScenePath = "res://node_2d.tscn"; // <--- MUDE AQUI

	private VideoStreamPlayer _videoPlayer;
	private bool _isSkipping = false; // Impede clique duplo

	public override void _Ready()
	{
		// Pega o nó do player de vídeo
		_videoPlayer = GetNode<VideoStreamPlayer>("VideoPlayer");
		
		// Conecta o sinal "finished" (quando o vídeo termina)
		// à nossa função "GoToNextScene"
		_videoPlayer.Finished += GoToNextScene;
		
		// Dá o foco para esta cena para que ela possa receber inputs (para pular)
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
		
		// Para o vídeo (boa prática)
		_videoPlayer.Stop();

		// Manda o Godot carregar a próxima cena (Sua Fase 1)
		GetTree().ChangeSceneToFile(_nextScenePath);
	}
}
