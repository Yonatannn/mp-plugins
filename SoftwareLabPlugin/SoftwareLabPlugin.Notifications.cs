using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace MissionPlanner.SoftwareLab
{
    public partial class SoftwareLabPlugin
    {
        private const int NotificationPadding = 6;
        private const int NotificationLineSpacing = 2;

        private sealed class NotificationLine
        {
            public NotificationLine(string text, Color color)
            {
                Text = text;
                Color = color;
            }

            public string Text { get; }
            public Color Color { get; }
        }

        private sealed class NotificationView : Control
        {
            private readonly IReadOnlyList<NotificationLine> lines;

            public NotificationView(IReadOnlyList<NotificationLine> lines)
            {
                this.lines = lines;
                DoubleBuffered = true;
                ResizeRedraw = true;
                BackColor = SystemColors.Control;
                Font = new Font("Arial", 12, FontStyle.Bold);
                Padding = new Padding(NotificationPadding);
                Dock = DockStyle.Fill;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);

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
                    TextRenderer.DrawText(e.Graphics, line.Text, Font, textBounds, line.Color, flags);
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
        }

        private void ShowAutoCloseMessage(string message)
        {
            ShowAutoCloseMessage(message, SystemColors.ControlText);
        }

        private void ShowAutoCloseMessage(string message, Color textColor)
        {
            ShowAutoCloseMessage(new List<NotificationLine>
            {
                new NotificationLine(message, textColor)
            });
        }

        private void ShowAutoCloseMessage(IReadOnlyList<NotificationLine> lines)
        {
            if (lines == null || lines.Count == 0)
                return;

            Form form = new Form
            {
                Text = "SoftwareLab Info",
                Size = new Size(NotificationWidth, NotificationHeight),
                StartPosition = FormStartPosition.Manual,
                TopMost = true,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false
            };

            NotificationView notificationView = new NotificationView(lines)
            {
                BackColor = form.BackColor
            };

            form.Controls.Add(notificationView);

            lock (activeNotifications)
            {
                form.Location = GetNotificationLocation(Screen.PrimaryScreen.WorkingArea, activeNotifications.Count);
                activeNotifications.Add(form);
            }

            Timer timer = new Timer { Interval = 2500 };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                form.Close();
            };

            form.FormClosed += (s, e) =>
            {
                timer.Dispose();

                lock (activeNotifications)
                {
                    activeNotifications.Remove(form);
                    RepositionActiveNotifications(Screen.PrimaryScreen.WorkingArea);
                }

                form.Dispose();
            };

            timer.Start();
            form.Show();
        }

        private Point GetNotificationLocation(Rectangle workingArea, int index)
        {
            int x = workingArea.Left + ((workingArea.Width - NotificationWidth) / 2);
            int y = workingArea.Top + ((workingArea.Height - NotificationHeight) / 2) + (index * NotificationSpacing);
            return new Point(x, y);
        }

        private void RepositionActiveNotifications(Rectangle workingArea)
        {
            for (int i = 0; i < activeNotifications.Count; i++)
                activeNotifications[i].Location = GetNotificationLocation(workingArea, i);
        }
    }
}
