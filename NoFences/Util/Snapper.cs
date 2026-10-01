namespace NoFences.Util
{
    /// <summary>
    /// Snapping while moving/resizing: edges jump to screen edges and to the edges of other fences
    /// (with a small gap) when they come within a few pixels. Pure functions, unit-tested.
    /// </summary>
    public static class Snapper
    {
        /// <summary>Offset to apply to a moving rectangle so its nearest edges line up with a target.</summary>
        public static Point SnapMove(Rectangle moving, IReadOnlyCollection<Rectangle> fences, IReadOnlyCollection<Rectangle> screens, int threshold, int gap)
        {
            var dx = Best(new[] { moving.Left, moving.Right }, XCandidates(moving, fences, screens, gap), threshold);
            var dy = Best(new[] { moving.Top, moving.Bottom }, YCandidates(moving, fences, screens, gap), threshold);
            return new Point(dx, dy);
        }

        /// <summary>Snapped position for one edge being dragged during a resize.</summary>
        public static int SnapEdge(int edge, bool horizontal, Rectangle moving, IReadOnlyCollection<Rectangle> fences, IReadOnlyCollection<Rectangle> screens, int threshold, int gap)
        {
            var candidates = horizontal ? XCandidates(moving, fences, screens, gap) : YCandidates(moving, fences, screens, gap);
            return edge + Best(new[] { edge }, candidates, threshold);
        }

        private static IEnumerable<int> XCandidates(Rectangle moving, IEnumerable<Rectangle> fences, IEnumerable<Rectangle> screens, int gap)
        {
            foreach (var s in screens)
            {
                yield return s.Left;
                yield return s.Right;
            }
            // Only fences that are vertically near enough to matter
            foreach (var f in fences.Where(f => f.Bottom + gap * 4 >= moving.Top && f.Top - gap * 4 <= moving.Bottom))
            {
                yield return f.Left;
                yield return f.Right;
                yield return f.Left - gap;
                yield return f.Right + gap;
            }
        }

        private static IEnumerable<int> YCandidates(Rectangle moving, IEnumerable<Rectangle> fences, IEnumerable<Rectangle> screens, int gap)
        {
            foreach (var s in screens)
            {
                yield return s.Top;
                yield return s.Bottom;
            }
            foreach (var f in fences.Where(f => f.Right + gap * 4 >= moving.Left && f.Left - gap * 4 <= moving.Right))
            {
                yield return f.Top;
                yield return f.Bottom;
                yield return f.Top - gap;
                yield return f.Bottom + gap;
            }
        }

        /// <summary>Smallest correction (within the threshold) that puts one of the edges on a candidate; 0 if none.</summary>
        private static int Best(int[] edges, IEnumerable<int> candidates, int threshold)
        {
            var best = 0;
            var bestDistance = threshold + 1;
            foreach (var c in candidates)
            {
                foreach (var e in edges)
                {
                    var d = c - e;
                    if (Math.Abs(d) < bestDistance)
                    {
                        best = d;
                        bestDistance = Math.Abs(d);
                    }
                }
            }
            return bestDistance <= threshold ? best : 0;
        }
    }
}
