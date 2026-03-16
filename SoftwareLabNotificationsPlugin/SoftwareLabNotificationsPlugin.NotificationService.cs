using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace MissionPlanner.SoftwareLab.Notifications
{
    internal sealed class SoftwareLabNotificationService : IConfigurableNotificationService, IDisposable
    {
        private const int NotificationWidth = 320;
        private const int NotificationHeight = 160;
        private const int NotificationSpacing = 9;
        private const int NotificationPadding = 14;
        private const int NotificationLineSpacing = 4;
        private const int ManualHeaderHeight = 34;
        private const int CloseButtonSize = 28;

        private static readonly Color SuccessTextColor = Color.FromArgb(124, 208, 142);
        private static readonly Color ErrorTextColor = Color.FromArgb(255, 135, 135);
        private static readonly Color DefaultBackgroundColor = Color.FromArgb(66, 72, 79);
        private static readonly Color DefaultBorderColor = Color.FromArgb(97, 104, 114);
        private static readonly Color DefaultTextColor = Color.FromArgb(245, 247, 250);
        private static readonly Color CloseButtonTextColor = Color.FromArgb(224, 228, 234);
        private static readonly Color CloseButtonHoverColor = Color.FromArgb(86, 94, 104);
        private static readonly Color CloseButtonPressedColor = Color.FromArgb(104, 112, 122);

        private readonly Func<Form> hostFormProvider;
        private readonly List<NotificationForm> activeNotifications = new List<NotificationForm>();
        private readonly List<NotificationForm> openForms = new List<NotificationForm>();
        private readonly object syncRoot = new object();

        public SoftwareLabNotificationService(Func<Form> hostFormProvider)
        {
            this.hostFormProvider = hostFormProvider;
        }

        public void ShowMessage(string message)
        {
            ShowMessage(message, NotificationSeverity.Info, NotificationDisplayOptions.Default);
        }

        public void ShowMessage(string message, NotificationDisplayOptions options)
        {
            ShowMessage(message, NotificationSeverity.Info, options);
        }

        public void ShowMessage(string message, NotificationSeverity severity)
        {
            ShowMessage(message, severity, NotificationDisplayOptions.Default);
        }

        public void ShowMessage(string message, NotificationSeverity severity, NotificationDisplayOptions options)
        {
            ShowMessage(
                new List<NotificationLine>
                {
                    new NotificationLine(message, severity)
                },
                options);
        }

        public void ShowMessage(IReadOnlyList<NotificationLine> lines)
        {
            ShowMessage(lines, NotificationDisplayOptions.Default);
        }

        public void ShowMessage(IReadOnlyList<NotificationLine> lines, NotificationDisplayOptions options)
        {
            if (lines == null || lines.Count == 0)
                return;

            Form owner = GetOwnerForm();
            NotificationDisplayOptions displayOptions = options ?? NotificationDisplayOptions.Default;

            if (owner != null && owner.InvokeRequired)
            {
                owner.BeginInvoke(new Action(() => ShowMessage(lines, displayOptions)));
                return;
            }

            ShowMessageInternal(lines, displayOptions, owner);
        }

        public void Dispose()
        {
            NotificationForm[] formsToClose;

            lock (syncRoot)
            {
                formsToClose = openForms.ToArray();
                openForms.Clear();
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

        private void ShowMessageInternal(IReadOnlyList<NotificationLine> lines, NotificationDisplayOptions options, Form owner)
        {
            NotificationTheme theme = NotificationTheme.Create(owner);
            NotificationForm form = CreateNotificationForm(theme, options.RequiresManualClose);
            NotificationView notificationView = new NotificationView(lines, theme);

            ConfigureFormContent(form, notificationView, theme, options.RequiresManualClose);

            Rectangle workingArea = GetWorkingArea(owner);

            lock (syncRoot)
            {
                openForms.Add(form);

                if (!options.RequiresManualClose)
                {
                    form.Location = GetNotificationLocation(workingArea, activeNotifications.Count);
                    activeNotifications.Add(form);
                }
            }

            if (options.RequiresManualClose)
            {
                form.Location = GetCenteredNotificationLocation(workingArea);
                ConfigureManualCloseLifecycle(form);

                if (owner != null)
                    form.ShowDialog(owner);
                else
                    form.ShowDialog();

                return;
            }

            ConfigureAutoCloseLifecycle(form, options.AutoCloseDurationMs);

            if (owner != null)
                form.Show(owner);
            else
                form.Show();
        }

        private static NotificationForm CreateNotificationForm(NotificationTheme theme, bool requiresManualClose)
        {
            return new NotificationForm(requiresManualClose)
            {
                Size = new Size(NotificationWidth, NotificationHeight),
                StartPosition = FormStartPosition.Manual,
                TopMost = true,
                ShowInTaskbar = false,
                FormBorderStyle = FormBorderStyle.None,
                BackColor = theme.BorderColor,
                Padding = new Padding(1)
            };
        }

        private void ConfigureFormContent(NotificationForm form, NotificationView notificationView, NotificationTheme theme, bool requiresManualClose)
        {
            Panel contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = theme.BackgroundColor
            };

            notificationView.Dock = DockStyle.Fill;
            contentPanel.Controls.Add(notificationView);

            if (requiresManualClose)
            {
                Panel headerPanel = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = ManualHeaderHeight,
                    BackColor = theme.BackgroundColor
                };

                Button closeButton = CreateCloseButton(form, theme);
                headerPanel.Controls.Add(closeButton);
                contentPanel.Controls.Add(headerPanel);
                form.CancelButton = closeButton;
            }

            form.Controls.Add(contentPanel);
        }

        private static Button CreateCloseButton(Form form, NotificationTheme theme)
        {
            Button closeButton = new Button
            {
                Text = "X",
                Dock = DockStyle.Right,
                Width = CloseButtonSize,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                TabStop = false,
                Cursor = Cursors.Hand,
                ForeColor = CloseButtonTextColor,
                BackColor = theme.BackgroundColor,
                Margin = Padding.Empty
            };

            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.FlatAppearance.MouseOverBackColor = CloseButtonHoverColor;
            closeButton.FlatAppearance.MouseDownBackColor = CloseButtonPressedColor;
            closeButton.Click += (sender, args) => form.Close();

            return closeButton;
        }

        private void ConfigureManualCloseLifecycle(NotificationForm form)
        {
            form.FormClosed += (sender, args) =>
            {
                lock (syncRoot)
                {
                    openForms.Remove(form);
                }

                form.Dispose();
            };
        }

        private void ConfigureAutoCloseLifecycle(NotificationForm form, int autoCloseDurationMs)
        {
            Timer timer = new Timer { Interval = autoCloseDurationMs };
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
                    openForms.Remove(form);
                    activeNotifications.Remove(form);
                    RepositionActiveNotifications(GetWorkingArea(GetOwnerForm()));
                }

                form.Dispose();
            };

            timer.Start();
        }

        private Form GetOwnerForm()
        {
            Form owner = hostFormProvider?.Invoke();
            if (owner != null && !owner.IsDisposed)
                return owner;

            return Application.OpenForms.Count > 0 ? Application.OpenForms[0] : null;
        }

        private static Point GetNotificationLocation(Rectangle workingArea, int index)
        {
            int x = workingArea.Left + ((workingArea.Width - NotificationWidth) / 2);
            int y = workingArea.Top + ((workingArea.Height - NotificationHeight) / 2) + (index * NotificationSpacing);
            return new Point(x, y);
        }

        private static Point GetCenteredNotificationLocation(Rectangle workingArea)
        {
            int x = workingArea.Left + ((workingArea.Width - NotificationWidth) / 2);
            int y = workingArea.Top + ((workingArea.Height - NotificationHeight) / 2);
            return new Point(x, y);
        }

        private static Rectangle GetWorkingArea(Form owner)
        {
            if (owner != null && !owner.IsDisposed)
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
                Font baseFont = owner?.Font ?? SystemFonts.MessageBoxFont;
                Font font = new Font(baseFont.FontFamily, Math.Max(11f, baseFont.Size + 1.5f), FontStyle.Bold);

                return new NotificationTheme(DefaultBackgroundColor, DefaultBorderColor, DefaultTextColor, font);
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
            private readonly bool activateOnShow;

            public NotificationForm(bool activateOnShow)
            {
                this.activateOnShow = activateOnShow;
            }

            protected override bool ShowWithoutActivation => !activateOnShow;
        }
    }
}
