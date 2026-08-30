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
        private Label _lblHealth;
        private ProgressBar _pbHealth;
        private Label _lblActiveItem;
        private Label _lblAtmos;
        private Label _lblPower;
        private ProgressBar _pbAPC;
        private TextBox _txtLaws;
        private ListBox _lstChat;
        private TextBox _txtChatInput;
        private ComboBox _cbFreq;
        private Label _lblInspector;
        private Label _lblGameMode;
        private FacingDir _playerFacing = FacingDir.South;
        private CombatIntent _currentIntent = CombatIntent.Help;

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

            // Build Authentic 24x24 Space Station Sector
            StationSectorBuilder.BuildFullStationSector(DMSpatialGrid.Instance);

            // Spawn player on Command Bridge
            if (LocalPlayer.Mob != null)
            {
                LocalPlayer.Mob.x = new DMValue(12);
                LocalPlayer.Mob.y = new DMValue(18);
                LocalPlayer.Mob.z = new DMValue(1);
                var spawnTurf = DMSpatialGrid.Instance.GetTurf(12, 18, 1);
                if (spawnTurf != null)
                {
                    LocalPlayer.Mob.loc = new DMValue(spawnTurf);
                    spawnTurf.contents.Add(new DMValue(LocalPlayer.Mob));
                }
                SSLighting.Instance.RegisterLight(new LightSource(LocalPlayer.Mob, 6, 1.0, "#60a5fa"));
            }

            // Radio broadcasts
            SSRadio.Instance.Broadcast("Station AI", "AI", SSRadio.FREQ_COMMON, "Welcome aboard Space Station 13. All station wings pressurized.");
            SSRadio.Instance.Broadcast("Captain", "Command", SSRadio.FREQ_COMMAND, "All department heads report to bridge.");
            SSRadio.Instance.Broadcast("Chief Medical Officer", "Medical", SSRadio.FREQ_MEDICAL, "Medbay triage active and stocked.");
        }

        private void InitializeComponent()
        {
            this.Text = "Space Station 13 - .NET Game Client";
            this.Size = new Size(1180, 780);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(9, 13, 22);
            this.ForeColor = Color.FromArgb(226, 232, 240);
            this.Font = new Font("Segoe UI", 9.5f);
            this.KeyPreview = true;

            // 2D Viewport Canvas Panel
            _canvasPanel = new DoubleBufferedPanel();
            _canvasPanel.Location = new Point(20, 20);
            _canvasPanel.Size = new Size(600, 600);
            _canvasPanel.BackColor = Color.FromArgb(2, 6, 23);
            _canvasPanel.Paint += CanvasPanel_Paint;
            _canvasPanel.MouseClick += CanvasPanel_MouseClick;
            this.Controls.Add(_canvasPanel);

            // Controls Hint
            Label lblHint = new Label();
            lblHint.Text = "🎮 W / A / S / D or Arrow Keys: Walk around station | Click on airlocks or items to interact";
            lblHint.Location = new Point(20, 630);
            lblHint.Size = new Size(600, 25);
            lblHint.ForeColor = Color.FromArgb(96, 165, 250);
            lblHint.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            this.Controls.Add(lblHint);

            // Tile Inspector
            _lblInspector = new Label();
            _lblInspector.Text = "Location: Command Bridge | Facing: South | Coordinates: (12, 18, 1)";
            _lblInspector.Location = new Point(20, 660);
            _lblInspector.Size = new Size(600, 50);
            _lblInspector.ForeColor = Color.FromArgb(148, 163, 184);
            this.Controls.Add(_lblInspector);

            int rightX = 640;

            // COMBAT INTENT & TARGET DOLL BOX
            GroupBox gbIntent = CreateGroupBox("INTENT & TARGET ZONE", rightX, 20, 500, 75);
            Button btnHelp = CreateIntentButton("HELP 🟢", 15, 25, 110, 35, Color.FromArgb(16, 185, 129), () => _currentIntent = CombatIntent.Help);
            Button btnDisarm = CreateIntentButton("DISARM 🟡", 135, 25, 110, 35, Color.FromArgb(234, 179, 8), () => _currentIntent = CombatIntent.Disarm);
            Button btnGrab = CreateIntentButton("GRAB 🟠", 255, 25, 110, 35, Color.FromArgb(249, 115, 22), () => _currentIntent = CombatIntent.Grab);
            Button btnHarm = CreateIntentButton("HARM 🔴", 375, 25, 110, 35, Color.FromArgb(239, 68, 68), () => _currentIntent = CombatIntent.Harm);
            gbIntent.Controls.AddRange(new Control[] { btnHelp, btnDisarm, btnGrab, btnHarm });
            this.Controls.Add(gbIntent);

            // PLAYER & HEALTH HUD
            GroupBox gbPlayer = CreateGroupBox("PLAYER & HEALTH STATUS", rightX, 105, 500, 110);
            _lblHealth = new Label { Location = new Point(15, 25), Size = new Size(220, 20), Text = "Health: 100 / 100 HP (Healthy)", ForeColor = Color.FromArgb(16, 185, 129) };
            _pbHealth = new ProgressBar { Location = new Point(15, 48), Size = new Size(220, 16), Value = 100 };
            _lblActiveItem = new Label { Location = new Point(250, 25), Size = new Size(230, 20), Text = "Active Hand: Mechanical Crowbar", ForeColor = Color.FromArgb(96, 165, 250) };
            
            Button btnSwap = CreateButton("Swap Hands", 250, 48, 110, 28, (s, e) => { PlayerInventory.SwapHands(); UpdateUI(); });
            Button btnMed = CreateButton("Use Medkit", 370, 48, 110, 28, (s, e) => { PlayerHealth.HealDamage(DamageType.Brute, 15); UpdateUI(); });
            
            gbPlayer.Controls.AddRange(new Control[] { _lblHealth, _pbHealth, _lblActiveItem, btnSwap, btnMed });
            this.Controls.Add(gbPlayer);

            // ATMOSPHERICS & POWER
            GroupBox gbAtmos = CreateGroupBox("ATMOSPHERICS & DOORS", rightX, 225, 500, 110);
            _lblAtmos = new Label { Location = new Point(15, 25), Size = new Size(230, 20), Text = "Pressure: 101.3 kPa | 20.0 °C", ForeColor = Color.FromArgb(56, 189, 248) };
            _lblPower = new Label { Location = new Point(250, 25), Size = new Size(230, 20), Text = "APC Power: 100.0% Optimal", ForeColor = Color.FromArgb(245, 158, 11) };
            _pbAPC = new ProgressBar { Location = new Point(250, 48), Size = new Size(230, 16), Value = 100 };
            
            Button btnVent = CreateButton("Vent Air", 15, 52, 105, 28, (s, e) => { StationAir.RemoveRatio(0.15); UpdateUI(); });
            Button btnRepress = CreateButton("Repressurize", 125, 52, 110, 28, (s, e) => { StationAir.AdjustMoles(GasType.Oxygen, 5); UpdateUI(); });
            Button btnDoor = CreateButton("Open Airlock", 250, 72, 110, 28, (s, e) => { ToggleFacingAirlock(); UpdateUI(); });
            Button btnBolt = CreateButton("Toggle Bolts", 370, 72, 110, 28, (s, e) => { ToggleAirlockBolts(); UpdateUI(); });

            gbAtmos.Controls.AddRange(new Control[] { _lblAtmos, _lblPower, _pbAPC, btnVent, btnRepress, btnDoor, btnBolt });
            this.Controls.Add(gbAtmos);

            // GAME MODE & SILICON LAWS
            GroupBox gbAI = CreateGroupBox("GAME MODE & AI TERMINAL", rightX, 345, 500, 120);
            _lblGameMode = new Label { Location = new Point(15, 22), Size = new Size(460, 20), Text = "Mode: TRAITOR (20 TC) | Objective: Assassinate RD", ForeColor = Color.FromArgb(245, 158, 11) };
            _txtLaws = new TextBox { Location = new Point(15, 45), Size = new Size(340, 65), Multiline = true, ReadOnly = true, BackColor = Color.FromArgb(15, 23, 42), ForeColor = Color.FromArgb(252, 165, 165), ScrollBars = ScrollBars.Vertical };
            _txtLaws.Text = "Law 1: You may not injure a human being.\r\nLaw 2: Obey human orders.\r\nLaw 3: Protect self.";
            
            Button btnLockdown = CreateButton("AI Lockdown", 365, 45, 120, 30, (s, e) => { StationAI.EmergencyLockdown(); UpdateUI(); });
            Button btnCorp = CreateButton("Corporate Laws", 365, 80, 120, 30, (s, e) => { StationAI.Laws.ApplyPreset("Corporate"); UpdateUI(); });

            gbAI.Controls.AddRange(new Control[] { _lblGameMode, _txtLaws, btnLockdown, btnCorp });
            this.Controls.Add(gbAI);

            // TELECOMMS & RADIO CHAT
            GroupBox gbChat = CreateGroupBox("TELECOMMS & RADIO TRANSCEIVER", rightX, 475, 500, 240);
            _lstChat = new ListBox { Location = new Point(15, 25), Size = new Size(470, 150), BackColor = Color.FromArgb(2, 6, 23), ForeColor = Color.FromArgb(226, 232, 240), Font = new Font("Consolas", 9f) };
            
            _cbFreq = new ComboBox { Location = new Point(15, 185), Size = new Size(110, 25), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(15, 23, 42), ForeColor = Color.FromArgb(96, 165, 250) };
            _cbFreq.Items.AddRange(new object[] { "145.9 Common", "135.3 Command", "135.9 Security", "135.5 Medical", "135.7 Engi" });
            _cbFreq.SelectedIndex = 0;

            _txtChatInput = new TextBox { Location = new Point(135, 185), Size = new Size(260, 25), BackColor = Color.FromArgb(15, 23, 42), ForeColor = Color.White };
            _txtChatInput.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { SendRadioMessage(); e.SuppressKeyPress = true; } };

            Button btnSend = CreateButton("Transmit", 405, 184, 80, 27, (s, e) => SendRadioMessage());

            gbChat.Controls.AddRange(new Control[] { _lstChat, _cbFreq, _txtChatInput, btnSend });
            this.Controls.Add(gbChat);

            // KeyDown Handler
            this.KeyDown += SS13DesktopApp_KeyDown;

            // 60 FPS Game Loop Timer
            _gameLoopTimer = new Timer();
            _gameLoopTimer.Interval = 33; // ~30-60 FPS
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

        private Button CreateIntentButton(string text, int x, int y, int w, int h, Color color, Action onClick)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, h),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(20, 30, 48),
                ForeColor = color,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = color;
            btn.Click += (s, e) => { onClick(); UpdateUI(); };
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

        private void ToggleFacingAirlock()
        {
            var grid = DMSpatialGrid.Instance;
            int px = LocalPlayer.Mob != null ? LocalPlayer.Mob.x.ToNumberAsInt() : 12;
            int py = LocalPlayer.Mob != null ? LocalPlayer.Mob.y.ToNumberAsInt() : 18;

            int tx = px;
            int ty = py;
            if (_playerFacing == FacingDir.North) ty++;
            else if (_playerFacing == FacingDir.South) ty--;
            else if (_playerFacing == FacingDir.East) tx++;
            else if (_playerFacing == FacingDir.West) tx--;

            var t = grid.GetTurf(tx, ty, 1);
            if (t != null)
            {
                foreach (var c in t.contents)
                {
                    if (c.IsObject && c.AsObject.name.AsString.ToLowerInvariant().Contains("airlock"))
                    {
                        bool op = c.AsObject.GetVar("opened").ToBool();
                        c.AsObject.SetVar("opened", new DMValue(!op));
                        SSAudio.Instance.PlaySound(op ? "door_close.ogg" : "door_open.ogg", tx, ty, 1);
                    }
                }
            }
        }

        private void ToggleAirlockBolts()
        {
            var grid = DMSpatialGrid.Instance;
            int px = LocalPlayer.Mob != null ? LocalPlayer.Mob.x.ToNumberAsInt() : 12;
            int py = LocalPlayer.Mob != null ? LocalPlayer.Mob.y.ToNumberAsInt() : 18;

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
            if (e.KeyCode == Keys.W || e.KeyCode == Keys.Up) { dir = "w"; _playerFacing = FacingDir.North; }
            else if (e.KeyCode == Keys.S || e.KeyCode == Keys.Down) { dir = "s"; _playerFacing = FacingDir.South; }
            else if (e.KeyCode == Keys.A || e.KeyCode == Keys.Left) { dir = "a"; _playerFacing = FacingDir.West; }
            else if (e.KeyCode == Keys.D || e.KeyCode == Keys.Right) { dir = "d"; _playerFacing = FacingDir.East; }
            else if (e.KeyCode == Keys.Space || e.KeyCode == Keys.E) { ToggleFacingAirlock(); e.Handled = true; return; }

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

            _lblActiveItem.Text = "Active Hand: " + (PlayerInventory.GetActiveHandItem() != null ? PlayerInventory.GetActiveHandItem().name.AsString : "Empty Hand");

            _lblAtmos.Text = string.Format("Pressure: {0:F1} kPa | {1:F1} °C", StationAir.Pressure, StationAir.Temperature - 273.15);
            _lblPower.Text = string.Format("APC Power: {0:F1}% ({1:F0} W)", StationAPC.ChargePercentage, StationAPC.TotalLoad);
            _pbAPC.Value = Math.Max(0, Math.Min(100, (int)StationAPC.ChargePercentage));

            _txtLaws.Text = string.Join("\r\n", StationAI.Laws.GetFormattedLaws().ToArray());

            int px = LocalPlayer.Mob != null ? LocalPlayer.Mob.x.ToNumberAsInt() : 12;
            int py = LocalPlayer.Mob != null ? LocalPlayer.Mob.y.ToNumberAsInt() : 18;
            string roomName = GetRoomName(px, py);
            _lblInspector.Text = string.Format("Location: {0} | Facing: {1} | Coordinates: ({2}, {3}, 1) | Intent: {4}",
                roomName, _playerFacing, px, py, _currentIntent);

            UpdateChat();
        }

        private string GetRoomName(int x, int y)
        {
            if (x >= 11 && x <= 14 && y >= 17) return "Command Bridge";
            if (x >= 15 && y >= 17) return "Medbay Surgery & Triage";
            if (x <= 10 && y <= 10) return "Security Department & Brig";
            if (x >= 15 && y <= 10) return "Atmospherics & Engineering";
            if (x < 3 || x > 22 || y < 3 || y > 22) return "Deep Space";
            return "Primary Station Hallway";
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
            g.InterpolationMode = InterpolationMode.NearestNeighbor;

            var grid = DMSpatialGrid.Instance;
            int viewTiles = 13; // 13x13 viewport
            float tileSize = (float)_canvasPanel.Width / viewTiles;

            int px = LocalPlayer.Mob != null ? LocalPlayer.Mob.x.ToNumberAsInt() : 12;
            int py = LocalPlayer.Mob != null ? LocalPlayer.Mob.y.ToNumberAsInt() : 18;

            int startX = px - viewTiles / 2;
            int startY = py + viewTiles / 2;

            for (int dy = 0; dy < viewTiles; dy++)
            {
                for (int dx = 0; dx < viewTiles; dx++)
                {
                    int gx = startX + dx;
                    int gy = startY - dy;

                    float screenX = dx * tileSize;
                    float screenY = dy * tileSize;

                    var t = grid.GetTurf(gx, gy, 1);
                    if (t == null || t.name.AsString == "space")
                    {
                        SS13PixelRenderer.DrawSpaceTile(g, screenX, screenY, tileSize, gx * 37 + gy * 91);
                        continue;
                    }

                    string turfName = t.name.AsString.ToLowerInvariant();
                    if (turfName.Contains("reinforced wall") || turfName.Contains("wall"))
                    {
                        SS13PixelRenderer.DrawReinforcedWall(g, screenX, screenY, tileSize, 0);
                    }
                    else if (turfName.Contains("window"))
                    {
                        SS13PixelRenderer.DrawGlassWindow(g, screenX, screenY, tileSize);
                    }
                    else
                    {
                        bool isHazard = (gx == 11 || gx == 14 || gy == 11 || gy == 14);
                        SS13PixelRenderer.DrawStationFloor(g, screenX, screenY, tileSize, isHazard);

                        // Draw Objects on Turf
                        foreach (var c in t.contents)
                        {
                            if (c.IsObject && c.AsObject != null)
                            {
                                string oName = c.AsObject.name.AsString.ToLowerInvariant();
                                if (oName.Contains("airlock"))
                                {
                                    bool op = c.AsObject.GetVar("opened").ToBool();
                                    bool b = c.AsObject.GetVar("bolted").ToBool();
                                    SS13PixelRenderer.DrawAirlock(g, screenX, screenY, tileSize, op, b);
                                }
                                else if (oName.Contains("console") || oName.Contains("computer"))
                                {
                                    SS13PixelRenderer.DrawConsole(g, screenX, screenY, tileSize, oName);
                                }
                            }
                        }
                    }

                    // Dynamic Lighting Occlusion Circle
                    double distFromPlayer = Math.Sqrt((gx - px) * (gx - px) + (gy - py) * (gy - py));
                    double darkness = Math.Max(0.0, Math.Min(0.85, (distFromPlayer - 2.5) / 5.0));
                    if (darkness > 0)
                    {
                        using (Brush db = new SolidBrush(Color.FromArgb((int)(darkness * 255), 2, 6, 23)))
                            g.FillRectangle(db, screenX, screenY, tileSize, tileSize);
                    }
                }
            }

            // Draw Centered Player Avatar
            float playerScreenX = (viewTiles / 2) * tileSize;
            float playerScreenY = (viewTiles / 2) * tileSize;
            string activeItem = PlayerInventory.GetActiveHandItem() != null ? PlayerInventory.GetActiveHandItem().name.AsString : "";
            SS13PixelRenderer.DrawPlayerMob(g, playerScreenX, playerScreenY, tileSize, _playerFacing, activeItem);
        }

        private void CanvasPanel_MouseClick(object sender, MouseEventArgs e)
        {
            var grid = DMSpatialGrid.Instance;
            int viewTiles = 13;
            float tileSize = (float)_canvasPanel.Width / viewTiles;

            int px = LocalPlayer.Mob != null ? LocalPlayer.Mob.x.ToNumberAsInt() : 12;
            int py = LocalPlayer.Mob != null ? LocalPlayer.Mob.y.ToNumberAsInt() : 18;

            int startX = px - viewTiles / 2;
            int startY = py + viewTiles / 2;

            int dx = (int)(e.X / tileSize);
            int dy = (int)(e.Y / tileSize);

            int clickX = startX + dx;
            int clickY = startY - dy;

            var t = grid.GetTurf(clickX, clickY, 1);
            if (t != null)
            {
                // If clicked airlock or console, interact with it!
                foreach (var c in t.contents)
                {
                    if (c.IsObject && c.AsObject != null)
                    {
                        string oName = c.AsObject.name.AsString.ToLowerInvariant();
                        if (oName.Contains("airlock"))
                        {
                            bool op = c.AsObject.GetVar("opened").ToBool();
                            c.AsObject.SetVar("opened", new DMValue(!op));
                            SSAudio.Instance.PlaySound(op ? "door_close.ogg" : "door_open.ogg", clickX, clickY, 1);
                        }
                    }
                }
                UpdateUI();
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
