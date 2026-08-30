using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using DMToCSharp.Runtime.Atmos;
using DMToCSharp.Runtime.MC;
using DMToCSharp.Runtime.Power;

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

        public TGUIHttpServer(int port = 8080)
        {
            Port = port;
            AirlockBolted = false;
            AirlockOpen = false;
            StationAir = GasMixture.CreateStandardStationAir();
            StationAPC = new APC("Bridge", 50000.0);
            StationSMES = new SMES(5000000.0);

            // Register in Master Controller
            MasterController.Instance.RegisterSubsystem(SSAir.Instance);
            MasterController.Instance.RegisterSubsystem(SSPower.Instance);
            SSPower.Instance.RegisterAPC(StationAPC);
            SSPower.Instance.RegisterSMES(StationSMES);
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

        private void HandleApiStatus(HttpListenerContext ctx)
        {
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
                "\"airlock_bolted\": {12}" +
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
                AirlockBolted ? "true" : "false"
            );

            byte[] data = Encoding.UTF8.GetBytes(json);
            ctx.Response.ContentType = "application/json";
            ctx.Response.OutputStream.Write(data, 0, data.Length);
            ctx.Response.Close();
        }

        private void HandleApiAct(HttpListenerContext ctx)
        {
            string action = ctx.Request.QueryString["action"];
            if (action == "toggle_airlock" && !AirlockBolted)
            {
                AirlockOpen = !AirlockOpen;
            }
            else if (action == "toggle_bolt")
            {
                AirlockBolted = !AirlockBolted;
            }
            else if (action == "toggle_breaker")
            {
                StationAPC.MainBreaker = !StationAPC.MainBreaker;
            }
            else if (action == "vent_air")
            {
                StationAir.RemoveRatio(0.15); // Vent 15% gas
            }
            else if (action == "repressurize")
            {
                StationAir.AdjustMoles(GasType.Oxygen, 5.0);
                StationAir.AdjustMoles(GasType.Nitrogen, 18.0);
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
    <title>Space Station 13 - TGUI Live Console (.NET Runtime)</title>
    <link href=""https://fonts.googleapis.com/css2?family=Orbitron:wght@400;700;900&family=Inter:wght@300;400;600;700&display=swap"" rel=""stylesheet"">
    <style>
        :root {
            --bg: #090d16;
            --panel: rgba(18, 25, 41, 0.85);
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
            padding: 24px;
        }
        .header {
            display: flex;
            align-items: center;
            justify-content: space-between;
            background: var(--panel);
            backdrop-filter: blur(12px);
            padding: 16px 28px;
            border-radius: 12px;
            border: 1px solid var(--border);
            margin-bottom: 24px;
            box-shadow: 0 8px 32px rgba(0, 0, 0, 0.4);
        }
        .logo {
            font-family: 'Orbitron', sans-serif;
            font-size: 20px;
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
            grid-template-columns: repeat(auto-fit, minmax(340px, 1fr));
            gap: 20px;
        }
        .card {
            background: var(--panel);
            backdrop-filter: blur(12px);
            border: 1px solid var(--border);
            border-radius: 12px;
            padding: 22px;
            box-shadow: 0 8px 24px rgba(0,0,0,0.3);
            transition: transform 0.2s ease, border-color 0.2s ease;
        }
        .card:hover {
            transform: translateY(-2px);
            border-color: rgba(96, 165, 250, 0.4);
        }
        .card-title {
            font-family: 'Orbitron', sans-serif;
            font-size: 15px;
            letter-spacing: 1px;
            color: #93c5fd;
            margin-bottom: 18px;
            display: flex;
            align-items: center;
            justify-content: space-between;
        }
        .metric-row {
            display: flex;
            justify-content: space-between;
            align-items: center;
            padding: 10px 0;
            border-bottom: 1px solid rgba(255,255,255,0.06);
        }
        .metric-row:last-child { border-bottom: none; }
        .metric-label { color: var(--text-dim); font-size: 14px; }
        .metric-val { font-family: 'Orbitron', monospace; font-size: 16px; font-weight: 700; color: #fff; }
        .bar-container {
            width: 100%;
            height: 10px;
            background: rgba(255,255,255,0.08);
            border-radius: 6px;
            margin-top: 8px;
            overflow: hidden;
        }
        .bar-fill {
            height: 100%;
            background: linear-gradient(90deg, #3b82f6, #60a5fa);
            border-radius: 6px;
            transition: width 0.4s ease;
        }
        .btn-group {
            display: flex;
            gap: 10px;
            margin-top: 18px;
        }
        .btn {
            flex: 1;
            padding: 10px 14px;
            border-radius: 8px;
            border: 1px solid var(--border);
            background: rgba(30, 58, 138, 0.3);
            color: #bfdbfe;
            font-size: 13px;
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
    </style>
</head>
<body>
    <div class=""header"">
        <div class=""logo"">
            <span>🚀 PSYCHONAUT STATION // TGUI</span>
        </div>
        <div class=""status-badge"">
            <div class=""status-dot""></div>
            <span>.NET CORE RUNTIME ACTIVE</span>
        </div>
    </div>

    <div class=""grid"">
        <!-- ATMOSPHERICS CARD -->
        <div class=""card"">
            <div class=""card-title"">
                <span>ATMOSPHERICS (SSair)</span>
                <span style=""font-size:12px; color:#60a5fa;"">ENV-1</span>
            </div>
            <div class=""metric-row"">
                <span class=""metric-label"">Station Pressure</span>
                <span class=""metric-val"" id=""val-pressure"">101.3 kPa</span>
            </div>
            <div class=""metric-row"">
                <span class=""metric-label"">Temperature</span>
                <span class=""metric-val"" id=""val-temp"">20.0 °C</span>
            </div>
            <div class=""metric-row"">
                <span class=""metric-label"">Oxygen (O2)</span>
                <span class=""metric-val"" id=""val-o2"">21.8 mol</span>
            </div>
            <div class=""metric-row"">
                <span class=""metric-label"">Nitrogen (N2)</span>
                <span class=""metric-val"" id=""val-n2"">82.2 mol</span>
            </div>
            <div class=""btn-group"">
                <button class=""btn"" onclick=""sendAct('repressurize')"">Repressurize</button>
                <button class=""btn btn-danger"" onclick=""sendAct('vent_air')"">Emergency Vent</button>
            </div>
        </div>

        <!-- POWER GRID CARD -->
        <div class=""card"">
            <div class=""card-title"">
                <span>POWERNET (SSpower)</span>
                <span style=""font-size:12px; color:#f59e0b;"">APC & SMES</span>
            </div>
            <div class=""metric-row"">
                <span class=""metric-label"">Bridge APC Battery</span>
                <span class=""metric-val"" id=""val-apc-pct"">100.0%</span>
            </div>
            <div class=""bar-container""><div class=""bar-fill"" id=""bar-apc"" style=""width: 100%;""></div></div>
            <div class=""metric-row"" style=""margin-top:10px;"">
                <span class=""metric-label"">Main SMES Grid Storage</span>
                <span class=""metric-val"" id=""val-smes-pct"">80.0%</span>
            </div>
            <div class=""bar-container""><div class=""bar-fill"" id=""bar-smes"" style=""width: 80%; background:linear-gradient(90deg, #f59e0b, #fbbf24);""></div></div>
            <div class=""metric-row"" style=""margin-top:10px;"">
                <span class=""metric-label"">Active Equipment Load</span>
                <span class=""metric-val"" id=""val-load"">2200 W</span>
            </div>
            <div class=""btn-group"">
                <button class=""btn"" onclick=""sendAct('toggle_breaker')"">Toggle Main Breaker</button>
            </div>
        </div>

        <!-- AIRLOCK & SECURITY CARD -->
        <div class=""card"">
            <div class=""card-title"">
                <span>AIRLOCK ACCESS</span>
                <span style=""font-size:12px; color:#10b981;"">BRIDGE-01</span>
            </div>
            <div class=""metric-row"">
                <span class=""metric-label"">Door Status</span>
                <span class=""metric-val"" id=""val-door"" style=""color:#10b981;"">CLOSED</span>
            </div>
            <div class=""metric-row"">
                <span class=""metric-label"">Safety Bolts</span>
                <span class=""metric-val"" id=""val-bolts"">DISENGAGED</span>
            </div>
            <div class=""metric-row"">
                <span class=""metric-label"">Master Controller Iteration</span>
                <span class=""metric-val"" id=""val-mc"">0</span>
            </div>
            <div class=""btn-group"">
                <button class=""btn"" onclick=""sendAct('toggle_airlock')"">Toggle Open/Close</button>
                <button class=""btn btn-danger"" onclick=""sendAct('toggle_bolt')"">Toggle Bolts</button>
            </div>
        </div>
    </div>

    <script>
        async function fetchStatus() {
            try {
                const res = await fetch('/api/status');
                const data = await res.json();
                document.getElementById('val-pressure').innerText = data.air_pressure.toFixed(1) + ' kPa';
                document.getElementById('val-temp').innerText = data.air_temp_c.toFixed(1) + ' °C';
                document.getElementById('val-o2').innerText = data.air_o2.toFixed(1) + ' mol';
                document.getElementById('val-n2').innerText = data.air_n2.toFixed(1) + ' mol';

                document.getElementById('val-apc-pct').innerText = data.apc_charge_pct.toFixed(1) + '%';
                document.getElementById('bar-apc').style.width = data.apc_charge_pct + '%';
                document.getElementById('val-smes-pct').innerText = data.smes_charge_pct.toFixed(1) + '%';
                document.getElementById('bar-smes').style.width = data.smes_charge_pct + '%';
                document.getElementById('val-load').innerText = data.apc_load_w + ' W';

                const door = document.getElementById('val-door');
                door.innerText = data.airlock_open ? 'OPEN' : 'CLOSED';
                door.style.color = data.airlock_open ? '#f59e0b' : '#10b981';

                const bolts = document.getElementById('val-bolts');
                bolts.innerText = data.airlock_bolted ? 'ENGAGED' : 'DISENGAGED';
                bolts.style.color = data.airlock_bolted ? '#ef4444' : '#94a3b8';

                document.getElementById('val-mc').innerText = '#' + data.mc_iteration;
            } catch(e) { }
        }

        async function sendAct(action) {
            await fetch('/api/act?action=' + action);
            fetchStatus();
        }

        setInterval(fetchStatus, 500);
        fetchStatus();
    </script>
</body>
</html>";

            byte[] data = Encoding.UTF8.GetBytes(html);
            ctx.Response.ContentType = "text/html; charset=utf-8";
            ctx.Response.OutputStream.Write(data, 0, data.Length);
            ctx.Response.Close();
        }
    }
}
