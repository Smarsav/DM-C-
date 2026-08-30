using System;
using System.Collections.Generic;
using DMToCSharp.Core;
using DMToCSharp.Runtime.MC;
using DMToCSharp.Runtime.Network;

namespace DMToCSharp.Runtime.Radio
{
    public class RadioMessage
    {
        public string SenderName { get; set; }
        public string JobTitle { get; set; }
        public double Frequency { get; set; }
        public string Content { get; set; }
        public string ChannelName { get; set; }
        public DateTime Timestamp { get; set; }

        public RadioMessage(string sender, string job, double freq, string content, string channel)
        {
            SenderName = sender;
            JobTitle = job;
            Frequency = freq;
            Content = content;
            ChannelName = channel;
            Timestamp = DateTime.Now;
        }

        public override string ToString()
        {
            return string.Format("[{0:HH:mm:ss}] [{1} ({2:F1})] {3} ({4}): \"{5}\"",
                Timestamp, ChannelName, Frequency, SenderName, JobTitle, Content);
        }
    }

    public class SSRadio : DMSubsystem
    {
        public static readonly SSRadio Instance = new SSRadio();

        public const double FREQ_COMMON = 145.9;
        public const double FREQ_COMMAND = 135.3;
        public const double FREQ_SECURITY = 135.9;
        public const double FREQ_MEDICAL = 135.5;
        public const double FREQ_ENGINEERING = 135.7;
        public const double FREQ_SCIENCE = 135.1;

        private readonly List<RadioMessage> _messageHistory = new List<RadioMessage>();
        public int TotalTransmissions { get; private set; }

        public SSRadio()
        {
            SubsystemName = "Telecomms & Radio";
            Priority = 40;
            WaitMilliseconds = 100;
        }

        public static string GetChannelName(double freq)
        {
            if (Math.Abs(freq - FREQ_COMMON) < 0.05) return "Common";
            if (Math.Abs(freq - FREQ_COMMAND) < 0.05) return "Command";
            if (Math.Abs(freq - FREQ_SECURITY) < 0.05) return "Security";
            if (Math.Abs(freq - FREQ_MEDICAL) < 0.05) return "Medical";
            if (Math.Abs(freq - FREQ_ENGINEERING) < 0.05) return "Engineering";
            if (Math.Abs(freq - FREQ_SCIENCE) < 0.05) return "Science";
            return "Custom";
        }

        public RadioMessage Broadcast(string sender, string job, double freq, string text)
        {
            string channel = GetChannelName(freq);
            RadioMessage msg = new RadioMessage(sender, job, freq, text, channel);

            lock (_messageHistory)
            {
                _messageHistory.Add(msg);
                if (_messageHistory.Count > 100) _messageHistory.RemoveAt(0);
            }
            TotalTransmissions++;

            return msg;
        }

        public List<RadioMessage> GetRecentMessages(int count = 20)
        {
            lock (_messageHistory)
            {
                int start = Math.Max(0, _messageHistory.Count - count);
                return _messageHistory.GetRange(start, _messageHistory.Count - start);
            }
        }

        public override string StatEntry()
        {
            return string.Format("{0}: {1}ms (Transmissions: {2})", SubsystemName, Cost.ToString("F2"), TotalTransmissions);
        }
    }
}
