using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DBQ.Utils;

namespace DBQ.Framework
{

    public class QueueFactory
    {
        protected static int defaultWorkerCount = 2;

        public static Queue createAndStartQueue(string queueType, QueueSettings settings = null)
        {
            Queue q = null;
            int workerCount = 0;

            //if (true == queueType.Equals("APITransactionQueue"))
            //{
            //    int.TryParse(System.Web.Configuration.WebConfigurationManager.AppSettings["Queue.WorkerCount"].ToString(), out workerCount);
            //    workerCount = workerCount == 0 ? defaultWorkerCount : workerCount;
            //    if(settings==null)
            //        settings = new GenericQueueSettings(false, workerCount, queueType);
            //    q = new APITransactionQueue(settings);
            //    q.startQueue();
            //}
            /*
        else if (true == queueType.Equals("ScheduledGivingQueue"))
        {
            ScheduledGivingQueueSettings settings = new ScheduledGivingQueueSettings(queueType);
            q = new ScheduledGivingQueue(settings);

        }
        else if (true == queueType.Equals("SAPSyncQueue"))
        {
            ConfigurableQueueSettings settings = new ConfigurableQueueSettings(queueType);
            q = new SAPSyncQueue(settings);

        }
        else if (true == queueType.Equals("DisbConfQueue"))
        {
            ConfigurableQueueSettings settings = new ConfigurableQueueSettings(queueType);
            q = new DisbConfQueue(settings);

        }
         */
            //else
            //{
            q = createQueue(queueType, settings);

            //}

            if (q.Settings.Enable)
                q.startQueue();
            else
                QueueDebug.WriteLine("The queue " + queueType + " will not be started because it's not set to be enabled in the config", true);

            return q;
        }

        public static Queue createQueue(string queueType, QueueSettings settings = null)
        {
            Queue q = null;
            int workerCount;
            //if (true == queueType.Equals("APITransactionQueue"))
            //{
            //    int.TryParse(System.Web.Configuration.WebConfigurationManager.AppSettings["Queue.WorkerCount"].ToString(), out workerCount);
            //    workerCount = workerCount == 0 ? defaultWorkerCount : workerCount;

            //    if(settings==null)
            //         settings = new GenericQueueSettings(false, workerCount, queueType);
            //    q = new APITransactionQueue(settings);
            //}

            //else
            //{
            //if(settings==null)
            ////     settings = new ConfigurableQueueSettings(queueType);
            //Type type = Type.GetType("CanadaHelps.DataModel.Queues.Implementation." + queueType, false);
            //q = Activator.CreateInstance(type, new object[] { settings }) as Queue;

            //}

            return q;
        }
    }

}
