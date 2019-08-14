using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace XmlConfigNS
{

    public class XmlConfig<ConfigType> where ConfigType : new()
    {
        public ConfigType data;
        private string fname;

        public XmlConfig() : this(Application.StartupPath + "\\config.xml") { }

        public XmlConfig( string _fname )
        {
            fname = _fname;
            data = new ConfigType();
            if (File.Exists(fname) )
            {
                XmlSerializer ser = new XmlSerializer(typeof(ConfigType));
                using (FileStream fs = File.OpenRead(Application.StartupPath + "\\config.xml"))
                {
                    try
                    {
                        data = (ConfigType)ser.Deserialize(fs);
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine(e.ToString());
                    }
                }
            }
        }

        public void write()
        {
            using (StreamWriter sw = new StreamWriter(fname))
            {
                try
                {
                    XmlSerializer ser = new XmlSerializer(typeof(ConfigType));
                    ser.Serialize(sw, data);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                }
            }
        }
    }

}
