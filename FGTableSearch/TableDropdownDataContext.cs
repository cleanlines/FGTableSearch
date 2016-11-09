using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ESRI.ArcGIS.Geodatabase;

namespace FGTableSearch {
   public class TableDropdownDataContext {

        public string TableName { get; set; }
        public IDataset Value { get; set; }
        public List<string> Fields { get; set; }
    }
}
