using SerializationNS;
using StorableFormState;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace QSOclient
{
    [DataContract]
    public class QsoConfig : StorableFormConfig
    {
        static readonly string[] OptionalColumnsList = new string[] { "RDA", "RAFA", "WFF", "Locator" };

        [XmlIgnore]
        string _gpsReaderDeviceID;
        public string gpsReaderDeviceID
        {
            get { return _gpsReaderDeviceID; }
            set { _gpsReaderDeviceID = value; }
        }
        public bool gpsReaderWirelessGW;
        public bool gpsServerLoad;

        [XmlIgnore]
        Dictionary<string, string> rafaData = new Dictionary<string, string>();
        [XmlIgnore]
        public EventHandler<OptionalColumnValueChangedEventArgs> optionalColumnValueChanged;
        [XmlIgnore]
        public EventHandler<EventArgs> logIO;

        public void setOptionalColumnValue(string column, string value)
        {
            if (optionalColumns[column].value != value)
            {
                optionalColumns[column].value = value;
                if (column == "Locator")
                {
                    string newRafa = rafaData.ContainsKey(value) ? rafaData[value] : null;
                    setOptionalColumnValue("RAFA", newRafa);
                }
                optionalColumnValueChanged?.Invoke(this, new OptionalColumnValueChangedEventArgs
                {
                    column = column,
                    value = value
                });
            }

        }

        public string[] getUserColumnsValues()
        {
            string[] r = new string[userColumns.Count];
            for (int c = 0; c < r.Length; c++)
                r[c] = userColumns[c].value;
            return r;
        }

        public string getOptionalColumnValue(string column)
        {
            if (optionalColumns != null && optionalColumns.ContainsKey(column) && optionalColumns[column] != null)
                return optionalColumns[column].value;
            else
                return null;
        }

        public List<int> dgvQSOColumnsWidth;

        [DataMember]
        public string callsign;
        [DataMember]
        public string password;
        [XmlIgnore]
        private string _token;
        [DataMember]
        public string token
        {
            get { return _token; }
            set
            {
                if (_token != value)
                {
                    _token = value;
                    logIO?.Invoke(this, new EventArgs());
                }
            }
        }


        public QsoConfig() : base()
        {
            try
            {
                using (StreamReader sr = new StreamReader(Application.StartupPath + "\\rafa.csv"))
                {
                    do
                    {
                        string line = sr.ReadLine();
                        string[] lineData = line.Split(';');
                        if (lineData[0] == "")
                        {
                            string[] keys = lineData[3].Split(',');
                            foreach (string key in keys)
                            {
                                string entry = lineData[1];
                                if (rafaData.ContainsKey(key))
                                    rafaData[key] += ", " + entry;
                                else
                                    rafaData[key] = entry;
                            }
                        }
                    } while (sr.Peek() >= 0);
                    System.Diagnostics.Debug.WriteLine(rafaData["KN97TF"]);
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
                MessageBox.Show("Rafa data could not be loaded: " + e.ToString(), "DXpedition", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        public void initialize()
        {
            foreach (string c in OptionalColumnsList)
                if (!optionalColumns.ContainsKey(c))
                    optionalColumns[c] = new OptionalColumnSettings
                    {
                        _show = true,
                        _value = null
                    };

        }

        public string toJSON()
        {
            return JSONSerializer.Serialize<QsoConfig>(this);
        }



        public static string qth(Coords c)
        {
            double lat = c.lat;
            double lng = c.lng;
            string qth = "";
            lat += 90;
            lng += 180;
            lat = lat / 10 + 0.0000001;
            lng = lng / 20 + 0.0000001;
            qth += (char)(65 + lng);
            qth += (char)(65 + lat);
            lat = 10 * (lat - Math.Truncate(lat));
            lng = 10 * (lng - Math.Truncate(lng));
            qth += (char)(48 + lng);
            qth += (char)(48 + lat);
            lat = 24 * (lat - Math.Truncate(lat));
            lng = 24 * (lng - Math.Truncate(lng));
            qth += (char)(65 + lng);
            qth += (char)(65 + lat);
            lat = 10 * (lat - Math.Truncate(lat));
            lng = 10 * (lng - Math.Truncate(lng));
            /*            qth += (char)(48 + lng) + (char)(48 + lat);
                        lat = 24 * (lat - Math.Truncate(lat));
                        lng = 24 * (lng - Math.Truncate(lng));
                        qth += (char)(65 + lng) + (char)(65 + lat);*/
            System.Diagnostics.Debug.WriteLine(qth);
            return qth;
        } // returnQth()


    }
}
