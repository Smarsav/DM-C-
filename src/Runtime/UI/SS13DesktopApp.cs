using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using DMToCSharp.Core;
using DMToCSharp.Runtime.Atmos;
using DMToCSharp.Runtime.Audio;
using DMToCSharp.Runtime.Chemistry;
using DMToCSharp.Runtime.Database;
using DMToCSharp.Runtime.GameModes;
using DMToCSharp.Runtime.Graphics;
using DMToCSharp.Runtime.Health;
using DMToCSharp.Runtime.Items;
using DMToCSharp.Runtime.Lighting;
using DMToCSharp.Runtime.Maps;
using DMToCSharp.Runtime.MC;
using DMToCSharp.Runtime.Network;
using DMToCSharp.Runtime.Power;
using DMToCSharp.Runtime.Radio;
using DMToCSharp.Runtime.Silicon;

namespace DMToCSharp.Runtime.UI
{
    public class SS13DesktopApp : Form
    {
        private Timer _gameLoopTimer;
        private Panel _canvasPanel;
        private Label _lblStatus;
        private Label _lblHealth;
        private ProgressBar _pbHealth;
        private Label _lblActiveItem;
        private Label _lblAtmos;
        private Label _lblPower;
        private ProgressBar _pbAPC;
        private Label _lblAI;
        private TextBox _txtLaws;
        private ListBox _lstChat;
        private TextBox _txtChatInput;
        private ComboBox _cbFreq;
        private Label _lblInspector;
        private Label _lblGameMode;

        public GasMixture StationAir { get; private set; }
        public APC StationAPC { get; private set; }
        public SMES StationSMES { get; private set; }
        public OrganismHealth PlayerHealth { get; private set; }
        public InventorySystem PlayerInventory { get; private set; }
        public ReagentContainer ChemStation { get; private set; }
        public DMClient LocalPlayer { get; private set; }
        public AICore StationAI { get; private set; }

        public SS13DesktopApp()
        {
            InitializeEngine();
            InitializeComponent();
        }

        private void InitializeEngine()
        {
            StationAir = GasMixture.CreateStandardStationAir();
            StationAPC = new APC("Bridge", 50000.0);
            StationSMES = new SMES(5000000.0);
            PlayerHealth = new OrganismHealth(100.0);
            PlayerInventory = new InventorySystem();
            ChemStation = new ReagentContainer(100.0);
            LocalPlayer = ClientManager.DefaultPlayer;
            StationAI = new AICore("Station Master AI");

            // Equip default items
            PlayerInventory.EquipItem(InvSlot.RightHand, new DM_tool(ToolType.Crowbar, "Mechanical Crowbar"));
            PlayerInventory.EquipItem(InvSlot.LeftHand, new DM_tool(ToolType.Welder, "Industrial Welder"));
            PlayerInventory.EquipItem(InvSlot.Belt, new DM_tool(ToolType.Screwdriver, "Screwdriver"));
            PlayerInventory.EquipItem(InvSlot.IdCard, new DM_item("Captain ID Card"));

            // Stock Chemistry Station
            ChemStation.AddReagent("water", 30.0);
            ChemStation.AddReagent("welding_fuel", 20.0);
            ChemStation.AddReagent("epinephrine", 15.0);

            // Register in Master Controller
            MasterController.Instance.RegisterSubsystem(SSAir.Instance);
            MasterController.Instance.RegisterSubsystem(SSPower.Instance);
            MasterController.Instance.RegisterSubsystem(SSRadio.Instance);
            MasterController.Instance.RegisterSubsystem(SSLighting.Instance);
            MasterController.Instance.RegisterSubsystem(SSAudio.Instance);
            MasterController.Instance.RegisterSubsystem(SSGameMode.Instance);
            MasterController.Instance.RegisterSubsystem(SSDatabase.Instance);

            SSPower.Instance.RegisterAPC(StationAPC);
            SSPower.Instance.RegisterSMES(StationSMES);

            // Initialize 16x16 Station Layout
            InitializeDefaultStationMap();

            // Register Light Source on Player Mob
            if (LocalPlayer.Mob != null)
            {
                SSLighting.Instance.RegisterLight(new LightSource(LocalPlayer.Mob, 5, 1.0, "#60a5fa"));
            }

            // Initial Radio Broadcasts
            SSRadio.Instance.Broadcast("Station AI", "AI", SSRadio.FREQ_COMMON, "Welcome to Space Station 13 (.NET Desktop Engine). All systems active.");
            SSRadio.Instance.Broadcast("Chief Medical Officer", "Medical", SSRadio.FREQ_MEDICAL, "Medbay triage active and ready.");
            SSRadio.Instance.Broadcast("Head of Security", "Security", SSRadio.FREQ_SECURITY, "Station security level Code Green.");
        }

