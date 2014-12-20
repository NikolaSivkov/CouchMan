using System.Collections.Generic;

namespace CouchMan
{
    public class ViewsHolder
    {
        public ViewsHolder()
        {
            views = new Dictionary<string, MapString>();
        }
        public Dictionary<string, MapString> views { get; set; }
    }
 
    public class Rootobject
    {
        public Rootobject()
        {
            rows = new List<Row>();
        }
        public List<Row> rows { get; set; }
    }

    public class Row
    {
        public Doc doc { get; set; }
        public Controllers controllers { get; set; }
    }

    public class Doc
    {
        public Meta meta { get; set; }
        public ViewsHolder json { get; set; }
    }

    public class Meta
    {
        public string id { get; set; }
        public string rev { get; set; }
    }
    
    public class Controllers
    {
        public string compact { get; set; }
        public string setUpdateMinChanges { get; set; }
    }
}