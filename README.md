# MessageKeeper
Store data and messages for later retries when other services are not available at that moment. This is a super simple library that inserts and retrieves messages (or data classes) as JSON in a FIFO data store. Each type of message you want to store is put in a "keep".

So for example, if your code has a critical message to send, but the messaging system is unavailable, simply store the message in a keep, then next time you are sending messages, just check the keep for stored messages and retry them.

This works well with Polly. Use Polly to perform retries but if those retries get exhausted then you can either return an error to a caller if there is one or store the data in a keep to try again later. 

You can combine it with a Polly circuit breaker. If  the circuit is open, then periodically try to send messages, once the circuit is closed again, then retrieve your stored messages and send them.

## Backends
Currently I have only developed a SQL Server backend. When you store messages, you specify a keep name which correlates to a table. So if you use two keep names, you'll need two keep tables. Using the SQL Server OUTPUT statement with readpast lock (among other lock types), MessageKeeper can create a FIFO queue store. Transactions are not used in order to provide decent performance so due to the concurrent access and readpast lock, under load MessageKeeper does not offer 100% FIFO guarantee.

Just use the MessageKeeperFactory to create you a message keeper that can read and write to a database. You can host your Keep tables in your application database. All keep tables have the naming convention: (keep name)Keep. So for example: OrdersKeep.

If you use dependency injection, then where you register your interfaces and implementations create the message keeper.
```csharp
builder.Register<IMessageKeeper>().As<SqlServerMessageKeeper>(MessageKeeperFactory.GetMessageKeeper(ConnectionString));
```

Then in your classes where you need a message keeper just add the IMessageKeeper interface to your constructor.

```csharp
private IMessageKeeper _messageKeeper;

public void MyService(IMessageKeeper messageKeeper)
{
    _messageKeeper = messageKeeper;
}
```

To create a table use the following script:
```sql
CREATE TABLE [dbo].[<Keep Name>Keep](
	[MessageId] [bigint] IDENTITY(1,1) NOT NULL,
	[OriginalStoreTime] [datetimeoffset](7) NOT NULL,
	[LastStoreTime] [datetimeoffset](7) NOT NULL,
	[StoreCount] [smallint] NOT NULL,
	[Payload] [nvarchar](max) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[MessageId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
)
```

## Store messages in the keep that cannot be sent to a messaging system or web service
```csharp
var messagePublisher = new SingleMessagePublisher();
var messageState = messagePublisher.Send<Order>("order", "new", newOrder);

if (messageState.Status == SendStatus.CouldNotEstablishConnection)
{
    await _messageKeeper.KeepAsync("Orders", newOrder);
}
```

## Retrieve messages from the keep to try again
RetrieveMessage will dequeue a IStoredMessage<T> message from the queue. If no messages are on the queue then it will return null. If the message cannot be sent, then just Rekeep it. This will increment the StoreCount and set the LastStoredTime.

```csharp
var keptOrder = await _messageKeeper.RetrieveMessageAsync<Order>("Orders");
if(keptOrder != null)
{
    var messagePublisher = new SingleMessagePublisher();
    var messageState = messagePublisher.Send<Order>("order", "new", keptOrder.Payload);

    if (messageState.Status == SendStatus.CouldNotEstablishConnection)
    {
      await _messageKeeper.RekeepAsync("Orders", keptOrder);
    }
}
```

Use the OriginalStoreTime, LastStoreTime and StoreCount properties to make decisions about if you wish to retry them or do something else.

