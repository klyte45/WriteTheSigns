using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Libraries;
using System;
using System.Text;

namespace Klyte.WriteTheSigns.Data
{
    public abstract class WTSLibBaseData<LIB, DESC> : BasicLib<LIB, DESC>, IDataExtensor
        where LIB : WTSLibBaseData<LIB, DESC>, new()
        where DESC : ILibable
    {
        public abstract string SaveId { get; }
        public static LIB Instance
        {
            get {
                if (!ExtensorContainer.instance.Instances.TryGetValue(typeof(LIB), out IDataExtensor result) || result == null)
                {
                    ExtensorContainer.instance.Instances[typeof(LIB)] = new LIB();
                }
                return ExtensorContainer.instance.Instances[typeof(LIB)] as LIB;
            }
        }


        public IDataExtensor Deserialize(Type type, byte[] data)
        {
            string content;
            if (data[0] == '<')
            {
                content = Encoding.UTF8.GetString(data);
            }
            else
            {
                content = ZipUtils.Unzip(data);
            }

            return XmlUtils.DefaultXmlDeserialize<LIB>(content);
        }

        public byte[] Serialize() => ZipUtils.Zip(XmlUtils.DefaultXmlSerialize((LIB)this, false));
        public virtual void OnReleased() { }

        public virtual void LoadDefaults() { }

        protected override void Save() { }
    }
}