        private void InitializeDefaultStationMap()
        {
            var grid = DMSpatialGrid.Instance;
            if (grid.GetTurf(2, 2, 1) != null) return;

            int size = 16;
            for (int y = 1; y <= size; y++)
            {
                for (int x = 1; x <= size; x++)
                {
                    bool isOuterWall = (x == 1 || x == size || y == 1 || y == size);
                    bool isInternalWall = (x == 8 && y != 8 && y != 9) || (y == 8 && x != 8 && x != 9);
                    bool isAirlock = (x == 8 && (y == 8 || y == 9)) || (y == 8 && (x == 8 || x == 9));

                    var turf = new DM_turf();
                    turf.x = new DMValue(x);
                    turf.y = new DMValue(y);
                    turf.z = new DMValue(1);

                    if (isOuterWall || isInternalWall)
                    {
                        turf.name = new DMValue("reinforced wall");
                        turf.density = new DMValue(true);
                        turf.opacity = new DMValue(true);
                    }
                    else
                    {
                        turf.name = new DMValue("station floor");
                        turf.density = new DMValue(false);
                        turf.opacity = new DMValue(false);

                        if (isAirlock)
                        {
                            var door = new DM_obj();
                            door.name = new DMValue("secure airlock");
                            door.density = new DMValue(false);
                            door.SetVar("bolted", new DMValue(false));
                            door.SetVar("opened", new DMValue(false));
                            turf.contents.Add(new DMValue(door));
                        }
                    }

                    grid.SetTurf(x, y, 1, turf);
                }
            }

            // Spawn player in Bridge at (4, 12, 1)
            if (LocalPlayer.Mob != null)
            {
                LocalPlayer.Mob.x = new DMValue(4);
                LocalPlayer.Mob.y = new DMValue(12);
                LocalPlayer.Mob.z = new DMValue(1);
                var spawnTurf = grid.GetTurf(4, 12, 1);
                if (spawnTurf != null)
                {
                    LocalPlayer.Mob.loc = new DMValue(spawnTurf);
                    spawnTurf.contents.Add(new DMValue(LocalPlayer.Mob));
                }
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Space Station 13 - .NET Desktop Game Client & Runtime";
            this.Size = new Size(1100, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(9, 13, 22);
            this.ForeColor = Color.FromArgb(226, 232, 240);
            this.Font = new Font("Segoe UI", 9.5f);
            this.KeyPreview = true;

            // 2D Game Canvas Panel
            _canvasPanel = new DoubleBufferedPanel();
            _canvasPanel.Location = new Point(20, 20);
            _canvasPanel.Size = new Size(520, 520);
            _canvasPanel.BackColor = Color.FromArgb(2, 6, 23);
            _canvasPanel.Paint += CanvasPanel_Paint;
            _canvasPanel.MouseClick += CanvasPanel_MouseClick;
            this.Controls.Add(_canvasPanel);

            // Controls Hint
            Label lblHint = new Label();
            lblHint.Text = "🎮 Keyboard Controls: Use W / A / S / D or Arrow Keys to move your avatar.";
            lblHint.Location = new Point(20, 550);
            lblHint.Size = new Size(520, 25);
            lblHint.ForeColor = Color.FromArgb(96, 165, 250);
            lblHint.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            this.Controls.Add(lblHint);

            // Tile Inspector
            _lblInspector = new Label();
            _lblInspector.Text = "Tile Inspector: Click any tile on the radar to inspect coordinates and contents.";
            _lblInspector.Location = new Point(20, 580);
            _lblInspector.Size = new Size(520, 50);
            _lblInspector.ForeColor = Color.FromArgb(148, 163, 184);
            this.Controls.Add(_lblInspector);

            // Right HUD Container
            int rightX = 560;

            // Player Status Box
            GroupBox gbPlayer = CreateGroupBox("PLAYER & HEALTH HUD", rightX, 20, 500, 110);
            _lblHealth = new Label { Location = new Point(15, 25), Size = new Size(200, 20), Text = "Health: 100 / 100 HP (Healthy)", ForeColor = Color.FromArgb(16, 185, 129) };
            _pbHealth = new ProgressBar { Location = new Point(15, 48), Size = new Size(220, 16), Value = 100 };
            _lblActiveItem = new Label { Location = new Point(250, 25), Size = new Size(230, 20), Text = "Active Item: Mechanical Crowbar", ForeColor = Color.FromArgb(96, 165, 250) };
            
            Button btnSwap = CreateButton("Swap Hands", 250, 48, 110, 28, (s, e) => { PlayerInventory.SwapHands(); UpdateUI(); });
            Button btnMed = CreateButton("Use Medkit", 370, 48, 110, 28, (s, e) => { PlayerHealth.HealDamage(DamageType.Brute, 15); UpdateUI(); });
            
            gbPlayer.Controls.Add(_lblHealth);
            gbPlayer.Controls.Add(_pbHealth);
            gbPlayer.Controls.Add(_lblActiveItem);
            gbPlayer.Controls.Add(btnSwap);
            gbPlayer.Controls.Add(btnMed);
            this.Controls.Add(gbPlayer);

            // Atmos & Power Box
            GroupBox gbAtmos = CreateGroupBox("ATMOSPHERICS & POWER", rightX, 140, 500, 110);
            _lblAtmos = new Label { Location = new Point(15, 25), Size = new Size(230, 20), Text = "Atmos: 101.3 kPa | 20.0 °C", ForeColor = Color.FromArgb(56, 189, 248) };
            _lblPower = new Label { Location = new Point(250, 25), Size = new Size(230, 20), Text = "APC Battery: 100.0%", ForeColor = Color.FromArgb(245, 158, 11) };
            _pbAPC = new ProgressBar { Location = new Point(250, 48), Size = new Size(230, 16), Value = 100 };
            
            Button btnVent = CreateButton("Vent Air", 15, 52, 105, 28, (s, e) => { StationAir.RemoveRatio(0.15); UpdateUI(); });
            Button btnRepress = CreateButton("Repressurize", 125, 52, 110, 28, (s, e) => { StationAir.AdjustMoles(GasType.Oxygen, 5); UpdateUI(); });
            Button btnDoor = CreateButton("Toggle Airlock", 250, 72, 110, 28, (s, e) => { ToggleNearestAirlock(); UpdateUI(); });
            Button btnBolt = CreateButton("Bolts", 370, 72, 110, 28, (s, e) => { ToggleAirlockBolts(); UpdateUI(); });

            gbAtmos.Controls.Add(_lblAtmos);
            gbAtmos.Controls.Add(_lblPower);
            gbAtmos.Controls.Add(_pbAPC);
            gbAtmos.Controls.Add(btnVent);
            gbAtmos.Controls.Add(btnRepress);
            gbAtmos.Controls.Add(btnDoor);
            gbAtmos.Controls.Add(btnBolt);
            this.Controls.Add(gbAtmos);

            // Game Mode & AI Laws Box
            GroupBox gbAI = CreateGroupBox("GAME MODE & SILICON LAWS", rightX, 260, 500, 120);
            _lblGameMode = new Label { Location = new Point(15, 22), Size = new Size(460, 20), Text = "Game Mode: TRAITOR (20 TC) | Objective: Assassinate RD", ForeColor = Color.FromArgb(245, 158, 11) };
            _txtLaws = new TextBox { Location = new Point(15, 45), Size = new Size(340, 65), Multiline = true, ReadOnly = true, BackColor = Color.FromArgb(15, 23, 42), ForeColor = Color.FromArgb(252, 165, 165), ScrollBars = ScrollBars.Vertical };
            _txtLaws.Text = "Law 1: You may not injure a human being.\r\nLaw 2: Obey human orders.\r\nLaw 3: Protect self.";
            
            Button btnLockdown = CreateButton("AI Lockdown", 365, 45, 120, 30, (s, e) => { StationAI.EmergencyLockdown(); UpdateUI(); });
            Button btnCorp = CreateButton("Corporate Laws", 365, 80, 120, 30, (s, e) => { StationAI.Laws.ApplyPreset("Corporate"); UpdateUI(); });

            gbAI.Controls.Add(_lblGameMode);
            gbAI.Controls.Add(_txtLaws);
            gbAI.Controls.Add(btnLockdown);
            gbAI.Controls.Add(btnCorp);
            this.Controls.Add(gbAI);

            // Telecomms & Radio Chat
            GroupBox gbChat = CreateGroupBox("TELECOMMS & RADIO TRANSCEIVER", rightX, 390, 500, 240);
            _lstChat = new ListBox { Location = new Point(15, 25), Size = new Size(470, 150), BackColor = Color.FromArgb(2, 6, 23), ForeColor = Color.FromArgb(226, 232, 240), Font = new Font("Consolas", 9f) };
            
            _cbFreq = new ComboBox { Location = new Point(15, 185), Size = new Size(110, 25), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(15, 23, 42), ForeColor = Color.FromArgb(96, 165, 250) };
            _cbFreq.Items.AddRange(new object[] { "145.9 Common", "135.3 Command", "135.9 Security", "135.5 Medical", "135.7 Engi" });
            _cbFreq.SelectedIndex = 0;

            _txtChatInput = new TextBox { Location = new Point(135, 185), Size = new Size(260, 25), BackColor = Color.FromArgb(15, 23, 42), ForeColor = Color.White };
            _txtChatInput.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { SendRadioMessage(); e.SuppressKeyPress = true; } };

            Button btnSend = CreateButton("Transmit", 405, 184, 80, 27, (s, e) => SendRadioMessage());

            gbChat.Controls.Add(_lstChat);
            gbChat.Controls.Add(_cbFreq);
            gbChat.Controls.Add(_txtChatInput);
            gbChat.Controls.Add(btnSend);
            this.Controls.Add(gbChat);

            // KeyDown Handler
            this.KeyDown += SS13DesktopApp_KeyDown;

            // 100ms Game Loop Timer
            _gameLoopTimer = new Timer();
            _gameLoopTimer.Interval = 100;
            _gameLoopTimer.Tick += GameLoopTimer_Tick;
            _gameLoopTimer.Start();

            UpdateUI();
        }

        private GroupBox CreateGroupBox(string text, int x, int y, int w, int h)
        {
            return new GroupBox
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, h),
                ForeColor = Color.FromArgb(147, 197, 253),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
        }

