using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
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

namespace DMToCSharp.Runtime.TGUI
{
    public class TGUIHttpServer
    {
        private HttpListener _listener;
        private Thread _serverThread;
        private bool _isRunning;
        public int Port { get; private set; }

        public bool AirlockBolted { get; set; }
        public bool AirlockOpen { get; set; }
        public GasMixture StationAir { get; private set; }
        public APC StationAPC { get; private set; }
        public SMES StationSMES { get; private set; }
        public OrganismHealth PlayerHealth { get; private set; }
        public InventorySystem PlayerInventory { get; private set; }
        public ReagentContainer ChemStation { get; private set; }
        public DMClient LocalPlayer { get; private set; }
        public AICore StationAI { get; private set; }

        public TGUIHttpServer(int port = 8080)
        {
            Port = port;
            AirlockBolted = false;
            AirlockOpen = false;
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

            // Initialize Station Grid & Rooms
            InitializeDefaultStationMap();

            // Register Light Source on Player Mob
            if (LocalPlayer.Mob != null)
            {
                SSLighting.Instance.RegisterLight(new LightSource(LocalPlayer.Mob, 5, 1.0, "#60a5fa"));
            }

            // Broadcast initial station messages
            SSRadio.Instance.Broadcast("Station AI", "AI", SSRadio.FREQ_COMMON, "Welcome to Space Station 13 (.NET Runtime). All subsystems online.");
            SSRadio.Instance.Broadcast("Chief Medical Officer", "Medical", SSRadio.FREQ_MEDICAL, "Medbay triage active and stocked.");
            SSRadio.Instance.Broadcast("Head of Security", "Security", SSRadio.FREQ_SECURITY, "Station security level set to Code Green.");
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

        public void Start()
        {
            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add(string.Format("http://localhost:{0}/", Port));
                _listener.Prefixes.Add(string.Format("http://127.0.0.1:{0}/", Port));
                _listener.Start();
                _isRunning = true;

                _serverThread = new Thread(ListenLoop);
                _serverThread.IsBackground = true;
                _serverThread.Start();

                Console.WriteLine(string.Format("[TGUI Server] Running at http://localhost:{0}/", Port));
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("[TGUI Server Warning] Could not bind to port {0}: {1}", Port, ex.Message));
            }
        }

        public void Stop()
        {
            _isRunning = false;
            if (_listener != null)
            {
                try { _listener.Stop(); } catch { }
            }
        }

        private void ListenLoop()
        {
            while (_isRunning && _listener != null && _listener.IsListening)
            {
                try
                {
                    var ctx = _listener.GetContext();
                    ThreadPool.QueueUserWorkItem(ProcessRequest, ctx);
                }
                catch
                {
                    if (!_isRunning) break;
                }
            }
        }

        private void ProcessRequest(object state)
        {
            var ctx = (HttpListenerContext)state;
            string url = ctx.Request.Url.AbsolutePath;

            // Tick subsystems on request
            MasterController.Instance.Tick();

            try
            {
                if (url == "/api/status")
                {
                    HandleApiStatus(ctx);
                }
                else if (url == "/api/act")
                {
                    HandleApiAct(ctx);
                }
                else if (url == "/api/player/move")
                {
                    HandlePlayerMove(ctx);
                }
                else if (url == "/api/map/tiles")
                {
                    HandleApiMapTiles(ctx);
                }
                else if (url == "/api/radio/messages")
                {
                    HandleRadioMessages(ctx);
                }
                else if (url == "/api/radio/send")
                {
                    HandleRadioSend(ctx);
                }
                else if (url == "/api/ai/laws")
                {
                    HandleAiLaws(ctx);
                }
                else if (url == "/api/ai/set_preset")
                {
                    HandleAiSetPreset(ctx);
                }
                else if (url == "/api/gamemode")
                {
                    HandleGameMode(ctx);
                }
                else if (url == "/api/database/players")
                {
                    HandleDatabasePlayers(ctx);
                }
                else
                {
                    HandleIndexHtml(ctx);
                }
            }
            catch (Exception ex)
            {
                byte[] err = Encoding.UTF8.GetBytes("Server Error: " + ex.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.OutputStream.Write(err, 0, err.Length);
                ctx.Response.Close();
            }
        }

        private void SendJsonResponse(HttpListenerContext ctx, string json)
        {
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(json);
                ctx.Response.ContentType = "application/json; charset=utf-8";
                ctx.Response.ContentLength64 = data.Length;
                ctx.Response.StatusCode = 200;
                ctx.Response.OutputStream.Write(data, 0, data.Length);
                ctx.Response.OutputStream.Flush();
                ctx.Response.Close();
            }
            catch { }
        }

