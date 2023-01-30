using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Color = System.Windows.Media.Color;
using Point = System.Drawing.Point;

namespace Project_10_01_2023
{
    public partial class MainWindow : Window
    {
        private List<DrawingPoint> cities;  //list of cit points that can be drawn on panels
        private List<List<Arrow>> arrowLists;     //list of arrows connecting cities at each algorithm level
        private List<Arrow> solutionArrows;     //list of arrows connecting cities at each algorithm level
        private Random random = new Random(22618);

        private DispatcherTimer animationTimer;
        private int currentArrowListID = 0;

        public MainWindow()
        {
            InitializeComponent();
            cities = new List<DrawingPoint>();
            arrowLists = new List<List<Arrow>>();
            solutionArrows = new List<Arrow>();

            animationTimer = new DispatcherTimer();
            animationTimer.Interval = TimeSpan.FromSeconds(2);
            animationTimer.Tick += AnimationTimer_Tick;
        }

        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            if (currentArrowListID > arrowLists.Count)
            {
                ClearCanvasArrows();
                currentArrowListID = 0;
            }
            else if (currentArrowListID == arrowLists.Count)
            {
                foreach (var arrow in solutionArrows)
                    VisCanvas.Children.Add(arrow);
                VisCanvas.UpdateLayout();
                currentArrowListID++;
            }
            else
            {
                foreach (var arrow in arrowLists[currentArrowListID])
                    VisCanvas.Children.Add(arrow);
                VisCanvas.UpdateLayout();
                currentArrowListID++;
            }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (cities.Count > 0 && arrowLists.Count > 0)
            {
                animationTimer.Start();
                StartButton.Background = Brushes.LightGreen;
            }
            else
            {
                MessageBox.Show("Solution hasn't been computed yet");
                StartButton.Background = Brushes.GhostWhite;
            }
            PauseButton.Background = Brushes.GhostWhite;
            StopButton.Background = Brushes.GhostWhite;
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            animationTimer.Stop();
            StartButton.Background = Brushes.GhostWhite;
            PauseButton.Background = Brushes.LightSalmon;
            StopButton.Background = Brushes.GhostWhite;
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            animationTimer.Stop();
            ClearCanvasArrows();
            currentArrowListID = 0;
            StartButton.Background = Brushes.GhostWhite;
            PauseButton.Background = Brushes.GhostWhite;
            StopButton.Background = Brushes.LightSteelBlue;
        }

        private void ClearCanvasArrows()
        {
            foreach (var arrowList in arrowLists)
                foreach (var arrow in arrowList)
                    VisCanvas.Children.Remove(arrow);
            foreach (var arrow in solutionArrows)
                VisCanvas.Children.Remove(arrow);
        }

        private void GenCitiesButton_Click(object sender, RoutedEventArgs e)
        {
            //clearing possible previous states
            StopButton_Click(sender, e);
            VisCanvas.Children.Clear();
            cities.Clear();
            arrowLists.Clear();
            solutionArrows.Clear();
            StartButton.Background = Brushes.GhostWhite;
            PauseButton.Background = Brushes.GhostWhite;
            StopButton.Background = Brushes.GhostWhite;

            //generating cities that would fit in visualization canvas with padding
            for (int i = 0; i < (int)CityCountSlider.Value; i++)
            {
                Point p = new Point(random.Next((int)VisCanvas.ActualWidth - 20) + 10, random.Next((int)VisCanvas.ActualHeight - 20) + 10);
                DrawingPoint dp = new DrawingPoint(p);
                cities.Add(dp);
                dp.DrawOnCanvas(VisCanvas);
            }

            var citiesXY = cities.Select(dp => new Point(dp.point.X, dp.point.Y)).ToList();
            BeamSearch(citiesXY, (int)BeamWidthSlider.Value, arrowLists, solutionArrows);
        }

