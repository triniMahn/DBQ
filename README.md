DBQ
===

A database queuing framework (currently only) for .NET.

Overview
--------

- This DB Queuing Framework (database portion only) is based upon the [work done by/paper written by Omar Al-Zabir](http://omaralzabir.com/building-high-performance-queue-in-database-for-storing-orders-notifications-tasks/)
- The framework's classes are, of course, found in DBQ.Framework

Design Decisions (Q & A)
----------------

- Why build your own queuing infrastructure? Why not use MSMQ, or something open-source like RabbitMQ?
	- **Fewer moving parts:** The queues can run within a web service process (provided by IIS), or a Windows Service. 
	Separate servers, installations, and individual management for any of the above is not needed. They would also require updates, and testing of updates.

	- **Persistence:** RabbitMQ (see note on message persistence: http://www.rabbitmq.com/tutorials/tutorial-two-python.html), 
	or MSMQ do not guarantee the persistence of work items submitted to the queue. By using an SQL Server DB/table as our queue structure,
	we can leverage a pre-existing infrastructure. The main advantage, especially for an OLTP, or order processing scenario,
	is that no work items are lost if the Queue server crashes, and when the Queue restarts, it can pick up where it left off.
	And, because we maintain item statuses, we can run a job that scavenges the queue for unprocessed (for whatever reason) items 
	and notify someone to do something about it.
	
- Why not use Windows Azure Service Bus Queues?
	- Correct me if I'm wrong, but they're non-persistent, and they probably cost more $$
	
- Ok, so why would you ever run the Queues in a web service, and not solely in a Windows Service? Wouldn't that be safer?
	- If we place each web service with a Queue in its own Application Pool, and the web service generates no unhandled exceptions, 
	then the Queue can run uninterrupted.
	- Even if unhandled exceptions are generated, and the application pool restarts, the Queue is first shutdown via the 
	IIS (as the queue object is registered with IIS when it is created), and is then restarted when the app pool restarts
	- The Windows Service doesn't offer much more benefit. If the machine crashes, the service crashes as well. 
	Since we have the separation of the Queue structure (SQL DB), and the consumer/Queue application, 
	we don't have to worry about crashes
	- **Deployment (Deploy changes once):** When deploying new versions of code that may have new versions of QueueItem classes, 
	etc, we don't have to stop services and deploy separately. Everything is deployed to each web server in the cluster via the 
	web app deployment process
	
- If all the Queues in each app pool write to the same queue, don't we then have a single point of failure, 
and/or a scalability bottleneck?
	- Yes, but we have ways to mitigate the risk of failure:
		- Redundancy/Failover: The Queue DB can be configured for DB mirroring
		- Bottleneck:
			- each web service could be set up to write to its own Queue, or Query Load Balancing could be used to write to 
			any number of DB Queues
			
- Queue Shut Down:
	- When stopping worker threads:
		- Give the thread 10 seconds to end gracefully; otherwise, abort it.
			- This will ensure that if item processing is negatively affecting records, the system, etc. then the thread will be prevented from processing further items as soon as possible.
			
- Default batch size:
	- Set to 1 to ensure the least number of queue items are affected by some "glitch" in processing
	
- What about performance? You're essentially polling your DB, isn't a "push" based system better? 
	- Yes, we all remember writing Assembler code in our Computer Hardware class to support polling, and interrupt handling of system events.
	And yes, the performance of this Queue probably isn't as good as a system that can notify the subscriber when a new item has arrived -- 
	on the Queue system an event is probably raised when something is added to the Queue data structure being used in C/C++.
	Listen folks, everything in computing is a trade off. We're trading less complexity, better manageability, and durability for decreased performance.
	- To narrow that performance gap, a thread "peek" throttling system is used to scale down worker threads (and their query frequency) when the queue is sparse
	
Batch Processing Scenarios
--------------------------

- **Continuous:** Example: In the case of transaction processing, orders will be queued and processed in a continuous manner
- **Command-based:** Example: In the case where a batch of items must be processed, once the batch items have been queued and processed, queue processing no longer needs to be active, and the queue terminates itself
- **Schedule-based:** A set of items must be queued and processed at a certain time. For example, scheduled batch processing must occur on a daily basis and preferably in the early hours of the day to reduce transaction processing load. 
Tech. Note: Use Timer class for implementation.

Framework (from the ground, up)
-------------------------------

**ThreadContainer**

- The "worker" thread for a specific Queue
- Wraps the standard .NET, System.Threading.Thread
- Provides properties and methods to set operational attributes, such as:
	- the rate at which items are processed (i.e. time between processing each item)
	- the rate at which batches are processed
	- etc.
- Holds a reference to the Queue for which it is processing items
- Contains the "main" method for the thread
	-calls Queue::DequeueForProcessing to obtain items for processing

**ThreadGuardian**

- In order to scale more easily, and when it's necessary to process more items more quickly, functionality has been created to allow each Queue have more than one worker thread. The collection of threads that any Queue might posses is managed/housed within the ThreadGuardian class
- This class provides a few key operations on groups of ThreadContainer objects. It can:
	- create the objects and start the threads
	- stop all the threads that are running for a specific Queue
	- check to see if all of the threads for a specific queue are running
	- re-start (but not recreate any instances) the threads if they were previously stopped
- The threads are started with a delay between each start to mitigate the risk of machine resource issues

**Queue**

- A class that houses operations and settings related to QueueItem processing queues
- Contains basic operations that should be performed on a Queue:
	- Start
	- Stop
	- Verify that the worker threads are running (via it's own ThreadGuardian instance)
	- A way to dequeue items from the DB for individual processing
	- A way to enqueue items for processing

**QueueContainer**

- Simple wrapper for Queue instance

**QueueItem**

- The actual class that contains data related a queue item that requires processing

**QueueItemProcessor**

- For use with a QueueItemProcessorController, each processor has one significant method, "process", which does something with the QueueItem instance it is passed

**QueueItemProcessorController**

- In it's most important method, "processItem", it cycles through any QueueItem pre, or post processors that may exist within it's internal collections. It also executes the "defaultProcessAction" of the QueueItem in between all of the pre/post processors
- Note that QueueItem::defaultProcessAction may not execute if a pre-processor has its HaltProcessingOnError property set to true. This is useful in the case where a pre-processor is used to validate some data (asynchronously from submission, of course) before the bulk of the processing is done on the QueueItem

**QueueItemProcessorControllerFactory**

- Class names are getting a bit out-of-hand at this point, but they're still descriptive. This class manages the creation of and caching of QueueItemProcessorController instances for use in the ThreadMain methods of the worker threads when processing queue items. The factory returns the appropriate controller based on the QueueItem type

Class Diagram
-------------

![Framework Class Diagram](https://github.com/triniMahn/DBQ/raw/master/DBQ/Doc/Framework_Class_Diagram.png)

All questions welcome. Find me here: www.arkitekt.ca


