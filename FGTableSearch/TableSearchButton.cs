using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using ESRI.ArcGIS.Desktop.AddIns;
using System.Diagnostics;
using ESRI.ArcGIS.Framework;
using ESRI.ArcGIS.esriSystem;

namespace FGTableSearch {
    public class TableSearchButton : ESRI.ArcGIS.Desktop.AddIns.Button {

        IDockableWindow SearchWindow { get; set; }

        public event EventHandler VisibilityChangedEvent;

        public TableSearchButton() {

            SetupDockableWindow();
        }

        protected override void OnClick() {
            if(SearchWindow == null)
                return;
            SearchWindow.Show(!SearchWindow.IsVisible());
            NotifyVisibiltyChanged();
        }

        private void NotifyVisibiltyChanged() {
            VisibilityChangedEvent?.Invoke(this, new EventArgs());
        }

        protected override void OnUpdate() {
        }

        private void SetupDockableWindow() {
            if(SearchWindow == null) 
                SearchWindow = ArcMap.DockableWindowManager.GetDockableWindow(ThisAddIn.IDs.TableSearchWindow.ToUID());
        }
    }
}
