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