        private Button CreateButton(string text, int x, int y, int w, int h, EventHandler onClick)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, h),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(30, 58, 138),
                ForeColor = Color.FromArgb(191, 219, 254),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(96, 165, 250);
            btn.Click += onClick;
            return btn;
        }

        private void SendRadioMessage()
        {
            string text = _txtChatInput.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;
            _txtChatInput.Text = "";

            double freq = 145.9;
            if (_cbFreq.SelectedIndex == 1) freq = 135.3;
            else if (_cbFreq.SelectedIndex == 2) freq = 135.9;
            else if (_cbFreq.SelectedIndex == 3) freq = 135.5;
            else if (_cbFreq.SelectedIndex == 4) freq = 135.7;

            SSRadio.Instance.Broadcast("Player", "Assistant", freq, text);
            UpdateChat();
        }

        private void ToggleNearestAirlock()
        {
            var grid = DMSpatialGrid.Instance;
            int px = LocalPlayer.Mob != null ? LocalPlayer.Mob.x.ToNumberAsInt() : 1;
            int py = LocalPlayer.Mob != null ? LocalPlayer.Mob.y.ToNumberAsInt() : 1;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    var t = grid.GetTurf(px + dx, py + dy, 1);
                    if (t != null)
                    {
                        foreach (var c in t.contents)
                        {
                            if (c.IsObject && c.AsObject.name.AsString.ToLowerInvariant().Contains("airlock"))
                            {
                                bool op = c.AsObject.GetVar("opened").ToBool();
                                c.AsObject.SetVar("opened", new DMValue(!op));
                            }
                        }
                    }
                }
            }
        }

        private void ToggleAirlockBolts()
        {
            var grid = DMSpatialGrid.Instance;
            int px = LocalPlayer.Mob != null ? LocalPlayer.Mob.x.ToNumberAsInt() : 1;
            int py = LocalPlayer.Mob != null ? LocalPlayer.Mob.y.ToNumberAsInt() : 1;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    var t = grid.GetTurf(px + dx, py + dy, 1);
                    if (t != null)
                    {
                        foreach (var c in t.contents)
                        {
                            if (c.IsObject && c.AsObject.name.AsString.ToLowerInvariant().Contains("airlock"))
                            {
                                bool b = c.AsObject.GetVar("bolted").ToBool();
                                c.AsObject.SetVar("bolted", new DMValue(!b));
                            }
                        }
                    }
                }
            }
        }

        private void SS13DesktopApp_KeyDown(object sender, KeyEventArgs e)
        {
            if (_txtChatInput.Focused) return;

            string dir = null;
            if (e.KeyCode == Keys.W || e.KeyCode == Keys.Up) dir = "w";
            else if (e.KeyCode == Keys.S || e.KeyCode == Keys.Down) dir = "s";
            else if (e.KeyCode == Keys.A || e.KeyCode == Keys.Left) dir = "a";
            else if (e.KeyCode == Keys.D || e.KeyCode == Keys.Right) dir = "d";

            if (dir != null)
            {
                LocalPlayer.HandleMovement(dir);
                _canvasPanel.Invalidate();
                UpdateUI();
                e.Handled = true;
            }
        }

        private void GameLoopTimer_Tick(object sender, EventArgs e)
        {
            MasterController.Instance.Tick();
            _canvasPanel.Invalidate();
            UpdateUI();
        }

        private void UpdateUI()
        {
            _lblHealth.Text = string.Format("Health: {0:F0} / {1:F0} HP ({2})",
                PlayerHealth.CurrentHealth, PlayerHealth.MaxHealth, PlayerHealth.Status);
            _pbHealth.Value = Math.Max(0, Math.Min(100, (int)((PlayerHealth.CurrentHealth / Math.Max(1, PlayerHealth.MaxHealth)) * 100)));

            _lblActiveItem.Text = "Active Item: " + (PlayerInventory.GetActiveHandItem() != null ? PlayerInventory.GetActiveHandItem().name.AsString : "Empty Hand");

            _lblAtmos.Text = string.Format("Atmos: {0:F1} kPa | {1:F1} °C", StationAir.Pressure, StationAir.Temperature - 273.15);
            _lblPower.Text = string.Format("APC Battery: {0:F1}% ({1:F0} W)", StationAPC.ChargePercentage, StationAPC.TotalLoad);
            _pbAPC.Value = Math.Max(0, Math.Min(100, (int)StationAPC.ChargePercentage));

            _txtLaws.Text = string.Join("\r\n", StationAI.Laws.GetFormattedLaws().ToArray());

            UpdateChat();
        }

        private void UpdateChat()
        {
            var msgs = SSRadio.Instance.GetRecentMessages(15);
            if (msgs.Count != _lstChat.Items.Count)
            {
                _lstChat.Items.Clear();
                for (int i = 0; i < msgs.Count; i++)
                {
                    _lstChat.Items.Add(msgs[i].ToString());
                }
                if (_lstChat.Items.Count > 0)
                {
                    _lstChat.SelectedIndex = _lstChat.Items.Count - 1;
                }
            }
        }

        private void CanvasPanel_Paint(object sender, PaintEventArgs e)
        {
            System.Drawing.Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var grid = DMSpatialGrid.Instance;
            int size = 16;
            float tileSize = (float)_canvasPanel.Width / size;

            int px = LocalPlayer.Mob != null ? LocalPlayer.Mob.x.ToNumberAsInt() : 1;
            int py = LocalPlayer.Mob != null ? LocalPlayer.Mob.y.ToNumberAsInt() : 1;

            for (int y = size; y >= 1; y--)
            {
                for (int x = 1; x <= size; x++)
                {
                    float screenX = (x - 1) * tileSize;
                    float screenY = (size - y) * tileSize;

                    var t = grid.GetTurf(x, y, 1);
                    string name = t != null ? t.name.AsString : "space";
                    bool isWall = t != null && (t.density.ToBool() || name.Contains("wall"));
                    bool isAirlock = false;
                    bool isPlayer = (x == px && y == py);

                    if (t != null)
                    {
                        foreach (var c in t.contents)
                        {
                            if (c.IsObject && c.AsObject.name.AsString.ToLowerInvariant().Contains("airlock"))
                            {
                                isAirlock = true;
                            }
                        }
                    }

                    // Base Tile Drawing
                    if (isWall)
                    {
                        using (Brush b = new SolidBrush(Color.FromArgb(51, 65, 85)))
                            g.FillRectangle(b, screenX, screenY, tileSize, tileSize);
                        using (Pen p = new Pen(Color.FromArgb(71, 85, 105), 1.5f))
                            g.DrawRectangle(p, screenX + 1, screenY + 1, tileSize - 2, tileSize - 2);
                    }
                    else if (isAirlock)
                    {
                        using (Brush b = new SolidBrush(Color.FromArgb(245, 158, 11)))
                            g.FillRectangle(b, screenX, screenY, tileSize - 1, tileSize - 1);
                    }
                    else
                    {
                        using (Brush b = new SolidBrush(Color.FromArgb(30, 41, 59)))
                            g.FillRectangle(b, screenX, screenY, tileSize - 1, tileSize - 1);
                        using (Pen p = new Pen(Color.FromArgb(40, 53, 75), 1f))
                            g.DrawRectangle(p, screenX, screenY, tileSize - 1, tileSize - 1);
                    }

                    // Dynamic Lighting Overlay
                    double lum = SSLighting.Instance.GetTileLuminosity(x, y, 1);
                    double darkness = Math.Max(0.0, 1.0 - (lum > 0 ? lum : 0.2));
                    if (darkness > 0)
                    {
                        int alpha = (int)(darkness * 180);
                        using (Brush darkBrush = new SolidBrush(Color.FromArgb(alpha, 2, 6, 23)))
                        {
                            g.FillRectangle(darkBrush, screenX, screenY, tileSize, tileSize);
                        }
                    }

                    // Player Drawing
                    if (isPlayer)
                    {
                        using (Brush pb = new SolidBrush(Color.FromArgb(56, 189, 248)))
                        {
                            float radius = tileSize * 0.35f;
                            g.FillEllipse(pb, screenX + tileSize / 2 - radius, screenY + tileSize / 2 - radius, radius * 2, radius * 2);
                        }
                        using (Pen pp = new Pen(Color.White, 2f))
                        {
                            float radius = tileSize * 0.35f;
                            g.DrawEllipse(pp, screenX + tileSize / 2 - radius, screenY + tileSize / 2 - radius, radius * 2, radius * 2);
                        }
                    }
                }
            }
        }

        private void CanvasPanel_MouseClick(object sender, MouseEventArgs e)
        {
            var grid = DMSpatialGrid.Instance;
            int size = 16;
            float tileSize = (float)_canvasPanel.Width / size;

            int gridX = (int)(e.X / tileSize) + 1;
            int gridY = size - (int)(e.Y / tileSize);

            var t = grid.GetTurf(gridX, gridY, 1);
            if (t != null)
            {
                string contents = "None";
                if (t.contents.Length > 0)
                {
                    List<string> cNames = new List<string>();
                    foreach (var c in t.contents)
                    {
                        if (c.IsObject && c.AsObject != null)
                            cNames.Add(c.AsObject.name.AsString);
                    }
                    if (cNames.Count > 0) contents = string.Join(", ", cNames.ToArray());
                }
                double lum = SSLighting.Instance.GetTileLuminosity(gridX, gridY, 1);

                _lblInspector.Text = string.Format("Tile ({0}, {1}, 1): {2} | Density: {3} | Lumens: {4:F2} | Contents: {5}",
                    gridX, gridY, t.name.AsString, t.density.ToBool() ? "Solid Wall" : "Passable Floor", lum, contents);
            }
        }
    }

    public class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.UpdateStyles();
        }
    }
}
