using Godot;
using System;
using System.Security.Cryptography.X509Certificates;

public partial class VideoManager : VideoStreamPlayer
{
	[Export] private Godot.Collections.Array<VideoStream> _videos;

	[Signal] public delegate void OnVideoFinishedEventHandler(int index);

	public override void _Ready()
	{
		Finished += VideoFinished;
		PlayVideo(0); 
	}

	public void VideoFinished()
	{
		int currentIndex = _videos.IndexOf(Stream);
		EmitSignal(SignalName.OnVideoFinished, currentIndex);
	}
	
	public void PlayVideo(int index)
	{
		if (index < 0 || index >= _videos.Count) { return; }

		Stream = _videos[index];
		Play();
	}
}
