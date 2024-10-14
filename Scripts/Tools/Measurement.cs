using Godot;
using System;

[Tool]
public partial class Measurement : CsgBox3D
{
    public event Action<Vector3> OnSizeChanged;

    // Events for X, Y, Z visibility changes
    public event Action<bool> OnXLabelVisibilityChanged;
    public event Action<bool> OnYLabelVisibilityChanged;
    public event Action<bool> OnZLabelVisibilityChanged;

    // Events for font size changes
    public event Action<int> OnRefFontSizeChanged;
    public event Action<int> OnMeasurementFontSizeChanged;

    private Vector3 _previousSize;

    private bool _showXLabel = true;
    private bool _showYLabel = true;
    private bool _showZLabel = true;

    [Export]
    public bool ShowXLabel
    {
        get { return _showXLabel; }
        set
        {
            if (_showXLabel != value)
            {
                _showXLabel = value;
                OnXLabelVisibilityChanged?.Invoke(_showXLabel);
            }
        }
    }

    [Export]
    public bool ShowYLabel
    {
        get { return _showYLabel; }
        set
        {
            if (_showYLabel != value)
            {
                _showYLabel = value;
                OnYLabelVisibilityChanged?.Invoke(_showYLabel);
            }
        }
    }

    [Export]
    public bool ShowZLabel
    {
        get { return _showZLabel; }
        set
        {
            if (_showZLabel != value)
            {
                _showZLabel = value;
                OnZLabelVisibilityChanged?.Invoke(_showZLabel);
            }
        }
    }

    [ExportCategory("Reference Text")]
    [Export] private string _refText;
    
    private int _refFontSize = 50;
    [Export(PropertyHint.Range, "15, 200, 1, or_greater")]
    public int RefFontSize
    {
        get { return _refFontSize; }
        set
        {
            if (_refFontSize != value)
            {
                _refFontSize = value;
                OnRefFontSizeChanged?.Invoke(_refFontSize); // Trigger font size change event
            }
        }
    }

    private int _measurementFontSize = 50;
    [Export(PropertyHint.Range, "15, 200, 1, or_greater")]
    public int MeasurementFontSize
    {
        get { return _measurementFontSize; }
        set
        {
            if (_measurementFontSize != value)
            {
                _measurementFontSize = value;
                OnMeasurementFontSizeChanged?.Invoke(_measurementFontSize); // Trigger font size change event
            }
        }
    }

    [Export(PropertyHint.Range, "0.5, 2")] private float _refFontOffset = 0.75f;

    private Node3D _refControl;
    private Label3D _reference;
    private Label3D _yHeight;
    private Label3D _xWidth;
    private Label3D _zWidth;

    public override void _EnterTree()
    {
        if (Engine.IsEditorHint())
        {
            _refControl = GetNode<Node3D>("RefControl");
            _reference = GetNode<Label3D>("RefControl/Reference");
            _yHeight = GetNode<Label3D>("RefControl/Y-height");
            _xWidth = GetNode<Label3D>("RefControl/X-width");
            _zWidth = GetNode<Label3D>("RefControl/Z-width");

            _previousSize = Size;

            OnSizeChanged += HandleSizeChanged;
            OnXLabelVisibilityChanged += HandleXLabelVisibility;
            OnYLabelVisibilityChanged += HandleYLabelVisibility;
            OnZLabelVisibilityChanged += HandleZLabelVisibility;
            
            // Subscribe to font size change events
            OnRefFontSizeChanged += HandleRefFontSizeChanged;
            OnMeasurementFontSizeChanged += HandleMeasurementFontSizeChanged;

            HandleXLabelVisibility(_showXLabel);
            HandleYLabelVisibility(_showYLabel);
            HandleZLabelVisibility(_showZLabel);
            
            OnSizeChanged?.Invoke(Size);
            UpdatePositions();
            }
    }

    public override void _Ready()
    {
        _refControl = GetNode<Node3D>("RefControl");
        _reference = GetNode<Label3D>("RefControl/Reference");
        _yHeight = GetNode<Label3D>("RefControl/Y-height");
        _xWidth = GetNode<Label3D>("RefControl/X-width");
        _zWidth = GetNode<Label3D>("RefControl/Z-width");

        _previousSize = Size;

        OnSizeChanged += HandleSizeChanged;
        OnXLabelVisibilityChanged += HandleXLabelVisibility;
        OnYLabelVisibilityChanged += HandleYLabelVisibility;
        OnZLabelVisibilityChanged += HandleZLabelVisibility;
        
        // Subscribe to font size change events
        OnRefFontSizeChanged += HandleRefFontSizeChanged;
        OnMeasurementFontSizeChanged += HandleMeasurementFontSizeChanged;

        HandleXLabelVisibility(_showXLabel);
        HandleYLabelVisibility(_showYLabel);
        HandleZLabelVisibility(_showZLabel);
        
        OnSizeChanged?.Invoke(Size);
        UpdatePositions();
    }

