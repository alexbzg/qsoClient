using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using XmlConfigNS;
using InvokeFormNS;
using System.Runtime.Serialization;

namespace StorableFormState
{
    public class FormWStorableState<ConfigType> : InvokeForm where ConfigType : StorableFormConfig, new()
    {
        public XmlConfig<ConfigType> config;
        public virtual void writeConfig() {
            config.write();
        }
        public bool loaded = false;

        public void storeFormState()
        {
            Rectangle bounds = this.WindowState != FormWindowState.Normal ? this.RestoreBounds : this.DesktopBounds;
            config.data.formLocation = bounds.Location;
            config.data.formSize = bounds.Size;
        }

        public void restoreFormState()
        {
              if (config != null && config.data != null && config.data.formLocation != null && !config.data.formLocation.IsEmpty)
                  this.DesktopBounds =
                          new Rectangle(config.data.formLocation, config.data.formSize);
        }

        public FormWStorableState()
        {
            config = new XmlConfig<ConfigType>();
            Load += FormWStorableState_Load;
            ResizeEnd += FormWStorableState_MoveResize;
            Move += FormWStorableState_MoveResize;
        }

        private void FormWStorableState_MoveResize(object sender, EventArgs e)
        {
            if (loaded)
            {
                storeFormState();
                writeConfig();
            }
        }


        private void FormWStorableState_Load(object sender, EventArgs e)
        {
            restoreFormState();
            loaded = true;

        }
    }


    [DataContractAttribute]
    public class StorableFormConfig
    {
        public System.Drawing.Point formLocation;
        public System.Drawing.Size formSize;

        public StorableFormConfig() { }
    }

}
