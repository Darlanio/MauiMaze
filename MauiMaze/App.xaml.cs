#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

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

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (Grid == null || Grid.Length == 0) return;

        // 1. Fill ENTIRE screen with Black (creates the black outside border)
        canvas.FillColor = Colors.Black;
        canvas.FillRectangle(dirtyRect);

        int gridW = Grid.GetLength(0);
        int gridH = Grid.GetLength(1);

        CellSize = Math.Min(dirtyRect.Width / gridW, dirtyRect.Height / gridH);
        OffsetX = (dirtyRect.Width - gridW * CellSize) / 2;
        OffsetY = (dirtyRect.Height - gridH * CellSize) / 2;

        // 2. Draw a solid Light Gray block where the maze boundaries are
        canvas.FillColor = LightGrayBg;
        canvas.FillRectangle(OffsetX, OffsetY, gridW * CellSize, gridH * CellSize);

        // 3. Draw Black walls on top of the gray block 
        // (Added +0.5f to width/height to prevent microscopic anti-aliasing gaps)
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

        // 4. Draw Player
        canvas.FillColor = IsSolved ? Colors.Green : Colors.Blue;
        canvas.FillCircle(OffsetX + PlayerX * CellSize, OffsetY + PlayerY * CellSize, CellSize * 0.8f);
    }
}

// ---------------------------------------------------------
// 4. MAIN PAGE WITH TOUCH INTERACTION
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

    private bool isDragging;
    private float lastTouchX;
    private float lastTouchY;
    private bool hasInitializedSize = false;

    const int c = 4;
    const int nodesPerLogicalUnit = 40;

    private int currentMaxX = 8;
    private int currentMaxY = 12;

    public MazePage()
    {
        // Set the page background to black so no white flashes during load
        BackgroundColor = Colors.Black;

        drawable = new MazeDrawable();
        // Set GraphicsView background to black
        graphicsView = new GraphicsView { Drawable = drawable, BackgroundColor = Colors.Black };

        graphicsView.StartInteraction += OnTouchStart;
        graphicsView.DragInteraction += OnTouchMove;
        graphicsView.EndInteraction += OnTouchEnd;

        graphicsView.SizeChanged += GraphicsView_SizeChanged;

        var btnNewMaze = new Button { Text = "Generate New Maze", Margin = new Thickness(10), BackgroundColor = Colors.DarkGray, TextColor = Colors.White };
        Grid.SetRow(btnNewMaze, 1);
        btnNewMaze.Clicked += (s, e) => GenerateNewMaze();

        Content = new Grid
        {
            BackgroundColor = Colors.Black,
            RowDefinitions = { new RowDefinition { Height = GridLength.Star }, new RowDefinition { Height = GridLength.Auto } },
            Children = { graphicsView, btnNewMaze }
        };
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
    }

    private void OnTouchStart(object sender, TouchEventArgs e)
    {
        var touch = e.Touches[0];
        float mazeX = (touch.X - drawable.OffsetX) / drawable.CellSize;
        float mazeY = (touch.Y - drawable.OffsetY) / drawable.CellSize;

        float distance = Math.Abs(mazeX - drawable.PlayerX) + Math.Abs(mazeY - drawable.PlayerY);
        if (distance < c * 1.5f)
        {
            isDragging = true;
            lastTouchX = touch.X;
            lastTouchY = touch.Y;
        }
    }

    private void OnTouchMove(object sender, TouchEventArgs e)
    {
        if (!isDragging || drawable.IsSolved || grid.Length == 0) return;

        var touch = e.Touches[0];
        float dx = touch.X - lastTouchX;
        float dy = touch.Y - lastTouchY;

        float mazeDx = dx / drawable.CellSize;
        float mazeDy = dy / drawable.CellSize;

        float newX = drawable.PlayerX + mazeDx;
        float newY = drawable.PlayerY + mazeDy;

        bool canMoveX = !IsWall(newX, drawable.PlayerY);
        bool canMoveY = !IsWall(drawable.PlayerX, newY);
        bool canMoveXY = !IsWall(newX, newY);

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

        lastTouchX = touch.X;
        lastTouchY = touch.Y;

        if (drawable.PlayerX >= gridWidth - 1)
        {
            CheckWinCondition();
        }

        graphicsView.Invalidate();
    }

    private void OnTouchEnd(object sender, TouchEventArgs e)
    {
        isDragging = false;
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