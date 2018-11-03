using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

/*
 * Written by Christian Howland
 * Sept. 2018
 * 
 * howland.christian@gmail.com
 */

public class BoardManager : MonoBehaviour
{
    [Header("Game Settings")]
    public int BoardWidth;
    public int BoardHeight;
    public float ColumnRefillAnimationLength;

    [Header("Spot Prefabs")]
    public GameObject[] SpotPrefabs;

    private Spot[,] _allSpots;

    private List<Spot> _connectedSpots = new List<Spot>();

    public bool HasConnectedSpots
    {
        get { return _connectedSpots.Count > 0; }
    }

    private Spot LastConnectedSpot
    {
        get { return _connectedSpots.Count > 0 ? _connectedSpots[_connectedSpots.Count - 1] : null; }
    }

    private bool _squareIsConnected = false;

    private List<LineRenderer> _lines = new List<LineRenderer>();

    void Start()
    {
        transform.position = new Vector3(0, 0, 0);
        FillBoardWithSpots();
        CenterCameraOnBoard();
    }

    void Update()
    {
        HandleTouches();
    }

    private void HandleTouches()
    {
        // TODO: Dynamically handle inputs for different environments (e.g. touch input)
        if (Input.GetMouseButtonUp(0))
        {
            if (_connectedSpots.Count > 1)
            {
                ClearConnectedSpots();
            }
            else
            {
                DisconnectAllSpots();
            }

            _lines.ForEach(r => r.Reset());
            _lines.Clear();
        } 
    }

    private void FillBoardWithSpots()
    {
        if (BoardWidth < 4 || BoardHeight < 4 || BoardWidth > 15 || BoardHeight > 8)
        {
            print("EROR: Board dimensions cannot be smaller than 4x4 or larger than 15x8");
            return;
        }

        if (SpotPrefabs == null || SpotPrefabs.Any(s => s == null))
        {
            print("EROR: Spot prefabs must be assigned!");
            return;
        }

        _allSpots = new Spot[BoardWidth, BoardHeight];

        for (var x = 0; x < BoardWidth; x++)
        {
            for (var y = 0; y < BoardHeight; y++)
            {
                CreateSpotAt(x, y);
            }
        }
    }

    private void CenterCameraOnBoard()
    {
        // NOTE: Camera must have a Z value of <= -1 in order for the LineRenderers to be visible.
        var boardCenter = new Vector3((float)BoardWidth / 2, (float)BoardHeight / 2, -1);
        var absoluteBoardCenter = transform.position + boardCenter;
        Camera.main.transform.position = absoluteBoardCenter;
    }


    private void CreateSpotAt(int finalX, int finalY, int? animateFromY = null)
    {
        var spawnPositionY = animateFromY ?? finalY;

        var boardPosition = transform.position;
        var spawnPosition = boardPosition + new Vector3(finalX, spawnPositionY, 0);

        var randomSpotPrefab = SpotPrefabs.RandomItem();

        var newSpot = Instantiate(randomSpotPrefab, spawnPosition, Quaternion.identity);
        newSpot.transform.parent = this.transform;
        _allSpots[finalX, finalY] = newSpot.GetComponent<Spot>();

        if (animateFromY != null)
        {
            _allSpots[finalX, finalY].TargetPosition = new Vector3(finalX, finalY, 0);
        }
    }

    public bool CanConnectSpot(Spot newSpot)
    {
        if (CompletesSquare(newSpot))
        {
            return true;
        }

        var nothingToConnectTo = _connectedSpots.Count == 0;
        var alreadyConnected = _connectedSpots.Any(s => s.GetInstanceID() == newSpot.GetInstanceID());

        if (nothingToConnectTo || alreadyConnected)
        {
            return false;
        }
       
        var colorsMatch = LastConnectedSpot.CompareTag(newSpot.tag);

        var xDistance = Math.Abs(newSpot.XPosition - LastConnectedSpot.XPosition);
        var yDistance = Math.Abs(newSpot.YPosition - LastConnectedSpot.YPosition);

        var isValidHorizontalConnection = xDistance == 1 && yDistance == 0;
        var isValidVerticalConnection = yDistance == 1 && xDistance == 0;

        var isValidConnection = isValidHorizontalConnection || isValidVerticalConnection;

        return colorsMatch && isValidConnection;
    }

    public void ConnectSpot(Spot newSpot)
    {
        if (CompletesSquare(newSpot))
        {
            _squareIsConnected = true;;
        }

        _connectedSpots.Add(newSpot);

        var hasPreviousSpots = _connectedSpots.Count > 1;

        if (hasPreviousSpots)
        {
            var previousSpot = _connectedSpots[_connectedSpots.Count - 2];
            DrawLineBetween(previousSpot, newSpot);
        }

        newSpot.IsConnected = true;
    }

