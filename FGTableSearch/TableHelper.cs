using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geodatabase;

namespace FGTableSearch {
    public sealed class TableHelper : IEnumerable<TableDropdownDataContext> {

        private static readonly Lazy<TableHelper> lazy = new Lazy<TableHelper>(() => new TableHelper());
        public static TableHelper Instance { get { return lazy.Value; } }
        public int Count { get; set; } = 0;
        public List<TableDropdownDataContext> TableList { get; set; } = new List<TableDropdownDataContext>();
        public Dictionary<string, List<IRelationshipClass>> RelationLookup { get; set; } = new Dictionary<string, List<IRelationshipClass>>();

        private TableHelper() {
        }

        public void Refresh() {
            TableList.Clear();
            RelationLookup.Clear();
            IMap aMap = ArcMap.Document.FocusMap as IMap;
            IMapLayers2 layers = ArcMap.Document.FocusMap as IMapLayers2;
            IEnumLayer someLayers = layers.Layers;
            ILayer aLayer = null;
            while((aLayer = someLayers.Next()) != null) {
                IFeatureLayer2 someFeatlayer = aLayer as IFeatureLayer2;
                if(someFeatlayer != null) {
                    IFeatureClass featClass = someFeatlayer.FeatureClass as IFeatureClass;
                    if(featClass != null) {
                        IEnumRelationshipClass someRelationships = featClass.RelationshipClasses[esriRelRole.esriRelRoleAny] as IEnumRelationshipClass;
                        if(someRelationships != null) {
                            IRelationshipClass aRelationship = someRelationships.Next();
                            while(aRelationship != null) {
                                Debug.WriteLine(aRelationship.OriginClass.AliasName + " <--> " + aRelationship.DestinationClass.AliasName);
                                IDataset aDataset = aRelationship.OriginClass as IDataset;
                                IDataset anotherDataset = aRelationship.DestinationClass as IDataset;
                                AddDataset(new IDataset[] { aDataset, anotherDataset });
                                IDataset aDS = featClass as IDataset;
                                if(aDS != null) {
                                    Debug.WriteLine(aDS.Name + "," + aDataset.Name + "," + anotherDataset.Name);
                                    string name = aDS.Name.Equals(aDataset.Name, StringComparison.InvariantCultureIgnoreCase) ? anotherDataset.Name : aDataset.Name;
                                    if(RelationLookup.ContainsKey(name)) {
                                        RelationLookup[name].Add(aRelationship);
                                    } else {
                                        RelationLookup.Add(name, new List<IRelationshipClass>() { aRelationship });
                                    }
                                }
                                aRelationship = someRelationships.Next();
                            }
                        }
                    }

                }
            }
            TableList.Sort((a, b) => a.TableName.CompareTo(b.TableName));
        }

        private void AddDataset(IDataset[] datasets) {

            foreach(IDataset aDataset in datasets) {
                if(aDataset.Type == esriDatasetType.esriDTTable) {
                    if(aDataset.Name.IndexOf("_ATTACH") == -1) {
                        TableDropdownDataContext item = new TableDropdownDataContext { TableName = Utility.ParseTableName(aDataset), Value = aDataset, Fields = Utility.ReturnFieldNames(aDataset) };
                        if(!TableList.Any(x => x.TableName == item.TableName)) {
                            TableList.Add(item);
                        }
                    }
                }
            }
        }

        public ILayer GetLayerFromFeature(IFeature feat) {
            IFeatureClass fc = feat.Class as IFeatureClass;
            IMap aMap = ArcMap.Document.FocusMap as IMap;
            IMapLayers2 layers = ArcMap.Document.FocusMap as IMapLayers2;
            IEnumLayer someLayers = layers.Layers;
            ILayer aLayer = null;
            while((aLayer = someLayers.Next()) != null) {
                IFeatureLayer2 someFeatlayer = aLayer as IFeatureLayer2;
                if(someFeatlayer != null) {
                    IDataset newDS = someFeatlayer.FeatureClass as IDataset;
                    IDataset ds = feat.Class as IDataset;
                    Debug.WriteLine($"{newDS.Name},{ds.Name}");
                    if(newDS.Name.Equals(ds.Name, StringComparison.InvariantCultureIgnoreCase)) {
                        return aLayer;
                    }
                }
            }
            return null;
        }

        public IEnumerator<TableDropdownDataContext> GetEnumerator() {
            Count += 1;
            //(new List<TableDropdownDataContext>() { new TableDropdownDataContext { TableName = "PoopX", Value = "HeadX" }, new TableDropdownDataContext { TableName = "PoopieX" + Count.ToString(), Value = "HeadieX" + Count.ToString() } })
            return TableList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }
}
