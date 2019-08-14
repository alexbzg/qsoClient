using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SerializationNS
{
    /// <summary>
    /// Functions for performing common binary Serialization operations.
    /// <para>All properties and variables will be serialized.</para>
    /// <para>Object type (and all child types) must be decorated with the [Serializable] attribute.</para>
    /// <para>To prevent a variable from being serialized, decorate it with the [NonSerialized] attribute; cannot be applied to properties.</para>
    /// </summary>
    public static class BinarySerialization
    {
        /// <summary>
        /// Writes the given object instance to a binary file.
        /// <para>Object type (and all child types) must be decorated with the [Serializable] attribute.</para>
        /// <para>To prevent a variable from being serialized, decorate it with the [NonSerialized] attribute; cannot be applied to properties.</para>
        /// </summary>
        /// <typeparam name="T">The type of object being written to the XML file.</typeparam>
        /// <param name="filePath">The file path to write the object instance to.</param>
        /// <param name="objectToWrite">The object instance to write to the XML file.</param>
        /// <param name="append">If false the file will be overwritten if it already exists. If true the contents will be appended to the file.</param>
        public static void WriteToBinaryFile<T>(string filePath, T objectToWrite, bool append = false)
        {
            using (Stream stream = File.Open(filePath, append ? FileMode.Append : FileMode.Create))
            {
                var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                binaryFormatter.Serialize(stream, objectToWrite);
            }
        }

        /// <summary>
        /// Reads an object instance from a binary file.
        /// </summary>
        /// <typeparam name="T">The type of object to read from the XML.</typeparam>
        /// <param name="filePath">The file path to read the object instance from.</param>
        /// <returns>Returns a new instance of the object read from the binary file.</returns>
        public static T ReadFromBinaryFile<T>(string filePath)
        {
            using (Stream stream = File.Open(filePath, FileMode.Open))
            {
                var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                return (T)binaryFormatter.Deserialize(stream);
            }
        }
    }

    public static class ProtoBufSerialization
    {
        public static void Write<T>(string filePath, T objectToWrite, bool append = false)
        {
            try
            {
                using (FileStream file = File.Open(filePath, append ? FileMode.Append : FileMode.Create))
                {
                    Serializer.SerializeWithLengthPrefix(file, objectToWrite,
                        PrefixStyle.Base128, Serializer.ListItemTag);
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }
        }

        public static void WriteList<T>(string filePath, List<T> list, bool append = false)
        {
            try
            {
                using (FileStream file = File.Open(filePath, append ? FileMode.Append : FileMode.Create))
                {
                    foreach ( T o in list )
                        Serializer.SerializeWithLengthPrefix(file, o,
                            PrefixStyle.Base128, Serializer.ListItemTag);
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }
        }


        public static T Read<T>(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    using (FileStream file = File.OpenRead(filePath))
                    {
                        return Serializer.DeserializeItems<T>(
                            file, PrefixStyle.Base128, Serializer.ListItemTag).FirstOrDefault();
                    }
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }
            return default(T);
        }


        public static List<T> ReadList<T>(string filePath)
        {
            List<T> r = new List<T>();
            try
            {
                if (File.Exists(filePath))
                {
                    using (FileStream file = File.OpenRead(filePath))
                    {
                        foreach (T o in Serializer.DeserializeItems<T>(
                            file, PrefixStyle.Base128, Serializer.ListItemTag))
                            r.Add(o);
                    }
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }
            return r;
        }

        public static List<T> ReadListItems<T>(string filePath, out bool er)
        {
            List<T> r = new List<T>();
            er = false;
            try
            {
                if (File.Exists(filePath))
                {
                    using (FileStream file = File.OpenRead(filePath))
                    {
                        T o;
                        while (file.Position != file.Length) {
                            try
                            {
                                o = Serializer.DeserializeWithLengthPrefix<T>(file, PrefixStyle.Base128, Serializer.ListItemTag);
                                r.Add(o);
                            }
                            catch (Exception e)
                            {
                                System.Diagnostics.Debug.WriteLine(e.ToString());
                                er = true;
                                /*file.Position += recLength;
                                recLength = 1;*/
                            }

                        }
                    }
                }
            }
            catch (Exception e)
            {

                System.Diagnostics.Debug.WriteLine(e.ToString());
            }
            return r;
        }
    }

    public static class JSONSerializer
    {
        public static string Serialize<T> (T obj)
        {
            try
            {
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(T));
                string output = string.Empty;

                using (MemoryStream ms = new MemoryStream())
                {
                    ser.WriteObject(ms, obj);
                    output = Encoding.UTF8.GetString(ms.ToArray());
                }
                return output;
            } catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }
            return string.Empty;
        }
    }

    [XmlRoot("Dictionary")]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IXmlSerializable
    {
        #region IXmlSerializable Members
        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }
        public void ReadXml(System.Xml.XmlReader reader)
        {
            XmlSerializer keySerializer = new XmlSerializer(typeof(TKey));
            XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue));
            bool wasEmpty = reader.IsEmptyElement;
            reader.Read();
            if (wasEmpty)
                return;
            while (reader.NodeType != System.Xml.XmlNodeType.EndElement)
            {
                reader.ReadStartElement("item");
                reader.ReadStartElement("key");
                TKey key = (TKey)keySerializer.Deserialize(reader);
                reader.ReadEndElement();
                reader.ReadStartElement("value");
                TValue value = (TValue)valueSerializer.Deserialize(reader);
                reader.ReadEndElement();
                this.Add(key, value);
                reader.ReadEndElement();
                reader.MoveToContent();
            }
            reader.ReadEndElement();
        }
        public void WriteXml(System.Xml.XmlWriter writer)
        {
            XmlSerializer keySerializer = new XmlSerializer(typeof(TKey));
            XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue));
            foreach (TKey key in this.Keys)
            {
                writer.WriteStartElement("item");
                writer.WriteStartElement("key");
                keySerializer.Serialize(writer, key);
                writer.WriteEndElement();
                writer.WriteStartElement("value");
                TValue value = this[key];
                valueSerializer.Serialize(writer, value);
                writer.WriteEndElement();
                writer.WriteEndElement();
            }
        }
        #endregion
    }
}
