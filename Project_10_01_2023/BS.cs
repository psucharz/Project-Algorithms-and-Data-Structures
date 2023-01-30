using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Project_10_01_2023
{
    internal class BS
    {
        /// <summary>
        /// Using a beam search algorithm, calculates the Hamiltonian cycle of an undirected complete graph with smallest length.
        /// </summary>
        /// <param name="points">Collection of points in the graph.</param>
        /// <param name="beamWidth">Size of beam for each search cycle. Cannot be bigger or equal to circle collection size.</param>
        /// <returns>List of points in a sequence that is the shortest cycle connecting all points, and length of the cycle</returns>
        public static void BeamSearch(List<Point> points, int beamWidth)
        {
            if (points == null || points.Count == 0 || beamWidth <= 0 || beamWidth >= points.Count)
                return;

            List<Path> beams = new List<Path>(beamWidth);
            //setting city of origin
            beams.Add(new Path(points[0]));
            //searching for {beamWidth} closest points of each patch at this level
            while (beams[0].Points.Count() < points.Count)
            {
                List<(Path, Point)> nextPaths = new List<(Path, Point)>(beamWidth);
                foreach (Path beam in beams)
                    foreach (Point point in points)
                        if (!beam.Points.Contains(point))
                            nextPaths.Add((beam, point));

                //selecting {beamWidth) points that would form shortest paths when added
                nextPaths = nextPaths.OrderBy(x => x.Item1.Distance + Distance(x.Item1.Points.Last(), x.Item2)).Take(beamWidth).ToList();

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
                Points = new List<Point>(path.Points.Count + 1);
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


        public static double Distance(Point point1, Point point2)
        {
            return Math.Sqrt(Math.Pow(point1.X - point2.X, 2) + Math.Pow(point1.Y - point2.Y, 2));
        }
    }
}
