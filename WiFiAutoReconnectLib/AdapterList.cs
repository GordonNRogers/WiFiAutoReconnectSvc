using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Xml;

namespace WiFiAutoReconnectLib
{

    public class AdapterList : IConfigurationSectionHandler
    {
        private List<string> adapters = new List<string>();
        public string[] Adapters { get { return adapters.ToArray(); } }

        public object Create(object parent, object configContext, XmlNode section)
        {
            foreach(XmlNode child in section.ChildNodes )
            {
                if (string.Compare(child.Name, "Adapter", true) == 0)
                {
                    adapters.Add(child.InnerText.Trim());
                }
            }
            return this;
        }
    }
}
