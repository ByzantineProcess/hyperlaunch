using Godot;
using System;

public partial class TooSmall : Control
{
	public override void _Ready()
    {
        GetTree().Root.SizeChanged += OnSizeChanged;
    }

    public void OnSizeChanged()
    {
        GD.Print("Window size changed: " + GetWindow().Size);
        if (GetWindow().Size.X >= 900 && GetWindow().Size.Y >= 500)
        {
            GetTree().ChangeSceneToFile("res://godot-gui/Main.tscn");
        }
    }
}
