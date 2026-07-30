using Godot;
using System;
using System.Linq;

public partial class Router : Node
{
    PackedScene gui = GD.Load<PackedScene>("res://godot-gui/main.tscn");
	public override void _Process(double delta)
    {
        string[] args = OS.GetCmdlineArgs();
        if (args.Contains("--service"))
        {
           // TODO: service mode, --headless is assumed.
        }
        else
        {
            GD.Print("Starting GUI");
            // otherwise, load the gui
            GetTree().ChangeSceneToPacked(gui);
        }

        
    }
}
