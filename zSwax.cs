// zSwax - Guvenlik & Sistem Araclari
// Gelistiren: elifnazpamks  |  Discord: zSwaxx
//
// Sekmeler: Secure Boot | TPM | Cekirdek | Defender | Oyun | Gizlilik | Ag | Bakim
// Status okumalari GERCEK calisan durumu yansitir (HVCI->WMI DeviceGuard, TPM->Get-Tpm,
// SecureBoot->Confirm-SecureBootUEFI, Defender->Get-MpComputerStatus).
//
// Derleme: Derle.bat (yerlesik csc.exe; ekstra kurulum gerekmez)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace SwaxSecureBoot
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    static class Theme
    {
        public static readonly Color Bg      = ColorTranslator.FromHtml("#14141F");
        public static readonly Color Card    = ColorTranslator.FromHtml("#1E1E2B");
        public static readonly Color CardHi  = ColorTranslator.FromHtml("#262638");
        public static readonly Color Accent  = ColorTranslator.FromHtml("#8B5CF6");
        public static readonly Color AccentHi= ColorTranslator.FromHtml("#7C3AED");
        public static readonly Color Text    = ColorTranslator.FromHtml("#ECECF1");
        public static readonly Color Muted   = ColorTranslator.FromHtml("#9A9AB5");
        public static readonly Color Green   = ColorTranslator.FromHtml("#22C55E");
        public static readonly Color GreenHi = ColorTranslator.FromHtml("#16A34A");
        public static readonly Color Red     = ColorTranslator.FromHtml("#EF4444");
        public static readonly Color RedHi   = ColorTranslator.FromHtml("#DC2626");
        public static readonly Color Amber   = ColorTranslator.FromHtml("#F59E0B");
        public static readonly Color Border  = ColorTranslator.FromHtml("#30304A");
    }

    static class Gfx
    {
        public static GraphicsPath Round(Rectangle r, int radius)
        {
            GraphicsPath p = new GraphicsPath();
            if (radius <= 0) { p.AddRectangle(r); p.CloseFigure(); return p; }
            int d = radius * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static void Shield(Graphics g, RectangleF r, Color fill, Color mark)
        {
            float x = r.X, y = r.Y, w = r.Width, h = r.Height;
            using (GraphicsPath p = new GraphicsPath())
            {
                p.AddLine(x, y + h * 0.16f, x + w * 0.5f, y);
                p.AddLine(x + w * 0.5f, y, x + w, y + h * 0.16f);
                p.AddLine(x + w, y + h * 0.16f, x + w, y + h * 0.52f);
                p.AddBezier(x + w, y + h * 0.52f, x + w, y + h * 0.80f, x + w * 0.74f, y + h * 0.95f, x + w * 0.5f, y + h);
                p.AddBezier(x + w * 0.5f, y + h, x + w * 0.26f, y + h * 0.95f, x, y + h * 0.80f, x, y + h * 0.52f);
                p.CloseFigure();
                using (SolidBrush b = new SolidBrush(fill)) g.FillPath(b, p);
            }
            using (Pen pen = new Pen(mark, Math.Max(2f, w * 0.085f)))
            {
                pen.StartCap = LineCap.Round; pen.EndCap = LineCap.Round; pen.LineJoin = LineJoin.Round;
                PointF a = new PointF(x + w * 0.30f, y + h * 0.50f);
                PointF b = new PointF(x + w * 0.44f, y + h * 0.64f);
                PointF c = new PointF(x + w * 0.72f, y + h * 0.34f);
                g.DrawLines(pen, new PointF[] { a, b, c });
            }
        }

        public static Color Hover(Color c)
        {
            if (c == Theme.Accent) return Theme.AccentHi;
            if (c == Theme.Red) return Theme.RedHi;
            if (c == Theme.Green) return Theme.GreenHi;
            return Theme.Border;
        }
    }

    class Card : Panel
    {
        public int Radius = 16;
        public Color Fill = Theme.Card;
        public Color BorderColor = Theme.Border;
        public bool DrawBorder = true;
        public Card()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
                   | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Bg;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Bg);
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = Gfx.Round(r, Radius))
            {
                using (SolidBrush b = new SolidBrush(Fill)) g.FillPath(b, path);
                if (DrawBorder) using (Pen p = new Pen(BorderColor)) g.DrawPath(p, path);
            }
        }
    }

    class RoundButton : Button
    {
        public int Radius = 12;
        public Color Fill = Theme.Accent;
        public Color FillHover = Theme.AccentHi;
        public Color TextColor = Color.White;
        private bool hover = false;
        public RoundButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
                   | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = Theme.Card;
            ForeColor = Color.White;
            Font = new Font("Segoe UI Semibold", 10.5f);
            Cursor = Cursors.Hand;
        }
        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Card);
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            Color fill = Enabled ? (hover ? FillHover : Fill) : Theme.CardHi;
            using (GraphicsPath path = Gfx.Round(r, Radius))
            using (SolidBrush b = new SolidBrush(fill))
                g.FillPath(b, path);
            TextRenderer.DrawText(g, Text, Font, r, Enabled ? TextColor : Theme.Muted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
        }
    }

    class ShieldBox : Control
    {
        public Color Fill = Theme.Accent;
        public Color Mark = Color.White;
        public ShieldBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
                   | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Bg;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Bg);
            Gfx.Shield(g, new RectangleF(1, 0, Width - 2, Height - 1), Fill, Mark);
        }
    }

    class HoverLabel : Label
    {
        public Color HoverBack = Theme.CardHi;
        public Color NormalBack = Theme.Bg;
        public HoverLabel()
        {
            BackColor = NormalBack;
            TextAlign = ContentAlignment.MiddleCenter;
            Cursor = Cursors.Hand;
        }
        protected override void OnMouseEnter(EventArgs e) { BackColor = HoverBack; base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { BackColor = NormalBack; base.OnMouseLeave(e); }
    }

    class Segmented : Control
    {
        public string[] Items = new string[] { "A", "B" };
        public int Rows = 1;
        public int Selected = 0;
        public event EventHandler SelectedChanged;
        public Segmented()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
                   | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Bg;
            Cursor = Cursors.Hand;
            Font = new Font("Segoe UI Semibold", 9f);
        }
        int Cols() { return (Items.Length + Rows - 1) / Rows; }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            int cols = Cols();
            int col = (int)((long)e.X * cols / Width);
            int row = (int)((long)e.Y * Rows / Height);
            if (col < 0) col = 0; if (col >= cols) col = cols - 1;
            if (row < 0) row = 0; if (row >= Rows) row = Rows - 1;
            int idx = row * cols + col;
            if (idx < Items.Length && idx != Selected)
            {
                Selected = idx; Invalidate();
                if (SelectedChanged != null) SelectedChanged(this, EventArgs.Empty);
            }
            base.OnMouseDown(e);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Bg);
            Rectangle full = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath p = Gfx.Round(full, 12))
            using (SolidBrush b = new SolidBrush(Theme.CardHi)) g.FillPath(b, p);

            int cols = Cols();
            int cellW = Width / cols;
            int cellH = Height / Rows;
            TextFormatFlags f = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
            for (int i = 0; i < Items.Length; i++)
            {
                int row = i / cols, col = i % cols;
                Rectangle cell = new Rectangle(col * cellW, row * cellH, cellW, cellH);
                if (i == Selected)
                {
                    Rectangle pill = new Rectangle(cell.X + 3, cell.Y + 3, cell.Width - 6, cell.Height - 6);
                    using (GraphicsPath pp = Gfx.Round(pill, 9))
                    using (SolidBrush b = new SolidBrush(Theme.Accent)) g.FillPath(b, pp);
                }
                TextRenderer.DrawText(g, Items[i], Font, cell, i == Selected ? Color.White : Theme.Muted, f);
            }
        }
    }

    class ToggleSwitch : Control
    {
        public bool Checked = true;
        public event EventHandler CheckedChanged;
        public ToggleSwitch()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
                   | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Bg;
            Cursor = Cursors.Hand;
            Size = new Size(46, 26);
            TabStop = false;
        }
        public void Toggle() { DoToggle(); }
        void DoToggle()
        {
            Checked = !Checked; Invalidate();
            if (CheckedChanged != null) CheckedChanged(this, EventArgs.Empty);
        }
        protected override void OnClick(EventArgs e) { DoToggle(); base.OnClick(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Bg);
            int tw = 46, thh = 22, ty = (Height - thh) / 2;
            Rectangle track = new Rectangle(0, ty, tw - 1, thh - 1);
            using (GraphicsPath p = Gfx.Round(track, thh / 2))
            using (SolidBrush b = new SolidBrush(Checked ? Theme.Accent : Theme.Border)) g.FillPath(b, p);
            int kn = 16, ky = (Height - kn) / 2;
            int kx = Checked ? tw - kn - 3 : 3;
            using (SolidBrush b = new SolidBrush(Color.White)) g.FillEllipse(b, kx, ky, kn, kn);
        }
    }

    class TweakRow
    {
        public ToggleSwitch Sw;
        public Func<bool> IsOn;
        public Action<bool> Set;
    }

    // ------------------------------ Form ------------------------------
    class MainForm : Form
    {
        [DllImport("user32.dll")] static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll", CharSet = CharSet.Auto)] static extern IntPtr SendMessageCue(IntPtr hWnd, int Msg, int wParam, string lParam);
        [DllImport("user32.dll")] static extern bool ReleaseCapture();
        [DllImport("gdi32.dll")] static extern IntPtr CreateRoundRectRgn(int x1, int y1, int x2, int y2, int w, int h);
        [DllImport("user32.dll", SetLastError = true)] static extern bool SystemParametersInfo(uint uiAction, uint uiParam, int[] pvParam, uint fWinIni);
        [DllImport("user32.dll", CharSet = CharSet.Auto)] static extern IntPtr SendMessageTimeout(IntPtr hWnd, int Msg, IntPtr wParam, string lParam, int flags, int timeout, out IntPtr result);
        const int WM_NCLBUTTONDOWN = 0xA1;
        const int HTCAPTION = 0x2;
        const int EM_SETCUEBANNER = 0x1501;
        const int COUNTDOWN = 15;
        const uint SPI_SETMOUSE = 0x0004;
        const uint SPIF_UPDATE = 0x01;
        const uint SPIF_SEND = 0x02;
        const int HWND_BROADCAST = 0xFFFF;
        const int WM_SETTINGCHANGE = 0x1A;

        const string TPM_KEY       = @"SYSTEM\CurrentControlSet\Services\Tpm";
        const string TPM_KEY_FULL  = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tpm";
        const string HVCI_KEY      = @"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity";
        const string HVCI_KEY_FULL = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity";
        const string DG_FULL       = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\DeviceGuard";

        const string DEF_QUERY = @"try { $s=Get-MpComputerStatus; [string]$s.RealTimeProtectionEnabled + ';' + [string]$s.IsTamperProtected + ';' + [string]$s.AntivirusEnabled } catch { 'ERR;ERR;ERR' }";
        const string DEF_OFF = @"Set-MpPreference -DisableRealtimeMonitoring $true -ErrorAction SilentlyContinue; Set-MpPreference -DisableBehaviorMonitoring $true -ErrorAction SilentlyContinue; Set-MpPreference -DisableIOAVProtection $true -ErrorAction SilentlyContinue; Set-MpPreference -DisableScriptScanning $true -ErrorAction SilentlyContinue; Set-MpPreference -DisableBlockAtFirstSeen $true -ErrorAction SilentlyContinue; Set-MpPreference -MAPSReporting 0 -ErrorAction SilentlyContinue; Set-MpPreference -SubmitSamplesConsent 2 -ErrorAction SilentlyContinue";
        const string DEF_ON = @"Set-MpPreference -DisableRealtimeMonitoring $false -ErrorAction SilentlyContinue; Set-MpPreference -DisableBehaviorMonitoring $false -ErrorAction SilentlyContinue; Set-MpPreference -DisableIOAVProtection $false -ErrorAction SilentlyContinue; Set-MpPreference -DisableScriptScanning $false -ErrorAction SilentlyContinue; Set-MpPreference -DisableBlockAtFirstSeen $false -ErrorAction SilentlyContinue; Set-MpPreference -MAPSReporting 2 -ErrorAction SilentlyContinue; Set-MpPreference -SubmitSamplesConsent 1 -ErrorAction SilentlyContinue";
        const string HVCI_QUERY = @"try { $g=Get-CimInstance -Namespace root\Microsoft\Windows\DeviceGuard -ClassName Win32_DeviceGuard -ErrorAction Stop; $run=@($g.SecurityServicesRunning); $cfg=@($g.SecurityServicesConfigured); ([string][int]($run -contains 2)) + ';' + ([string][int]($cfg -contains 2)) } catch { 'ERR;ERR' }";
        const string TPM_QUERY = @"try { $t=Get-Tpm -ErrorAction Stop; [string]$t.TpmPresent + ';' + [string]$t.TpmReady } catch { 'ERR;ERR' }";

        string accessKey = "SWAX-2024";
        bool cleanLogs = true;

        TextBox txtKey;
        HoverLabel lblEye;
        Label lblError;
        Segmented seg;
        ToggleSwitch toggleClean;
        Panel pnlSB, pnlTpm, pnlHvci, pnlDefender, pnlGame, pnlPriv, pnlNet, pnlMaint;
        Label sbVal, sbDot, sbHint;
        Label tpmVal, tpmDot, tpmHint;
        RoundButton tpmBtnOff, tpmBtnOn;
        Label hvciVal, hvciDot, hvciHint;
        Label defVal, defDot, defHint;
        bool defTamper, defRealtime;
        List<TweakRow> tweaks = new List<TweakRow>();

        Panel pnlOverlay;
        ShieldBox icoOverlay;
        Label lblOvTitle, lblOvCount, lblOvMsg;
        RoundButton btnOvPrimary, btnOvCancel;
        System.Windows.Forms.Timer timer;
        int counter;
        bool isCountdown;
        Action pendingAction;

        public MainForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(460, 726);
            BackColor = Theme.Bg;
            ForeColor = Theme.Text;
            Font = new Font("Segoe UI", 9.75f);
            Text = "zSwax";
            DoubleBuffered = true;
            KeyPreview = true;

            LoadKey();
            BuildUi();

            try { Icon = BuildIcon(); } catch { }
            Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, Width + 1, Height + 1, 22, 22));

            this.KeyDown += delegate(object s, KeyEventArgs e) {
                if (e.KeyCode == Keys.Escape && !pnlOverlay.Visible) this.Close();
            };
            this.Shown += delegate {
                try { SendMessageCue(txtKey.Handle, EM_SETCUEBANNER, 1, "Erişim anahtarını girin"); } catch { }
                RefreshSecureBoot();
                RefreshTpm();
                RefreshHvci();
                RefreshDefender();
            };
        }

        Icon BuildIcon()
        {
            Bitmap bmp = new Bitmap(32, 32);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Gfx.Shield(g, new RectangleF(3, 1, 26, 30), Theme.Accent, Color.White);
            }
            return Icon.FromHandle(bmp.GetHicon());
        }

        void LoadKey()
        {
            try
            {
                string path = Path.Combine(Application.StartupPath, "key.txt");
                if (File.Exists(path))
                {
                    string k = File.ReadAllText(path).Trim();
                    if (k.Length > 0) accessKey = k;
                }
            }
            catch { }
        }

        void Drag(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0); }
        }

        // ------------------------- UI kurulum -------------------------
        void BuildUi()
        {
            Panel bar = new Panel();
            bar.SetBounds(0, 0, 460, 46);
            bar.BackColor = Theme.Bg;
            bar.MouseDown += Drag;
            Controls.Add(bar);

            ShieldBox logo = new ShieldBox();
            logo.SetBounds(16, 12, 22, 22);
            logo.MouseDown += Drag;
            bar.Controls.Add(logo);

            Label title = new Label();
            title.Text = "zSwax";
            title.Font = new Font("Segoe UI Semibold", 11f);
            title.ForeColor = Theme.Text;
            title.BackColor = Theme.Bg;
            title.SetBounds(46, 13, 260, 22);
            title.MouseDown += Drag;
            bar.Controls.Add(title);

            HoverLabel btnMin = new HoverLabel();
            btnMin.Text = "—";
            btnMin.Font = new Font("Segoe UI", 11f);
            btnMin.ForeColor = Theme.Muted;
            btnMin.SetBounds(380, 8, 34, 30);
            btnMin.Click += delegate { WindowState = FormWindowState.Minimized; };
            bar.Controls.Add(btnMin);

            HoverLabel btnClose = new HoverLabel();
            btnClose.Text = "✕";
            btnClose.Font = new Font("Segoe UI", 11f);
            btnClose.ForeColor = Theme.Muted;
            btnClose.HoverBack = Theme.Red;
            btnClose.SetBounds(418, 8, 34, 30);
            btnClose.Click += delegate { this.Close(); };
            bar.Controls.Add(btnClose);

            ShieldBox hero = new ShieldBox();
            hero.SetBounds(202, 52, 56, 56);
            Controls.Add(hero);

            Label subtitle = new Label();
            subtitle.Text = "Güvenlik & Sistem Araçları";
            subtitle.Font = new Font("Segoe UI", 10f);
            subtitle.ForeColor = Theme.Muted;
            subtitle.TextAlign = ContentAlignment.MiddleCenter;
            subtitle.BackColor = Theme.Bg;
            subtitle.SetBounds(0, 112, 460, 22);
            Controls.Add(subtitle);

            seg = new Segmented();
            seg.Items = new string[] { "Secure Boot", "TPM", "Çekirdek", "Defender", "Oyun", "Gizlilik", "Ağ", "Bakım" };
            seg.Rows = 2;
            seg.Font = new Font("Segoe UI Semibold", 9f);
            seg.SetBounds(20, 142, 420, 88);
            seg.SelectedChanged += delegate { ShowTab(seg.Selected); };
            Controls.Add(seg);

            int py = 248, ph = 300;
            pnlSB = BuildSbPanel();             pnlSB.SetBounds(20, py, 420, ph);       Controls.Add(pnlSB);
            pnlTpm = BuildTpmPanel();           pnlTpm.SetBounds(20, py, 420, ph);      pnlTpm.Visible = false;      Controls.Add(pnlTpm);
            pnlHvci = BuildHvciPanel();         pnlHvci.SetBounds(20, py, 420, ph);     pnlHvci.Visible = false;     Controls.Add(pnlHvci);
            pnlDefender = BuildDefenderPanel(); pnlDefender.SetBounds(20, py, 420, ph); pnlDefender.Visible = false; Controls.Add(pnlDefender);
            pnlGame = BuildGamePanel();         pnlGame.SetBounds(20, py, 420, ph);     pnlGame.Visible = false;     Controls.Add(pnlGame);
            pnlPriv = BuildPrivPanel();         pnlPriv.SetBounds(20, py, 420, ph);     pnlPriv.Visible = false;     Controls.Add(pnlPriv);
            pnlNet = BuildNetPanel();           pnlNet.SetBounds(20, py, 420, ph);      pnlNet.Visible = false;      Controls.Add(pnlNet);
            pnlMaint = BuildMaintPanel();       pnlMaint.SetBounds(20, py, 420, ph);    pnlMaint.Visible = false;    Controls.Add(pnlMaint);

            toggleClean = new ToggleSwitch();
            toggleClean.Checked = true;
            toggleClean.SetBounds(22, 562, 46, 26);
            toggleClean.CheckedChanged += delegate { cleanLogs = toggleClean.Checked; };
            Controls.Add(toggleClean);

            HoverLabel lblClean = new HoverLabel();
            lblClean.Text = "İşlem sonrası logları temizle  (App/System/Setup + geçici dosyalar)";
            lblClean.Font = new Font("Segoe UI", 9f);
            lblClean.ForeColor = Theme.Text;
            lblClean.NormalBack = Theme.Bg;
            lblClean.HoverBack = Theme.Bg;
            lblClean.TextAlign = ContentAlignment.MiddleLeft;
            lblClean.SetBounds(78, 560, 360, 30);
            lblClean.Click += delegate { toggleClean.Toggle(); };
            Controls.Add(lblClean);

            Label kTitle = new Label();
            kTitle.Text = "ERİŞİM ANAHTARI";
            kTitle.Font = new Font("Segoe UI Semibold", 8.25f);
            kTitle.ForeColor = Theme.Muted;
            kTitle.BackColor = Theme.Bg;
            kTitle.SetBounds(22, 600, 200, 16);
            Controls.Add(kTitle);

            Card field = new Card();
            field.SetBounds(20, 622, 420, 48);
            field.Fill = Theme.CardHi;
            field.BorderColor = Theme.Border;
            field.Radius = 12;
            Controls.Add(field);

            txtKey = new TextBox();
            txtKey.BorderStyle = BorderStyle.None;
            txtKey.BackColor = Theme.CardHi;
            txtKey.ForeColor = Theme.Text;
            txtKey.Font = new Font("Segoe UI", 12f);
            txtKey.PasswordChar = '●';
            int th = txtKey.PreferredHeight;
            txtKey.SetBounds(16, (48 - th) / 2, 320, th);
            field.Controls.Add(txtKey);

            lblEye = new HoverLabel();
            lblEye.Text = "Göster";
            lblEye.Font = new Font("Segoe UI", 9f);
            lblEye.ForeColor = Theme.Muted;
            lblEye.NormalBack = Theme.CardHi;
            lblEye.HoverBack = Theme.Border;
            lblEye.SetBounds(344, 6, 66, 36);
            lblEye.Click += delegate
            {
                if (txtKey.PasswordChar == '\0') { txtKey.PasswordChar = '●'; lblEye.Text = "Göster"; }
                else { txtKey.PasswordChar = '\0'; lblEye.Text = "Gizle"; }
                txtKey.Focus();
            };
            field.Controls.Add(lblEye);

            lblError = new Label();
            lblError.Font = new Font("Segoe UI", 9f);
            lblError.ForeColor = Theme.Red;
            lblError.BackColor = Theme.Bg;
            lblError.SetBounds(22, 676, 416, 18);
            lblError.Visible = false;
            Controls.Add(lblError);

            Label footer = new Label();
            footer.Text = "elifnazpamks tarafından geliştirildi   •   Discord: zSwaxx";
            footer.Font = new Font("Segoe UI", 8.5f);
            footer.ForeColor = Theme.Muted;
            footer.TextAlign = ContentAlignment.MiddleCenter;
            footer.BackColor = Theme.Bg;
            footer.SetBounds(0, 700, 460, 22);
            Controls.Add(footer);

            BuildOverlay();
        }

        Card MakeStatusCard(Panel parent, string titleText, out Label dot, out Label val, EventHandler refresh)
        {
            Card card = new Card();
            card.SetBounds(0, 0, 420, 88);
            card.Fill = Theme.Card;
            parent.Controls.Add(card);

            Label tt = new Label();
            tt.Text = titleText;
            tt.Font = new Font("Segoe UI Semibold", 8.25f);
            tt.ForeColor = Theme.Muted;
            tt.BackColor = Theme.Card;
            tt.AutoSize = false;
            tt.SetBounds(22, 14, 290, 16);
            card.Controls.Add(tt);

            dot = new Label();
            dot.Text = "●";
            dot.Font = new Font("Segoe UI", 17f);
            dot.ForeColor = Theme.Muted;
            dot.BackColor = Theme.Card;
            dot.SetBounds(20, 38, 28, 34);
            card.Controls.Add(dot);

            val = new Label();
            val.Text = "Kontrol ediliyor...";
            val.Font = new Font("Segoe UI Semibold", 14f);
            val.ForeColor = Theme.Muted;
            val.BackColor = Theme.Card;
            val.AutoSize = false;
            val.SetBounds(48, 41, 262, 30);
            card.Controls.Add(val);

            RoundButton refreshBtn = new RoundButton();
            refreshBtn.Text = "Yenile";
            refreshBtn.Fill = Theme.CardHi;
            refreshBtn.FillHover = Theme.Border;
            refreshBtn.TextColor = Theme.Text;
            refreshBtn.Radius = 10;
            refreshBtn.Font = new Font("Segoe UI", 9f);
            refreshBtn.SetBounds(322, 26, 80, 36);
            refreshBtn.Click += refresh;
            card.Controls.Add(refreshBtn);

            return card;
        }

        Label MakeHint(Panel parent)
        {
            Label h = new Label();
            h.Font = new Font("Segoe UI", 9f);
            h.ForeColor = Theme.Muted;
            h.BackColor = Theme.Bg;
            h.AutoSize = false;
            h.SetBounds(4, 166, 412, 82);
            parent.Controls.Add(h);
            return h;
        }

        void AddDualButtons(Panel parent, string disableText, EventHandler onDisable, string enableText, EventHandler onEnable)
        {
            RoundButton bd = new RoundButton();
            bd.Text = disableText;
            bd.Fill = Theme.Red; bd.FillHover = Theme.RedHi;
            bd.SetBounds(0, 104, 203, 54);
            bd.Radius = 14; bd.Font = new Font("Segoe UI Semibold", 10.5f);
            bd.Click += onDisable;
            parent.Controls.Add(bd);

            RoundButton be = new RoundButton();
            be.Text = enableText;
            be.Fill = Theme.Green; be.FillHover = Theme.GreenHi;
            be.SetBounds(217, 104, 203, 54);
            be.Radius = 14; be.Font = new Font("Segoe UI Semibold", 10.5f);
            be.Click += onEnable;
            parent.Controls.Add(be);
        }

        int AddToggleRow(Panel p, int y, string title, string desc, Func<bool> isOn, Action<bool> set)
        {
            Label t = new Label();
            t.Text = title; t.Font = new Font("Segoe UI Semibold", 10f);
            t.ForeColor = Theme.Text; t.BackColor = Theme.Bg; t.AutoSize = false;
            t.SetBounds(2, y, 350, 20);
            p.Controls.Add(t);

            Label d = new Label();
            d.Text = desc; d.Font = new Font("Segoe UI", 8.25f);
            d.ForeColor = Theme.Muted; d.BackColor = Theme.Bg; d.AutoSize = false;
            d.SetBounds(2, y + 19, 360, 18);
            p.Controls.Add(d);

            ToggleSwitch sw = new ToggleSwitch();
            sw.SetBounds(372, y + 5, 46, 26);
            bool init = false; try { init = isOn(); } catch { }
            sw.Checked = init;
            sw.CheckedChanged += delegate { try { set(sw.Checked); } catch { } };
            p.Controls.Add(sw);

            TweakRow tr = new TweakRow();
            tr.Sw = sw; tr.IsOn = isOn; tr.Set = set;
            tweaks.Add(tr);
            return y + 46;
        }

        int AddActionRow(Panel p, int y, string title, string desc, string btnText, Color col, EventHandler onClick, int rowH)
        {
            Label t = new Label();
            t.Text = title; t.Font = new Font("Segoe UI Semibold", 10f);
            t.ForeColor = Theme.Text; t.BackColor = Theme.Bg; t.AutoSize = false;
            t.SetBounds(2, y, 290, 20);
            p.Controls.Add(t);

            Label d = new Label();
            d.Text = desc; d.Font = new Font("Segoe UI", 8.25f);
            d.ForeColor = Theme.Muted; d.BackColor = Theme.Bg; d.AutoSize = false;
            d.SetBounds(2, y + 19, 300, 18);
            p.Controls.Add(d);

            RoundButton b = new RoundButton();
            b.Text = btnText; b.Fill = col; b.FillHover = Gfx.Hover(col); b.TextColor = Color.White;
            b.Radius = 10; b.Font = new Font("Segoe UI Semibold", 9f);
            b.SetBounds(304, y + 2, 116, 36);
            b.Click += onClick;
            p.Controls.Add(b);
            return y + rowH;
        }

        Panel NewPanel()
        {
            Panel p = new Panel();
            p.BackColor = Theme.Bg;
            p.Size = new Size(420, 300);
            return p;
        }

        Panel BuildSbPanel()
        {
            Panel p = NewPanel();
            MakeStatusCard(p, "SECURE BOOT DURUMU", out sbDot, out sbVal, delegate { RefreshSecureBoot(); });
            RoundButton act = new RoundButton();
            act.Text = "UEFI Ayarlarına Yeniden Başlat";
            act.SetBounds(0, 104, 420, 54);
            act.Radius = 14; act.Font = new Font("Segoe UI Semibold", 11.5f);
            act.Click += delegate { OnSecureBootReboot(); };
            p.Controls.Add(act);
            sbHint = MakeHint(p);
            return p;
        }

        Panel BuildTpmPanel()
        {
            Panel p = NewPanel();
            MakeStatusCard(p, "TPM SÜRÜCÜSÜ (WINDOWS)", out tpmDot, out tpmVal, delegate { RefreshTpm(); });
            tpmBtnOff = new RoundButton();
            tpmBtnOff.Text = "Devre Dışı Bırak"; tpmBtnOff.Fill = Theme.Red; tpmBtnOff.FillHover = Theme.RedHi;
            tpmBtnOff.SetBounds(0, 104, 203, 54); tpmBtnOff.Radius = 14; tpmBtnOff.Font = new Font("Segoe UI Semibold", 10.5f);
            tpmBtnOff.Click += delegate { OnTpmDisable(); };
            p.Controls.Add(tpmBtnOff);
            tpmBtnOn = new RoundButton();
            tpmBtnOn.Text = "Aktif Et"; tpmBtnOn.Fill = Theme.Green; tpmBtnOn.FillHover = Theme.GreenHi;
            tpmBtnOn.SetBounds(217, 104, 203, 54); tpmBtnOn.Radius = 14; tpmBtnOn.Font = new Font("Segoe UI Semibold", 10.5f);
            tpmBtnOn.Click += delegate { OnTpmEnable(); };
            p.Controls.Add(tpmBtnOn);
            tpmHint = MakeHint(p);
            return p;
        }

        Panel BuildHvciPanel()
        {
            Panel p = NewPanel();
            MakeStatusCard(p, "ÇEKİRDEK YALITIMI (BELLEK BÜTÜNLÜĞÜ)", out hvciDot, out hvciVal, delegate { RefreshHvci(); });
            AddDualButtons(p, "Devre Dışı Bırak", delegate { OnHvciDisable(); }, "Aktif Et", delegate { OnHvciEnable(); });
            hvciHint = MakeHint(p);
            return p;
        }

        Panel BuildDefenderPanel()
        {
            Panel p = NewPanel();
            MakeStatusCard(p, "WINDOWS DEFENDER", out defDot, out defVal, delegate { RefreshDefender(); });
            AddDualButtons(p, "Tam Koruma KAPAT", delegate { OnDefenderDisable(); }, "Tam Koruma AÇ", delegate { OnDefenderEnable(); });
            defHint = MakeHint(p);
            defHint.SetBounds(4, 166, 412, 44);

            RoundButton tamper = new RoundButton();
            tamper.Text = "Kurcalama Korumasını Aç";
            tamper.Fill = Theme.CardHi; tamper.FillHover = Theme.Border; tamper.TextColor = Theme.Text;
            tamper.Radius = 10; tamper.Font = new Font("Segoe UI", 9f);
            tamper.SetBounds(0, 214, 206, 34);
            tamper.Click += delegate { OpenDefenderSettings(); };
            p.Controls.Add(tamper);

            RoundButton allow = new RoundButton();
            allow.Text = "Bu Klasörü İzin Ver";
            allow.Fill = Theme.CardHi; allow.FillHover = Theme.Border; allow.TextColor = Theme.Text;
            allow.Radius = 10; allow.Font = new Font("Segoe UI", 9f);
            allow.SetBounds(214, 214, 206, 34);
            allow.Click += delegate { OnAddExclusion(); };
            p.Controls.Add(allow);
            return p;
        }

        Panel BuildGamePanel()
        {
            Panel p = NewPanel();
            int y = 6;
            y = AddToggleRow(p, y, "Game DVR / Xbox kapalı", "Arka plan oyun kaydını kapatır (FPS'e iyi gelir).",
                delegate { return RegGetInt(@"HKEY_CURRENT_USER\System\GameConfigStore", "GameDVR_Enabled", 1) == 0; },
                delegate(bool on) {
                    RegSetInt(@"HKEY_CURRENT_USER\System\GameConfigStore", "GameDVR_Enabled", on ? 0 : 1);
                    RegSetInt(@"HKEY_CURRENT_USER\System\GameConfigStore", "GameDVR_FSEBehaviorMode", on ? 2 : 0);
                    RegSetInt(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled", on ? 0 : 1);
                    RegSetInt(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\GameDVR", "AllowGameDVR", on ? 0 : 1);
                });
            y = AddToggleRow(p, y, "Donanım Hızl. GPU Zamanlama (HAGS)", "Performans için. YENİDEN BAŞLATMA gerekir.",
                delegate { return RegGetInt(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 1) == 2; },
                delegate(bool on) { RegSetInt(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", on ? 2 : 1); });
            y = AddToggleRow(p, y, "Fare ivmesi kapalı", "Ham fare girişi (nişan). Anında uygulanır.",
                delegate { return RegGetStr(@"HKEY_CURRENT_USER\Control Panel\Mouse", "MouseSpeed", "1") == "0"; },
                delegate(bool on) {
                    RegSetStr(@"HKEY_CURRENT_USER\Control Panel\Mouse", "MouseSpeed", on ? "0" : "1");
                    RegSetStr(@"HKEY_CURRENT_USER\Control Panel\Mouse", "MouseThreshold1", on ? "0" : "6");
                    RegSetStr(@"HKEY_CURRENT_USER\Control Panel\Mouse", "MouseThreshold2", on ? "0" : "10");
                    ApplyMouseLive(on);
                });
            y = AddToggleRow(p, y, "Görsel efektler → performans", "Animasyonları kısar. Tam etki için oturumu kapat-aç.",
                delegate { return RegGetInt(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", "VisualFXSetting", 0) == 2; },
                delegate(bool on) {
                    RegSetInt(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", "VisualFXSetting", on ? 2 : 1);
                    RegSetStr(@"HKEY_CURRENT_USER\Control Panel\Desktop", "DragFullWindows", on ? "0" : "1");
                    RegSetStr(@"HKEY_CURRENT_USER\Control Panel\Desktop\WindowMetrics", "MinAnimate", on ? "0" : "1");
                    RegSetInt(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarAnimations", on ? 0 : 1);
                    RegSetInt(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\DWM", "EnableAeroPeek", on ? 0 : 1);
                    BroadcastSetting("WindowMetrics");
                });
            return p;
        }

        Panel BuildPrivPanel()
        {
            Panel p = NewPanel();
            int y = 6;
            y = AddToggleRow(p, y, "Telemetri en aza", "Win10 Pro: 'Basic' + DiagTrack servisini kapatır.",
                delegate { return RegGetInt(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 1) == 0; },
                delegate(bool on) {
                    RegSetInt(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", on ? 0 : 1);
                    if (on) RunPS(@"Stop-Service DiagTrack -Force -ErrorAction SilentlyContinue; Set-Service DiagTrack -StartupType Disabled -ErrorAction SilentlyContinue");
                    else RunPS(@"Set-Service DiagTrack -StartupType Automatic -ErrorAction SilentlyContinue; Start-Service DiagTrack -ErrorAction SilentlyContinue");
                });
            y = AddToggleRow(p, y, "Reklam Kimliği kapalı", "Kişiselleştirilmiş reklam kimliğini kapatır.",
                delegate { return RegGetInt(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", "Enabled", 1) == 0; },
                delegate(bool on) { RegSetInt(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", "Enabled", on ? 0 : 1); });
            y = AddToggleRow(p, y, "Cortana kapalı", "Policy ile kapatır (22H2'de etkisi sınırlı).",
                delegate { return RegGetInt(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana", 1) == 0; },
                delegate(bool on) { RegSetInt(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana", on ? 0 : 1); });
            y = AddToggleRow(p, y, "Etkinlik Geçmişi kapalı", "Zaman tüneli / etkinlik toplamayı kapatır.",
                delegate { return RegGetInt(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System", "EnableActivityFeed", 1) == 0; },
                delegate(bool on) {
                    RegSetInt(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System", "EnableActivityFeed", on ? 0 : 1);
                    RegSetInt(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System", "PublishUserActivities", on ? 0 : 1);
                    RegSetInt(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System", "UploadUserActivities", on ? 0 : 1);
                });
            y = AddToggleRow(p, y, "Önerilen içerik / ipuçları kapalı", "Başlat & ayar önerilerini kapatır (yeniden giriş).",
                delegate { return RegGetInt(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SystemPaneSuggestionsEnabled", 1) == 0; },
                delegate(bool on) {
                    string k = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager";
                    RegSetInt(k, "SystemPaneSuggestionsEnabled", on ? 0 : 1);
                    RegSetInt(k, "SilentInstalledAppsEnabled", on ? 0 : 1);
                    RegSetInt(k, "SoftLandingEnabled", on ? 0 : 1);
                    RegSetInt(k, "SubscribedContent-338388Enabled", on ? 0 : 1);
                    RegSetInt(k, "SubscribedContent-338389Enabled", on ? 0 : 1);
                    RegSetInt(k, "SubscribedContent-353694Enabled", on ? 0 : 1);
                });
            return p;
        }

        Panel BuildNetPanel()
        {
            Panel p = NewPanel();
            int y = 4;
            y = AddActionRow(p, y, "DNS önbelleğini temizle", "ipconfig /flushdns", "Temizle", Theme.Accent,
                delegate { RunTool("DNS Temizle", "DNS önbelleği temizlendi.", delegate { RunPS("ipconfig /flushdns | Out-Null"); }); }, 42);
            y = AddActionRow(p, y, "DNS → 1.1.1.1 (Cloudflare)", "Aktif adaptörlere Cloudflare DNS.", "Uygula", Theme.Accent,
                delegate { if (!CheckKey()) return; RunTool("DNS Ayarı", "DNS Cloudflare (1.1.1.1) yapıldı.", delegate {
                    RunPS("Get-NetAdapter -Physical | Where-Object { $_.Status -eq 'Up' } | Set-DnsClientServerAddress -ServerAddresses ('1.1.1.1','1.0.0.1')"); }); }, 42);
            y = AddActionRow(p, y, "DNS → 8.8.8.8 (Google)", "Aktif adaptörlere Google DNS.", "Uygula", Theme.Accent,
                delegate { if (!CheckKey()) return; RunTool("DNS Ayarı", "DNS Google (8.8.8.8) yapıldı.", delegate {
                    RunPS("Get-NetAdapter -Physical | Where-Object { $_.Status -eq 'Up' } | Set-DnsClientServerAddress -ServerAddresses ('8.8.8.8','8.8.4.4')"); }); }, 42);
            y = AddActionRow(p, y, "DNS → Otomatik (DHCP)", "DNS ayarını varsayılana döndürür.", "Sıfırla", Theme.CardHi,
                delegate { if (!CheckKey()) return; RunTool("DNS Ayarı", "DNS otomatiğe (DHCP) alındı.", delegate {
                    RunPS("Get-NetAdapter -Physical | Where-Object { $_.Status -eq 'Up' } | Set-DnsClientServerAddress -ResetServerAddresses"); }); }, 42);
            y = AddActionRow(p, y, "Nagle algoritması kapalı", "Düşük gecikme (TcpAckFrequency=1).", "Kapat", Theme.Accent,
                delegate { if (!CheckKey()) return; RunTool("Nagle", "Nagle algoritması tüm arayüzlerde kapatıldı.", delegate {
                    RunPS(@"$b='HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces'; Get-ChildItem $b | ForEach-Object { Set-ItemProperty -Path $_.PSPath -Name TcpAckFrequency -Value 1 -Type DWord -Force; Set-ItemProperty -Path $_.PSPath -Name TCPNoDelay -Value 1 -Type DWord -Force }"); }); }, 42);
            y = AddActionRow(p, y, "Nagle varsayılana (geri al)", "TcpAckFrequency/TCPNoDelay siler.", "Geri Al", Theme.CardHi,
                delegate { if (!CheckKey()) return; RunTool("Nagle", "Nagle varsayılana döndürüldü.", delegate {
                    RunPS(@"$b='HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces'; Get-ChildItem $b | ForEach-Object { Remove-ItemProperty -Path $_.PSPath -Name TcpAckFrequency -ErrorAction SilentlyContinue; Remove-ItemProperty -Path $_.PSPath -Name TCPNoDelay -ErrorAction SilentlyContinue }"); }); }, 42);
            y = AddActionRow(p, y, "Ağı sıfırla (Winsock + IP)", "Bağlantı sorunları. REBOOT gerekir.", "Sıfırla", Theme.Red,
                delegate { if (!CheckKey()) return; ShowConfirm("Ağı Sıfırla", "Winsock ve IP yığını sıfırlanacak; bunun için bilgisayarın yeniden başlatılması gerekir.\n\nDevam edilsin mi?", Theme.Red, "Sıfırla",
                    delegate { RunTool("Ağ Sıfırlama", "Ağ sıfırlandı. Lütfen bilgisayarı yeniden başlat.", delegate { RunPS("netsh winsock reset; netsh int ip reset"); }); }); }, 42);
            return p;
        }

        Panel BuildMaintPanel()
        {
            Panel p = NewPanel();
            int y = 6;
            y = AddActionRow(p, y, "Sistem Geri Yükleme Noktası", "Riskli işlem öncesi güvenlik ağı (doğrulanır).", "Oluştur", Theme.Green,
                delegate { RunTool("Geri Yükleme Noktası", "Geri yükleme noktası oluşturuldu (zSwax).", delegate {
                    string r = RunPS(@"$ErrorActionPreference='Stop'; try { $pol=Get-ItemProperty 'HKLM:\SOFTWARE\Policies\Microsoft\Windows NT\SystemRestore' -ErrorAction SilentlyContinue; if ($pol.DisableSR -eq 1) { throw 'System Restore policy ile devre disi (DisableSR=1)' }; Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore' -Name SystemRestorePointCreationFrequency -Value 0 -Type DWord -Force -ErrorAction SilentlyContinue; Enable-ComputerRestore -Drive ($env:SystemDrive + '\'); $b=@(Get-ComputerRestorePoint).Count; Checkpoint-Computer -Description 'zSwax' -RestorePointType 'MODIFY_SETTINGS'; $a=@(Get-ComputerRestorePoint).Count; if ($a -le $b) { throw 'nokta olusturulamadi (System Restore kapali olabilir)' }; 'ZSWAX_OK' } catch { 'ZSWAX_FAIL: ' + $_.Exception.Message }");
                    if (r == null || r.IndexOf("ZSWAX_OK") < 0) throw new Exception(r == null ? "bilinmeyen hata" : r.Replace("ZSWAX_FAIL:", "").Trim());
                }); }, 46);
            y = AddActionRow(p, y, "Gelişmiş disk temizliği", "Temp, prefetch, Update önbelleği, çöp kutusu.", "Temizle", Theme.Accent,
                delegate { if (!CheckKey()) return; ShowConfirm("Disk Temizliği", "Geçici dosyalar, prefetch, Windows Update önbelleği ve geri dönüşüm kutusu temizlenecek.\n\nDevam edilsin mi?", Theme.Accent, "Temizle",
                    delegate { RunTool("Disk Temizliği", "Geçici dosyalar ve önbellekler temizlendi.", delegate { ToolDiskCleanup(); }); }); }, 46);
            y = AddActionRow(p, y, "Başlangıç Yöneticisi", "Açılışta çalışan programları yönet.", "Aç", Theme.CardHi,
                delegate { try { Process.Start("ms-settings:startupapps"); } catch { } }, 46);
            y = AddActionRow(p, y, "Explorer'ı yeniden başlat", "Donmuş görev çubuğu / masaüstü için.", "Yeniden Başlat", Theme.CardHi,
                delegate { if (!CheckKey()) return; RunTool("Explorer", "Windows Explorer yeniden başlatıldı.", delegate {
                    RunPS("Stop-Process -Name explorer -Force -ErrorAction SilentlyContinue; Start-Sleep -Milliseconds 800; if (-not (Get-Process -Name explorer -ErrorAction SilentlyContinue)) { Start-Process explorer }"); }); }, 46);
            y = AddActionRow(p, y, "Tweak'leri Kaldır (varsayılana)", "Oyun + Gizlilik ayarlarını siler/sıfırlar.", "Kaldır", Theme.Red,
                delegate { if (!CheckKey()) return; ShowConfirm("Tweak'leri Kaldır", "Oyun ve Gizlilik tweak'leri varsayılana döndürülecek (policy değerleri silinir, kişisel tercihler sıfırlanır).\n\nDevam edilsin mi?", Theme.Red, "Kaldır",
                    delegate { RestoreDefaults(); }); }, 46);
            return p;
        }

        void BuildOverlay()
        {
            pnlOverlay = new Panel();
            pnlOverlay.SetBounds(0, 0, 460, 726);
            pnlOverlay.BackColor = Theme.Bg;
            pnlOverlay.Visible = false;
            Controls.Add(pnlOverlay);

            icoOverlay = new ShieldBox();
            icoOverlay.Fill = Theme.Amber;
            icoOverlay.SetBounds(196, 190, 68, 72);
            pnlOverlay.Controls.Add(icoOverlay);

            lblOvTitle = new Label();
            lblOvTitle.Font = new Font("Segoe UI Semibold", 15f);
            lblOvTitle.ForeColor = Theme.Text;
            lblOvTitle.BackColor = Theme.Bg;
            lblOvTitle.TextAlign = ContentAlignment.MiddleCenter;
            lblOvTitle.SetBounds(20, 286, 420, 30);
            pnlOverlay.Controls.Add(lblOvTitle);

            lblOvCount = new Label();
            lblOvCount.Font = new Font("Segoe UI", 50f);
            lblOvCount.ForeColor = Theme.Amber;
            lblOvCount.BackColor = Theme.Bg;
            lblOvCount.TextAlign = ContentAlignment.MiddleCenter;
            lblOvCount.SetBounds(0, 318, 460, 86);
            pnlOverlay.Controls.Add(lblOvCount);

            lblOvMsg = new Label();
            lblOvMsg.Font = new Font("Segoe UI", 9.75f);
            lblOvMsg.ForeColor = Theme.Muted;
            lblOvMsg.BackColor = Theme.Bg;
            lblOvMsg.TextAlign = ContentAlignment.MiddleCenter;
            lblOvMsg.AutoSize = false;
            lblOvMsg.SetBounds(28, 344, 404, 170);
            pnlOverlay.Controls.Add(lblOvMsg);

            btnOvPrimary = new RoundButton();
            btnOvPrimary.Radius = 14;
            btnOvPrimary.Font = new Font("Segoe UI Semibold", 11f);
            btnOvPrimary.SetBounds(40, 536, 180, 50);
            btnOvPrimary.Click += delegate
            {
                Action a = pendingAction; pendingAction = null;
                if (a != null) a();
            };
            pnlOverlay.Controls.Add(btnOvPrimary);

            btnOvCancel = new RoundButton();
            btnOvCancel.Radius = 14;
            btnOvCancel.Fill = Theme.CardHi;
            btnOvCancel.FillHover = Theme.Border;
            btnOvCancel.TextColor = Theme.Text;
            btnOvCancel.Font = new Font("Segoe UI Semibold", 11f);
            btnOvCancel.SetBounds(240, 536, 180, 50);
            btnOvCancel.Click += delegate
            {
                if (isCountdown) AbortReboot();
                else { pnlOverlay.Visible = false; pendingAction = null; }
            };
            pnlOverlay.Controls.Add(btnOvCancel);

            timer = new System.Windows.Forms.Timer();
            timer.Interval = 1000;
            timer.Tick += delegate
            {
                counter--;
                if (counter < 0) counter = 0;
                lblOvCount.Text = counter.ToString();
                if (counter <= 0) timer.Stop();
            };
        }

        void ShowTab(int i)
        {
            pnlSB.Visible = (i == 0);
            pnlTpm.Visible = (i == 1);
            pnlHvci.Visible = (i == 2);
            pnlDefender.Visible = (i == 3);
            pnlGame.Visible = (i == 4);
            pnlPriv.Visible = (i == 5);
            pnlNet.Visible = (i == 6);
            pnlMaint.Visible = (i == 7);
            RefreshCurrent();
        }

        void RefreshCurrent()
        {
            switch (seg.Selected)
            {
                case 0: RefreshSecureBoot(); break;
                case 1: RefreshTpm(); break;
                case 2: RefreshHvci(); break;
                case 3: RefreshDefender(); break;
                case 4: case 5: RefreshTweakStates(); break;
            }
        }

        // ----------------------- Overlay modlari -----------------------
        void ShowConfirm(string title, string msg, Color primaryColor, string primaryText, Action onConfirm)
        {
            isCountdown = false;
            pendingAction = onConfirm;
            icoOverlay.Fill = primaryColor;
            lblOvTitle.Text = title;
            lblOvMsg.Text = msg;
            lblOvCount.Visible = false;
            lblOvMsg.SetBounds(28, 344, 404, 170);
            btnOvPrimary.Visible = true;
            btnOvPrimary.Text = primaryText;
            btnOvPrimary.Fill = primaryColor;
            btnOvPrimary.FillHover = Gfx.Hover(primaryColor);
            btnOvPrimary.SetBounds(40, 536, 180, 50);
            btnOvCancel.Visible = true;
            btnOvCancel.Text = "Vazgeç";
            btnOvCancel.SetBounds(240, 536, 180, 50);
            pnlOverlay.Visible = true;
            pnlOverlay.BringToFront();
        }

        void ShowCountdown(string title, string msg)
        {
            isCountdown = true;
            icoOverlay.Fill = Theme.Amber;
            lblOvTitle.Text = title;
            lblOvMsg.Text = msg;
            lblOvCount.Visible = true;
            lblOvCount.Text = COUNTDOWN.ToString();
            lblOvMsg.SetBounds(28, 414, 404, 110);
            btnOvPrimary.Visible = false;
            btnOvCancel.Visible = true;
            btnOvCancel.Text = "İPTAL ET";
            btnOvCancel.SetBounds(130, 538, 200, 50);
            counter = COUNTDOWN;
            pnlOverlay.Visible = true;
            pnlOverlay.BringToFront();
            timer.Start();
        }

        void ShowProcessing(string title, string msg)
        {
            isCountdown = false;
            pendingAction = null;
            icoOverlay.Fill = Theme.Accent;
            lblOvTitle.Text = title;
            lblOvMsg.Text = msg;
            lblOvCount.Visible = false;
            lblOvMsg.SetBounds(28, 344, 404, 170);
            btnOvPrimary.Visible = false;
            btnOvCancel.Visible = false;
            pnlOverlay.Visible = true;
            pnlOverlay.BringToFront();
        }

        void ShowResult(string title, string msg, Color color, string primaryText, Action primaryAction)
        {
            isCountdown = false;
            icoOverlay.Fill = color;
            lblOvTitle.Text = title;
            lblOvMsg.Text = msg;
            lblOvCount.Visible = false;
            lblOvMsg.SetBounds(28, 344, 404, 170);
            if (primaryText != null)
            {
                pendingAction = primaryAction;
                btnOvPrimary.Visible = true;
                btnOvPrimary.Text = primaryText;
                btnOvPrimary.Fill = color;
                btnOvPrimary.FillHover = Gfx.Hover(color);
                btnOvPrimary.SetBounds(40, 536, 180, 50);
                btnOvCancel.Visible = true;
                btnOvCancel.Text = "Kapat";
                btnOvCancel.SetBounds(240, 536, 180, 50);
            }
            else
            {
                pendingAction = null;
                btnOvPrimary.Visible = false;
                btnOvCancel.Visible = true;
                btnOvCancel.Text = "Tamam";
                btnOvCancel.SetBounds(140, 536, 180, 50);
            }
            pnlOverlay.Visible = true;
            pnlOverlay.BringToFront();
        }

        // ----------------------- Secure Boot -----------------------
        void RefreshSecureBoot()
        {
            sbVal.Text = "Kontrol ediliyor...";
            sbVal.ForeColor = Theme.Muted;
            sbDot.ForeColor = Theme.Muted;
            sbHint.Text = "";
            ThreadPool.QueueUserWorkItem(delegate
            {
                string raw = RunPS("try { if (Confirm-SecureBootUEFI) { 'ON' } else { 'OFF' } } catch { 'NA' }");
                string res = raw.Trim().ToUpperInvariant();
                try { this.BeginInvoke((MethodInvoker)delegate { ApplySbStatus(res); }); }
                catch { }
            });
        }

        void ApplySbStatus(string res)
        {
            Color c; string t; string hint;
            if (res == "ON") { c = Theme.Green; t = "AÇIK"; hint = "Secure Boot AÇIK (gerçek UEFI durumu).\nKAPATMAK için yeniden başlat → UEFI'de 'Disabled' yap → F10."; }
            else if (res == "OFF") { c = Theme.Red; t = "KAPALI"; hint = "Secure Boot KAPALI (gerçek UEFI durumu).\nAÇMAK için yeniden başlat → UEFI'de 'Enabled' yap → F10."; }
            else { c = Theme.Amber; t = "BİLİNMİYOR"; hint = "Durum okunamadı. Sistem Legacy BIOS modunda olabilir ya da yetki gerekiyor."; }
            sbDot.ForeColor = c; sbVal.ForeColor = c; sbVal.Text = t; sbHint.Text = hint;
        }

        void OnSecureBootReboot()
        {
            if (!CheckKey()) return;
            FinishWithReboot(true, "UEFI ayar ekranına geçilecek.\nSecure Boot'u oradan Aç/Kapat yap, F10 ile kaydet.");
        }

        // -------------------------- TPM --------------------------
        int? ReadTpmStart()
        {
            try
            {
                using (RegistryKey k = Registry.LocalMachine.OpenSubKey(TPM_KEY))
                {
                    if (k == null) return null;
                    object v = k.GetValue("Start");
                    if (v == null) return null;
                    return Convert.ToInt32(v);
                }
            }
            catch { return null; }
        }

        bool SetTpmStart(int val)
        {
            try { Registry.SetValue(TPM_KEY_FULL, "Start", val, RegistryValueKind.DWord); return true; }
            catch { return false; }
        }

        void RefreshTpm()
        {
            tpmVal.Text = "Kontrol ediliyor...";
            tpmVal.ForeColor = Theme.Muted; tpmDot.ForeColor = Theme.Muted; tpmHint.Text = "";
            int? s = ReadTpmStart();
            ThreadPool.QueueUserWorkItem(delegate
            {
                string raw = RunPS(TPM_QUERY);
                try { this.BeginInvoke((MethodInvoker)delegate { ApplyTpmStatus(raw, s); }); }
                catch { }
            });
        }

        void ApplyTpmStatus(string raw, int? s)
        {
            string[] p = (raw == null ? "" : raw.Trim()).Split(';');
            string present = p.Length > 0 ? p[0].Trim().ToUpperInvariant() : "";
            string startTxt = s == null ? "?" : s.Value.ToString();
            Color c; string t; string hint; bool tpmThere;
            if (present == "TRUE")
            {
                tpmThere = true;
                if (s.HasValue && s.Value == 4) { c = Theme.Red; t = "DEVRE DIŞI"; hint = "TPM mevcut ama Windows sürücüsü KAPALI (Start=4). Yeniden başlatma gerekir."; }
                else { c = Theme.Green; t = "AKTİF"; hint = "TPM mevcut ve Windows tarafından kullanılıyor (Start=" + startTxt + ")."; }
            }
            else if (present == "FALSE")
            {
                tpmThere = false;
                c = Theme.Amber; t = "TPM YOK";
                hint = "Cihazda TPM bulunamadı (Get-Tpm: TpmPresent=False).\nServis Start=" + startTxt + " yalnızca Windows sürücü modudur; donanım yok, butonlar etkisiz.";
            }
            else
            {
                tpmThere = false;
                c = Theme.Amber; t = "BİLİNMİYOR";
                hint = "TPM durumu okunamadı (Get-Tpm hatası). Servis Start=" + startTxt + ".";
            }
            tpmDot.ForeColor = c; tpmVal.ForeColor = c; tpmVal.Text = t; tpmHint.Text = hint;
            if (tpmBtnOff != null) tpmBtnOff.Enabled = tpmThere;
            if (tpmBtnOn != null) tpmBtnOn.Enabled = tpmThere;
        }

        void OnTpmDisable()
        {
            if (!CheckKey()) return;
            bool bl = BitLockerOn();
            string msg = bl
                ? "DİKKAT: BitLocker AÇIK görünüyor!\nTPM sürücüsünü kapatırsan açılışta BitLocker KURTARMA ANAHTARI istenebilir. Anahtar elinde değilse sistemine giremezsin.\n\nYine de devam edilsin mi?"
                : "Windows TPM sürücüsü devre dışı bırakılacak (Start=4). Firmware'deki TPM çipi açık kalır.\nBitLocker/Aygıt Şifrelemesi kullanıyorsan kurtarma anahtarını hazır bulundur.\n\nDevam edilsin mi?";
            ShowConfirm("TPM Sürücüsünü Devre Dışı Bırak", msg, Theme.Red, "Devre Dışı Bırak", delegate
            {
                if (SetTpmStart(4)) FinishWithReboot(false, "TPM sürücüsü devre dışı bırakıldı.\nDeğişiklik için yeniden başlatılıyor.");
                else { pnlOverlay.Visible = false; ShowError("TPM ayarı yazılamadı. Yönetici izni gerekiyor."); }
            });
        }

        void OnTpmEnable()
        {
            if (!CheckKey()) return;
            ShowConfirm("TPM Sürücüsünü Aktif Et",
                "Windows TPM sürücüsü varsayılana (Start=3) alınacak ve bilgisayar yeniden başlatılacak.\n\nDevam edilsin mi?",
                Theme.Green, "Aktif Et", delegate
            {
                if (SetTpmStart(3)) FinishWithReboot(false, "TPM sürücüsü aktif edildi.\nDeğişiklik için yeniden başlatılıyor.");
                else { pnlOverlay.Visible = false; ShowError("TPM ayarı yazılamadı. Yönetici izni gerekiyor."); }
            });
        }

        bool BitLockerOn()
        {
            string r = RunPS("try { [int](Get-BitLockerVolume -MountPoint $env:SystemDrive).ProtectionStatus } catch { '-1' }");
            return (r == null ? "" : r.Trim()) == "1";
        }

        // ------------------- Cekirdek Yalitimi (HVCI) -------------------
        int? ReadHvci()
        {
            try
            {
                using (RegistryKey k = Registry.LocalMachine.OpenSubKey(HVCI_KEY))
                {
                    if (k == null) return null;
                    object v = k.GetValue("Enabled");
                    if (v == null) return null;
                    return Convert.ToInt32(v);
                }
            }
            catch { return null; }
        }

        bool SetHvci(int val)
        {
            try
            {
                if (val == 1)
                {
                    Registry.SetValue(DG_FULL, "EnableVirtualizationBasedSecurity", 1, RegistryValueKind.DWord);
                    Registry.SetValue(DG_FULL, "RequirePlatformSecurityFeatures", 1, RegistryValueKind.DWord);
                    Registry.SetValue(DG_FULL, "Locked", 0, RegistryValueKind.DWord);
                }
                Registry.SetValue(HVCI_KEY_FULL, "Enabled", val, RegistryValueKind.DWord);
                return true;
            }
            catch { return false; }
        }

        void RefreshHvci()
        {
            hvciVal.Text = "Kontrol ediliyor...";
            hvciVal.ForeColor = Theme.Muted; hvciDot.ForeColor = Theme.Muted; hvciHint.Text = "";
            int? reg = ReadHvci();
            ThreadPool.QueueUserWorkItem(delegate
            {
                string raw = RunPS(HVCI_QUERY);
                try { this.BeginInvoke((MethodInvoker)delegate { ApplyHvciStatus(raw, reg); }); }
                catch { }
            });
        }

        void ApplyHvciStatus(string raw, int? reg)
        {
            string[] p = (raw == null ? "" : raw.Trim()).Split(';');
            bool running = p.Length > 0 && p[0].Trim() == "1";
            bool configured = p.Length > 1 && p[1].Trim() == "1";
            Color c; string t; string hint;
            if (running)
            {
                c = Theme.Green; t = "AÇIK (çalışıyor)";
                hint = "Bellek Bütünlüğü gerçekten ÇALIŞIYOR (HVCI running). Sistem korumalı.";
            }
            else if (configured || (reg.HasValue && reg.Value == 1))
            {
                c = Theme.Amber; t = "BEKLEMEDE — ÇALIŞMIYOR";
                hint = "HVCI yapılandırılmış ama henüz ÇALIŞMIYOR.\nNedeni: yeniden başlatma gerekiyor VEYA uyumsuz sürücü/sanallaştırma engeli.\nYeniden başlatıp bu ekrandan doğrula.";
            }
            else
            {
                c = Theme.Red; t = "KAPALI";
                hint = "Bellek Bütünlüğü KAPALI.\nAçmak için 'Aktif Et' → yeniden başlat (uyumsuz sürücü varsa Windows engelleyebilir).";
            }
            hvciDot.ForeColor = c; hvciVal.ForeColor = c; hvciVal.Text = t; hvciHint.Text = hint;
        }

        void OnHvciDisable()
        {
            if (!CheckKey()) return;
            ShowConfirm("Çekirdek Yalıtımını Kapat",
                "Bellek Bütünlüğü (HVCI) devre dışı bırakılacak (Enabled=0).\nBu bir güvenlik özelliğidir; kapatmak korumayı azaltır.\n\nDevam edilsin mi?",
                Theme.Red, "Devre Dışı Bırak", delegate
            {
                if (SetHvci(0)) FinishWithReboot(false, "Çekirdek Yalıtımı kapatıldı (HVCI=0).\nDeğişiklik için yeniden başlatılıyor.");
                else { pnlOverlay.Visible = false; ShowError("Ayar yazılamadı. Yönetici izni gerekiyor."); }
            });
        }

        void OnHvciEnable()
        {
            if (!CheckKey()) return;
            ShowConfirm("Çekirdek Yalıtımını Aç",
                "Bellek Bütünlüğü (HVCI) + VBS yapılandırılacak ve bilgisayar yeniden başlatılacak.\nWindows uyumsuz sürücü bulursa açılmayabilir; yeniden başlatma sonrası bu ekrandan GERÇEK durumu doğrula.\n\nDevam edilsin mi?",
                Theme.Green, "Aktif Et", delegate
            {
                if (SetHvci(1)) FinishWithReboot(false, "Çekirdek Yalıtımı yapılandırıldı.\nYeniden başlatılıyor — sonra durumu doğrula.");
                else { pnlOverlay.Visible = false; ShowError("Ayar yazılamadı. Yönetici izni gerekiyor."); }
            });
        }

        // ----------------------- Windows Defender -----------------------
        void RefreshDefender()
        {
            defVal.Text = "Kontrol ediliyor...";
            defVal.ForeColor = Theme.Muted;
            defDot.ForeColor = Theme.Muted;
            defHint.Text = "";
            ThreadPool.QueueUserWorkItem(delegate
            {
                string raw = RunPS(DEF_QUERY);
                try { this.BeginInvoke((MethodInvoker)delegate { ApplyDefenderStatus(raw); }); }
                catch { }
            });
        }

        void ApplyDefenderStatus(string raw)
        {
            string[] parts = (raw == null ? "" : raw.Trim()).Split(';');
            string rt = parts.Length > 0 ? parts[0].Trim().ToUpperInvariant() : "";
            string tp = parts.Length > 1 ? parts[1].Trim().ToUpperInvariant() : "";
            defRealtime = (rt == "TRUE");
            defTamper = (tp == "TRUE");

            Color c; string t;
            if (rt == "TRUE") { c = Theme.Green; t = "KORUMA AÇIK"; }
            else if (rt == "FALSE") { c = Theme.Red; t = "KORUMA KAPALI"; }
            else { c = Theme.Amber; t = "BİLİNMİYOR"; }

            string tamperNote = (tp == "TRUE")
                ? "Kurcalama Koruması (Tamper) AÇIK — programla kapatma ENGELLENİR; önce Windows Güvenliği'nden elle kapat."
                : (tp == "FALSE")
                ? "Kurcalama Koruması KAPALI — kapatma uygulanabilir."
                : "Kurcalama Koruması durumu okunamadı.";

            defDot.ForeColor = c; defVal.ForeColor = c; defVal.Text = t;
            defHint.Text = "Gerçek zamanlı koruma (canlı durum).\n" + tamperNote;
        }

        void OnDefenderDisable()
        {
            if (!CheckKey()) return;
            string msg = defTamper
                ? "DİKKAT: Kurcalama Koruması (Tamper Protection) AÇIK!\nBu yüzden Defender PROGRAMLA KAPATILAMAZ. Önce Windows Güvenliği > Virüs ve tehdit koruması > Ayarları yönet'ten 'Kurcalama koruması'nı KAPAT, sonra tekrar dene.\n\nYine de denensin mi?"
                : "Windows Defender'ın gerçek zamanlı koruması ve tüm tarama modülleri KAPATILACAK.\nBu, bilgisayarını zararlı yazılımlara karşı korumasız bırakır.\n\nDevam edilsin mi?";
            ShowConfirm("Defender'ı Tam Kapat", msg, Theme.Red, "Tam Kapat", delegate { RunDefenderAsync(true); });
        }

        void OnDefenderEnable()
        {
            if (!CheckKey()) return;
            ShowConfirm("Defender'ı Tam Aç",
                "Windows Defender'ın tüm koruma modülleri yeniden AÇILACAK (önerilen).\n\nDevam edilsin mi?",
                Theme.Green, "Tam Aç", delegate { RunDefenderAsync(false); });
        }

        void RunDefenderAsync(bool disable)
        {
            ShowProcessing(disable ? "Defender Kapatılıyor" : "Defender Açılıyor", "Lütfen bekle, ayarlar uygulanıyor...");
            ThreadPool.QueueUserWorkItem(delegate
            {
                RunPS(disable ? DEF_OFF : DEF_ON);
                bool cleaned = false;
                if (cleanLogs) { CleanLogs(); cleaned = true; }
                string raw = RunPS(DEF_QUERY);
                try { this.BeginInvoke((MethodInvoker)delegate { DefenderResult(disable, raw, cleaned); }); }
                catch { }
            });
        }

        void DefenderResult(bool disable, string raw, bool cleaned)
        {
            ApplyDefenderStatus(raw);
            string extra = cleaned ? "\n\nLoglar ve geçici dosyalar temizlendi." : "";
            if (disable)
            {
                if (!defRealtime)
                    ShowResult("Defender Kapatıldı", "Gerçek zamanlı koruma artık KAPALI." + extra, Theme.Green, null, null);
                else
                    ShowResult("Kapatılamadı",
                        "Koruma hâlâ açık. Büyük ihtimalle Kurcalama Koruması (Tamper) engelliyor. Windows Güvenliği'nden kapatıp tekrar dene." + extra,
                        Theme.Amber, "Windows Güvenliğini Aç", delegate { pnlOverlay.Visible = false; OpenDefenderSettings(); });
            }
            else
            {
                if (defRealtime)
                    ShowResult("Defender Açıldı", "Tüm koruma modülleri yeniden aktif." + extra, Theme.Green, null, null);
                else
                    ShowResult("Tam Açılamadı",
                        "Koruma tam açılamadı. Windows Güvenliği'ni kontrol et." + extra,
                        Theme.Amber, "Windows Güvenliğini Aç", delegate { pnlOverlay.Visible = false; OpenDefenderSettings(); });
            }
        }

        void OpenDefenderSettings()
        {
            try { Process.Start("windowsdefender://threatsettings"); }
            catch { try { Process.Start("windowsdefender:"); } catch { } }
        }

        void OnAddExclusion()
        {
            if (!CheckKey()) return;
            string folder = Application.StartupPath;
            ShowConfirm("Defender İzni (Exclusion)",
                "Bu klasör Windows Defender taramasından hariç tutulacak; böylece zSwax.exe silinmez:\n" + folder + "\n\n(Kurcalama Koruması AÇIKSA bu işlem engellenir — önce onu kapatman gerekir.)\n\nDevam edilsin mi?",
                Theme.Accent, "İzin Ver", delegate { RunExclusionAsync(folder); });
        }

        void RunExclusionAsync(string folder)
        {
            ShowProcessing("İzin Ekleniyor", "Lütfen bekle, Defender ayarı uygulanıyor...");
            ThreadPool.QueueUserWorkItem(delegate
            {
                string esc = folder.Replace("'", "''");
                string cmd = "$f='" + esc + "'; try { Add-MpPreference -ExclusionPath $f -ErrorAction Stop } catch {}; $ex=(Get-MpPreference).ExclusionPath; if ($ex | Where-Object { $_.TrimEnd([char]92) -ieq $f.TrimEnd([char]92) }) { 'OK' } else { 'NO' }";
                string raw = RunPS(cmd);
                bool ok = raw != null && raw.Trim().ToUpperInvariant().Contains("OK");
                try { this.BeginInvoke((MethodInvoker)delegate { ExclusionResult(ok, folder); }); }
                catch { }
            });
        }

        void ExclusionResult(bool ok, string folder)
        {
            if (ok)
                ShowResult("İzin Eklendi", folder + "\nartık Defender taramasından hariç. zSwax.exe silinmeyecek.", Theme.Green, null, null);
            else
                ShowResult("Eklenemedi",
                    "Muhtemelen Kurcalama Koruması açık. Önce 'Kurcalama Korumasını Aç' ile Tamper'ı kapat, ya da Windows Güvenliği > Koruma geçmişi'nden zSwax'a 'İzin ver' de.",
                    Theme.Amber, "Windows Güvenliğini Aç", delegate { pnlOverlay.Visible = false; OpenDefenderSettings(); });
        }

        // ----------------------- Tweak / Arac -----------------------
        void RefreshTweakStates()
        {
            foreach (TweakRow tr in tweaks)
            {
                try { tr.Sw.Checked = tr.IsOn(); tr.Sw.Invalidate(); } catch { }
            }
        }

        void RestoreDefaults()
        {
            // policy degerlerini SIL (Windows varsayilaninda yok)
            RegDel(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry");
            RegDel(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\GameDVR", "AllowGameDVR");
            RegDel(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana");
            RegDel(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System", "EnableActivityFeed");
            RegDel(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System", "PublishUserActivities");
            RegDel(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System", "UploadUserActivities");
            // HKCU tercihlerini varsayilana al
            RegSetInt(@"HKEY_CURRENT_USER\System\GameConfigStore", "GameDVR_Enabled", 1);
            RegSetInt(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", "Enabled", 1);
            RegSetInt(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SystemPaneSuggestionsEnabled", 1);
            RegSetStr(@"HKEY_CURRENT_USER\Control Panel\Mouse", "MouseSpeed", "1");
            RegSetStr(@"HKEY_CURRENT_USER\Control Panel\Mouse", "MouseThreshold1", "6");
            RegSetStr(@"HKEY_CURRENT_USER\Control Panel\Mouse", "MouseThreshold2", "10");
            ApplyMouseLive(false);
            RegSetInt(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", "VisualFXSetting", 1);
            RunPS(@"Set-Service DiagTrack -StartupType Automatic -ErrorAction SilentlyContinue; Start-Service DiagTrack -ErrorAction SilentlyContinue");
            RefreshTweakStates();
            ShowResult("Tweak'ler Kaldırıldı", "Oyun ve Gizlilik tweak'leri varsayılana döndürüldü (policy değerleri silindi).\n(HAGS donanım varsayılanında bırakıldı.)", Theme.Green, null, null);
        }

        void ToolDiskCleanup()
        {
            string win = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            TryClearDir(Path.GetTempPath());
            TryClearDir(Path.Combine(win, "Temp"));
            TryClearDir(Path.Combine(win, "Prefetch"));
            TryClearDir(Path.Combine(win, "SoftwareDistribution\\Download"));
            try { TryClearDir(Environment.GetFolderPath(Environment.SpecialFolder.InternetCache)); } catch { }
            RunPS("Clear-RecycleBin -Force -ErrorAction SilentlyContinue");
        }

        void RunTool(string title, string okMsg, Action work)
        {
            ShowProcessing(title, "Lütfen bekle, işlem uygulanıyor...");
            ThreadPool.QueueUserWorkItem(delegate
            {
                bool ok = true; string emsg = "";
                try { work(); } catch (Exception ex) { ok = false; emsg = ex.Message; }
                try { this.BeginInvoke((MethodInvoker)delegate {
                    ShowResult(title, ok ? okMsg : ("İşlem başarısız: " + Short(emsg)), ok ? Theme.Green : Theme.Red, null, null);
                }); }
                catch { }
            });
        }

        string Short(string s)
        {
            if (s == null) return "";
            s = s.Replace("\r", " ").Replace("\n", " ").Trim();
            return s.Length <= 200 ? s : s.Substring(0, 200) + "…";
        }

        void BroadcastSetting(string area)
        {
            try { IntPtr r; SendMessageTimeout((IntPtr)HWND_BROADCAST, WM_SETTINGCHANGE, IntPtr.Zero, area, 0x2, 1000, out r); }
            catch { }
        }

        void ApplyMouseLive(bool on)
        {
            try
            {
                int[] mp = on ? new int[] { 0, 0, 0 } : new int[] { 6, 10, 1 };
                SystemParametersInfo(SPI_SETMOUSE, 0, mp, SPIF_UPDATE | SPIF_SEND);
            }
            catch { }
        }

        // ----------------------- Registry yardimcilari -----------------------
        object RegGet(string fullKey, string name)
        {
            try { return Registry.GetValue(fullKey, name, null); } catch { return null; }
        }
        int RegGetInt(string fullKey, string name, int def)
        {
            object o = RegGet(fullKey, name);
            if (o == null) return def;
            try { return Convert.ToInt32(o); } catch { return def; }
        }
        string RegGetStr(string fullKey, string name, string def)
        {
            object o = RegGet(fullKey, name);
            if (o == null) return def;
            return o.ToString();
        }
        void RegSetInt(string fullKey, string name, int val)
        {
            try { Registry.SetValue(fullKey, name, val, RegistryValueKind.DWord); } catch { }
        }
        void RegSetStr(string fullKey, string name, string val)
        {
            try { Registry.SetValue(fullKey, name, val, RegistryValueKind.String); } catch { }
        }
        void RegDel(string fullKey, string name)
        {
            try
            {
                RegistryKey root; string sub;
                if (fullKey.StartsWith(@"HKEY_LOCAL_MACHINE\")) { root = Registry.LocalMachine; sub = fullKey.Substring(19); }
                else if (fullKey.StartsWith(@"HKEY_CURRENT_USER\")) { root = Registry.CurrentUser; sub = fullKey.Substring(18); }
                else return;
                using (RegistryKey k = root.OpenSubKey(sub, true)) { if (k != null) k.DeleteValue(name, false); }
            }
            catch { }
        }

        // ----------------------- Ortak akis -----------------------
        void FinishWithReboot(bool firmware, string okMsg)
        {
            string extra = "";
            if (cleanLogs) { CleanLogs(); extra = "\n\nLoglar ve geçici dosyalar temizlendi."; }
            if (ScheduleReboot(firmware))
                ShowCountdown("Yeniden Başlatılıyor", okMsg + extra);
            else
            {
                pnlOverlay.Visible = false;
                ShowError("Yeniden başlatma planlanamadı. Zaten bekleyen bir kapatma olabilir, ya da firmware /fw desteklemiyor / yetki yok.");
                RefreshCurrent();
            }
        }

        void CleanLogs()
        {
            string[] logs = { "Application", "System", "Setup" };
            foreach (string lg in logs)
            {
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo("wevtutil.exe", "cl \"" + lg + "\"");
                    psi.UseShellExecute = false;
                    psi.CreateNoWindow = true;
                    Process p = Process.Start(psi);
                    if (p != null) p.WaitForExit(4000);
                }
                catch { }
            }
            TryClearDir(Path.GetTempPath());
            try { TryClearDir(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp")); }
            catch { }
        }

        void TryClearDir(string dir)
        {
            try
            {
                if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return;
                foreach (string f in Directory.GetFiles(dir))
                {
                    try { File.SetAttributes(f, FileAttributes.Normal); File.Delete(f); } catch { }
                }
                foreach (string d in Directory.GetDirectories(dir))
                {
                    try { Directory.Delete(d, true); } catch { }
                }
            }
            catch { }
        }

        bool CheckKey()
        {
            HideError();
            string key = txtKey.Text.Trim();
            if (key.Length == 0) { ShowError("Lütfen erişim anahtarını girin."); txtKey.Focus(); return false; }
            if (!string.Equals(key, accessKey, StringComparison.OrdinalIgnoreCase))
            {
                ShowError("Hatalı erişim anahtarı.");
                txtKey.SelectAll(); txtKey.Focus();
                return false;
            }
            return true;
        }

        bool ScheduleReboot(bool firmware)
        {
            try
            {
                string a = firmware
                    ? "/r /fw /t " + COUNTDOWN + " /c \"zSwax - UEFI ayarlarina yeniden baslatiliyor\""
                    : "/r /t " + COUNTDOWN + " /c \"zSwax - yeniden baslatiliyor\"";
                ProcessStartInfo psi = new ProcessStartInfo("shutdown.exe", a);
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                using (Process p = Process.Start(psi))
                {
                    p.WaitForExit(6000);
                    return p.ExitCode == 0;
                }
            }
            catch { return false; }
        }

        void AbortReboot()
        {
            timer.Stop();
            TryAbortSilent();
            pnlOverlay.Visible = false;
            RefreshCurrent();
        }

        void TryAbortSilent()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("shutdown.exe", "/a");
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                Process p = Process.Start(psi);
                if (p != null) p.WaitForExit(3000);
            }
            catch { }
        }

        string RunPS(string command)
        {
            Process p = null;
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("powershell.exe",
                    "-NoProfile -ExecutionPolicy Bypass -Command \"" + command + "\"");
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                p = Process.Start(psi);
                Process pp = p;
                Thread te = new Thread(delegate() { try { pp.StandardError.ReadToEnd(); } catch { } });
                te.IsBackground = true;
                te.Start();
                string o = p.StandardOutput.ReadToEnd();
                if (!p.WaitForExit(90000)) { try { p.Kill(); } catch { } }
                try { te.Join(500); } catch { }
                return o;
            }
            catch { return ""; }
            finally { try { if (p != null) p.Dispose(); } catch { } }
        }

        void ShowError(string msg) { lblError.Text = msg; lblError.Visible = true; }
        void HideError() { lblError.Visible = false; }
    }
}