        private void SendHtmlResponse(HttpListenerContext ctx, string html)
        {
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(html);
                ctx.Response.ContentType = "text/html; charset=utf-8";
                ctx.Response.ContentLength64 = data.Length;
                ctx.Response.StatusCode = 200;
                ctx.Response.OutputStream.Write(data, 0, data.Length);
                ctx.Response.OutputStream.Flush();
                ctx.Response.Close();
            }
            catch { }
        }

        private void HandleGameMode(HttpListenerContext ctx)
        {
            var mode = SSGameMode.Instance;
            List<string> antags = new List<string>();
            foreach (var a in mode.Antagonists)
            {
                List<string> objs = new List<string>();
                foreach (var o in a.Objectives)
                {
                    objs.Add(string.Format("{{\"desc\":\"{0}\",\"done\":{1}}}", o.Description.Replace("\"", "\\\""), o.Completed ? "true" : "false"));
                }
                antags.Add(string.Format("{{\"name\":\"{0}\",\"role\":\"{1}\",\"tc\":{2},\"objs\":[{3}]}}",
                    a.CharacterName, a.Role, a.Telecrystals, string.Join(",", objs.ToArray())));
            }

            string json = string.Format("{{\"mode\":\"{0}\",\"stage\":\"{1}\",\"time\":{2},\"antags\":[{3}]}}",
                mode.ModeName, mode.Stage, mode.RoundTimeSeconds, string.Join(",", antags.ToArray()));
            SendJsonResponse(ctx, json);
        }

        private void HandleDatabasePlayers(HttpListenerContext ctx)
        {
            var players = SSDatabase.Instance.GetAllPlayers();
            List<string> list = new List<string>();
            foreach (var p in players)
            {
                list.Add(string.Format("{{\"ckey\":\"{0}\",\"name\":\"{1}\",\"job\":\"{2}\",\"rounds\":{3},\"karma\":{4}}}",
                    p.CKey, p.CharacterName, p.PreferredJob, p.RoundsPlayed, p.Karma));
            }
            string json = string.Format("[{0}]", string.Join(",", list.ToArray()));
            SendJsonResponse(ctx, json);
        }

        private void HandleAiLaws(HttpListenerContext ctx)
        {
            var laws = StationAI.Laws.GetFormattedLaws();
            List<string> lawJson = new List<string>();
            foreach (var l in laws)
            {
                lawJson.Add(string.Format("\"{0}\"", l.Replace("\"", "\\\"")));
            }
            string json = string.Format("{{\"preset\":\"{0}\",\"laws\":[{1}]}}", StationAI.Laws.Name, string.Join(",", lawJson.ToArray()));
            SendJsonResponse(ctx, json);
        }

        private void HandleAiSetPreset(HttpListenerContext ctx)
        {
            string preset = ctx.Request.QueryString["preset"] ?? "Asimov";
            StationAI.Laws.ApplyPreset(preset);
            SSRadio.Instance.Broadcast("Station AI", "AI", SSRadio.FREQ_COMMON, string.Format("Silicon law set updated to: {0}", preset));
            SSAudio.Instance.PlaySound("law_update.ogg", 1, 1, 1);
            HandleAiLaws(ctx);
        }

        private void HandleRadioMessages(HttpListenerContext ctx)
        {
            var msgs = SSRadio.Instance.GetRecentMessages(30);
            List<string> jsonMsgs = new List<string>();
            foreach (var m in msgs)
            {
                jsonMsgs.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "{{\"sender\":\"{0}\",\"job\":\"{1}\",\"freq\":{2:F1},\"channel\":\"{3}\",\"text\":\"{4}\",\"time\":\"{5:HH:mm:ss}\"}}",
                    m.SenderName.Replace("\"", "\\\""),
                    m.JobTitle.Replace("\"", "\\\""),
                    m.Frequency,
                    m.ChannelName,
                    m.Content.Replace("\"", "\\\""),
                    m.Timestamp));
            }

            string json = string.Format("[{0}]", string.Join(",", jsonMsgs.ToArray()));
            SendJsonResponse(ctx, json);
        }

        private void HandleRadioSend(HttpListenerContext ctx)
        {
            string sender = ctx.Request.QueryString["sender"] ?? "Captain";
            string text = ctx.Request.QueryString["text"] ?? "Hello Station";
            double freq = 145.9;
            if (ctx.Request.QueryString["freq"] != null) double.TryParse(ctx.Request.QueryString["freq"], out freq);

            SSRadio.Instance.Broadcast(sender, "Command", freq, text);
            SSAudio.Instance.PlaySound("radio_beep.ogg", 1, 1, 1);
            HandleRadioMessages(ctx);
        }

        private void HandlePlayerMove(HttpListenerContext ctx)
        {
            string dir = ctx.Request.QueryString["dir"] ?? "w";
            bool moved = LocalPlayer.HandleMovement(dir);

            if (moved)
            {
                SSAudio.Instance.PlaySound("footstep.ogg",
                    LocalPlayer.Mob != null ? LocalPlayer.Mob.x.ToNumberAsInt() : 1,
                    LocalPlayer.Mob != null ? LocalPlayer.Mob.y.ToNumberAsInt() : 1, 1, 50.0);
            }

            string json = string.Format("{{\"success\":{0},\"x\":{1},\"y\":{2}}}",
                moved ? "true" : "false",
                LocalPlayer.Mob != null ? LocalPlayer.Mob.x.ToNumberAsInt() : 1,
                LocalPlayer.Mob != null ? LocalPlayer.Mob.y.ToNumberAsInt() : 1);

            SendJsonResponse(ctx, json);
        }

        private void HandleApiStatus(HttpListenerContext ctx)
        {
            int pX = LocalPlayer.Mob != null ? LocalPlayer.Mob.x.ToNumberAsInt() : 1;
            int pY = LocalPlayer.Mob != null ? LocalPlayer.Mob.y.ToNumberAsInt() : 1;

            string json = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "{{" +
                "\"mc_iteration\": {0}," +
                "\"mc_avg_tick_ms\": {1:F2}," +
                "\"air_pressure\": {2:F1}," +
                "\"air_temp_c\": {3:F1}," +
                "\"air_o2\": {4:F2}," +
                "\"air_n2\": {5:F2}," +
                "\"air_plasma\": {6:F2}," +
                "\"apc_charge_pct\": {7:F1}," +
                "\"apc_load_w\": {8:F0}," +
                "\"apc_breaker\": {9}," +
                "\"smes_charge_pct\": {10:F1}," +
                "\"airlock_open\": {11}," +
                "\"airlock_bolted\": {12}," +
                "\"player_x\": {13}," +
                "\"player_y\": {14}," +
                "\"health_hp\": {15:F0}," +
                "\"health_max\": {16:F0}," +
                "\"health_status\": \"{17}\"," +
                "\"health_blood\": {18:F0}," +
                "\"active_item\": \"{19}\"," +
                "\"radio_transmissions\": {20}," +
                "\"ai_law_preset\": \"{21}\"," +
                "\"active_lights\": {22}," +
                "\"sounds_played\": {23}," +
                "\"gamemode\": \"{24}\"," +
                "\"round_time\": {25}" +
                "}}",
                MasterController.Instance.CurrentIteration,
                MasterController.Instance.AverageTickTimeMs,
                StationAir.Pressure,
                StationAir.Temperature - 273.15,
                StationAir.GetMoles(GasType.Oxygen),
                StationAir.GetMoles(GasType.Nitrogen),
                StationAir.GetMoles(GasType.Plasma),
                StationAPC.ChargePercentage,
                StationAPC.TotalLoad,
                StationAPC.MainBreaker ? "true" : "false",
                StationSMES.ChargePercentage,
                AirlockOpen ? "true" : "false",
                AirlockBolted ? "true" : "false",
                pX, pY,
                PlayerHealth.CurrentHealth,
                PlayerHealth.MaxHealth,
                PlayerHealth.Status,
                PlayerHealth.BloodVolume,
                PlayerInventory.GetActiveHandItem() != null ? PlayerInventory.GetActiveHandItem().name.AsString : "Empty Hand",
                SSRadio.Instance.TotalTransmissions,
                StationAI.Laws.Name,
                SSLighting.Instance.ActiveLightsCount,
                SSAudio.Instance.TotalSoundsPlayed,
                SSGameMode.Instance.ModeName,
                SSGameMode.Instance.RoundTimeSeconds
            );

            SendJsonResponse(ctx, json);
        }

        private void HandleApiMapTiles(HttpListenerContext ctx)
        {
            var grid = DMSpatialGrid.Instance;
            int maxX = Math.Min(grid.MaxX > 0 ? grid.MaxX : 16, 20);
            int maxY = Math.Min(grid.MaxY > 0 ? grid.MaxY : 16, 20);
            int z = 1;

            int pX = LocalPlayer.Mob != null ? LocalPlayer.Mob.x.ToNumberAsInt() : 1;
            int pY = LocalPlayer.Mob != null ? LocalPlayer.Mob.y.ToNumberAsInt() : 1;

            List<string> tileJsonList = new List<string>();
            for (int y = maxY; y >= 1; y--)
            {
                for (int x = 1; x <= maxX; x++)
                {
                    var t = grid.GetTurf(x, y, z);
                    string name = t != null ? t.name.AsString : "space";
                    bool isWall = t != null && (t.density.ToBool() || name.Contains("wall"));
                    bool isAirlock = false;
                    bool isPlayer = (x == pX && y == pY);

                    // Autotile neighbor checking
                    var tN = grid.GetTurf(x, y + 1, z);
                    var tS = grid.GetTurf(x, y - 1, z);
                    var tE = grid.GetTurf(x + 1, y, z);
                    var tW = grid.GetTurf(x - 1, y, z);

                    bool nWall = tN != null && (tN.density.ToBool() || tN.name.AsString.Contains("wall"));
                    bool sWall = tS != null && (tS.density.ToBool() || tS.name.AsString.Contains("wall"));
                    bool eWall = tE != null && (tE.density.ToBool() || tE.name.AsString.Contains("wall"));
                    bool wWall = tW != null && (tW.density.ToBool() || tW.name.AsString.Contains("wall"));
                    int autotileMask = DMIParser.CalculateAutotileMask(nWall, sWall, eWall, wWall);

                    // Dynamic Lighting
                    double lum = SSLighting.Instance.GetTileLuminosity(x, y, z);

                    if (t != null)
                    {
                        foreach (DMValue c in t.contents)
                        {
                            if (c.IsObject)
                            {
                                string cName = c.AsObject.name.AsString.ToLowerInvariant();
                                if (cName.Contains("airlock") || cName.Contains("door")) isAirlock = true;
                            }
                        }
                    }

                    tileJsonList.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                        "{{\"x\":{0},\"y\":{1},\"name\":\"{2}\",\"wall\":{3},\"door\":{4},\"player\":{5},\"mask\":{6},\"lum\":{7:F2}}}",
                        x, y, name, isWall ? "true" : "false", isAirlock ? "true" : "false", isPlayer ? "true" : "false", autotileMask, lum));
                }
            }

            string json = string.Format("{{\"width\":{0},\"height\":{1},\"player_x\":{2},\"player_y\":{3},\"tiles\":[{4}]}}",
                maxX, maxY, pX, pY, string.Join(",", tileJsonList.ToArray()));
            SendJsonResponse(ctx, json);
        }

        private void HandleApiAct(HttpListenerContext ctx)
        {
            string action = ctx.Request.QueryString["action"];
            if (action == "toggle_airlock" && !AirlockBolted)
            {
                AirlockOpen = !AirlockOpen;
                SSAudio.Instance.PlaySound(AirlockOpen ? "door_open.ogg" : "door_close.ogg", 1, 1, 1);
            }
            else if (action == "toggle_bolt")
            {
                AirlockBolted = !AirlockBolted;
                SSAudio.Instance.PlaySound("door_bolt.ogg", 1, 1, 1);
            }
            else if (action == "toggle_breaker")
            {
                StationAPC.MainBreaker = !StationAPC.MainBreaker;
                SSAudio.Instance.PlaySound("breaker_click.ogg", 1, 1, 1);
            }
            else if (action == "ai_lockdown")
            {
                StationAI.EmergencyLockdown();
                AirlockBolted = true;
                AirlockOpen = false;
                SSRadio.Instance.Broadcast("Station AI", "AI", SSRadio.FREQ_COMMAND, "EMERGENCY: Complete station lockdown protocol initiated.");
                SSAudio.Instance.PlaySound("alarm_klaxon.ogg", 1, 1, 1, 100.0, 30.0);
            }
            else if (action == "vent_air")
            {
                StationAir.RemoveRatio(0.15);
                SSAudio.Instance.PlaySound("air_vent.ogg", 1, 1, 1);
            }
            else if (action == "repressurize")
            {
                StationAir.AdjustMoles(GasType.Oxygen, 5.0);
                StationAir.AdjustMoles(GasType.Nitrogen, 18.0);
                SSAudio.Instance.PlaySound("gas_inject.ogg", 1, 1, 1);
            }
            else if (action == "swap_hands")
            {
                PlayerInventory.SwapHands();
            }
            else if (action == "apply_medkit")
            {
                PlayerHealth.HealDamage(DamageType.Brute, 15.0);
                PlayerHealth.HealDamage(DamageType.Burn, 15.0);
                SSAudio.Instance.PlaySound("medkit_use.ogg", 1, 1, 1);
            }
            else if (action == "mix_chem")
            {
                ChemStation.AddReagent("oxygen", 10.0);
                ChemStation.CheckReactions();
                SSAudio.Instance.PlaySound("chem_sizzle.ogg", 1, 1, 1);
            }

            HandleApiStatus(ctx);
        }

        private void HandleIndexHtml(HttpListenerContext ctx)
        {
            string html = @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Space Station 13 - Complete .NET Engine Suite</title>
    <link href=""https://fonts.googleapis.com/css2?family=Orbitron:wght@400;700;900&family=Inter:wght@300;400;600;700&display=swap"" rel=""stylesheet"">
    <style>
        :root {
            --bg: #090d16;
            --panel: rgba(18, 25, 41, 0.9);
            --border: rgba(64, 120, 220, 0.25);
            --accent: #2e75d3;
            --accent-glow: rgba(46, 117, 211, 0.4);
            --success: #10b981;
            --warning: #f59e0b;
            --danger: #ef4444;
            --text: #e2e8f0;
            --text-dim: #94a3b8;
        }
        * { box-sizing: border-box; margin: 0; padding: 0; }
        body {
            font-family: 'Inter', sans-serif;
            background: radial-gradient(circle at top right, #111a2e 0%, var(--bg) 100%);
            color: var(--text);
            min-height: 100vh;
            padding: 20px;
        }
        .header {
            display: flex;
            align-items: center;
            justify-content: space-between;
            background: var(--panel);
            backdrop-filter: blur(12px);
            padding: 16px 24px;
            border-radius: 12px;
            border: 1px solid var(--border);
            margin-bottom: 20px;
            box-shadow: 0 8px 32px rgba(0, 0, 0, 0.4);
        }
        .logo {
            font-family: 'Orbitron', sans-serif;
            font-size: 19px;
            font-weight: 900;
            letter-spacing: 1.5px;
            color: #60a5fa;
            display: flex;
            align-items: center;
            gap: 12px;
        }
        .status-badge {
            background: rgba(16, 185, 129, 0.15);
            color: var(--success);
            padding: 6px 14px;
            border-radius: 20px;
            font-size: 13px;
            font-weight: 600;
            border: 1px solid rgba(16, 185, 129, 0.3);
            display: flex;
            align-items: center;
            gap: 8px;
        }
        .status-dot {
            width: 8px;
            height: 8px;
            background: var(--success);
            border-radius: 50%;
            box-shadow: 0 0 10px var(--success);
            animation: pulse 2s infinite;
        }
        @keyframes pulse { 0% { opacity: 0.4; } 50% { opacity: 1; } 100% { opacity: 0.4; } }
        .grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
            gap: 18px;
            margin-bottom: 20px;
        }
        .card {
            background: var(--panel);
            backdrop-filter: blur(12px);
            border: 1px solid var(--border);
            border-radius: 12px;
            padding: 20px;
            box-shadow: 0 8px 24px rgba(0,0,0,0.3);
            transition: transform 0.2s ease, border-color 0.2s ease;
        }
        .card:hover {
            transform: translateY(-2px);
            border-color: rgba(96, 165, 250, 0.4);
        }
        .card-title {
            font-family: 'Orbitron', sans-serif;
            font-size: 14px;
            letter-spacing: 1px;
            color: #93c5fd;
            margin-bottom: 14px;
            display: flex;
            align-items: center;
            justify-content: space-between;
        }
        .metric-row {
            display: flex;
            justify-content: space-between;
            align-items: center;
            padding: 8px 0;
            border-bottom: 1px solid rgba(255,255,255,0.06);
        }
        .metric-row:last-child { border-bottom: none; }
        .metric-label { color: var(--text-dim); font-size: 13px; }
        .metric-val { font-family: 'Orbitron', monospace; font-size: 15px; font-weight: 700; color: #fff; }
        .bar-container {
            width: 100%;
            height: 8px;
            background: rgba(255,255,255,0.08);
            border-radius: 6px;
            margin-top: 6px;
            overflow: hidden;
        }
        .bar-fill {
            height: 100%;
            background: linear-gradient(90deg, #3b82f6, #60a5fa);
            border-radius: 6px;
            transition: width 0.3s ease;
        }
        .btn-group {
            display: flex;
            gap: 8px;
            margin-top: 14px;
        }
        .btn {
            flex: 1;
            padding: 8px 12px;
            border-radius: 6px;
            border: 1px solid var(--border);
            background: rgba(30, 58, 138, 0.3);
            color: #bfdbfe;
            font-size: 12px;
            font-weight: 600;
            cursor: pointer;
            transition: all 0.2s ease;
        }
        .btn:hover {
            background: rgba(37, 99, 235, 0.5);
            border-color: #60a5fa;
            color: #fff;
        }
        .btn-danger {
            background: rgba(220, 38, 38, 0.2);
            border-color: rgba(220, 38, 38, 0.4);
            color: #fca5a5;
        }
        .btn-danger:hover {
            background: rgba(220, 38, 38, 0.5);
            border-color: #ef4444;
            color: #fff;
        }

        /* 2D CANVAS RADAR STYLES */
        .radar-section {
            background: var(--panel);
            border: 1px solid var(--border);
            border-radius: 12px;
            padding: 20px;
            box-shadow: 0 8px 24px rgba(0,0,0,0.3);
            margin-bottom: 20px;
        }
        .canvas-wrapper {
            display: flex;
            gap: 20px;
            align-items: flex-start;
            margin-top: 14px;
        }
        #stationCanvas {
            background: #020617;
            border: 2px solid rgba(59, 130, 246, 0.4);
            border-radius: 8px;
            box-shadow: 0 0 20px rgba(37, 99, 235, 0.2);
            cursor: crosshair;
        }
        .tile-inspector {
            flex: 1;
            background: rgba(15, 23, 42, 0.6);
            border: 1px solid rgba(255,255,255,0.08);
            border-radius: 8px;
            padding: 16px;
        }
        .controls-hint {
            margin-top: 10px;
            padding: 8px 12px;
            background: rgba(59, 130, 246, 0.1);
            border: 1px solid rgba(59, 130, 246, 0.2);
            border-radius: 6px;
            font-size: 12px;
            color: #93c5fd;
        }

        /* SILICON LAWS & TELECOMMS CHAT STYLES */
        .chat-section {
            background: var(--panel);
            border: 1px solid var(--border);
            border-radius: 12px;
            padding: 20px;
            box-shadow: 0 8px 24px rgba(0,0,0,0.3);
        }
        .chat-box {
            height: 180px;
            background: #020617;
            border: 1px solid var(--border);
            border-radius: 8px;
            padding: 12px;
            overflow-y: auto;
            font-family: monospace;
            font-size: 13px;
            display: flex;
            flex-direction: column;
            gap: 6px;
        }
        .chat-msg {
            line-height: 1.4;
        }
        .chat-channel { font-weight: bold; color: #60a5fa; }
        .chat-sender { font-weight: bold; color: #34d399; }
        .chat-input-row {
            display: flex;
            gap: 10px;
            margin-top: 12px;
        }
        .chat-input {
            flex: 1;
            background: rgba(15, 23, 42, 0.8);
            border: 1px solid var(--border);
            border-radius: 6px;
            color: #fff;
            padding: 10px 14px;
            font-size: 14px;
        }
        .freq-select {
            background: rgba(15, 23, 42, 0.8);
            border: 1px solid var(--border);
            border-radius: 6px;
            color: #60a5fa;
            padding: 0 12px;
            font-family: 'Orbitron', sans-serif;
            font-size: 13px;
        }
        .laws-box {
            background: rgba(15, 23, 42, 0.9);
            border: 1px solid rgba(239, 68, 68, 0.3);
            border-radius: 8px;
            padding: 12px;
            font-size: 13px;
            color: #fca5a5;
            margin-top: 12px;
            max-height: 120px;
            overflow-y: auto;
        }
    </style>
</head>
<body>
    <div class=""header"">
        <div class=""logo"">
            <span>🚀 SPACE STATION 13 // .NET RUNTIME ENGINE</span>
        </div>
        <div class=""status-badge"">
            <div class=""status-dot""></div>
            <span>ALL 18 SUBSYSTEMS NOMINAL</span>
        </div>
    </div>

    <div class=""grid"">
        <!-- PLAYER & HEALTH HUD -->
        <div class=""card"">
            <div class=""card-title"">
                <span>PLAYER STATUS & HEALTH</span>
                <span style=""font-size:12px; color:#10b981;"" id=""val-health-status"">HEALTHY</span>
            </div>
            <div class=""metric-row"">
                <span class=""metric-label"">Health Points (HP)</span>
                <span class=""metric-val"" id=""val-hp"">100 / 100</span>
            </div>
            <div class=""bar-container""><div class=""bar-fill"" id=""bar-hp"" style=""width: 100%; background:linear-gradient(90deg, #10b981, #34d399);""></div></div>
            <div class=""metric-row"" style=""margin-top:8px;"">
                <span class=""metric-label"">Blood Volume</span>
                <span class=""metric-val"" id=""val-blood"">560 ml</span>
            </div>
            <div class=""metric-row"">
                <span class=""metric-label"">Active Hand Item</span>
                <span class=""metric-val"" id=""val-item"" style=""color:#60a5fa; font-size:13px;"">Mechanical Crowbar</span>
            </div>
            <div class=""btn-group"">
                <button class=""btn"" onclick=""sendAct('swap_hands')"">Swap Hands</button>
                <button class=""btn"" onclick=""sendAct('apply_medkit')"">Use Medkit</button>
            </div>
        </div>

        <!-- GAME MODE & OBJECTIVES CARD -->
        <div class=""card"">
            <div class=""card-title"">
                <span>ROUND & GAME MODE</span>
                <span style=""font-size:12px; color:#f59e0b;"" id=""val-gamemode"">TRAITOR</span>
            </div>
            <div class=""metric-row"">
                <span class=""metric-label"">Round Elapsed</span>
                <span class=""metric-val"" id=""val-round-time"">00:00</span>
            </div>
            <div class=""metric-row"">
                <span class=""metric-label"">Syndicate Uplink</span>
                <span class=""metric-val"" style=""color:#ef4444; font-size:13px;"">20 TC Available</span>
            </div>
            <div class=""laws-box"" id=""objectives-box"" style=""border-color:rgba(245, 158, 11, 0.4); color:#fde68a; max-height:80px;"">
                - Assassinate Research Director<br>- Steal Antique Laser Gun
            </div>
        </div>

        <!-- AI CORE & SILICON LAWS CARD -->
        <div class=""card"">
            <div class=""card-title"">
                <span>STATION MASTER AI</span>
                <span style=""font-size:12px; color:#ef4444;"">ASIMOV</span>
            </div>
            <div class=""laws-box"" id=""laws-list"">
                Law 1: You may not injure a human being.
            </div>
            <div class=""btn-group"">
                <button class=""btn"" onclick=""changeLaws('Corporate')"">Corporate</button>
                <button class=""btn"" onclick=""changeLaws('Paladin')"">Paladin</button>
                <button class=""btn btn-danger"" onclick=""sendAct('ai_lockdown')"">Lockdown</button>
            </div>
        </div>

        <!-- ATMOSPHERICS & LIGHTING CARD -->
        <div class=""card"">
            <div class=""card-title"">
                <span>ATMOS & LIGHTING</span>
                <span style=""font-size:12px; color:#60a5fa;"">ENV-1</span>
            </div>
            <div class=""metric-row"">
                <span class=""metric-label"">Pressure / Temp</span>
                <span class=""metric-val"" id=""val-pressure"" style=""font-size:13px;"">101.3 kPa / 20.0°C</span>
            </div>
            <div class=""metric-row"">
                <span class=""metric-label"">Active Lights / Audio</span>
                <span class=""metric-val"" id=""val-lights"" style=""font-size:13px; color:#38bdf8;"">1 Lights / 0 Sfx</span>
            </div>
            <div class=""btn-group"">
                <button class=""btn"" onclick=""sendAct('repressurize')"">Repressurize</button>
                <button class=""btn btn-danger"" onclick=""sendAct('vent_air')"">Vent Air</button>
            </div>
        </div>
    </div>

    <!-- 2D INTERACTIVE RADAR & PLAYER CANVAS WITH DYNAMIC LIGHTING -->
    <div class=""radar-section"">
        <div class=""card-title"">
            <span>2D STATION RADAR & DYNAMIC LIGHTING (LIVE WASD / ARROW CONTROLS)</span>
            <span style=""font-size:12px; color:#38bdf8;"">DYNAMIC LUMENS & OCCLUSION ACTIVE</span>
        </div>
        <div class=""canvas-wrapper"">
            <div>
                <canvas id=""stationCanvas"" width=""480"" height=""480"" tabindex=""1""></canvas>
                <div class=""controls-hint"">
                    🎮 <strong>Live Keyboard Controls:</strong> Use <strong>W / A / S / D</strong> or <strong>Arrow Keys</strong> to move your avatar in real-time across the station grid.
                </div>
            </div>
            <div class=""tile-inspector"">
                <h4 style=""color:#60a5fa; font-family:'Orbitron',sans-serif; margin-bottom:12px;"">Tile & Target Telemetry</h4>
                <div class=""metric-row""><span class=""metric-label"">Clicked Coordinate</span><span class=""metric-val"" id=""inspect-coord"">(1, 1, 1)</span></div>
                <div class=""metric-row""><span class=""metric-label"">Turf Name</span><span class=""metric-val"" id=""inspect-turf"">Floor</span></div>
                <div class=""metric-row""><span class=""metric-label"">Luminosity</span><span class=""metric-val"" id=""inspect-lum"" style=""font-size:13px; color:#38bdf8;"">1.00 Lumens</span></div>
                <div class=""metric-row""><span class=""metric-label"">Autotile Mask</span><span class=""metric-val"" id=""inspect-mask"" style=""font-size:13px; color:#f59e0b;"">0</span></div>
                
                <h4 style=""color:#93c5fd; font-family:'Orbitron',sans-serif; margin-top:16px; margin-bottom:8px; font-size:13px;"">Actions & Interactions</h4>
                <div class=""btn-group"" style=""margin-top:0;"">
                    <button class=""btn"" onclick=""sendAct('toggle_airlock')"">Toggle Airlock</button>
                    <button class=""btn"" onclick=""sendAct('mix_chem')"">Mix Chemistry</button>
                </div>
                
                <p style=""margin-top:14px; font-size:11px; color:var(--text-dim);"">Legend: <span style=""color:#475569;"">■ Wall</span> | <span style=""color:#1e3a8a;"">■ Floor</span> | <span style=""color:#f59e0b;"">■ Airlock</span> | <span style=""color:#38bdf8;"">● Player</span></p>
            </div>
        </div>
    </div>

    <!-- TELECOMMS & RADIO CHAT SECTION -->
    <div class=""chat-section"">
        <div class=""card-title"">
            <span>TELECOMMS & RADIO TRANSCEIVER (SSradio)</span>
            <span style=""font-size:12px; color:#34d399;"" id=""radio-count"">3 TRANSMISSIONS</span>
        </div>
        <div class=""chat-box"" id=""chat-history""></div>
        <div class=""chat-input-row"">
            <select class=""freq-select"" id=""freq-select"">
                <option value=""145.9"">145.9 Common</option>
                <option value=""135.3"">135.3 Command</option>
                <option value=""135.9"">135.9 Security</option>
                <option value=""135.5"">135.5 Medical</option>
                <option value=""135.7"">135.7 Engineering</option>
                <option value=""135.1"">135.1 Science</option>
            </select>
            <input type=""text"" class=""chat-input"" id=""chat-text"" placeholder=""Transmit radio message to channel... (Press Enter)"" onkeydown=""if(event.key==='Enter') sendRadioMsg()"">
            <button class=""btn"" style=""flex:0 0 100px;"" onclick=""sendRadioMsg()"">Send</button>
        </div>
    </div>

    <script>
        let mapData = null;
        const canvas = document.getElementById('stationCanvas');
        const ctx = canvas.getContext('2d');

        // Simple Web Audio Synthesizer for spatial sound effects
        const audioCtx = new (window.AudioContext || window.webkitAudioContext)();
        function playBeep(freq = 440, type = 'sine', duration = 0.1) {
            try {
                if (audioCtx.state === 'suspended') audioCtx.resume();
                const osc = audioCtx.createOscillator();
                const gain = audioCtx.createGain();
                osc.type = type;
                osc.frequency.value = freq;
                gain.gain.setValueAtTime(0.1, audioCtx.currentTime);
                gain.gain.exponentialRampToValueAtTime(0.001, audioCtx.currentTime + duration);
                osc.connect(gain);
                gain.connect(audioCtx.destination);
                osc.start();
                osc.stop(audioCtx.currentTime + duration);
            } catch(e) {}
        }

        async function fetchStatus() {
            try {
                const res = await fetch('/api/status');
                const data = await res.json();
                document.getElementById('val-pressure').innerText = data.air_pressure.toFixed(1) + ' kPa / ' + data.air_temp_c.toFixed(1) + '°C';

                document.getElementById('val-hp').innerText = data.health_hp + ' / ' + data.health_max;
                document.getElementById('bar-hp').style.width = ((data.health_hp / data.health_max) * 100) + '%';
                document.getElementById('val-health-status').innerText = data.health_status.toUpperCase();
                document.getElementById('val-blood').innerText = data.health_blood + ' ml';
                document.getElementById('val-item').innerText = data.active_item;
                document.getElementById('radio-count').innerText = data.radio_transmissions + ' TRANSMISSIONS';
                document.getElementById('val-gamemode').innerText = data.gamemode.toUpperCase();
                document.getElementById('val-lights').innerText = data.active_lights + ' Lights / ' + data.sounds_played + ' Sfx';

                const m = Math.floor(data.round_time / 60);
                const s = data.round_time % 60;
                document.getElementById('val-round-time').innerText = (m < 10 ? '0' + m : m) + ':' + (s < 10 ? '0' + s : s);
            } catch(e) { }
        }

        async function fetchLaws() {
            try {
                const res = await fetch('/api/ai/laws');
                const data = await res.json();
                const container = document.getElementById('laws-list');
                container.innerHTML = data.laws.map(function(l) {
                    return '<div style=""margin-bottom:4px;"">' + l + '</div>';
                }).join('');
            } catch(e) { }
        }

        async function changeLaws(preset) {
            await fetch('/api/ai/set_preset?preset=' + preset);
            playBeep(880, 'square', 0.2);
            fetchLaws();
            fetchStatus();
        }

        async function fetchMap() {
            try {
                const res = await fetch('/api/map/tiles');
                mapData = await res.json();
                renderMap();
            } catch(e) { }
        }

        async function fetchRadio() {
            try {
                const res = await fetch('/api/radio/messages');
                const msgs = await res.json();
                const container = document.getElementById('chat-history');
                container.innerHTML = msgs.map(function(m) {
                    return '<div class=""chat-msg"">' +
                        '<span style=""color:#64748b;"">[' + m.time + ']</span> ' +
                        '<span class=""chat-channel"">[' + m.channel + ' (' + m.freq.toFixed(1) + ')]</span> ' +
                        '<span class=""chat-sender"">' + m.sender + ' (' + m.job + '):</span> ' +
                        '<span style=""color:#e2e8f0;"">&quot;' + m.text + '&quot;</span>' +
                        '</div>';
                }).join('');
                container.scrollTop = container.scrollHeight;
            } catch(e) { }
        }

        async function sendRadioMsg() {
            const input = document.getElementById('chat-text');
            const freq = document.getElementById('freq-select').value;
            const text = input.value.trim();
            if (!text) return;
            input.value = '';
            playBeep(600, 'sine', 0.08);
            await fetch('/api/radio/send?freq=' + freq + '&text=' + encodeURIComponent(text));
            fetchRadio();
        }

        function renderMap() {
            if (!mapData) return;
            const tileSize = canvas.width / mapData.width;
            ctx.clearRect(0, 0, canvas.width, canvas.height);

            mapData.tiles.forEach(tile => {
                const px = (tile.x - 1) * tileSize;
                const py = (mapData.height - tile.y) * tileSize;

                if (tile.wall) {
                    ctx.fillStyle = '#334155';
                    ctx.fillRect(px, py, tileSize, tileSize);
                    ctx.strokeStyle = '#475569';
                    ctx.lineWidth = 1.5;
                    ctx.strokeRect(px + 1, py + 1, tileSize - 2, tileSize - 2);
                } else if (tile.door) {
                    ctx.fillStyle = '#f59e0b';
                    ctx.fillRect(px, py, tileSize - 1, tileSize - 1);
                } else {
                    ctx.fillStyle = '#1e293b';
                    ctx.fillRect(px, py, tileSize - 1, tileSize - 1);
                }

                // Dynamic light shading overlay
                const darkness = Math.max(0, 1.0 - (tile.lum || 0.2));
                if (darkness > 0) {
                    ctx.fillStyle = 'rgba(2, 6, 23, ' + (darkness * 0.7) + ')';
                    ctx.fillRect(px, py, tileSize, tileSize);
                }

                if (tile.player) {
                    ctx.fillStyle = '#38bdf8';
                    ctx.beginPath();
                    ctx.arc(px + tileSize/2, py + tileSize/2, tileSize/2.6, 0, Math.PI*2);
                    ctx.fill();
                    ctx.strokeStyle = '#fff';
                    ctx.lineWidth = 2;
                    ctx.stroke();
                }
            });
        }

        async function movePlayer(dir) {
            playBeep(220, 'triangle', 0.04);
            await fetch('/api/player/move?dir=' + dir);
            await fetchMap();
            fetchStatus();
        }

        window.addEventListener('keydown', (e) => {
            if (document.activeElement === document.getElementById('chat-text')) return;
            const key = e.key.toLowerCase();
            if (key === 'w' || key === 'arrowup') { e.preventDefault(); movePlayer('w'); }
            else if (key === 's' || key === 'arrowdown') { e.preventDefault(); movePlayer('s'); }
            else if (key === 'a' || key === 'arrowleft') { e.preventDefault(); movePlayer('a'); }
            else if (key === 'd' || key === 'arrowright') { e.preventDefault(); movePlayer('d'); }
        });

        canvas.addEventListener('click', (e) => {
            if (!mapData) return;
            const rect = canvas.getBoundingClientRect();
            const mouseX = e.clientX - rect.left;
            const mouseY = e.clientY - rect.top;
            const tileSize = canvas.width / mapData.width;

            const gridX = Math.floor(mouseX / tileSize) + 1;
            const gridY = mapData.height - Math.floor(mouseY / tileSize);

            const clicked = mapData.tiles.find(t => t.x === gridX && t.y === gridY);
            if (clicked) {
                document.getElementById('inspect-coord').innerText = '(' + clicked.x + ', ' + clicked.y + ', 1)';
                document.getElementById('inspect-turf').innerText = clicked.name;
                document.getElementById('inspect-lum').innerText = (clicked.lum || 0).toFixed(2) + ' Lumens';
                document.getElementById('inspect-mask').innerText = clicked.mask;
            }
        });

        async function sendAct(action) {
            playBeep(520, 'sine', 0.08);
            await fetch('/api/act?action=' + action);
            fetchStatus();
            fetchMap();
        }

        setInterval(fetchStatus, 500);
        setInterval(fetchMap, 1000);
        setInterval(fetchRadio, 2000);
        fetchStatus();
        fetchLaws();
        fetchMap();
        fetchRadio();
    </script>
</body>
</html>";

            SendHtmlResponse(ctx, html);
        }
    }
}
