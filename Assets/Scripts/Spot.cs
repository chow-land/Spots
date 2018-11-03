using UnityEngine;

/*
 * Written by Christian Howland
 * Sept. 2018
 * 
 * howland.christian@gmail.com
 */

public interface IBoardItem
{
    int XPosition { get; }
    int YPosition { get; }
    bool IsConnected { get; set; }
    Vector2 TargetPosition { set; }
}

public class Spot : MonoBehaviour, IBoardItem
{
    public int XPosition
    {
        get { return Mathf.RoundToInt(transform.localPosition.x); }
    }

    public int YPosition
    {
        get { return Mathf.RoundToInt(transform.localPosition.y); }
    }

    public bool IsConnected { get; set; }

    private Vector2 _startingPosition;
    private Vector2 _targetPosition;

    public Vector2 TargetPosition
    { 
        set { _targetPosition = value; }
    }

    private bool PositionNeedsToUpdate
    {
        get { return transform.localPosition != (Vector3)_targetPosition; }
    }

    private BoardManager board;

    void Awake()
    {
        // NOTE: _targetPosition must be set in Awake() rather than Start(), to ensure it's set 
        // early enough (before any subsequent updates to the target position), for animation purposes.
        _targetPosition = transform.localPosition;
    }

    void Start()
    {
        board = FindObjectOfType<BoardManager>();
        UpdateNameWithPosition();
    }

    void Update()
    {
        if (PositionNeedsToUpdate && !_isAnimating)
        {
            StartAnimating();
        }
    }

    void FixedUpdate()
    {
        if (_isAnimating)
        {
            ContinueAnimating();
        }
    }

    private void OnMouseDown()
    {
        if (!board.HasConnectedSpots && !_isAnimating)
        {
            board.ConnectSpot(this);
        }
    }

    private void OnMouseOver()
    {
        if (board.CanConnectSpot(this))
        {
            board.ConnectSpot(this);
        }
        else if (board.CanDisconnectSpot(this))
        {
            board.DisconnectLastSpot();
        }
    }

    private bool _isAnimating = false;

    private float _timeStartedAnimating;

    private void StartAnimating()
    {
        _startingPosition = transform.localPosition;
        _timeStartedAnimating = Time.time;
        _isAnimating = true;
    }

    private void ContinueAnimating()
    {
        var timeElapsed = Time.time - _timeStartedAnimating;
        var percentageComplete = timeElapsed / board.ColumnRefillAnimationLength;

        transform.localPosition = Vector2.Lerp(_startingPosition, _targetPosition, percentageComplete);

        if (percentageComplete >= 1.0f)
        {
            _isAnimating = false;
            UpdateNameWithPosition();
        }
    }

    private void UpdateNameWithPosition()
    {
        name = "Spot @ (" + transform.localPosition.x + ", " + transform.localPosition.y + ")";
    }
}