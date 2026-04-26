
#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Devices.Sensors;

namespace MauiMaze;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        if (Windows.Count > 0)
        {
            return Windows[0];
        }

        return new Window(new MazePage());
    }
}

// ---------------------------------------------------------
// 1. ALGORITHM STRUCTURES
// ---------------------------------------------------------
public struct MazePoint
{
    public int x, y;
}

public struct MazeLine
{
    public MazePoint a;
    public MazePoint b;
}

// ---------------------------------------------------------
// 2. MAZE GENERATOR
// ---------------------------------------------------------
public static class MazeGenerator
{
    public static MazeLine NewLine(int x1, int y1, int x2, int y2)
    {
        MazeLine line = new MazeLine();
        line.a = new MazePoint { x = x1, y = y1 };
        line.b = new MazePoint { x = x2, y = y2 };
        return line;
    }

    private static bool CanAddLine(List<MazeLine> allLines, int px, int py, int nx, int ny)
    {
        foreach (var line in allLines)
        {
            if ((line.a.x == nx && line.a.y == ny) || (line.b.x == nx && line.b.y == ny))
                return false;
        }

        foreach (var line in allLines)
        {
            if (ny == py)
            {
                if (line.a.x == line.b.x)
                {
                    int minY = Math.Min(line.a.y, line.b.y);
                    int maxY = Math.Max(line.a.y, line.b.y);
                    if (line.a.x == nx && minY < ny && ny < maxY)
                        return false;
                }
            }
            else
            {
                if (line.a.y == line.b.y)
                {
                    int minX = Math.Min(line.a.x, line.b.x);
                    int maxX = Math.Max(line.a.x, line.b.x);
                    if (line.a.y == ny && minX < nx && nx < maxX)
                        return false;
                }
            }
        }
        return true;
    }

    public static char[,] GenerateGrid(int maxx, int maxy, int c, out int entranceY, out int exitY)
    {
        Random rnd = new Random();
        List<MazeLine> upperLines = new List<MazeLine>();
        for (int i = 0; i < maxx; i++) upperLines.Add(NewLine(i, 0, i + 1, 0));

        List<MazeLine> lowerLines = new List<MazeLine>();
        for (int i = 0; i < maxx; i++) lowerLines.Add(NewLine(i, maxy, i + 1, maxy));

        int r1 = rnd.Next(1, maxy - 2);
        for (int i = 0; i < r1; i++) upperLines.Add(NewLine(0, i, 0, i + 1));
        for (int i = r1 + 1; i < maxy; i++) lowerLines.Add(NewLine(0, i, 0, i + 1));

        int r2 = rnd.Next(1, maxy - 2);
        for (int i = 0; i < r2; i++) upperLines.Add(NewLine(maxx, i, maxx, i + 1));
        for (int i = r2 + 1; i < maxy; i++) lowerLines.Add(NewLine(maxx, i, maxx, i + 1));

        bool done = false;
        while (!done)
        {
            foreach (var lines in new[] { upperLines, lowerLines })
            {
                done = true;
                List<MazeLine> allLines = upperLines.Concat(lowerLines).ToList();

                List<MazePoint> points = new List<MazePoint>();
                foreach (var line in lines) { points.Add(line.a); points.Add(line.b); }

                var uniquePoints = new HashSet<(int, int)>();
                var pointList = new List<MazePoint>();
                foreach (var p in points)
                {
                    if (uniquePoints.Add((p.x, p.y))) pointList.Add(p);
                }
                points = pointList;

                for (int i = points.Count - 1; i > 0; i--)
                {
                    int j = rnd.Next(i + 1);
                    var temp = points[i]; points[i] = points[j]; points[j] = temp;
                }

                while (points.Count > 0)
                {
                    MazePoint p = points[0];
                    int[] dxArr = { 1, -1, 0, 0 };
                    int[] dyArr = { 0, 0, 1, -1 };
                    int[] indices = { 0, 1, 2, 3 };

                    for (int i = 3; i > 0; i--)
                    {
                        int j = rnd.Next(i + 1);
                        int temp = indices[i]; indices[i] = indices[j]; indices[j] = temp;
                    }

                    bool expanded = false;
                    foreach (int idx in indices)
                    {
                        int nx = p.x + dxArr[idx];
                        int ny = p.y + dyArr[idx];
                        if (nx < 0 || nx > maxx || ny < 0 || ny > maxy) continue;

                        if (CanAddLine(allLines, p.x, p.y, nx, ny))
                        {
                            lines.Add(NewLine(p.x, p.y, nx, ny));
                            expanded = true;
                            break;
                        }
                    }

                    if (expanded) { points.Clear(); done = false; }
                    else { points.RemoveAt(0); }
                }
            }
        }

        char[,] maze = new char[maxx * c + 1, maxy * c + 1];
        for (int x = 0; x < maxx * c + 1; x++)
            for (int y = 0; y < maxy * c + 1; y++)
                maze[x, y] = ' ';

        foreach (var line in upperLines)
        {
            int dx = line.b.x - line.a.x;
            int dy = line.b.y - line.a.y;
            for (int i = 0; i < c + 1; i++)
                maze[line.a.x * c + dx * i, line.a.y * c + dy * i] = '#';
            maze[line.a.x * c, line.a.y * c] = '#';
            maze[line.a.x * c + dx, line.a.y * c + dy] = '#';
            maze[line.b.x * c, line.b.y * c] = '#';
        }
        foreach (var line in lowerLines)
        {
            int dx = line.b.x - line.a.x;
            int dy = line.b.y - line.a.y;
            for (int i = 0; i < c + 1; i++)
                maze[line.a.x * c + dx * i, line.a.y * c + dy * i] = '#';
        }

        entranceY = r1 * c;
        exitY = r2 * c;

        return maze;
    }
}

