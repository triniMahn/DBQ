using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBQ.Framework
{
    public abstract class QueueSettings
    {
        public static readonly int MAX_BATCH_SIZE = 10;

        protected QueueSettings()
        {

        }

        protected QueueSettings(bool shutDownWhenEmpty, int numWorkerThreads, string name)
        {
            int batchCount = 0;

            ShutDownWhenQueueEmpty = shutDownWhenEmpty;
            NumberWorkerThreads = numWorkerThreads;
            QueueName = name;
            //By default this is not the most efficient way to process items, but it is the safest in the case of a processing failure.
            ItemBatchSize = 1;

            int.TryParse(System.Web.Configuration.WebConfigurationManager.AppSettings["Queue.ItemBatchSize"].ToString(), out batchCount);
            ItemBatchSize = batchCount == 0 ? ItemBatchSize : batchCount;
        }

        public bool ShutDownWhenQueueEmpty { get; protected set; }
        public int NumberWorkerThreads { get; protected set; }
        public string QueueName { get; protected set; }
        public int ItemBatchSize { get; protected set; }
        public int BatchItemProcessRate { get; protected set; }
        public int ItemProcessRate { get; protected set; }
        public bool Enable
        {
            get { return enable; }
            set { enable = value; }
        }
        public bool DetectDuplicateSubmission
        {
            get;
            set;
        }
        public bool SendEmailIfQueueStalled { get; set; }

        private bool enable = true;

        //Use to validate how combinations of settings are combines before the queue is started
        protected abstract bool validateSettings();

        public abstract bool settingsValid();

        public void setItemBatchSize(int size)
        {
            if (size > MAX_BATCH_SIZE)
                return;

            ItemBatchSize = size;
        }
    }

}
