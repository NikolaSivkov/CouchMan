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
}