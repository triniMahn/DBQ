using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBQ.Utils;


namespace DBQ.Framework
{
    //This could be converted into a class hierarchy at a later date if we require different workflows.
    public enum QueueItemStatus
    {
        PENDING = 0,
        PROCESSING = 1,
        PROCESSED = 2,
        ERROR = 3,
        ARCHIVED = 4,
        DUPLICATE =5
    }

    public abstract class QueueItem
    {
        public int QueueID { get; protected set; }
        public DateTime QueueDateTime { get; set; }
        public string Title { get; set; }
        public int Status { get; set; }
        public object ItemData { get; set; }
        public int ItemType { get; set; }
        public abstract bool defaultProcessAction();

        public int? ProcessingErrorCode { get; protected set; }
        //So that this is only done once regardless of whether or not there are many processors acting upon this one item
        //that require data from within the ItemData property
        public abstract object[] getDeserializedData();

        public abstract void recordProcessingError(int errorCode);


        public QueueItem() { }

    }

    //Chain of Responsibility w.r.t. post and pre processing queueable items
    public abstract class QueueItemProcessor
    {

        public abstract string Name { get; protected set; }
        public abstract bool process(QueueItem item);
        public abstract bool HaltProcessingOnError { get; }
    }

    public abstract class QueueItemProcessorController
    {
        protected List<QueueItemProcessor> preProcessors = new List<QueueItemProcessor>();
        protected List<QueueItemProcessor> postProcessors = new List<QueueItemProcessor>();


        //protected, since we'll use it in a getInstance type situation.
        protected bool addPreProcessor(QueueItemProcessor processor)
        {
            if (null == processor)
                return false;

            if (true == preProcessors.Exists(p => p.Name == processor.Name))
            {
                QueueDebug.WriteLine("QueueItemProcessorController::addPreProcessor: Controller already contains processor " + processor.Name, true);
                return false;
            }

            preProcessors.Add(processor);

            return true;
        }

        //Common functionality with above. Factor out later.
        protected bool addPostProcessor(QueueItemProcessor processor)
        {
            if (null == processor)
                return false;

            if (true == postProcessors.Exists(p => p.Name == processor.Name))
            {
                QueueDebug.WriteLine("QueueItemProcessorController::addPostProcessor: Controller already contains processor " + processor.Name, false);
                return false;
            }

            postProcessors.Add(processor);

            return true;
        }

        //Template method for processing the item.
        //Similar to using priority to control the order in which processors tackle the QueueItem, but this way the order is more explicit,
        //and if there is no pre, or post processing then the QueueItem::defaultProcessAction method can be used. If a processor can be reused for
        //a queue item, then defaultProcessAction can be overriden and left empty. This gives us the option of not having to create a processor
        //for each concrete implementation of QueueItem.

        public void processItem(QueueItem item)
        {
            bool preProcResult = false, postProcResult = false;

            //It's best to handle the exception as close to the source as possible; however, in case the implementor of the concrete class forgets
            //then we can ensure that the calling thread isn't aborted due to the unhandled exception.
            try
            {
                foreach (QueueItemProcessor preQIP in preProcessors)
                {
                    preProcResult = preQIP.process(item);
                    if (false == preProcResult && false == preQIP.HaltProcessingOnError)
                    {
                        QueueDebug.WriteLine("Pre-processing of " + item.QueueID + " failed. Pre-processor: " + preQIP.Name, true);
                    }
                    else if (false == preProcResult && true == preQIP.HaltProcessingOnError)
                    {
                        //ApplicationSettings.getInstance().getLogger().logWarning("Pre-processing (" + preQIP.Name + ") error - Item ID: " + item.QueueID);
                        item.recordProcessingError(-1);
                        return;
                    }

                }

                if (false == item.defaultProcessAction())
                    QueueDebug.WriteLine("Default processing failed for :" + item.QueueID, true);



                foreach (QueueItemProcessor postQIP in postProcessors)
                {
                    postProcResult = postQIP.process(item);

                    if (false == postProcResult && false == postQIP.HaltProcessingOnError)
                    {
                        QueueDebug.WriteLine("Post-processing of " + item.QueueID + " failed. Post-processor: " + postQIP.Name, true);
                    }
                    else if (false == postProcResult && true == postQIP.HaltProcessingOnError)
                    {
                        //ApplicationSettings.getInstance().getLogger().logWarning("Post-processing (" + postQIP.Name + ") error - Item ID: " + item.QueueID);
                        item.recordProcessingError(-2);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                //Log Application Error here
                QueueDebug.WriteToLog("QueueItemProcessorController::processItem: Error processing QueueItem ID:" + item.QueueID + " " + ex.Message);
            }
        }
    }

    public class QueueItemProcessorControllerFactory
    {
        private static Dictionary<System.Type, QueueItemProcessorController> procControllers = new Dictionary<System.Type, QueueItemProcessorController>();
        private static readonly object procControllerLockObj = new object();

        public static QueueItemProcessorController getInstance(System.Type queueItemType)
        {
            if (false == procControllers.ContainsKey(queueItemType))
            {
                lock (procControllerLockObj)
                {
                    if (false == procControllers.ContainsKey(queueItemType))
                    {
                        QueueItemProcessorController c = getQueueItemProcessorController(queueItemType);
                        procControllers.Add(queueItemType, c);
                        return c;
                    }
                }
            }
            return procControllers[queueItemType];
        }

        protected static QueueItemProcessorController getQueueItemProcessorController(System.Type queueItemType)
        {
            QueueItemProcessorController controller = null;
            //TODO: Put this in config settings so that we don't have to reference the concrete classes.
            //Also cache these, so that we're not creating a new instance every time
            //if (queueItemType == typeof(APITransactionQueueItem))
            //{
            //    controller = APITransactionQueueItemProcessorController.getInstance();

            //}
            //else
            //{
                //controller = GenericItemProcessorController.getInstance();
            //}
            return controller;
        }

        public static List<QueueItemProcessor> getPreProcessors(System.Type queueItemType)
        {
            List<QueueItemProcessor> preProcessors = null;

            return preProcessors;
        }

        public static List<QueueItemProcessor> getPostProcessors(System.Type queueItemType)
        {
            List<QueueItemProcessor> postProcessors = null;

            return postProcessors;
        }
    }
}
