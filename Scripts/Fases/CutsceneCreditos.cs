using Godot;

public partial class CutsceneCreditos : Control
{
	private string _nextScenePath = "res://Cenas/UI/title_screen.tscn";

	private VideoStreamPlayer _videoPlayer;
	private AudioStreamPlayer _musicPlayer;
	private bool _isSkipping = false;

	public override void _Ready()
	{
		_videoPlayer = GetNode<VideoStreamPlayer>("VideoPlayer");
		_musicPlayer = GetNode<AudioStreamPlayer>("CutsceneMusic");

		_videoPlayer.Finished += GoToNextScene;

		FocusMode = FocusModeEnum.All;
		GrabFocus();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_accept") || @event.IsActionPressed("ui_cancel"))
		{
			GoToNextScene();
		}
	}

	private void GoToNextScene()
	{
		if (_isSkipping) return;
		_isSkipping = true;

		_videoPlayer.Stop();
		_musicPlayer.Stop();

		SceneTransitions.Instance.ChangeScene(_nextScenePath);
	}
}