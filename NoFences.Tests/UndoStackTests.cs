using NoFences.Model;

namespace NoFences.Tests
{
    public class UndoStackTests
    {
        [Fact]
        public void DeletedFence_ComesBack()
        {
            var fences = new List<FenceInfo> { new() { Name = "A" }, new() { Name = "B", Files = { @"C:\x.txt" }, Group = "Desk" } };
            var undo = new UndoStack();
            var b = fences[1];
            undo.Record("delete B", new[] { b });
            fences.Remove(b);

            var restored = UndoStack.Restore(undo.Pop()!, fences);

            var (info, added) = Assert.Single(restored);
            Assert.True(added);
            Assert.Equal(b.Id, info.Id);
            Assert.Equal("B", info.Name);
            Assert.Equal(@"C:\x.txt", Assert.Single(info.Files));
            Assert.Equal("Desk", info.Group);
            Assert.Equal(2, fences.Count);
        }

        [Fact]
        public void ChangedFence_GetsItsOldValuesInTheSameObject()
        {
            var a = new FenceInfo { Name = "Old", PosX = 10, Files = { "1", "2" } };
            var fences = new List<FenceInfo> { a };
            var undo = new UndoStack();
            undo.Record("rename", new[] { a });
            a.Name = "New";
            a.PosX = 500;
            a.Files.Remove("1");
            a.Folded = true;

            var (info, added) = Assert.Single(UndoStack.Restore(undo.Pop()!, fences));

            Assert.False(added);
            Assert.Same(a, info); // the window keeps its FenceInfo
            Assert.Equal("Old", a.Name);
            Assert.Equal(10, a.PosX);
            Assert.Equal(new[] { "1", "2" }, a.Files);
            Assert.False(a.Folded);
        }

        [Fact]
        public void MovedLinks_ReturnToBothFences()
        {
            var from = new FenceInfo { Name = "From", Files = { "x" } };
            var to = new FenceInfo { Name = "To" };
            var fences = new List<FenceInfo> { from, to };
            var undo = new UndoStack();
            undo.Record("move", new[] { to, from });
            to.Files.Add("x");
            from.Files.Clear();

            UndoStack.Restore(undo.Pop()!, fences);

            Assert.Equal(new[] { "x" }, from.Files);
            Assert.Empty(to.Files);
        }

        [Fact]
        public void LastStepFirst_AndLimited()
        {
            var undo = new UndoStack();
            var f = new FenceInfo();
            for (var i = 0; i < UndoStack.MaxSteps + 5; i++)
                undo.Record($"step {i}", new[] { f });
            Assert.Equal(UndoStack.MaxSteps, undo.Count);
            Assert.Equal($"step {UndoStack.MaxSteps + 4}", undo.NextDescription);
            Assert.Equal($"step {UndoStack.MaxSteps + 4}", undo.Pop()!.Description);
            Assert.Equal($"step {UndoStack.MaxSteps + 3}", undo.NextDescription);
        }

        [Fact]
        public void Pop_OnEmptyStack_IsNull()
        {
            var undo = new UndoStack();
            Assert.Null(undo.NextDescription);
            Assert.Null(undo.Pop());
        }

        [Fact]
        public void ReverseAction_IsKeptWithTheStep()
        {
            var undo = new UndoStack();
            var ran = false;
            undo.Record("rename file", new[] { new FenceInfo() }, () => ran = true);
            undo.Pop()!.Reverse!();
            Assert.True(ran);
        }
    }
}
