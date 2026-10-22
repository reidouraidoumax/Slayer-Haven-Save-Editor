using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ToukidenSaveEditor.Theme;

namespace ToukidenSaveEditor.UI
{
    /// <summary>
    /// The 78px bottom bar (.action-bar): hosts the Import / Export <see cref="InkButton"/>s
    /// (any InkButton dropped on it is laid out left-to-right by TabIndex) and shows the
    /// activity line - status dot, last notice and the "N CHANGES" counter - on the right.
    /// </summary>
    [ToolboxItem(true)]
    [DesignerCategory("Code")]
    public class ActionBar : Panel
    {
        private string _notice = "Awaiting a save file";
        private bool _isReady;
        private int _changeCount;

        public ActionBar()
        {
            SetStyle(ControlStyles.UserPaint
                   | ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.ResizeRedraw, true);
            BackColor = Color.FromArgb(15, 16, 14);
        }

        [Category("Appearance")]
        [DefaultValue("Awaiting a save file")]
        public string Notice
        {
            get => _notice;
            set { _notice = value ?? string.Empty; Invalidate(); }
        }

        [Category("Appearance")]
        [DefaultValue(false)]
        public bool IsReady
        {
            get => _isReady;
            set { _isReady = value; Invalidate(); }
        }

        [Category("Appearance")]
        [DefaultValue(0)]
        public int ChangeCount
        {
            get => _changeCount;
            set { _changeCount = Math.Max(0, value); Invalidate(); }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public float ViewportWidth { get; set; } = 1280f;

        private float K => Ui.ScaleOf(this);

        private float PadX => Gfx.Clamp(24f, 0.04f * ViewportWidth, 64f);

        public void SetViewport(float vw, float vh)
        {
            ViewportWidth = vw;
            foreach (var b in Controls.OfType<InkButton>())
            {
                b.SetViewport(vw, vh);
            }
            PerformLayout();
            Invalidate();
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);

            float k = K;
            int x = (int)Math.Round(PadX * k);
            int gap = (int)Math.Round(10f * k);
            foreach (var button in Controls.OfType<InkButton>().OrderBy(b => b.TabIndex))
            {
                if (!button.Visible)
                {
                    continue;
                }
                int w = button.PreferredButtonWidth();
                int h = button.PreferredButtonHeight();
                button.SetBounds(x, (Height - h) / 2, w, h);
                x += w + gap;
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // painted in OnPaint
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Backdrop.PaintFor(g, this);
            Gfx.Prepare(g);

            float k = K;
            using (var back = new SolidBrush(Palette.ActionBarBack))
            {
                g.FillRectangle(back, 0, 0, Width, Height);
            }
            // border-top: 1px solid rgba(230,220,196,.15)
            Gfx.Hairline(g, Gfx.Rgba(230, 220, 196, 0.15), 0, 0, Width, Math.Max(1f, k));

            // ---- .activity (right aligned)
            var font = FontLibrary.Get(Face.Serif500, 11f * k);
            float tracking = 0.05f * 11f * k;
            float lh = Gfx.NormalLineHeight(font);
            float noticeW = Gfx.Measure(_notice, font, tracking);

            var countFont = FontLibrary.Get(Face.Serif500, 9f * k);
            float countTracking = 0.08f * 9f * k;
            string countText = _changeCount == 1 ? "1 CHANGE" : $"{_changeCount} CHANGES";
            float countW = _changeCount > 0 ? Gfx.Measure(countText, countFont, countTracking) : 0f;
            float countBlock = _changeCount > 0 ? 10f * k + 6f * k + 1f * k + 15f * k + countW : 0f;

            float right = Width - PadX * k;
            float midY = Height / 2f;
            float x = right - (6f * k + 10f * k + noticeW + countBlock);

            Gfx.StatusDot(g, x + 3f * k, midY, k, _isReady);
            x += 6f * k + 10f * k;
            Gfx.DrawText(g, _notice, font, Palette.StatusText, x, midY - lh / 2f, lh, tracking);
            x += noticeW;

            if (_changeCount > 0)
            {
                float countLh = Gfx.NormalLineHeight(countFont);
                x += 10f * k + 6f * k;
                // border-left: 1px (a 1px-wide, line-box-tall rule)
                Gfx.Hairline(g, Palette.Hairline, x, midY - countLh / 2f, x + Math.Max(1f, k), countLh);
                x += 1f * k + 15f * k;
                Gfx.DrawText(g, countText, countFont, Palette.ChangeCount, x, midY - countLh / 2f, countLh, countTracking);
            }
        }
    }
}
