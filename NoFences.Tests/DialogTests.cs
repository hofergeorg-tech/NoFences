using NoFences.Model;

namespace NoFences.Tests
{
    public class DialogTests
    {
        /// <summary>WinForms dialogs need an STA thread; run the body on one and rethrow its exception.</summary>
        private static void OnSta(Action body)
        {
            Exception? error = null;
            var thread = new Thread(() =>
            {
                try { body(); }
                catch (Exception e) { error = e; }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            if (error != null)
                throw new Exception(error.Message, error);
        }

        // Regression: the settings of a widget fence crashed (no "Widget" entry in the type list).
        [Fact]
        public void SettingsDialog_OpensForEveryFenceKind()
        {
            OnSta(() =>
            {
                foreach (var kind in Enum.GetValues<FenceKind>())
                {
                    var info = new FenceInfo { Kind = kind, WidgetType = kind == FenceKind.Widget ? "clock" : null };
                    using var dialog = new FenceSettingsDialog(info);
                    dialog.ApplyTo(info);
                    Assert.Equal(kind, info.Kind);
                }
            });
        }
    }
}
