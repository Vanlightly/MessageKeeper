# MessageKeeper
Store data and messages for later retries when services are not available right now.

## Store messages in the keep that cannot be sent to a messaging system or web service
```csharp
var messagePublisher = new SingleMessagePublisher();
var messageState = messagePublisher.Send<Order>("order", "new", newOrder);

if (messageState.Status == SendStatus.CouldNotEstablishConnection)
{
    messageKeeper.Keep("Order", newOrder);
}
```

## Retrieve messages from the keep to try again

If the message cannot be sent, then just Rekeep it. This will increment the StoreCount.

```csharp
var keptOrder = messageKeeper.RetrieveMessage<Order>("Order");
if(keptOrder != null)
{
  var messagePublisher = new SingleMessagePublisher();
  var messageState = messagePublisher.Send<Order>("order", "new", keptOrder.Payload);

  if (messageState.Status == SendStatus.CouldNotEstablishConnection)
  {
      messageKeeper.Rekeep("Order", keptOrder);
  }
}
```

More details to follow.
