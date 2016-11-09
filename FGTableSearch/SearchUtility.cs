using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.ADF;
using ESRI.ArcGIS.esriSystem;

namespace FGTableSearch {
    public static class SearchUtility {

        static Dictionary<esriFieldType, Func<string, string, string>> QueryMap { get; set; } = new Dictionary<esriFieldType, Func<string, string, string>> {
            { esriFieldType.esriFieldTypeInteger ,(f,s) => EqualsQuery(f,s) },
            { esriFieldType.esriFieldTypeString,(f,s) => LikeQuery(f,s) },
            { esriFieldType.esriFieldTypeSmallInteger,(f,s) => EqualsQuery(f,s) },
            { esriFieldType.esriFieldTypeDouble,(f,s) => EqualsQuery(f,s) },
            { esriFieldType.esriFieldTypeDate,(f,s) => EqualsStringQuery(f,s) },
            { esriFieldType.esriFieldTypeOID,(f,s) => EqualsQuery(f,s) }
        };

        internal static List<object[]> Search(string searchText, TableDropdownDataContext item, string field) {
            LogLog.Instance.Logger[LogLogEnum.Debug]("Search");
            LogLog.Instance.Logger[LogLogEnum.Debug]($"{searchText},{field}");

            // get the fields back from the dataset item and check the type

            try {
                List<object[]> results = new List<object[]>();
                IQueryFilter queryFilter = new QueryFilterClass();
                // handle ints etc where they will need a = 
                esriFieldType aType = Utility.ReturnFieldType(item.Value, field);
                queryFilter.WhereClause = QueryMap[aType](field, searchText);
                LogLog.Instance.Logger[LogLogEnum.Debug](queryFilter.WhereClause);
                // Use the PostfixClause to alphabetically order the set by name.
                IQueryFilterDefinition queryFilterDef = (IQueryFilterDefinition)queryFilter;
                queryFilterDef.PostfixClause = $"ORDER BY {field}";
                // Output the returned names and addresses.
                if(item.Value.Type == esriDatasetType.esriDTTable) {
                    ITable table = item.Value as ITable;
                    if(table != null) {
                        using(ComReleaser comReleaser = new ComReleaser()) {
                            ICursor cursor = table.Search(queryFilter, true);
                            comReleaser.ManageLifetime(cursor);
                            IRow row = null;
                            while((row = cursor.NextRow()) != null) {
                                var oa = new object[item.Fields.Count];
                                foreach(string fdName in item.Fields) {
                                    int fdIndex = table.FindField(fdName);
                                    String fdValue = Convert.ToString(row.get_Value(fdIndex));
                                    Debug.WriteLine(fdValue);
                                    oa[fdIndex] = fdValue;
                                }
                                results.Add(oa);
                            }
                        }
                    }
                }
                return results;
            } catch(Exception e) {
                LogLog.Instance.Logger[LogLogEnum.Error](e.Message);
                throw;
            }
        }

        internal static List<FeatureModel> GetRelatedFeatures(TableDropdownDataContext selectedItem, object[] objarray) {
            List<FeatureModel> results = new List<FeatureModel>();
            int idx = selectedItem.Fields.IndexOf(selectedItem.Fields.Find(x => x.ToUpper().Equals("OBJECTID", StringComparison.InvariantCultureIgnoreCase)));
            ITable table = selectedItem.Value as ITable;
            if(table != null) {
                IRow aRow = table.GetRow(Convert.ToInt32(objarray[idx]));
                TableHelper th = TableHelper.Instance;
                th.RelationLookup[selectedItem.Value.Name].ForEach(x => {
                    IObject aRowObj = aRow as IObject;
                    if(aRowObj != null) {
                        ISet aSet = x.GetObjectsRelatedToObject(aRowObj);
                        object obj = null;
                        if(aSet.Count > 0) {
                            while((obj = aSet.Next()) != null) {
                                IFeature aFeat = obj as IFeature;
                                if(!results.Any(z => z.Feature.Class.ObjectClassID == aFeat.Class.ObjectClassID && z.ObjectID == aFeat.OID)) {
                                    results.Add(new FeatureModel { ObjectID = aFeat.OID, TableName = aFeat.Class.AliasName, Feature = aFeat });
                                }                       
                            }
                        }
                    }
                });
            }
            return results;
        }

        private static string LikeQuery(string field, string searchText) => $"{field} LIKE '%{searchText}%'";
        private static string EqualsStringQuery(string field, string searchText) => $"{field} = '{searchText}'";
        private static string EqualsQuery(string field, string searchText) => $"{field} = {searchText}";

    }
}