    private void DrawLineBetween(Spot spot1, Spot spot2)
    {
        var line = spot2.GetComponent<LineRenderer>();
        if (line == null)
        {
            line = spot2.gameObject.AddComponent<LineRenderer>();
            _lines.Add(line);
        }

        var spotColor = spot1.GetComponent<SpriteRenderer>().color;
        line.startColor = spotColor;
        line.endColor = spotColor;
        line.material = new Material(Shader.Find("Particles/Additive"));
        line.startWidth = .2f;

        line.positionCount = 2;
        line.SetPosition(0, spot1.transform.position);
        line.SetPosition(1, spot2.transform.position);
    }

    private bool CompletesSquare(Spot newSpot)
    {
        if (_connectedSpots.Count < 4)
        {
            return false;
        }

        // NOTE: To check for a square, we can lean on the fact that in a matrix, a 2x2 square
        // will always share a beginning and end node, with 3 nodes inbetween.
        var isSpotAlreadyConnected = _connectedSpots.Any(s => s.GetInstanceID() == newSpot.GetInstanceID());
        var indexOfAlreadyLinkedSpot = _connectedSpots.FindIndex(s => s.GetInstanceID() == newSpot.GetInstanceID());

        return indexOfAlreadyLinkedSpot == _connectedSpots.Count - 4;
    }

    public bool CanDisconnectSpot(Spot spotToCheck)
    {
        if (_connectedSpots.Count < 2)
        {
            return false;
        }

        var lastChainedSpot = _connectedSpots[_connectedSpots.Count - 2];

        return lastChainedSpot.GetInstanceID() == spotToCheck.GetInstanceID();
    }

    public void DisconnectLastSpot()
    {
        var lastConnectedSpot = _connectedSpots.Last();

        lastConnectedSpot.IsConnected = false;
        _connectedSpots.RemoveAt(_connectedSpots.Count - 1);

        var line = lastConnectedSpot.GetComponent<LineRenderer>();
        if (line != null)
        {
            line.Reset();
        }
    }

    private void DisconnectAllSpots()
    {
        if (_connectedSpots.Count < 1)
        {
            return;
        }

        _squareIsConnected = false;
        _connectedSpots.ForEach(s => s.IsConnected = false);
        _connectedSpots.Clear();
        _lines.ForEach(line => {
            line.Reset();
        });
        _lines.Clear();
    }

    private void ClearConnectedSpots()
    {
        if (_connectedSpots.Count < 1)
        {
            return;
        }

        if (_squareIsConnected)
        {
            ClearAllSpotsWithTag(_connectedSpots.FirstOrDefault().tag);
        }
        else
        {
            _connectedSpots.ForEach(s =>
            {
                _allSpots[s.XPosition, s.YPosition] = null;
                Destroy(s.gameObject);
            });

            _connectedSpots.Clear();
        }

        _lines.Clear();

        _squareIsConnected = false;

        CollapseColumns();
    }

    private void ClearAllSpotsWithTag(string tagName)
    {
        foreach (var spot in _allSpots)
        {
            if (spot.CompareTag(tagName))
            {
                _allSpots[spot.XPosition, spot.YPosition] = null;
                Destroy(spot.gameObject);
            }
        }

        _connectedSpots.Clear();
        CollapseColumns();
    }

    private void CollapseColumns()
    {
        for (var x = 0; x < BoardWidth; x++)
        {
            var emptySpaces = 0;
            for (var y = 0; y < BoardHeight; y++)
            {
                var currentSpot = _allSpots[x, y];

                if (currentSpot == null)
                {
                    emptySpaces++;
                }
                else if (emptySpaces > 0)
                {
                    var shiftDownY = currentSpot.YPosition - emptySpaces;
                    currentSpot.TargetPosition = new Vector2(currentSpot.XPosition, shiftDownY);

                    _allSpots[x, shiftDownY] = currentSpot;
                    _allSpots[x, y] = null;
                }

                var reachedTheTopRow = y == BoardHeight - 1;
                if (reachedTheTopRow)
                {
                    RefillColumn(x, emptySpaces);
                }
            }
        }
    }

    private void RefillColumn(int column, int numberOfSpotsToAdd) 
    {
        for (var spacesToFill = numberOfSpotsToAdd; spacesToFill > 0; spacesToFill--)
        {
            var finalY = BoardHeight - spacesToFill;
            var animateFromY = finalY + numberOfSpotsToAdd;
            CreateSpotAt(column, finalY, animateFromY);
        }
    }
}

public static class ArrayExtensions
{
    public static T RandomItem<T>(this T[] array)
    {
        return array[UnityEngine.Random.Range(0, array.Length)];
    }
}

public static class LineRendererExtensions
{
    public static void Reset(this LineRenderer lineRenderer)
    {
        lineRenderer.positionCount = 0;
    }
}