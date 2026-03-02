using Godot;
using System;

public partial class Main : Control
{
	public override void _Ready()
    {
        GetTree().Root.SizeChanged += OnSizeChanged;
        OnSizeChanged();
    }

	public override void _Process(double delta)
    {
        
    }

    public void OnSizeChanged()
    {
        if (GetWindow().Size.X < 900 || GetWindow().Size.Y < 500)
        {
            GetNode<Control>("TooSmall").Visible = true;
            GetNode<Control>("Main").Visible = false;
        }
        else
        {
            GetNode<Control>("TooSmall").Visible = false;
            GetNode<Control>("Main").Visible = true;
        }
    }
}
