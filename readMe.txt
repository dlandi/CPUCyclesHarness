This experiment utilizes a low level windows API: QueryThreadCycleTime.

The goal here is to use QueryThreadCycleTime to keep track of the number of CPU Cycles being using during a computation 
demonstrating how to use Delegates, Async Tasks and Cancellation Tokens to cancel an operation when a given "CPU Cycle Threshold" 
has been reached.

I used a hypothetical C# Smart Contract as a loose metaphor to test out some code.

One can see how this method could be used to meter code by cpu cycles. Something that Azure Cloud services to for things like
Azure Functions and Azure CosmosDB.


Turned out to be very interesting! Enjoy.

Dennis Landi September 2017