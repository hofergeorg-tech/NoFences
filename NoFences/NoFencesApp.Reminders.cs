using System.Media;
using NoFences.Model;
using NoFences.Util;

namespace NoFences
{
    /// <summary>Fires note reminders as tray notifications.</summary>
    public sealed partial class NoFencesApp
    {
        private readonly System.Windows.Forms.Timer reminderTimer = new() { Interval = 15_000 };

        private void InitReminders()
        {
            reminderTimer.Tick += (_, _) => CheckReminders();
            reminderTimer.Start();
            CheckReminders(); // reminders that came due while NoFences wasn't running
        }

        private void CheckReminders()
        {
            var now = DateTime.Now;
            foreach (var fence in Store.Config.Fences.Where(f => f.ReminderAt is DateTime at && at <= now).ToList())
            {
                fence.ReminderAt = null;
                Store.RequestSave();
                windows.FirstOrDefault(w => w.Info == fence)?.Invalidate();

                SystemSounds.Asterisk.Play();
                var summary = NoteText.Summary(fence.NoteText);
                var text = summary.Length > 0 ? $"{Strings.ReminderDue(fence.Name)}\n{summary}" : Strings.ReminderDue(fence.Name);
                // Clicking the notification brings the fences (and so the note) to the front.
                ShowBalloon(text, StartPeek, timeout: 15_000);
            }
        }

        private void DisposeReminders() => reminderTimer.Dispose();
    }
}
