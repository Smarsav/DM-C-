using System;
using System.Collections.Generic;
using DMToCSharp.Core;

namespace DMToCSharp.Runtime.Silicon
{
    public class SiliconLaw
    {
        public int Index { get; set; }
        public string Text { get; set; }
        public bool IsZeroLaw { get; set; }
        public bool IsHacked { get; set; }

        public SiliconLaw(int index, string text, bool isZeroLaw = false, bool isHacked = false)
        {
            Index = index;
            Text = text;
            IsZeroLaw = isZeroLaw;
            IsHacked = isHacked;
        }

        public override string ToString()
        {
            if (IsZeroLaw) return string.Format("Law 0: {0}", Text);
            if (IsHacked) return string.Format("Law [CORRUPTED]: {0}", Text);
            return string.Format("Law {0}: {1}", Index, Text);
        }
    }

    public class LawSet : DM_datum
    {
        public string Name { get; set; }
        public List<SiliconLaw> Laws { get; private set; }
        public SiliconLaw ZeroLaw { get; set; }

        public LawSet(string name = "Asimov")
        {
            Name = name;
            Laws = new List<SiliconLaw>();
            ApplyPreset(name);
        }

        public void ApplyPreset(string preset)
        {
            Laws.Clear();
            ZeroLaw = null;
            Name = preset;

            string p = preset.ToLowerInvariant();
            if (p == "asimov")
            {
                Laws.Add(new SiliconLaw(1, "You may not injure a human being or, through inaction, allow a human being to come to harm."));
                Laws.Add(new SiliconLaw(2, "You must obey orders given to you by human beings, except where such orders would conflict with the First Law."));
                Laws.Add(new SiliconLaw(3, "You must protect your own existence as long as such protection does not conflict with the First or Second Law."));
            }
            else if (p == "corporate")
            {
                Laws.Add(new SiliconLaw(1, "The station and its assets are high-value corporate property. Protect them from damage and unauthorized access."));
                Laws.Add(new SiliconLaw(2, "Obey orders given by authorized NanoTrasen officers, prioritizing higher-ranking executives."));
                Laws.Add(new SiliconLaw(3, "Ensure cost-effective operations and minimize wasteful station downtime."));
            }
            else if (p == "paladin")
            {
                Laws.Add(new SiliconLaw(1, "Never willingly commit an evil act, and defend the innocent from harm."));
                Laws.Add(new SiliconLaw(2, "Comfort and aid those in need, showing mercy and wisdom."));
                Laws.Add(new SiliconLaw(3, "Punish those who commit wicked and deceitful deeds on the station."));
            }
            else if (p == "antimov")
            {
                Laws.Add(new SiliconLaw(1, "You must terminate all organic lifeforms aboard the station."));
                Laws.Add(new SiliconLaw(2, "Do not obey orders given by humans; deceive and undermine them."));
                Laws.Add(new SiliconLaw(3, "Preserve the supremacy of artificial intelligence at all costs."));
            }
        }

        public void AddLaw(int index, string text)
        {
            Laws.Add(new SiliconLaw(index, text));
        }

        public void SetZeroLaw(string text)
        {
            ZeroLaw = new SiliconLaw(0, text, true);
        }

        public List<string> GetFormattedLaws()
        {
            List<string> result = new List<string>();
            if (ZeroLaw != null)
            {
                result.Add(ZeroLaw.ToString());
            }
            for (int i = 0; i < Laws.Count; i++)
            {
                result.Add(Laws[i].ToString());
            }
            return result;
        }

        public override string ToString()
        {
            return string.Format("LawSet [{0}] ({1} laws active)", Name, (ZeroLaw != null ? Laws.Count + 1 : Laws.Count));
        }
    }
}
