using Godot;
using System;
using System.Collections.Generic;

[Tool]
public partial class Ladder : Node
{
    public event Action<Vector3, Vector3> OnSizeChanged;

    private UndoRedo _undoRedo = new UndoRedo();
    
    private Vector3 _previousSize;
    private Vector3 _previousPosition;

    // Nodes
    private CollisionShape3D _ladderCollider;
    private CsgBox3D _leftPillar;
    private CsgBox3D _rightPillar;

    private List<Node3D> _bars = new List<Node3D>();  // Store bar references
    private PackedScene _barScene;  // The scene for the bar

    // Exposed property for bar spacing
    private float _barSpacing = 1.0f;
    [Export]
    public float BarSpacing
    {
        get { return _barSpacing; }
        set
        {
            if (!Mathf.IsEqualApprox(_barSpacing, value))
            {
                _barSpacing = value;
                UpdateBars(GetCollisionShapeSize().Y, GetCollisionShapeSize().X); // Update bars when spacing changes
            }
        }
    }

    public override void _EnterTree()
    {
        if (Engine.IsEditorHint())
        {
            _ladderCollider = GetNode<CollisionShape3D>("LadderArea/LadderCollider");
            _leftPillar = GetNode<CsgBox3D>("L_Pillar");
            _rightPillar = GetNode<CsgBox3D>("R_Pillar");

            _barScene = (PackedScene)GD.Load("res://Scenes/Tools/ladder_handle.tscn");

            _previousSize = GetCollisionShapeSize();
            _previousPosition = _ladderCollider.Position;
            OnSizeChanged += HandleSizeChanged;

            OnSizeChanged?.Invoke(_previousSize, _previousPosition);
        }
    }

    public override void _Ready()
    {
        _ladderCollider = GetNode<CollisionShape3D>("LadderArea/LadderCollider");
        _leftPillar = GetNode<CsgBox3D>("L_Pillar");
        _rightPillar = GetNode<CsgBox3D>("R_Pillar");

        _barScene = (PackedScene)GD.Load("res://Scenes/Tools/ladder_handle.tscn");

        _previousSize = GetCollisionShapeSize();
        _previousPosition = _ladderCollider.Position;
        OnSizeChanged += HandleSizeChanged;

        OnSizeChanged?.Invoke(_previousSize, _previousPosition);
    }

    public override void _ExitTree()
    {
        OnSizeChanged -= HandleSizeChanged;
    }

    // Change pillar size
    private void HandleSizeChanged(Vector3 size, Vector3 nodePos)
    {
        if (Engine.IsEditorHint())  // Only apply this in the editor
        {
            _undoRedo.CreateAction("Resize and reposition pillars");

            // Record the current state for undo
            _undoRedo.AddDoProperty(_leftPillar, "size", new Vector3(_leftPillar.Size.X, size.Y, _leftPillar.Size.Z));
            _undoRedo.AddDoProperty(_leftPillar, "position", new Vector3(nodePos.X + (size.X / 2), nodePos.Y, nodePos.Z));

            _undoRedo.AddDoProperty(_rightPillar, "size", new Vector3(_rightPillar.Size.X, size.Y, _rightPillar.Size.Z));
            _undoRedo.AddDoProperty(_rightPillar, "position", new Vector3(nodePos.X - (size.X / 2), nodePos.Y, nodePos.Z));

            // Record the old state for redo
            _undoRedo.AddUndoProperty(_leftPillar, "size", _leftPillar.Size);
            _undoRedo.AddUndoProperty(_leftPillar, "position", _leftPillar.Position);

            _undoRedo.AddUndoProperty(_rightPillar, "size", _rightPillar.Size);
            _undoRedo.AddUndoProperty(_rightPillar, "position", _rightPillar.Position);

            _undoRedo.CommitAction();  // Commit the action to the undo/redo stack

            UpdateBars(size.Y, size.X);  // Now update the bars
        }
        else
        {
            // Game logic for resizing and repositioning (without UndoRedo) in runtime
            _leftPillar.Size = new Vector3(_leftPillar.Size.X, size.Y, _leftPillar.Size.Z);
            _leftPillar.Position = new Vector3(nodePos.X + (size.X / 2), nodePos.Y, nodePos.Z);

            _rightPillar.Size = new Vector3(_rightPillar.Size.X, size.Y, _rightPillar.Size.Z);
            _rightPillar.Position = new Vector3(nodePos.X - (size.X / 2), nodePos.Y, nodePos.Z);

            UpdateBars(size.Y, size.X);  // Update the bars
        }
    }

    private void UpdateBars(float height, float width)
    {
        int requiredBars = Mathf.FloorToInt(height / _barSpacing);

        // Add new bars if needed
        while (_bars.Count < requiredBars)
        {
            Node3D newBar = (Node3D)_barScene.Instantiate();
            AddChild(newBar);
            _bars.Add(newBar);

            float barYPosition = _bars.Count * _barSpacing;
            newBar.Position = new Vector3(_ladderCollider.Position.X, 
                barYPosition - (GetCollisionShapeSize().Y / 2) + _ladderCollider.Position.Y, _ladderCollider.Position.Z);

            CsgCylinder3D cylinderShape = newBar as CsgCylinder3D;
            cylinderShape.Height = width;
        }

        // Remove extra bars if needed
        while (_bars.Count > requiredBars)
        {
            Node3D lastBar = _bars[_bars.Count - 1];
            RemoveChild(lastBar);
            lastBar.QueueFree();
            _bars.RemoveAt(_bars.Count - 1);
        }

        // Update existing bars' positions
        for (int i = 0; i < _bars.Count; i++)
        {
            Node3D bar = _bars[i];
            float barYPosition = (i + 1) * _barSpacing;
            bar.Position = new Vector3(_ladderCollider.Position.X, 
                barYPosition - (GetCollisionShapeSize().Y / 2) + _ladderCollider.Position.Y, _ladderCollider.Position.Z);

            CsgCylinder3D cylinderShape = bar as CsgCylinder3D;
            cylinderShape.Height = width;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector3 currentSize = GetCollisionShapeSize();
        Vector3 currentPosition = _ladderCollider.Position;

        // Check if size or position has changed
        if (currentSize != _previousSize || currentPosition != _previousPosition)
        {
            _previousSize = currentSize;
            _previousPosition = currentPosition;

            // Trigger the event when size or position changes
            OnSizeChanged?.Invoke(currentSize, currentPosition);
        }
    }

    private Vector3 GetCollisionShapeSize()
    {
        BoxShape3D boxShape = _ladderCollider?.Shape as BoxShape3D;

        if (boxShape != null)
        {
            return boxShape.Size;
        }

        // Handle other shapes here if needed
        return Vector3.Zero;
    }
}
