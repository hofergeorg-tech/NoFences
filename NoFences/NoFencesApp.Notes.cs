using NoFences.Model;
using NoFences.Util;

namespace NoFences
{
    /// <summary>Note templates: built-in ones (shopping list, week, meeting, packing list, daily routine) and own ones.</summary>
    public sealed partial class NoFencesApp
    {
        /// <summary>The built-in templates in the UI language: name, text, and whether it unticks itself daily.</summary>
        private static IEnumerable<(string Name, string Text, Repeat Reset)> BuiltInTemplates() => new[]
        {
            (Strings.TemplateShoppingName, Strings.TemplateShoppingText, Repeat.None),
            (Strings.TemplateWeekName, Strings.TemplateWeekText, Repeat.None),
            (Strings.TemplateMeetingName, Strings.TemplateMeetingText, Repeat.None),
            (Strings.TemplatePackingName, Strings.TemplatePackingText, Repeat.None),
            (Strings.TemplateRoutineName, Strings.TemplateRoutineText, Repeat.Daily)
        };

        /// <summary>"Note from template ▸" with the built-in and own templates.</summary>
        private ToolStripMenuItem NoteTemplateMenu()
        {
            var menu = new ToolStripMenuItem(Strings.NoteFromTemplate);
            foreach (var (name, text, reset) in BuiltInTemplates())
                menu.DropDownItems.Add(name, null, (_, _) => CreateNoteFromTemplate(name, text, reset));
            var own = Store.Config.NoteTemplates;
            if (own.Count > 0)
            {
                menu.DropDownItems.Add(new ToolStripSeparator());
                foreach (var template in own.OrderBy(t => t.Name, StringComparer.CurrentCultureIgnoreCase))
                {
                    var t = template;
                    menu.DropDownItems.Add(t.Name, null, (_, _) => CreateNoteFromTemplate(t.Name, t.Text, Repeat.None));
                }
                var remove = new ToolStripMenuItem(Strings.TemplateRemove);
                foreach (var template in own.OrderBy(t => t.Name, StringComparer.CurrentCultureIgnoreCase))
                {
                    var t = template;
                    remove.DropDownItems.Add(t.Name, null, (_, _) =>
                    {
                        Store.Config.NoteTemplates.Remove(t);
                        Store.RequestSave();
                    });
                }
                menu.DropDownItems.Add(remove);
            }
            return menu;
        }

        internal void CreateNoteFromTemplate(string name, string text, Repeat reset)
        {
            AddFence(new FenceInfo
            {
                Name = name,
                Kind = FenceKind.Note,
                Theme = "postit",
                Width = 280,
                Height = 300,
                TitleHeight = 30,
                NoteText = text,
                NoteResetRepeat = reset,
                NoteLastReset = reset == Repeat.None ? null : DateTime.Now
            });
        }

        /// <summary>Saves a note's text as an own template (a template with the same name is replaced).</summary>
        public void SaveNoteTemplate(string name, string text)
        {
            Store.Config.NoteTemplates.RemoveAll(t => t.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase));
            Store.Config.NoteTemplates.Add(new NoteTemplate { Name = name, Text = text });
            Store.RequestSave();
            ShowBalloon(Strings.TemplateSaved(name));
        }
    }
}
