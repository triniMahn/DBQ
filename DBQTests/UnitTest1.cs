using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DBQ.Framework;
using System.Configuration;
using System.Diagnostics;

namespace DBQTests
{
    [TestClass]
    public class ConfigurationTests
    {
        [TestMethod]
        public void ConfigurationLoadTest()
        {
            QueueConfigurationSection config = (QueueConfigurationSection)System.Configuration.ConfigurationManager.GetSection("DBQueueConfig");

            QueueCollection qs = config.Queues;

            foreach (QueueElement q in qs)
            {
                Debug.WriteLine("Queue Name: " + q.QueueName);
            }
        }
    }
}