    public override void _ExitTree()
    {
        OnSizeChanged -= HandleSizeChanged;
        OnXLabelVisibilityChanged -= HandleXLabelVisibility;
        OnYLabelVisibilityChanged -= HandleYLabelVisibility;
        OnZLabelVisibilityChanged -= HandleZLabelVisibility;

        OnRefFontSizeChanged -= HandleRefFontSizeChanged;
        OnMeasurementFontSizeChanged -= HandleMeasurementFontSizeChanged;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Size != _previousSize)
        {
            OnSizeChanged?.Invoke(Size);
            _previousSize = Size;
        }
    }

    private void HandleXLabelVisibility(bool show)
    {
        if (show) _xWidth.Show();
        else _xWidth.Hide();
    }

    private void HandleYLabelVisibility(bool show)
    {
        if (show) _yHeight.Show();
        else _yHeight.Hide();
    }

    private void HandleZLabelVisibility(bool show)
    {
        if (show) _zWidth.Show();
        else _zWidth.Hide();
    }

    private void HandleSizeChanged(Vector3 newSize)
    {
        UpdatePositions();
    }

    private void UpdatePositions()
    {
        _reference.Text = _refText;

        _yHeight.Text = Math.Round(Size.Y, 2).ToString() + "m";
        _xWidth.Text = Math.Round(Size.X, 2).ToString() + "m";
        _zWidth.Text = Math.Round(Size.Z, 2).ToString() + "m";

        Vector3 referencePos = _reference.Position;
        referencePos.Y = GetTransformedValue(referencePos.Y, _refFontOffset, Size.Y, Position.Y);
        _reference.Position = referencePos;

        _yHeight.Position = GetSizedTextPosition(_yHeight.Position, -0.5f, 0.5f, 0.525f, true, false, false);
        _xWidth.Position = GetSizedTextPosition(_xWidth.Position, 0.5f, -0.452f, 0.525f, false, true, false);
        _zWidth.Position = GetSizedTextPosition(_zWidth.Position, -0.5f, -0.453f, -0.525f, true, true, true);
    }

    // Handlers for font size changes
    private void HandleRefFontSizeChanged(int newSize)
    {
        _reference.FontSize = newSize;
    }

    private void HandleMeasurementFontSizeChanged(int newSize)
    {
        _yHeight.FontSize = newSize;
        _xWidth.FontSize = newSize;
        _zWidth.FontSize = newSize;
    }

    private Vector3 GetSizedTextPosition(Vector3 startPosition, float startPosX, float startPosY, float startPosZ, bool flipX, bool flipY, bool flipZ)
    {
        Vector3 sizedPosition = startPosition;

        sizedPosition.Z = GetTransformedValue(sizedPosition.Z, startPosZ, Size.Z, Position.Z, flipZ);
        sizedPosition.X = GetTransformedValue(sizedPosition.X, startPosX, Size.X, Position.X, flipX);
        sizedPosition.Y = GetTransformedValue(sizedPosition.Y, startPosY, Size.Y, Position.Y, flipY);

        return sizedPosition;
    }

    private float GetTransformedValue(float sizedValue, float startValue, float startShapeSize, float startShapePos, bool flip = false)
    {
        float transformedValue = sizedValue;

        if (startShapeSize >= 1 && startShapePos >= 0)
        {
            transformedValue = !flip ? Mathf.Abs(1 - startShapeSize) + startValue : startValue;
            transformedValue -= Mathf.Abs((startShapeSize - 1) / 2);
        }
        else if (startShapeSize < 1 && startShapePos < 0)
        {
            transformedValue = !flip ? startValue - Mathf.Abs(1 - startShapeSize) : startValue;
            transformedValue += Mathf.Abs((startShapeSize - 1) / 2);
        }
        else if (startShapeSize < 1 && startShapePos >= 0)
        {
            float halfValue = Mathf.Abs(1 - startShapeSize) / 2;
            transformedValue = startValue;
            transformedValue = !flip ? transformedValue - halfValue : transformedValue + halfValue;
        }
        else if (startShapeSize >= 1 && startShapePos < 0)
        {
            float halfValue = Mathf.Abs(1 - startShapeSize) / 2;
            transformedValue = startValue;
            transformedValue = !flip ? transformedValue + halfValue : transformedValue - halfValue;
        }

        return transformedValue;
    }
}
