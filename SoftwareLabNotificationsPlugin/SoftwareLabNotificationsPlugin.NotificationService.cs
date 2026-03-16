using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace MissionPlanner.SoftwareLab.Notifications
{
    internal sealed class SoftwareLabNotificationService : INotificationService, IDisposable
    {
        private const int NotificationWidth = 270;
        private const int NotificationHeight = 135;
        private const int NotificationSpacing = 9;
        private const int NotificationPadding = 8;
        private const int NotificationLineSpacing = 2;
        private const int NotificationDurationMs = 2500;

        private static readonly Color SuccessTextColor = Color.FromArgb(26, 110, 56);
        private static readonly Color ErrorTextColor = Color.FromArgb(154, 40, 40);
        private static readonly Color DefaultBackgroundColor = Color.FromArgb(244, 239, 221);
        private static readonly Color DefaultBorderColor = Color.FromArgb(191, 184, 160);

        private readonly Func<Form> hostFormProvider;
        private readonly List<NotificationForm> activeNotifications = new List<NotificationForm>();
        private readonly object syncRoot = new object();

        public SoftwareLabNotificationService(Func<Form> hostFormProvider)
        {
            this.hostFormProvider = hostFormProvider;
        }

        public void ShowMessage(string message)
        {
            ShowMessage(message, NotificationSeverity.Info);
        }

        public void ShowMessage(string message, NotificationSeverity severity)
        {
            ShowMessage(new List<NotificationLine>
            {
                new NotificationLine(message, severity)
            });
        }

        public void ShowMessage(IReadOnlyList<NotificationLine> lines)
        {
            if (lines == null || lines.Count == 0)
                return;

            Form owner = GetOwnerForm();
            if (owner != null && owner.InvokeRequired)
            {
                owner.BeginInvoke(new Action(() => ShowMessage(lines)));
                return;
            }

            ShowMessageInternal(lines, owner);
        }

        public void Dispose()
        {
            NotificationForm[] formsToClose;

            lock (syncRoot)
            {
                formsToClose = activeNotifications.ToArray();
                activeNotifications.Clear();
            }

            foreach (NotificationForm form in formsToClose)
            {
                if (form.IsDisposed)
                    continue;

                if (form.InvokeRequired)
                    form.BeginInvoke(new Action(form.Close));
                else
                    form.Close();
            }
        }

        private void ShowMessageInternal(IReadOnlyList<NotificationLine> lines, Form owner)
        {
            NotificationTheme theme = NotificationTheme.Create(owner);
            NotificationForm form = new NotificationForm
            {
                Size = new Size(NotificationWidth, NotificationHeight),
                StartPosition = FormStartPosition.Manual,
                TopMost = true,
                ShowInTaskbar = false,
                FormBorderStyle = FormBorderStyle.None,
                BackColor = theme.BorderColor,
                Padding = new Padding(1)
            };

            NotificationView notificationView = new NotificationView(lines, theme);
            form.Controls.Add(notificationView);

            Rectangle workingArea = GetWorkingArea(owner);

            lock (syncRoot)
            {
                form.Location = GetNotificationLocation(workingArea, activeNotifications.Count);
                activeNotifications.Add(form);
            }

            Timer timer = new Timer { Interval = NotificationDurationMs };
            timer.Tick += (sender, args) =>
            {
                timer.Stop();
                form.Close();
            };

            form.FormClosed += (sender, args) =>
            {
                timer.Dispose();

                lock (syncRoot)
                {
                    activeNotifications.Remove(form);
                    RepositionActiveNotifications(GetWorkingArea(GetOwnerForm()));
                }

                form.Dispose();
            };

            timer.Start();

            if (owner != null)
                form.Show(owner);
            else
                form.Show();
        }

        private Form GetOwnerForm()
        {
            Form owner = hostFormProvider?.Invoke();
            if (owner != null)
                return owner;

            return Application.OpenForms.Count > 0 ? Application.OpenForms[0] : null;
        }

        private static Point GetNotificationLocation(Rectangle workingArea, int index)
        {
            int x = workingArea.Left + ((workingArea.Width - NotificationWidth) / 2);
            int y = workingArea.Top + ((workingArea.Height - NotificationHeight) / 2) + (index * NotificationSpacing);
            return new Point(x, y);
        }

        private static Rectangle GetWorkingArea(Form owner)
        {
            if (owner != null)
                return Screen.FromControl(owner).WorkingArea;

            return Screen.PrimaryScreen.WorkingArea;
        }

        private void RepositionActiveNotifications(Rectangle workingArea)
        {
            for (int i = 0; i < activeNotifications.Count; i++)
                activeNotifications[i].Location = GetNotificationLocation(workingArea, i);
        }

        private sealed class NotificationTheme
        {
            private NotificationTheme(Color backgroundColor, Color borderColor, Color infoTextColor, Font font)
            {
                BackgroundColor = backgroundColor;
                BorderColor = borderColor;
                InfoTextColor = infoTextColor;
                Font = font;
            }

            public Color BackgroundColor { get; }
            public Color BorderColor { get; }
            public Color InfoTextColor { get; }
            public Font Font { get; }

            public static NotificationTheme Create(Form owner)
            {
                Color baseBackColor = owner?.BackColor ?? SystemColors.Control;
                Color baseForeColor = owner?.ForeColor ?? SystemColors.ControlText;
                Font baseFont = owner?.Font ?? SystemFonts.MessageBoxFont;

                Color backgroundColor = Blend(baseBackColor, DefaultBackgroundColor, 0.6f);
                Color borderColor = Blend(baseBackColor, DefaultBorderColor, 0.5f);
                Font font = new Font(baseFont.FontFamily, Math.Max(9f, baseFont.Size - 0.5f), FontStyle.Bold);

                return new NotificationTheme(backgroundColor, borderColor, baseForeColor, font);
            }

            private static Color Blend(Color first, Color second, float ratio)
            {
                float clampedRatio = Math.Max(0f, Math.Min(1f, ratio));
                float inverseRatio = 1f - clampedRatio;

                return Color.FromArgb(
                    (int)((first.R * inverseRatio) + (second.R * clampedRatio)),
                    (int)((first.G * inverseRatio) + (second.G * clampedRatio)),
                    (int)((first.B * inverseRatio) + (second.B * clampedRatio)));
            }
        }

        private sealed class NotificationView : Control
        {
            private readonly IReadOnlyList<NotificationLine> lines;
            private readonly NotificationTheme theme;

            public NotificationView(IReadOnlyList<NotificationLine> lines, NotificationTheme theme)
            {
                this.lines = lines;
                this.theme = theme;
                DoubleBuffered = true;
                ResizeRedraw = true;
                BackColor = theme.BackgroundColor;
                ForeColor = theme.InfoTextColor;
                Font = theme.Font;
                Padding = new Padding(NotificationPadding);
                Dock = DockStyle.Fill;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                    theme.Font.Dispose();

                base.Dispose(disposing);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);

                e.Graphics.Clear(theme.BackgroundColor);

                if (lines == null || lines.Count == 0)
                    return;

                TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.WordBreak | TextFormatFlags.NoPadding;
                int availableWidth = Math.Max(0, ClientSize.Width - Padding.Horizontal);
                int totalHeight = GetTotalTextHeight(e.Graphics, availableWidth, flags);
                int currentY = Padding.Top + Math.Max(0, (ClientSize.Height - Padding.Vertical - totalHeight) / 2);

                foreach (NotificationLine line in lines)
                {
                    Size lineSize = TextRenderer.MeasureText(
                        e.Graphics,
                        line.Text,
                        Font,
                        new Size(availableWidth, int.MaxValue),
                        flags);

                    Rectangle textBounds = new Rectangle(Padding.Left, currentY, availableWidth, lineSize.Height);
                    TextRenderer.DrawText(e.Graphics, line.Text, Font, textBounds, GetTextColor(line.Severity), flags);
                    currentY += lineSize.Height + NotificationLineSpacing;
                }
            }

            private int GetTotalTextHeight(Graphics graphics, int availableWidth, TextFormatFlags flags)
            {
                int totalHeight = 0;

                for (int i = 0; i < lines.Count; i++)
                {
                    Size lineSize = TextRenderer.MeasureText(
                        graphics,
                        lines[i].Text,
                        Font,
                        new Size(availableWidth, int.MaxValue),
                        flags);

                    totalHeight += lineSize.Height;

                    if (i < lines.Count - 1)
                        totalHeight += NotificationLineSpacing;
                }

                return totalHeight;
            }

            private Color GetTextColor(NotificationSeverity severity)
            {
                switch (severity)
                {
                    case NotificationSeverity.Success:
                        return SuccessTextColor;
                    case NotificationSeverity.Error:
                        return ErrorTextColor;
                    default:
                        return theme.InfoTextColor;
                }
            }
        }

        private sealed class NotificationForm : Form
        {
            protected override bool ShowWithoutActivation => true;
        }
    }
}
