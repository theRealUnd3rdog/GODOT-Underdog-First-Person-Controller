using Godot;
using Godot.Collections;
using System;
using System.Linq;

public partial class CollisionChecker : Godot.Area3D
{
    public static event Action<string> OnGroupChange; // Event that's invoked whenever the collision group (tag) changes
    public string CurrentTouchingGroup;
    private Array<string> _touchingGroups = new();

    public override void _Ready()
    {
        BodyEntered += StartRepeatCall;
        AreaEntered += StartRepeatCall;
        
        OnGroupChange += DebugGroup;

        BodyExited += StopRepeatCall;
        AreaExited += StopRepeatCall;
    }

    private void DebugGroup(string group)
    {
        //GD.Print(group);
    }

    public override void _ExitTree()
    {
        BodyEntered -= StartRepeatCall;
        AreaEntered -= StartRepeatCall;

        OnGroupChange -= DebugGroup;

        BodyExited -= StopRepeatCall;
        AreaExited -= StopRepeatCall;
    }

	public static string GetGroupFromBody(Node3D body)
	{
        char[] trimArr = {'"', '[', '&', ']'};
        string group = string.Empty;

		group = body.GetGroups().ToString();
        group = group.Trim(trimArr);

        return group;
	}

    private void StartRepeatCall(Node3D body)
    {
        _touchingGroups.Add(GetGroupFromBody(body));

        // Priority
        if (_touchingGroups.Contains("Water"))
        {
            if (CurrentTouchingGroup != "Water")
            {
                CurrentTouchingGroup = "Water";
                OnGroupChange?.Invoke(CurrentTouchingGroup);
            }
        }
        else
        {
            if (CurrentTouchingGroup != _touchingGroups.LastOrDefault(string.Empty))
            {
                CurrentTouchingGroup = _touchingGroups.LastOrDefault(string.Empty);
                OnGroupChange?.Invoke(CurrentTouchingGroup);
            }
        }
    }

    private void StopRepeatCall(Node3D body)
    {
        _touchingGroups.Remove(GetGroupFromBody(body));

        if (_touchingGroups.Contains("Water"))
        {
            if (CurrentTouchingGroup != "Water")
            {
                CurrentTouchingGroup = "Water";
                OnGroupChange?.Invoke(CurrentTouchingGroup);
            }
        }
        else
        {
            if (CurrentTouchingGroup != _touchingGroups.LastOrDefault(string.Empty))
            {
                CurrentTouchingGroup = _touchingGroups.LastOrDefault(string.Empty);
                OnGroupChange?.Invoke(CurrentTouchingGroup);
            }
        }
    }
}
