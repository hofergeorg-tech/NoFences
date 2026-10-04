using System.Text.Json;

namespace NoFences.Model
{
    /// <summary>
    /// Ctrl+Z for fences: before a change, the affected fences are copied (as JSON); undoing puts
    /// the copies back. A deleted fence comes back, a moved or renamed one returns to its old place
    /// and name, removed or moved links reappear. Steps that also changed something on disk (a
    /// renamed file) carry an action that reverses it.
    /// </summary>
    public sealed class UndoStack
    {
        public const int MaxSteps = 30;

        public sealed class Step
        {
            public Step(string description, IReadOnlyList<string> fences, Action? reverse)
            {
                Description = description;
                Fences = fences;
                Reverse = reverse;
            }

            public string Description { get; }

            /// <summary>The fences as they were before (JSON).</summary>
            public IReadOnlyList<string> Fences { get; }

            /// <summary>Reverses what happened outside the config (e.g. renames a file back); may throw.</summary>
            public Action? Reverse { get; }
        }

        private readonly LinkedList<Step> steps = new();

        public int Count => steps.Count;

        /// <summary>What Ctrl+Z would undo, or null.</summary>
        public string? NextDescription => steps.Last?.Value.Description;

        public static string Snapshot(FenceInfo info) => JsonSerializer.Serialize(info, FenceStore.JsonOptions);

        /// <summary>Remembers the fences as they are now, before they change.</summary>
        public void Record(string description, IEnumerable<FenceInfo> fences, Action? reverse = null) =>
            Record(description, fences.Select(Snapshot).ToList(), reverse);

        /// <summary>Remembers fences copied earlier (e.g. when a drag started).</summary>
        public void Record(string description, IReadOnlyList<string> snapshots, Action? reverse = null)
        {
            if (snapshots.Count == 0 && reverse == null)
                return;
            steps.AddLast(new Step(description, snapshots, reverse));
            while (steps.Count > MaxSteps)
                steps.RemoveFirst();
        }

        public Step? Pop()
        {
            var last = steps.Last?.Value;
            if (last != null)
                steps.RemoveLast();
            return last;
        }

        public void Clear() => steps.Clear();

        /// <summary>
        /// Puts the step's fences back into <paramref name="fences"/>: existing ones (same id) get their old
        /// values, deleted ones are added again. Returns the restored fences and whether each was re-added.
        /// </summary>
        public static List<(FenceInfo Info, bool Added)> Restore(Step step, List<FenceInfo> fences)
        {
            var result = new List<(FenceInfo, bool)>();
            foreach (var json in step.Fences)
            {
                var old = JsonSerializer.Deserialize<FenceInfo>(json, FenceStore.JsonOptions);
                if (old == null)
                    continue;
                var existing = fences.FirstOrDefault(f => f.Id == old.Id);
                if (existing != null)
                {
                    CopyInto(old, existing);
                    result.Add((existing, false));
                }
                else
                {
                    fences.Add(old);
                    result.Add((old, true));
                }
            }
            return result;
        }

        /// <summary>Copies every stored property, so windows holding <paramref name="to"/> keep working.</summary>
        public static void CopyInto(FenceInfo from, FenceInfo to)
        {
            foreach (var p in typeof(FenceInfo).GetProperties().Where(p => p.CanRead && p.CanWrite))
                p.SetValue(to, p.GetValue(from));
        }
    }
}
