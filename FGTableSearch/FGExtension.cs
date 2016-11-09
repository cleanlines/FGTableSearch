using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace FGTableSearch {
    public class FGExtension : ESRI.ArcGIS.Desktop.AddIns.Extension {
        public FGExtension() {
        }

        protected override void OnStartup() {
            LogLog.Instance.Logger[LogLogEnum.Information]("First Gas Extension Starting");
        }

        protected override void OnShutdown() {
            LogLog.Instance.Logger[LogLogEnum.Information]("First Gas Extension Stopping");
            LogLog.Instance.Close();
        }
        // TODO: Move common code in here
    }
}
