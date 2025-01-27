using Godot;
using System;

public partial class Audio : Node3D
{
    private RandomNumberGenerator _rng = new RandomNumberGenerator();
    
    public void PlayAnyAudio(NodePath player, AudioStream stream)
	{
		AudioStreamPlayer3D streamPlayer = (AudioStreamPlayer3D)GetNode(player);
		
		streamPlayer.Stream = stream;

		streamPlayer.PitchScale = _rng.RandfRange(0.9f, 1.1f);
		streamPlayer.Play();
	}
}
