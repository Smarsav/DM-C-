using System;
using System.Collections.Generic;
using DMToCSharp.Core;

namespace DMToCSharp.Runtime.TGUI
{
    public class TGUIWindow
    {
        public string WindowId { get; set; }
        public string Title { get; set; }
        public DMObject SrcObject { get; set; }
        public DMObject User { get; set; }
        public DMValue Data { get; set; }
        public bool IsOpen { get; set; }

        public TGUIWindow(string windowId, string title, DMObject srcObject, DMObject user)
        {
            WindowId = windowId;
            Title = title;
            SrcObject = srcObject;
            User = user;
            Data = DMValue.Null;
            IsOpen = true;
        }
    }

    public class TGUIManager
    {
        public static readonly TGUIManager Instance = new TGUIManager();

        private readonly Dictionary<string, TGUIWindow> _activeWindows = new Dictionary<string, TGUIWindow>();

        public TGUIWindow OpenUI(DMObject user, DMObject srcObject, string windowId, string title = "TGUI Window")
        {
            string key = string.Format("{0}_{1}", srcObject.GetHashCode(), windowId);
            TGUIWindow win = new TGUIWindow(key, title, srcObject, user);
            _activeWindows[key] = win;
            return win;
        }

        public void UpdateUI(DMObject srcObject, string windowId, DMValue data)
        {
            string key = string.Format("{0}_{1}", srcObject.GetHashCode(), windowId);
            TGUIWindow win;
            if (_activeWindows.TryGetValue(key, out win))
            {
                win.Data = data;
            }
        }

        public DMValue HandleAct(DMObject user, DMObject srcObject, string action, DMValue payload)
        {
            if (srcObject != null)
            {
                return srcObject.CallProc("ui_act", new DMValue[] { new DMValue(action), payload, new DMValue(user) });
            }
            return DMValue.Null;
        }

        public void CloseUI(DMObject srcObject, string windowId)
        {
            string key = string.Format("{0}_{1}", srcObject.GetHashCode(), windowId);
            _activeWindows.Remove(key);
        }
    }
}
