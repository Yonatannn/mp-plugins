using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace MissionPlanner.SoftwareLab
{
    public partial class SoftwareLabPlugin
    {
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

            TableLayoutPanel layout = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = lines.Count,
                Padding = new Padding(6)
            };

            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            for (int i = 0; i < lines.Count; i++)
            {
                NotificationLine line = lines[i];
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                layout.Controls.Add(new Label
                {
                    Text = line.Text,
                    AutoSize = true,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Arial", 12, FontStyle.Bold),
                    ForeColor = line.Color,
                    MaximumSize = new Size(NotificationWidth - 24, 0),
                    Margin = new Padding(0, 0, 0, 3)
                }, 0, i);
            }

            form.Controls.Add(layout);
            Size layoutSize = layout.GetPreferredSize(form.ClientSize);
            layout.Location = new Point(
                Math.Max(0, (form.ClientSize.Width - layoutSize.Width) / 2),
                Math.Max(0, (form.ClientSize.Height - layoutSize.Height) / 2));

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
