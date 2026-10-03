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
            CheckTodos(now);
            foreach (var fence in Store.Config.Fences.Where(f => f.ReminderAt is DateTime at && at <= now).ToList())
            {
                // Repeating reminders move on to their next time
                fence.ReminderAt = Recurrence.Next(fence.ReminderAt!.Value, fence.ReminderRepeat, now);
                Store.RequestSave();
                windows.FirstOrDefault(w => w.Info == fence)?.Invalidate();

                SystemSounds.Asterisk.Play();
                var summary = NoteText.Summary(fence.NoteText);
                var text = summary.Length > 0 ? $"{Strings.ReminderDue(fence.Name)}\n{summary}" : Strings.ReminderDue(fence.Name);
                // Clicking the notification brings the fences (and so the note) to the front.
                ShowBalloon(text, StartPeek, timeout: 15_000);
            }
        }

        /// <summary>To-dos in to-do widgets that came due: one notification each (also while the widget is hidden).</summary>
        private void CheckTodos(DateTime now)
        {
            foreach (var fence in Store.Config.Fences.Where(f => f.Kind == FenceKind.Widget && f.WidgetType == "todo"))
            {
                var items = TodoList.Parse(fence.WidgetOption);
                var due = items.Where(i => !i.Done && !i.Notified && i.Due is DateTime d && d <= now).ToList();
                if (due.Count == 0)
                    continue;
                foreach (var item in due)
                    item.Notified = true;
                fence.WidgetOption = TodoList.Format(items);
                Store.RequestSave();
                windows.FirstOrDefault(w => w.Info == fence)?.Invalidate();
                SystemSounds.Asterisk.Play();
                ShowBalloon(Strings.TodoDue(fence.Name, string.Join("\n", due.Select(i => "• " + i.Text))), StartPeek, timeout: 15_000);
            }
        }

        private void DisposeReminders() => reminderTimer.Dispose();
    }
}