// ---------------------------------------------------------
// 3. CUSTOM DRAWABLE FOR GRAPHICSVIEW
// ---------------------------------------------------------
public class MazeDrawable : IDrawable
{
    public char[,] Grid { get; set; } = new char[0, 0];
    public float PlayerX { get; set; }
    public float PlayerY { get; set; }
    public bool IsSolved { get; set; }

    public float CellSize { get; private set; }
    public float OffsetX { get; private set; }
    public float OffsetY { get; private set; }

    private static readonly Color LightGrayBg = Color.FromArgb("#F0F0F0");

    public bool HardMode = false;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (Grid == null || Grid.Length == 0) return;

        canvas.FillColor = Colors.Black;
        canvas.FillRectangle(dirtyRect);

        int gridW = Grid.GetLength(0);
        int gridH = Grid.GetLength(1);

        CellSize = Math.Min(dirtyRect.Width / gridW, dirtyRect.Height / gridH);
        OffsetX = (dirtyRect.Width - gridW * CellSize) / 2;
        OffsetY = (dirtyRect.Height - gridH * CellSize) / 2;

        canvas.FillColor = LightGrayBg;
        canvas.FillRectangle(OffsetX, OffsetY, gridW * CellSize, gridH * CellSize);

        canvas.FillColor = Colors.Black;
        for (int x = 0; x < gridW; x++)
        {
            for (int y = 0; y < gridH; y++)
            {
                if (Grid[x, y] == '#')
                {
                    canvas.FillRectangle(OffsetX + x * CellSize, OffsetY + y * CellSize, CellSize + 0.5f, CellSize + 0.5f);
                }
            }
        }

        canvas.FillColor = IsSolved ? Colors.Green : Colors.Blue;
        canvas.FillCircle(OffsetX + PlayerX * CellSize, OffsetY + PlayerY * CellSize, CellSize * 0.8f);
    }
}

// ---------------------------------------------------------
// 4. MAIN PAGE WITH GYRO/ACCELEROMETER CONTROL
// ---------------------------------------------------------
public class MazePage : ContentPage
{
    private GraphicsView graphicsView;
    private MazeDrawable drawable;
    private char[,] grid = new char[0, 0];

