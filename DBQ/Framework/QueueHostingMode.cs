using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBQ.Framework
{
    public abstract class QueueHostingMode
    {
        protected QueueContainer queueContainerContext = null;

        public QueueHostingMode(QueueContainer queueContainerContext)
        {
            if (null == queueContainerContext)
                throw new ArgumentException("QueueHostingMode concrete class will contain log to decide what to return");

            this.queueContainerContext = queueContainerContext;
        }

        abstract public bool RegisterQueueContainerWithHostingEnvironment();
    }

    public class IISHostingMode : QueueHostingMode
    {
        public IISHostingMode(QueueContainer qcc)
            : base(qcc)
        {
            this.queueContainerContext = qcc;
        }

        public override bool RegisterQueueContainerWithHostingEnvironment()
        {
            System.Web.Hosting.HostingEnvironment.RegisterObject(this.queueContainerContext);

            return true;
        }
    }

    public class WindowsServiceHostingMode : QueueHostingMode
    {
        public WindowsServiceHostingMode(QueueContainer qcc)
            : base(qcc)
        {
            this.queueContainerContext = qcc;
        }

        public override bool RegisterQueueContainerWithHostingEnvironment()
        {
            return false;
        }
    }
}
