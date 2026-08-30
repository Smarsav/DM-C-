using System;
using System.Collections.Generic;
using DMToCSharp.Core;

namespace DMToCSharp.Runtime.Items
{
    public enum InvSlot
    {
        Head,
        Suit,
        Uniform,
        Back,
        Belt,
        Gloves,
        Shoes,
        LeftHand,
        RightHand,
        PocketLeft,
        PocketRight,
        IdCard
    }

    public class DM_item : DM_obj
    {
        public InvSlot AllowedSlots { get; set; }
        public double Weight { get; set; }
        public bool IsTool { get; set; }
        public string ToolKind { get; set; }

        public DM_item(string itemName = "Item")
        {
            name = new DMValue(itemName);
            AllowedSlots = InvSlot.RightHand;
            Weight = 1.0;
            IsTool = false;
        }
    }

    public class InventorySystem : DM_datum
    {
        private readonly Dictionary<InvSlot, DM_item> _slots = new Dictionary<InvSlot, DM_item>();
        public bool IsRightHandActive { get; set; }

        public InventorySystem()
        {
            IsRightHandActive = true;
        }

        public DM_item GetItem(InvSlot slot)
        {
            DM_item item;
            _slots.TryGetValue(slot, out item);
            return item;
        }

        public bool EquipItem(InvSlot slot, DM_item item)
        {
            if (item == null) return false;
            if (_slots.ContainsKey(slot) && _slots[slot] != null) return false; // Slot occupied

            _slots[slot] = item;
            return true;
        }

        public DM_item DropItem(InvSlot slot)
        {
            DM_item item;
            if (_slots.TryGetValue(slot, out item))
            {
                _slots.Remove(slot);
                return item;
            }
            return null;
        }

        public void SwapHands()
        {
            IsRightHandActive = !IsRightHandActive;
        }

        public DM_item GetActiveHandItem()
        {
            return GetItem(IsRightHandActive ? InvSlot.RightHand : InvSlot.LeftHand);
        }

        public void PutInActiveHand(DM_item item)
        {
            EquipItem(IsRightHandActive ? InvSlot.RightHand : InvSlot.LeftHand, item);
        }

        public override string ToString()
        {
            List<string> filled = new List<string>();
            foreach (var kv in _slots)
            {
                if (kv.Value != null)
                {
                    filled.Add(string.Format("{0}: {1}", kv.Key, kv.Value.name.AsString));
                }
            }
            return string.Format("Inventory ({0} items): {1}", filled.Count, string.Join(", ", filled.ToArray()));
        }
    }
}
