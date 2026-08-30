using System;
using DMToCSharp.Core;

namespace DMToCSharp.Runtime
{
    public class DM_datum : DMObject
    {
        public override DreamPath TypePath
        {
            get { return DreamPath.Datum; }
        }

        public override DreamPath ParentTypePath
        {
            get { return DreamPath.Root; }
        }
    }

    public class DM_atom : DM_datum
    {
        public override DreamPath TypePath
        {
            get { return DreamPath.Atom; }
        }

        public override DreamPath ParentTypePath
        {
            get { return DreamPath.Datum; }
        }

        public DMValue icon { get; set; }
        public DMValue icon_state { get; set; }
        public DMValue x { get; set; }
        public DMValue y { get; set; }
        public DMValue z { get; set; }
        public DMValue density { get; set; }
        public DMValue opacity { get; set; }

        public DM_atom()
        {
            icon = DMValue.Null;
            icon_state = new DMValue("");
            x = 0.0;
            y = 0.0;
            z = 0.0;
            density = 0.0;
            opacity = 0.0;
            DMWorld.Instance.Contents.Add(this);
        }

        public override DMValue Del()
        {
            DMWorld.Instance.Contents.Remove(this);
            return base.Del();
        }
    }

    public class DM_atom_movable : DM_atom
    {
        public override DreamPath TypePath
        {
            get { return DreamPath.Movable; }
        }

        public override DreamPath ParentTypePath
        {
            get { return DreamPath.Atom; }
        }

        public virtual DMValue Move(DMValue newLoc, DMValue dir = default(DMValue))
        {
            loc = newLoc;
            return 1.0;
        }
    }

    public class DM_obj : DM_atom_movable
    {
        public override DreamPath TypePath
        {
            get { return DreamPath.Obj; }
        }

        public override DreamPath ParentTypePath
        {
            get { return DreamPath.Movable; }
        }
    }

    public class DM_mob : DM_atom_movable
    {
        public override DreamPath TypePath
        {
            get { return DreamPath.Mob; }
        }

        public override DreamPath ParentTypePath
        {
            get { return DreamPath.Movable; }
        }

        public DMValue key { get; set; }
        public DMValue ckey { get; set; }
        public DMValue client { get; set; }

        public DM_mob()
        {
            key = new DMValue("");
            ckey = new DMValue("");
            client = DMValue.Null;
        }
    }

    public class DM_turf : DM_atom
    {
        public override DreamPath TypePath
        {
            get { return DreamPath.Turf; }
        }

        public override DreamPath ParentTypePath
        {
            get { return DreamPath.Atom; }
        }
    }

    public class DM_area : DM_atom
    {
        public override DreamPath TypePath
        {
            get { return DreamPath.Area; }
        }

        public override DreamPath ParentTypePath
        {
            get { return DreamPath.Atom; }
        }
    }

    public class DM_client : DM_datum
    {
        public override DreamPath TypePath
        {
            get { return DreamPath.Client; }
        }

        public override DreamPath ParentTypePath
        {
            get { return DreamPath.Datum; }
        }

        public DMValue mob { get; set; }
        public DMValue key { get; set; }
        public DMValue ckey { get; set; }

        public DM_client()
        {
            mob = DMValue.Null;
            key = new DMValue("");
            ckey = new DMValue("");
        }
    }
}
