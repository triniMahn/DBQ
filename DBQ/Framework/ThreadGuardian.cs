using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using DBQ.Utils;

namespace DBQ.Framework
{
    public class ThreadGuardian
    {
        protected List<ThreadContainer> myThreads = null;
        protected static int workerInstantiationDelay = 2000;
        public event EventHandler queueEmpty;
        
        private static long deadThreads=0;
        
        public int ThreadCount { get; protected set; }

        protected ThreadGuardian(int numThreads, Queue queue)
        {
            ThreadCount = numThreads;
            myThreads = new List<ThreadContainer>(ThreadCount);
            for (int i = 0; i < ThreadCount; i++)
            {
                myThreads.Add(new ThreadContainer(("WorkerThread" + i), queue));
                myThreads[i].setItemProcessRate(queue.Settings.ItemProcessRate, queue.Settings.BatchItemProcessRate);
            }

            myThreads.ForEach(o => o.noItems += new EventHandler(onEmptyItemsList));  
        }

        public bool verifyWorkersRunning()
        {
            foreach (ThreadContainer tc in myThreads)
            {
                if (tc.threadAlive() == true)
                    return true;
            }
            return false;
        }
    
        public static ThreadGuardian createThreads(int numThreads,Queue queue)
        {
           ThreadGuardian tg = new ThreadGuardian(numThreads, queue);     
           return tg;
        }

        public static ThreadGuardian createAndStartThreads(int numThreads, Queue queue)
        {
            ThreadGuardian tg = new ThreadGuardian(numThreads,queue);
            bool started = false;

            for (int i = 0; i < tg.ThreadCount; i++)
            {
                tg.myThreads.Add(new ThreadContainer(("WorkerThread" + i), queue));
                tg.myThreads[i].setItemProcessRate(queue.Settings.ItemProcessRate, queue.Settings.BatchItemProcessRate);
                started = tg.myThreads[i].startThread();            
                if (false == started)
                {
                    QueueDebug.WriteLine("Problem starting Worker Thread(s) for Queue: ", queue, tg.myThreads[i], true);
                    break;
                }

                Thread.Sleep(workerInstantiationDelay);
            }

            return tg;
        }

        public bool startThreads()
        {
            if (null == myThreads)
                return false;

            bool started = false;
            foreach (ThreadContainer tc in myThreads)
            {
                started = tc.startThread();
                if (false == started)
                {
                    QueueDebug.WriteLine("Problem starting Worker Thread: " + tc.Name,true);
                    break;
                }
                Thread.Sleep(workerInstantiationDelay);
            }
                
            return true;
        }

        private void onEmptyItemsList(object sender, EventArgs e)
        {
            //increment each time we received no item event from threadcontainer          
            //Interlocked is atomic 
            Interlocked.Increment(ref deadThreads); 
            //if all threads reported that they don't have any items, fire queueEmpty event
            if ( Interlocked.Read(ref deadThreads) == ThreadCount)
            {
                Interlocked.Exchange(ref deadThreads, 0);
                if(queueEmpty!=null)
                    queueEmpty(this, new EventArgs());
            }
        }
        public bool stopThreads()
        {
            foreach (ThreadContainer tc in myThreads)
            {
                if (false == tc.endThread())
                    return false;

            }
            return true;
        }
        public bool gracefulStopThreads()
        {
            foreach (ThreadContainer tc in myThreads)
            {
                if (false == tc.gracefulEndThread())
                    return false;

            }
            return true;
        }
        public bool setThreadItemProcessRate(int newItemProcessRate, int newBatchItemProcessRate)
        {
            if (null == myThreads)
                return false;

            foreach (ThreadContainer tc in myThreads)
            {
                tc.setItemProcessRate(newItemProcessRate,newBatchItemProcessRate);

            }
            return true;

        }
    }
}
