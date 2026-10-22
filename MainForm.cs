using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using ToukidenSaveEditor.Services;
using ToukidenSaveEditor.Theme;
using ToukidenSaveEditor.UI;

namespace ToukidenSaveEditor
{
    /// <summary>
    /// "Slayer's Haven" main window - the WinForms port of the Figma Make redesign.
    ///
    /// Layout: TopBar (brand + status) / Sidebar (heading, chronicle card, note) +
    /// Workbench (parchment panel with the cheat tabs) / ActionBar (Import, Export,
    /// activity line). The window paints one shared background bitmap (gradients,
    /// grain, waves, hairlines, shadows) and every child control draws its own slice
    /// of it, so the look matches the web design without WinForms transparency.
    ///
    /// Save logic is unchanged from v1: SaveFileService loads the .bin, applies the
    /// data-driven cheats from Cheats.txt / MitamaCheats.txt and repairs the checksum
    /// on export.
    /// </summary>
    public partial class MainForm : Form, IBackdropHost
    {
        private readonly SaveFileService _saveService = new();
        private Bitmap? _backdrop;

        public MainForm()
        {
            InitializeComponent();

            SetStyle(ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.UserPaint
                   | ControlStyles.ResizeRedraw, true);

            workbench.SetTabs(new[]
            {
                new WorkbenchTab("Hunter Edicts",
                    CheatRepository.LoadFromEmbeddedResource("ToukidenSaveEditor.Cheats.txt")),
                new WorkbenchTab("Mitama Archive",
                    CheatRepository.LoadFromEmbeddedResource("ToukidenSaveEditor.MitamaCheats.txt")),
            });

            var version = typeof(MainForm).Assembly.GetName().Version;
            if (version != null)
            {
                sidebar.FooterRight = $"v{version.Major}.{version.Minor}";
            }

            ApplyInitialSize();
            RefreshState();
            LayoutShell();
        }

        // ---------------------------------------------------------------
        // Window size / responsive layout
        // ---------------------------------------------------------------

        private void ApplyInitialSize()
        {
            float k = Ui.ScaleOf(this);
            Rectangle area = Screen.FromPoint(Cursor.Position).WorkingArea;
            int chromeW = Width - ClientSize.Width;
            int chromeH = Height - ClientSize.Height;

            // The Make design was laid out at 1280px wide; 900px tall keeps it on a
            // 1080p screen. Never larger than the screen's working area.
            int w = Math.Min((int)Math.Round(1280 * k), area.Width - chromeW - 16);
            int h = Math.Min((int)Math.Round(900 * k), area.Height - chromeH - 16);
            ClientSize = new Size(Math.Max(640, w), Math.Max(480, h));

            MinimumSize = new Size(
                Math.Min((int)Math.Round(960 * k) + chromeW, area.Width),
                Math.Min((int)Math.Round(640 * k) + chromeH, area.Height));
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (WindowState != FormWindowState.Minimized)
            {
                LayoutShell();
            }
        }

        protected override void OnDpiChanged(DpiChangedEventArgs e)
        {
            base.OnDpiChanged(e);
            LayoutShell();
        }

        private void LayoutShell()
        {
            var client = ClientSize;
            if (client.Width <= 0 || client.Height <= 0)
            {
                return;
            }

            float k = Ui.ScaleOf(this);
            var layout = ShellLayout.Compute(client, k);

            SuspendLayout();
            topBar.Bounds = layout.TopBar;
            sidebar.ContentInset = layout.PadX;
            sidebar.Bounds = Rectangle.FromLTRB(0, layout.Sidebar.Top, layout.Sidebar.Right, layout.Sidebar.Bottom);
            workbench.Bounds = layout.Editor;
            actionBar.Bounds = layout.ActionBar;

            topBar.SetViewport(layout.Vw, layout.Vh);
            sidebar.SetViewport(layout.Vw, layout.Vh);
            workbench.SetViewport(layout.Vw, layout.Vh);
            actionBar.SetViewport(layout.Vw, layout.Vh);
            ResumeLayout(true);

            var old = _backdrop;
            _backdrop = BackdropRenderer.Render(client, layout);
            old?.Dispose();

            Invalidate(true);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (_backdrop != null)
            {
                e.Graphics.DrawImage(_backdrop,
                    new Rectangle(0, 0, _backdrop.Width, _backdrop.Height),
                    new Rectangle(0, 0, _backdrop.Width, _backdrop.Height),
                    GraphicsUnit.Pixel);
            }
            else
            {
                e.Graphics.Clear(Palette.Sumi);
            }
        }