        private void CityCountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (BeamWidthSlider != null)
                BeamWidthSlider.Maximum = CityCountSlider.Value - 1;
        }

        public static Color NextColor()
        {
            Random random = new Random();
            //skipping white colors because canvas has white background, skipping black because points are marked with black
            return Color.FromRgb((byte)(random.Next(220)+20), (byte)(random.Next(220)+20), (byte)(random.Next(220)+20));
        }

        public static void BeamSearch(List<Point> points, int beamWidth, List<List<Arrow>> arrowLists, List<Arrow> solutionArrows)
        {
            if (points == null || points.Count == 0 || beamWidth <= 0 || beamWidth >= points.Count)
                return;

            List<Path> beams = new List<Path>(beamWidth);
            Color color;
            //setting city of origin
            beams.Add(new Path(points[0]));
            //searching for {beamWidth} closest points of each patch at this level
            while (beams[0].Points.Count()<points.Count)
            {
                List<(Path, Point)> nextPaths = new List<(Path, Point)>(beamWidth);
                foreach (Path beam in beams)
                    foreach (Point point in points)
                        if (!beam.Points.Contains(point))
                            nextPaths.Add((beam, point));

                //selecting {beamWidth) points that would form shortest paths when added
                nextPaths = nextPaths.OrderBy(x => x.Item1.Distance + Distance(x.Item1.Points.Last(), x.Item2)).Take(beamWidth).ToList();

                //parsing discovered paths at this level to list of arrows for visualization
                arrowLists.Add(new List<Arrow>());
                color = NextColor();
                foreach ((Path, Point) nextPath in nextPaths)
                {
                    arrowLists.Last().Add(new Arrow()
                    {
                        X1 = nextPath.Item1.Points.Last().X,
                        Y1 = nextPath.Item1.Points.Last().Y,
                        X2 = nextPath.Item2.X,
                        Y2 = nextPath.Item2.Y,
                        HeadWidth = 10,
                        HeadHeight = 10,
                        Stroke = new SolidColorBrush(color),
                        StrokeThickness = 3
                    });
                }

                //passing to the next algorithm level, paths corresponding to selected {beamWidth} points
                beams.Clear();
                foreach ((Path, Point) nextPath in nextPaths)
                {
                    Path path = new Path(nextPath.Item1);
                    path.AddPoint(nextPath.Item2);
                    beams.Add(path);
                }
            }

            //closing paths into cycles and selecting a single shortest one
            beams.ForEach(x => x.CreateCycle());
            var solution = beams.OrderBy(x => x.Distance).First();
            color = NextColor();
            arrowLists.Add(new List<Arrow>() {
                new Arrow()
                {
                    X1 = solution.Points.Last().X,
                    Y1 = solution.Points.Last().Y,
                    X2 = solution.Points.First().X,
                    Y2 = solution.Points.First().Y,
                    HeadWidth = 10,
                    HeadHeight = 10,
                    Stroke = new SolidColorBrush(color),
                    StrokeThickness = 3
                } });

            //saving final cycle answer
            for(int i=0;i<solution.Points.Count;i++)
                solutionArrows.Add(new Arrow()
                {
                    X1 = solution.Points[i].X,
                    Y1 = solution.Points[i].Y,
                    X2 = solution.Points[(i+1)% solution.Points.Count].X,
                    Y2 = solution.Points[(i + 1) % solution.Points.Count].Y,
                    HeadWidth = 10,
                    HeadHeight = 10,
                    Stroke = Brushes.Black,
                    StrokeThickness = 5
                });
        }

        public class Path
        {
            public List<Point> Points { get; }
            private double _distance;
            public double Distance { get => _distance; }

            public Path()
            {
                Points = new List<Point>();
                _distance = 0;
            }

            public Path(int capacity)
            {
                Points = new List<Point>(capacity);
                _distance = 0;
            }

            public Path(Point point)
            {
                Points = new List<Point> { point };
                _distance = 0;
            }

            public Path(Path path)
            {
                Points = new List<Point>(path.Points.Count+1);
                foreach (var point in path.Points)
                    Points.Add(point);
                _distance = path.Distance;
            }

            public void AddPoint(Point point)
            {
                Points.Add(point);
                _distance += Distance(Points[Points.Count - 2], Points[Points.Count - 1]);
            }

            public void CreateCycle()
            {
                _distance += Distance(Points.First(), Points.Last());
            }
        }

        public class DrawingPoint
        {
            public Point point;
            public Ellipse circle;

            public DrawingPoint(Point coordinates)
            {
                this.point = coordinates;
                this.circle = new Ellipse()
                {
                    Width = 10,
                    Height = 10,
                    Fill = Brushes.Black
                };
            }

            public void DrawOnCanvas(Canvas canvas)
            {
                Canvas.SetLeft(circle, point.X - circle.Width / 2);
                Canvas.SetTop(circle, point.Y - circle.Height / 2);

                canvas.Children.Add(circle);
            }
        }

        public static double Distance(Point point1, Point point2)
        {
            return Math.Sqrt(Math.Pow(point1.X - point2.X, 2) + Math.Pow(point1.Y - point2.Y, 2));
        }
    }
}
