using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Timers;


using DBQ.Utils;

namespace DBQ.Framework
{
    public abstract class Queue
    {
        protected ThreadGuardian myThreadGuardian = null;
        protected object tgLockObj = new object();

        public QueueSettings Settings { get; protected set; }

        public bool QueueStarted { get; protected set; }
                
        protected Queue(QueueSettings settings)
        {
            Settings = settings;
        }

        //protected Timer queueTimer=null;

        public bool startQueue()
        {
            bool workersStarted = verifyWorkersStarted();

            if (false == workersStarted)
                return false;

            QueueStarted = true;

            return true;
        }


        public bool stopQueue()
        {
            bool threadsStopped = myThreadGuardian.stopThreads();

            if (false == threadsStopped)
                return false;

            QueueStarted = false;

            QueueDebug.WriteLine("Queue STOPPED", this, null, true);
            QueueDebug.WriteToLog("Queue STOPPED", this, null);
            return true;
        }
        public bool gracefulStopQueue()
        {
            bool threadsStopped = true;

            if (myThreadGuardian != null)
                threadsStopped = myThreadGuardian.gracefulStopThreads();

            if (false == threadsStopped)
                return false;

            QueueStarted = false;

            //QueueDebug.WriteLine("Queue STOPPED", this, null, true);
            QueueDebug.WriteToLog("Queue STOPPED", this, null);
            return true;
        }

        protected bool verifyWorkersStarted()
        {
            if (null == myThreadGuardian)
            {
                lock (tgLockObj)
                {
                    if (null == myThreadGuardian)
                    {
                        //Had to seperate out the create and start thread, to hook on the event handler
                        //Otherwise the 'Empty Queue' event would wire off without a handler being hooked on to it
                        myThreadGuardian = ThreadGuardian.createThreads(Settings.NumberWorkerThreads, this);
                        this.myThreadGuardian.queueEmpty += new EventHandler(OnQueueEmpty);
                        myThreadGuardian.startThreads();
                        //If the thread count isn't as expected then something may have went
                        //wrong while staring threads in the above method call
                        if (myThreadGuardian.ThreadCount != Settings.NumberWorkerThreads)
                            return false;

                        return true;
                    }
                }
            }
            else
            {
                lock (tgLockObj)
                {
                    myThreadGuardian.startThreads();
                }
            }
            return true;
        }

        public bool verifyWorkersRunning()
        {
            if (null == myThreadGuardian)
                return false;

            bool running = false;

            running = myThreadGuardian.verifyWorkersRunning();

            if (false == running)
                this.QueueStarted = false;

            return running;
        }

        public abstract List<QueueItem> DequeueForProcessing();
        public abstract int Enqueue(object[] itemData);
        
        /// <summary>
        /// This method is executed on the event that the queue is empty
        /// Allow each queue to implement it's own additional custom cleanup tasks.
        /// E.g for BatchTransactionQueue, on empty, the donationbatch status will be updated
        /// </summary>
        protected abstract void OnQueueEmpty(object sender, EventArgs e);

    }

    
    public class QueueContainer : System.Web.Hosting.IRegisteredObject
    {
        protected object queueLockObj = new object();
        protected Queue myQueue = null;

        //static QueueContainer()
        //{
        //    myQueue = QueueFactory.createAndStartQueue("");
        //    QueueDebug.WriteLine("***Queue Initialized***");
        //}

        public QueueContainer()
        {
            System.Web.Hosting.HostingEnvironment.RegisterObject(this);
        }

        public bool verifyQueueStarted(string queueName = "", QueueSettings settings=null)
        {
            if (null == myQueue)
            {
                lock (queueLockObj)
                {
                    if (null == myQueue)
                    {
                        //QueueDebug.WriteLine("verifyQueueStarted: creating and starting queue: "+ queueName,true);
                        QueueDebug.WriteToLog("verifyQueueStarted: creating and starting queue: " + queueName);
                        
                        //Default queue to start is APITransactionQueue
                        if (String.IsNullOrEmpty(queueName))
                            myQueue = QueueFactory.createAndStartQueue("APITransactionQueue",settings);
                        else
                            myQueue = QueueFactory.createAndStartQueue(queueName,settings);

                        return myQueue.QueueStarted;
                    }
                }
            }
            else
            {
                lock (queueLockObj)
                {
                    myQueue.verifyWorkersRunning();
                    if (false == myQueue.QueueStarted)
                    {
                        //QueueDebug.WriteLine("verifyQueueStarted: Queue exists but is stopped. Starting...",true);
                        QueueDebug.WriteToLog("verifyQueueStarted: Queue exists but is stopped. Starting... " + queueName);
                        myQueue.startQueue();
                    }
                    else
                    {
                        QueueDebug.WriteLine("verifyQueueStarted: Queue already started.", true);
                    }
                    //else if (myQueue.Settings.ShutDownWhenQueueEmpty == true)
                    //    myQueue.startQueue();

                    return myQueue.QueueStarted;
                }
            }
            return true;
        }

       /* public void createQueue(string queueName, QueueSettings settings=null)
        {
            if (myQueue == null)
            {
                lock (queueLockObj)
                {
                    if (myQueue == null)
                    { 
                        myQueue = QueueFactory.createQueue(queueName, settings);
                        QueueDebug.WriteToLog("createQueue: creating queue: " + queueName);
                    }
                }
            }
        }*/
        public bool shutDownQueue()
        {
            bool stopped = myQueue.stopQueue();

            if (false == stopped)
                QueueDebug.WriteLine("Unable to shut down queue: ", myQueue, null, true);

            return stopped;
        }

        public Queue getQueue
        {
            get { return myQueue; }
        }

        #region IRegisteredObject Members

        public void Stop(bool immediate)
        {
            if (myQueue != null)
            {
                if (!immediate)
                    myQueue.gracefulStopQueue();
                else
                {
                    //if (myQueue.QueueStarted && myQueue.Settings.SendEmailIfQueueStalled )
                        //ErrorNotifier.sendErrorNotificationToAdmin("Check batch queue processing status", myQueue.Settings.QueueName + " terminated. Please check for unprocessed items");

                }
            }
            System.Web.Hosting.HostingEnvironment.UnregisterObject(this);
        }

        #endregion
    }

   
}
