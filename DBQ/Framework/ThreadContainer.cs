using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using DBQ.Utils;

namespace DBQ.Framework
{
    /// <summary>
    /// Wraps built-in thread(ing) functionality for use with a Queue-based processing system.
    /// </summary>
    public class ThreadContainer
    {
        protected Thread t = null;
        protected bool runThread = false;
        protected bool waitAllThreadsFinish = false;

        public string Name { get; protected set; }

        //Both of these are in milliseconds
        public int ItemProcessRate { get; protected set; }
        
        public int ItemBatchProcessRate { get; protected set; }

        public DateTime ContainerCreated { get; protected set; }
        public DateTime ContainerStarted { get; protected set; }
      
        protected Queue myQueue = null;

        public event EventHandler noItems;
        
        public ThreadContainer(string name, Queue queue)
        {
            Name = name;
            ItemProcessRate = 5000;
            ItemBatchProcessRate = 5000;
            ContainerCreated = DateTime.Now;
            myQueue = queue;
        }

        protected static void writeOut(string msg, bool writeToLog)
        {
            QueueDebug.WriteLine(msg,writeToLog);
        }

        public void setItemProcessRate(int newItemProcessRate, int newItemBatchProcessRate)
        {
            if ( newItemProcessRate!=0)
                ItemProcessRate = newItemProcessRate;
            
            if(newItemBatchProcessRate!=0)
                ItemBatchProcessRate = newItemBatchProcessRate;
        }

        public bool threadAlive()
        {
            if (null == t)
                return false;

            if (true == t.IsAlive)
                return true;

            return false;
        }

        public bool endThread()
        {
            if (false == threadAlive())
                return false;

            runThread = false;
            writeOut(this.Name + ": runThread = false",true);

            //The time allowed before aborting the thread should be > ItemBatchProcessRate + [ItemProcessRate * (time allowed between calls to processItem) ]
            //60,000 = 60 seconds
            Thread.Sleep(10000);

            if (true == threadAlive())
            {
                QueueDebug.WriteLine("Thread (" + myQueue.Settings.QueueName + ":" + this.Name + ") Still Alive...aborting",true);

                try
                {
                    t.Abort();
                }
                catch (System.Security.SecurityException se)
                {
                    writeOut("Error aborting " + myQueue.Settings.QueueName + this.Name + " :" + se.Message,true);
                    return false;
                }
                catch (ThreadStateException tse)
                {
                    writeOut("Error aborting " + myQueue.Settings.QueueName + this.Name + " :" + tse.Message,true);
                    return false;
                }
            }
            return true;
        }

        public bool gracefulEndThread()
        {
            if (false == threadAlive())
                return false;

            runThread = false;
            waitAllThreadsFinish = true;
            writeOut(this.Name + ": runThread = false", true);
            
            if (t.IsAlive)
                t.Join();   //Wait for all threads to finish whatever they are doing
                
                try
                {
                    t.Abort();
                }
                catch (System.Security.SecurityException se)
                {
                    writeOut("Error aborting " + myQueue.Settings.QueueName + this.Name + " :" + se.Message, true);
                    return false;
                }
                catch (ThreadStateException tse)
                {
                    writeOut("Error aborting " + myQueue.Settings.QueueName + this.Name + " :" + tse.Message, true);
                    return false;
                }

            return true;

        }

        public bool startThread()
        {
            if (true == threadAlive())
            {
                writeOut("Thread already started",true);
                return false;
            }
            runThread = true;

            //Always handle exceptions as close to the source as possible
            try
            {
                t = new Thread(new ParameterizedThreadStart(ThreadMain));
                t.Name = this.myQueue.Settings.QueueName + ":" + this.Name;
                t.Start(new object[] { Name, ItemBatchProcessRate, ItemProcessRate });
                
                ContainerStarted = DateTime.Now;
            }
            catch (ThreadStateException tse)
            {
                writeOut(tse.Message,true);
                return false;
            }
            catch (OutOfMemoryException ome)
            {
                writeOut(ome.Message,true);
                return false;
            }
            catch (InvalidOperationException ioe)
            {
                writeOut(ioe.Message,true);
                return false;
            }
            return true;
        }

        //Could later factor this out into a "Strategy" type structure
        protected void ThreadMain(object data)
        {
            int count = 0;
            object[] parms = (object[])data;
            //These are parameters that we don't want changing once the thread has started
            string name = (string)parms[0];
            int batchProcessRate = (int)parms[1];
            int itemProcessRate = (int)parms[2];

            while (true == runThread)
            {
                writeOut(name + " (created: " + ContainerCreated.ToString() + " started: " + ContainerStarted + ")" + ": Main loop:" + (count++).ToString(),false);

                List<QueueItem> items = null;

                //In case the implementor of DequeueForProcessing forgets to handle exceptions, deal with any in this catch-all.
                try
                {
                    items = myQueue.DequeueForProcessing();
                }
                catch (Exception ex)
                {
                    //** Log application error here and send admin notification
                    writeOut("Error in ThreadMain while performing Dequeue: " + ex.Message,true);
                    Thread.Sleep(10000);
                }

                if (null != items)
                {
                    writeOut(myQueue.Settings.QueueName + " - " + name + ": Processing " + items.Count + " items",false);
                    if (items.Count != 0)
                    {
                        foreach (QueueItem q in items)
                        {
                            if (false == runThread && false == waitAllThreadsFinish)
                            {
                                writeOut(myQueue.Settings.QueueName + " - " + this.Name + ": Returning from ThreadMain",true);
                                return;
                            }

                            //Any exceptions that may be thrown down the call stack will be handled by processItem(), so that this thread can continue.
                            QueueItemProcessorControllerFactory.getInstance(q.GetType()).processItem(q);
                            Thread.Sleep(itemProcessRate);
                        }
                    }
                }
                else
                {
                    if (noItems != null)
                        noItems(this, new EventArgs());

                    if (true == myQueue.Settings.ShutDownWhenQueueEmpty)
                    {
                        writeOut(myQueue.Settings.QueueName + " - " + this.Name + ": ShutDownWhenQueueEmpty enabled",true);
                        break;
                    }
                }

                Thread.Sleep(batchProcessRate);

            }
            writeOut(name + " (" + ContainerCreated.ToString() + ")" + ": ThreadMain Ending...",true);
        }

    }
}
