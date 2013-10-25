using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace DBQ.Framework
{
    
    public class QueueConfigurationSection: ConfigurationSection
    {
        [ConfigurationProperty("Queues")]
        public QueueCollection Queues
        {
            get { return ((QueueCollection)(base["Queues"])); }
        }
    }

    [ConfigurationCollection(typeof(QueueElement), AddItemName="Queue")]
    public class QueueCollection : ConfigurationElementCollection, IEnumerable<QueueElement>
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new QueueElement();
        }
        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((QueueElement)(element)).QueueName;
        }
        public QueueElement this[int idx]
        {
            get
            {
                return (QueueElement)BaseGet(idx);
            }
        }
        public new QueueElement this[string key]
        {
            get { return (QueueElement)BaseGet(key); }
        }

        public new IEnumerator<QueueElement> GetEnumerator()
        {
            return (from i in Enumerable.Range(0, this.Count)
                    select this[i])
                    .GetEnumerator();
        }
    }

    public class QueueElement : ConfigurationElement
    {
        [ConfigurationProperty("QueueName", DefaultValue = "", IsRequired = true)]
        public string QueueName
        {
            get
            {
                return ((string)(this["QueueName"]));
            }
            set
            {
                this["QueueName"] = value;
            }
        }

        [ConfigurationProperty("Enabled", DefaultValue = "false", IsRequired = true)]
        public bool Enabled
        {
            get
            {
                return ((bool)(this["QueueName"]));
            }
            set
            {
                this["QueueName"] = value;
            }
        }

        [ConfigurationProperty("ConnectionString")]
        public ConnectionStringElement ConnectionString
        {
            get
            {
                return (ConnectionStringElement)this["ConnectionString"];
            }
            set
            {
                this["ConnectionString"] = value;
            }
        }

        [ConfigurationProperty("ShutDown")]
        public ShutDownElement ShutDown
        {
            get
            {
                return (ShutDownElement)this["ShutDown"];
            }
            set
            {
                this["ShutDown"] = value;
            }
        }

        [ConfigurationProperty("WorkerThreads")]
        public WorkerThreadsElement WorkerThreads
        {
            get
            {
                return (WorkerThreadsElement)this["WorkerThreads"];
            }
            set
            {
                this["WorkerThreads"] = value;
            }
        }

    }

    public class ConnectionStringElement : ConfigurationElement
    {
        [ConfigurationProperty("Name", IsRequired=true)]
        public string Name
        {
            get
            {
                return (string)this["Name"];
            }
            set
            {
                this["Name"] = value;
            }
        }
    }

    public class ShutDownElement : ConfigurationElement
    {
        [ConfigurationProperty("WhenQueueEmpty", IsRequired = true)]
        public bool WhenQueueEmpty
        {
            get
            {
                return (bool)this["WhenQueueEmpty"];
            }
            set
            {
                this["WhenQueueEmpty"] = value;
            }
        }
    }

    public class WorkerThreadsElement : ConfigurationElement
    {
        [ConfigurationProperty("Count", IsRequired = true)]
        public int Count
        {
            get
            {
                return (int)this["Count"];
            }
            set
            {
                this["Count"] = value;
            }
        }

        [ConfigurationProperty("ItemBatchSize", IsRequired = true)]
        public int ItemBatchSize
        {
            get
            {
                return (int)this["ItemBatchSize"];
            }
            set
            {
                this["ItemBatchSize"] = value;
            }
        }
        
        [ConfigurationProperty("ItemProcessRate", IsRequired = true)]
        public int ItemProcessRate
        {
            get
            {
                return (int)this["ItemProcessRate"];
            }
            set
            {
                this["ItemProcessRate"] = value;
            }
        }

        [ConfigurationProperty("PeekRateThrottlingEnabled", IsRequired = true)]
        public bool PeekRateThrottlingEnabled
        {
            get
            {
                return (bool)this["PeekRateThrottlingEnabled"];
            }
            set
            {
                this["PeekRateThrottlingEnabled"] = value;
            }
        }

    }


}
