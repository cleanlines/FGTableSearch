using ESRI.ArcGIS.Geodatabase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace FGTableSearch {
    public static class Utility {

        public static string ParseTableName(IDataset dataset) {
            if(dataset == null) {
                throw new ArgumentNullException("dataset");
            }
            string dbName = string.Empty;
            string tableName = string.Empty;
            string ownerName = string.Empty;
            ISQLSyntax syntax = (ISQLSyntax)dataset.Workspace;
            syntax.ParseTableName(dataset.Name, out dbName, out ownerName, out tableName);
            return tableName;
        }

        public static List<string> ReturnFieldNames(IDataset dataset) {
            if(dataset == null) {
                throw new ArgumentNullException("dataset");
            }
            IFields fields = null;
            if(dataset.Type == esriDatasetType.esriDTTable) {
                fields = ((ITable)dataset).Fields;

            } else if(dataset.Type == esriDatasetType.esriDTFeatureClass) {
                fields = ((IFeatureClass)dataset).Fields;
            }
            List<string> aList = new List<string>();
            for(int i = 0; i < fields.FieldCount; i++) {
                IField field = fields.Field[i];
                aList.Add(field.Name);
            }
            return aList;
        }

        public static esriFieldType ReturnFieldType(IDataset dataset, String fieldName) {
            if(dataset == null) {
                throw new ArgumentNullException("dataset");
            }
            IFields2 fields = null;
            if(dataset.Type == esriDatasetType.esriDTTable) {
                fields = ((ITable)dataset).Fields as IFields2;

            } else if(dataset.Type == esriDatasetType.esriDTFeatureClass) {
                fields = ((IFeatureClass)dataset).Fields as IFields2;
            }

            int fdIdx = -1;

            ISQLSyntax SQLsyn = dataset.Workspace as ISQLSyntax;
            fields.FindFieldIgnoreQualification(SQLsyn, fieldName,out fdIdx);
            if(fdIdx!= -1) {
                IField aFd = fields.Field[fdIdx];
                Debug.WriteLine(aFd.Name);
                Debug.WriteLine(aFd.Type.ToString());
                return aFd.Type;
            }

            throw new Exception("Cannot find this field");
        }
    }
}
