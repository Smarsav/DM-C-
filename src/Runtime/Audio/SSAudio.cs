using System;
using System.Collections.Generic;
using DMToCSharp.Core;
using DMToCSharp.Runtime.MC;

namespace DMToCSharp.Runtime.Audio
{
    public class SoundEvent
    {
        public string SoundName { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public double Volume { get; set; }
        public double MaxDistance { get; set; }
        public double Pitch { get; set; }
        public DateTime Timestamp { get; set; }

        public SoundEvent(string name, int x, int y, int z, double volume = 100.0, double maxDist = 12.0, double pitch = 1.0)
        {
            SoundName = name;
            X = x;
            Y = y;
            Z = z;
            Volume = volume;
            MaxDistance = maxDist;
            Pitch = pitch;
            Timestamp = DateTime.Now;
        }
    }

    public class PerceivedAudio
    {
        public string SoundName { get; set; }
        public double EffectiveVolume { get; set; } // 0.0 to 1.0
        public double StereoPan { get; set; } // -1.0 (left) to +1.0 (right)
        public double Pitch { get; set; }

        public PerceivedAudio(string name, double vol, double pan, double pitch)
        {
            SoundName = name;
            EffectiveVolume = vol;
            StereoPan = pan;
            Pitch = pitch;
        }
    }

    public class SSAudio : DMSubsystem
    {
        public static readonly SSAudio Instance = new SSAudio();

        private readonly List<SoundEvent> _recentSounds = new List<SoundEvent>();
        public int TotalSoundsPlayed { get; private set; }

        public SSAudio()
        {
            SubsystemName = "Audio & Acoustics";
            Priority = 30;
            WaitMilliseconds = 50;
        }

        public SoundEvent PlaySound(string soundName, int x, int y, int z = 1, double volume = 100.0, double maxDist = 12.0, double pitch = 1.0)
        {
            SoundEvent snd = new SoundEvent(soundName, x, y, z, volume, maxDist, pitch);
            lock (_recentSounds)
            {
                _recentSounds.Add(snd);
                if (_recentSounds.Count > 50) _recentSounds.RemoveAt(0);
            }
            TotalSoundsPlayed++;
            return snd;
        }

        public PerceivedAudio CalculatePerceivedAudio(int listenerX, int listenerY, int listenerZ, SoundEvent sound)
        {
            if (sound == null || sound.Z != listenerZ)
            {
                return new PerceivedAudio(sound != null ? sound.SoundName : "", 0, 0, 1.0);
            }

            double dx = sound.X - listenerX;
            double dy = sound.Y - listenerY;
            double dist = Math.Sqrt(dx * dx + dy * dy);

            if (dist > sound.MaxDistance)
            {
                return new PerceivedAudio(sound.SoundName, 0, 0, sound.Pitch);
            }

            double falloff = Math.Max(0.0, 1.0 - (dist / sound.MaxDistance));
            double effectiveVol = (sound.Volume / 100.0) * falloff;
            double pan = dist > 0 ? Math.Max(-1.0, Math.Min(1.0, dx / sound.MaxDistance)) : 0.0;

            return new PerceivedAudio(sound.SoundName, effectiveVol, pan, sound.Pitch);
        }

        public List<SoundEvent> GetRecentSounds(int count = 10)
        {
            lock (_recentSounds)
            {
                int start = Math.Max(0, _recentSounds.Count - count);
                return _recentSounds.GetRange(start, _recentSounds.Count - start);
            }
        }

        public override string StatEntry()
        {
            return string.Format("{0}: {1}ms (Sounds Played: {2})", SubsystemName, Cost.ToString("F2"), TotalSoundsPlayed);
        }
    }
}
