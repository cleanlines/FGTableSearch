using ESRI.ArcGIS.Geodatabase;

namespace FGTableSearch {
    public class FeatureModel {
        public int ObjectID { get; set; }
        public string TableName { get; set; }
        public IFeature Feature { get; set; }
    }
}
