using System.Media;
using NoFences.Model;
using NoFences.Util;

namespace NoFences
{
    /// <summary>Fires note reminders as tray notifications.</summary>
    public sealed partial class NoFencesApp
    {
        // Every 5 s: timers should ring on time
        private readonly System.Windows.Forms.Timer reminderTimer = new() { Interval = 5_000 };

        private void InitReminders()
        {
            reminderTimer.Tick += Util.UiWatchdog.Named("Reminders", (_, _) => CheckReminders());
            reminderTimer.Start();
            CheckReminders(); // reminders that came due while NoFences wasn't running
        }

        private void CheckReminders()
        {
            var now = DateTime.Now;
            CheckTodos(now);
            CheckTimers(now);
            CheckBreakReminder(now);
            CheckChecklistResets(now);
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
                ShowBalloon(text, () => StartPeek(), timeout: 15_000);
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
                ShowBalloon(Strings.TodoDue(fence.Name, string.Join("\n", due.Select(i => "• " + i.Text))), () => StartPeek(), timeout: 15_000);
            }
        }

        /// <summary>Recurring checklists: a new day/week/month unticks them (weekdays: not on weekends).</summary>
        private void CheckChecklistResets(DateTime now)
        {
            foreach (var fence in Store.Config.Fences.Where(f => f.Kind == FenceKind.Note && f.NoteResetRepeat != Repeat.None && f.NoteCipher == null))
            {
                if (fence.NoteResetRepeat == Repeat.Weekdays && now.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                    continue;
                if (!NoteLists.ResetDue(fence.NoteLastReset, fence.NoteResetRepeat, now))
                    continue;
                var window = windows.FirstOrDefault(w => w.Info == fence);
                if (window != null)
                {
                    window.ResetChecklist();
                }
                else
                {
                    fence.NoteText = NoteLists.Reset(fence.NoteText);
                    fence.NoteLastReset = now;
                    Store.RequestSave();
                }
            }
        }

        private readonly BreakTracker breaks = new();

        private void CheckBreakReminder(DateTime now)
        {
            if (!breaks.Update(now, IdleTime(), TimeSpan.FromMinutes(Store.Config.BreakReminderMinutes)))
                return;
            SystemSounds.Asterisk.Play();
            var text = string.IsNullOrWhiteSpace(Store.Config.BreakReminderText) ? Strings.BreakDefaultText : Store.Config.BreakReminderText!;
            ShowBalloon(Strings.BreakReminder(Store.Config.BreakReminderMinutes, text), timeout: 15_000);
        }

        private System.Media.SoundPlayer? alarmSound;
        private System.Windows.Forms.Timer? alarmStop;

        /// <summary>Timers and alarms of all timer widgets (also hidden ones): ring and notify.</summary>
        private void CheckTimers(DateTime now)
        {
            foreach (var fence in Store.Config.Fences.Where(f => f.Kind == FenceKind.Widget && f.WidgetType == "timer"))
            {
                var set = TimerSet.Parse(fence.WidgetOption);
                var before = set.Format();
                var due = set.Due(now);
                if (set.Format() != before)
                {
                    fence.WidgetOption = set.Format();
                    Store.RequestSave();
                    windows.FirstOrDefault(w => w.Info == fence)?.Invalidate();
                }
                if (due.Count == 0)
                    continue;
                StartAlarmSound();
                var label = string.Join(", ", due.Where(d => d.Length > 0).DefaultIfEmpty(Strings.WidgetTimer));
                var timerFence = fence;
                AlarmPopup.Show(Strings.TimerRinging(label), StopAlarmSound, minutes => Snooze(timerFence, label, minutes));
            }
        }

        /// <summary>"5 min" / "10 min" in the alarm popup: rings again later.</summary>
        private void Snooze(FenceInfo fence, string label, int minutes)
        {
            StopAlarmSound();
            var set = TimerSet.Parse(fence.WidgetOption);
            set.Snooze(label, minutes, DateTime.Now);
            fence.WidgetOption = set.Format();
            Store.RequestSave();
            windows.FirstOrDefault(w => w.Info == fence)?.Invalidate();
        }

        /// <summary>The Windows alarm sound, looping for at most 30 seconds (clicking the notification stops it).</summary>
        private void StartAlarmSound()
        {
            StopAlarmSound();
            var file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Media", "Alarm01.wav");
            if (!File.Exists(file))
            {
                SystemSounds.Exclamation.Play();
                return;
            }
            try
            {
                alarmSound = new System.Media.SoundPlayer(file);
                alarmSound.PlayLooping();
                alarmStop = new System.Windows.Forms.Timer { Interval = 30_000 };
                alarmStop.Tick += (_, _) => StopAlarmSound();
                alarmStop.Start();
            }
            catch (Exception)
            {
                SystemSounds.Exclamation.Play();
            }
        }

        public void StopAlarmSound()
        {
            alarmStop?.Dispose();
            alarmStop = null;
            alarmSound?.Stop();
            alarmSound?.Dispose();
            alarmSound = null;
        }

        private void DisposeReminders()
        {
            reminderTimer.Dispose();
            StopAlarmSound();
        }
    }
}