        void IBackdropHost.PaintBackdrop(Graphics g, Control target)
        {
            var size = target.ClientSize;
            if (_backdrop == null)
            {
                using var fill = new SolidBrush(Palette.Sumi);
                g.FillRectangle(fill, 0, 0, size.Width, size.Height);
                return;
            }

            Point offset = target == this
                ? Point.Empty
                : PointToClient(target.PointToScreen(Point.Empty));

            var src = new Rectangle(offset, size);
            var visible = Rectangle.Intersect(src, new Rectangle(0, 0, _backdrop.Width, _backdrop.Height));
            if (visible.Width < size.Width || visible.Height < size.Height)
            {
                using var fill = new SolidBrush(Palette.Sumi);
                g.FillRectangle(fill, 0, 0, size.Width, size.Height);
            }
            if (visible.Width <= 0 || visible.Height <= 0)
            {
                return;
            }

            var oldInterp = g.InterpolationMode;
            var oldPixel = g.PixelOffsetMode;
            var oldComp = g.CompositingMode;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.None;
            g.CompositingMode = CompositingMode.SourceCopy;

            var dest = new Rectangle(visible.X - offset.X, visible.Y - offset.Y, visible.Width, visible.Height);
            g.DrawImage(_backdrop, dest, visible, GraphicsUnit.Pixel);

            g.CompositingMode = oldComp;
            g.PixelOffsetMode = oldPixel;
            g.InterpolationMode = oldInterp;
        }

        // ---------------------------------------------------------------
        // State
        // ---------------------------------------------------------------

        private void RefreshState()
        {
            bool ready = _saveService.IsLoaded;
            topBar.IsReady = ready;
            actionBar.IsReady = ready;
            btnExport.Enabled = ready;
            actionBar.ChangeCount = workbench.AppliedCount;
        }

        // ---------------------------------------------------------------
        // Cheats
        // ---------------------------------------------------------------

        private void workbench_ApplyRequested(object? sender, CheatEventArgs e)
        {
            var cheat = e.Cheat;

            if (!_saveService.IsLoaded)
            {
                actionBar.Notice = "Import a save before invoking an edit";
                btnImport.Focus();
                return;
            }

            try
            {
                _saveService.ApplyCheat(cheat);
                workbench.MarkApplied(cheat);
                actionBar.Notice = $"{cheat.Name} has been prepared";
                RefreshState();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Could not apply cheat",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ---------------------------------------------------------------
        // Import / Export
        // ---------------------------------------------------------------

        private void btnImport_Click(object? sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Title = "Import Toukiden 2 save file",
                Filter = "Toukiden 2 save (*.bin)|*.bin|All files (*.*)|*.*"
            };

            if (dlg.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            try
            {
                _saveService.Load(dlg.FileName);

                var info = new FileInfo(dlg.FileName);
                sidebar.FileName = info.Name;
                sidebar.FileSize = info.Length;
                workbench.ClearApplied();
                actionBar.Notice = $"{info.Name} is ready for editing";
                RefreshState();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Could not import save",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnExport_Click(object? sender, EventArgs e)
        {
            if (!_saveService.IsLoaded)
            {
                actionBar.Notice = "Import a save before exporting";
                return;
            }

            using var dlg = new SaveFileDialog
            {
                Title = "Export Toukiden 2 save file",
                Filter = "Toukiden 2 save (*.bin)|*.bin|All files (*.*)|*.*",
                // Keep the original name: the PS4 expects play00_user.bin.
                FileName = Path.GetFileName(_saveService.CurrentPath ?? "play00_user.bin")
            };

            if (dlg.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            try
            {
                _saveService.Export(dlg.FileName);
                actionBar.Notice = "Edited save exported successfully";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Could not export save",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
