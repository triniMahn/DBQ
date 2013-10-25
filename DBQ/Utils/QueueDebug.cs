using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using DBQ.Framework;

namespace DBQ.Utils
{
    
    public class QueueDebug
    {
        protected static int lineNumber = 0;
        protected static Object lockObject = new Object();

        [Conditional("DEBUG")]
        public static void WriteLine(string output, Queue queue, ThreadContainer thread, bool writeToLog)
        {
            string l = "QD " + (null != queue ? queue.Settings.QueueName + " : " : "") + (null != thread ? thread.Name + " : " : "") + getLineNumber().ToString() + ":\t" + output;
            System.Diagnostics.Debug.WriteLine(l);

            //if (true == writeToLog)
            //    ApplicationSettings.getInstance().getLogger().log(l);
        }

        [Conditional("DEBUG")]
        public static void WriteLine(string output, bool writeToLog)
        {
            string l = "QD " + getLineNumber().ToString() + ":\t" + output;
            System.Diagnostics.Debug.WriteLine(l);

            //if (true == writeToLog)
            //    ApplicationSettings.getInstance().getLogger().log(l);
        }

        public static void WriteToLog(string output, Queue queue, ThreadContainer thread)
        {
            string l = "QR " + (null != queue ? queue.Settings.QueueName + " : " : "") + (null != thread ? thread.Name + " : " : "") + output;
            //ApplicationSettings.getInstance().getLogger().log(l);
        }

        public static void WriteToLog(string output)
        {
            string l = "QR " + output;
            //ApplicationSettings.getInstance().getLogger().log(l);
        }

        protected static int getLineNumber()
        {
            //lock (lockObject)
            //{
            lineNumber++;
            //}
            return lineNumber;
        }
    }
}