    private int gridWidth;
    private int gridHeight;
    private int entranceY;
    private int exitY;

    private bool hasInitializedSize = false;

    // Sensor & Physics Variables
    private double currentAccelX = 0;
    private double currentAccelY = 0;
    private CancellationTokenSource gameLoopCts;

    const int c = 4;
    const int nodesPerLogicalUnit = 40;

    private int currentMaxX = 8;
    private int currentMaxY = 12;

    public MazePage()
    {
        BackgroundColor = Colors.Black;

        drawable = new MazeDrawable();
        graphicsView = new GraphicsView { Drawable = drawable, BackgroundColor = Colors.Black };

        graphicsView.SizeChanged += GraphicsView_SizeChanged;

        var btnNewMaze = new Button { Text = "New Maze", Margin = new Thickness(10), BackgroundColor = Colors.DarkGray, TextColor = Colors.White, HorizontalOptions = LayoutOptions.FillAndExpand };
        var btnHardMode = new Button { Text = "Mode", Margin = new Thickness(10), BackgroundColor = Colors.DarkGreen, TextColor = Colors.White, HorizontalOptions = LayoutOptions.FillAndExpand };
        btnHardMode.Clicked += (s, e) => { drawable.HardMode = !drawable.HardMode; btnHardMode.BackgroundColor = drawable.HardMode ? Colors.DarkRed  : Colors.DarkGreen; };
        btnNewMaze.Clicked += (s, e) => GenerateNewMaze();

        var buttonRow = new HorizontalStackLayout
        {
            Spacing = 10,
            Padding = new Thickness(10),
            Children =
            {
                btnNewMaze,
                btnHardMode
            }
                };

                Content = new Grid
                {
                    BackgroundColor = Colors.Black,
                    RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Star },
                new RowDefinition { Height = GridLength.Auto }
            },
                    Children =
            {
                graphicsView,
                buttonRow
            }
        };

        Grid.SetRow(graphicsView, 0);
        Grid.SetRow(buttonRow, 1);

        // Start listening to device tilt
        if (Accelerometer.IsSupported)
        {
            Accelerometer.ReadingChanged += Accelerometer_ReadingChanged;
            Accelerometer.Start(SensorSpeed.Game);
        }
    }

    // Read the tilt data continuously
    private void Accelerometer_ReadingChanged(object sender, AccelerometerChangedEventArgs e)
    {
        currentAccelX = e.Reading.Acceleration.X;
        currentAccelY = e.Reading.Acceleration.Y;
    }

    private void GraphicsView_SizeChanged(object sender, EventArgs e)
    {
        if (!hasInitializedSize && graphicsView.Width > 0 && graphicsView.Height > 0)
        {
            hasInitializedSize = true;
            GenerateNewMaze();
        }
    }

    private void GenerateNewMaze()
    {
        if (graphicsView.Width > 0 && graphicsView.Height > 0)
        {
            currentMaxX = Math.Max(4, (int)(graphicsView.Width / nodesPerLogicalUnit));
            currentMaxY = Math.Max(4, (int)(graphicsView.Height / nodesPerLogicalUnit));
        }

        grid = MazeGenerator.GenerateGrid(currentMaxX, currentMaxY, c, out entranceY, out exitY);
        gridWidth = grid.GetLength(0);
        gridHeight = grid.GetLength(1);

        drawable.Grid = grid;
        drawable.IsSolved = false;
        drawable.PlayerX = 0.5f;
        drawable.PlayerY = entranceY + (c / 2f);

        graphicsView.Invalidate();

        // Restart the physics loop with the new maze
        StartGameLoop();
    }

    public static float dx, dy;

    private void StartGameLoop()
    {
        StopGameLoop(); // Stop existing loop if running
        gameLoopCts = new CancellationTokenSource();

        Task.Run(async () =>
        {
            while (!gameLoopCts.Token.IsCancellationRequested)
            {
                // Run at ~60 Frames Per Second
                await Task.Delay(16, gameLoopCts.Token);

                //float dx = 0, dy = 0;

                // Deadzone prevents the ball from drifting when phone is flat on a table
                const float deadzone = 0.05f; // 0.15f;

                // Speed factor (tweak this to make ball roll faster/slower)
                const float speed = 1.0f;
                const float timestep = 0.016f; // 16ms

                if (Math.Abs(currentAccelX) > deadzone)
                    dx += -(float)currentAccelX * speed * timestep;

                // Tilting phone towards you moves ball DOWN the screen, tilting away moves it UP
                if (Math.Abs(currentAccelY) > deadzone)
                    dy += (float)currentAccelY * speed * timestep;

                float maxSpeed = 20.0f;
                float friction = 0.99f;

                if (dx > maxSpeed) dx = maxSpeed;
                if (dx < -maxSpeed) dx = -maxSpeed;
                if (dy > maxSpeed) dy = maxSpeed;
                if (dy < -maxSpeed) dy = -maxSpeed;

                dx = friction * dx;
                dy = friction * dy;

                if (dx == 0 && dy == 0) continue;

                // Update UI on Main Thread
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (drawable.IsSolved || grid.Length == 0) return;

                    float newX = drawable.PlayerX + dx;
                    float newY = drawable.PlayerY + dy;

                    // Collision checks
                    bool canMoveX = !IsWall(newX, drawable.PlayerY);
                    bool canMoveY = !IsWall(drawable.PlayerX, newY);
                    bool canMoveXY = !IsWall(newX, newY);

                    if(!canMoveX) dx=0; 
                    if(!canMoveY) dy=0;

                    if (canMoveXY)
                    {
                        drawable.PlayerX = newX;
                        drawable.PlayerY = newY;
                    }
                    else if (canMoveX)
                    {
                        drawable.PlayerX = newX;
                    }
                    else if (canMoveY)
                    {
                        drawable.PlayerY = newY;
                    }

                    // Win condition
                    if (drawable.PlayerX >= gridWidth - 1)
                    {
                        CheckWinCondition();
                    }

                    // Hard Mode
                    if (drawable.HardMode && (!canMoveX || !canMoveY))
                    {
                        drawable.PlayerX = 0.5f;
                        drawable.PlayerY = entranceY + (c / 2f);
                        dx = 0;
                        dy = 0;
                    }

                    graphicsView.Invalidate();
                });
            }
        }, gameLoopCts.Token);
    }

    private void StopGameLoop()
    {
        if (gameLoopCts != null)
        {
            gameLoopCts.Cancel();
            gameLoopCts.Dispose();
            gameLoopCts = null;
        }
    }

    // Clean up battery usage when leaving app
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopGameLoop();
        if (Accelerometer.IsMonitoring)
            Accelerometer.Stop();
    }

    private async void CheckWinCondition()
    {
        drawable.IsSolved = true;
        graphicsView.Invalidate();

        await DisplayAlertAsync("Congratulations!", "You solved the maze!", "Play Again");
        GenerateNewMaze();
    }

    private bool IsWall(float x, float y)
    {
        float r = 0.4f;
        if (IsWallPoint(x, y)) return true;
        if (IsWallPoint(x - r, y)) return true;
        if (IsWallPoint(x + r, y)) return true;
        if (IsWallPoint(x, y - r)) return true;
        if (IsWallPoint(x, y + r)) return true;
        return false;
    }

    private bool IsWallPoint(float x, float y)
    {
        int ix = (int)x;
        int iy = (int)y;

        if (ix < 0 || iy < 0 || ix >= gridWidth || iy >= gridHeight)
        {
            if (ix >= gridWidth - 1 && iy >= exitY && iy <= exitY + c)
                return false;
            return true;
        }

        return grid[ix, iy] == '#';
    }
}