using Godot;
using System;

public partial class Main : Control
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
    {
        GetTree().Root.SizeChanged += OnSizeChanged;
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
    {
        
    }

    public void OnSizeChanged()
    {
        GD.Print("Window size changed: " + GetWindow().Size);
        if (GetWindow().Size.X < 900 || GetWindow().Size.Y < 500)
        {
            GetTree().ChangeSceneToFile("res://godot-gui/too_small.tscn");
        }
    }
}
