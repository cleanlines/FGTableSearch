using ESRI.ArcGIS.ArcMapUI;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.esriSystem;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FGTableSearch {
    public class TOCHelper {

        private Dictionary<int, ILayer> Tree { get; set; } = new Dictionary<int, ILayer>();
        private List<ILayer> rootLayers { get; set; } = new List<ILayer>();

        public TOCHelper() {
            CacheTOC();
        }

        private void CacheTOC() {
            IMap aMap = ArcMap.Document.FocusMap as IMap;
            //UID anUID = new UID();
            //anUID.Value = "{EDAD6644-1810-11D1-86AE-0000F8751720}";
            // get all the 'top level' items
            IEnumLayer someLayers = aMap.Layers[null, false];
            ILayer aLayer = null;
            int depth = 0;
            while((aLayer = someLayers.Next()) != null) {
                rootLayers.Add(aLayer);
            }
            someLayers = aMap.Layers[null, true];
            while((aLayer = someLayers.Next()) != null) {
                Tree.Add(depth, aLayer);
                depth += 1;
            }
        }

        public void MakeVisible(ILayer aLayer) {
            try {
                IFeatureLayer fl = aLayer as IFeatureLayer;
                int fcid = 0;
                if(fl != null) {
                    fcid = fl.FeatureClass.FeatureClassID;
                }
                IMap aMap = ArcMap.Document.FocusMap as IMap;
                Debug.WriteLine(aMap.LayerCount);
                var vals = Tree.Where(x => {
                    IFeatureLayer xfl = x.Value as IFeatureLayer;
                    if(xfl != null && xfl.FeatureClass != null) {
                        return xfl.FeatureClass.FeatureClassID == fcid;
                    } else {
                        return false;
                    }
                });
                foreach(var item in vals) {
                    aLayer = item.Value;
                    int index = item.Key;
                    //int index = Tree.Where(x => x.Value == aLayer).First().Key;
                    for(int i = index; i >= 0; i--) {
                        ILayer lay = Tree[i];//.Where(x => x.Value == i).First().Key;
                        lay.Visible = true;
                        if(rootLayers.Contains(lay)) {
                            break;
                        }
                    }
                }
                RefreshTOC();
            } catch(Exception e) {
                LogLog.Instance.Logger[LogLogEnum.Error](e.Message);
                throw;
            }
        }

        private void RefreshTOC() {
            IMxDocument mapDoc = ArcMap.Document as IMxDocument;
            if(mapDoc != null) {
                IContentsView tocView = mapDoc.ContentsView[0] as IContentsView;
                tocView.Refresh(null);
            }
        }
    }
}
