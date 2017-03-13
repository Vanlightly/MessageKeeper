using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace MessageKeeper.SqlServerBackend.Tests
{
    [TestClass]
    public class SynchronousTests
    {
        private const string ConnectionString = "Server=(local);Database=MessageKeep;Trusted_Connection=true;";

        [TestInitialize]
        public void Initialize()
        {
            KeepHelper.WipeKeep("Orders");
        }

        [TestMethod]
        public void IfSync_IsFifoInSingleThreadedScenario()
        {
            // ARRANGE
            var messageKeeper = MessageKeeperFactory.GetMessageKeeper(ConnectionString);

            // ACT
            for (int i = 0; i < 10; i++)
            {
                var order = CreateOrder(i);
                messageKeeper.Keep("Orders", order);
            }

            // ASSERT
            for (int i = 0; i < 10; i++)
            {
                var order = messageKeeper.RetrieveMessage<Order>("Orders");
                Assert.AreEqual(order.Payload.OrderId, i);
            }
        }

        [TestMethod]
        public void IfSync_RekeepIncrementsStoreCountAndSetsLastStoreTime()
        {
            // ARRANGE
            var messageKeeper = MessageKeeperFactory.GetMessageKeeper(ConnectionString);

            // ACT
            var order = CreateOrder();
            messageKeeper.Keep("Orders", order);
            var storedOrder = messageKeeper.RetrieveMessage<Order>("Orders");
            var storeCount1 = storedOrder.StoreCount;
            var lastStoreTime1 = storedOrder.LastStoreTime;

            Task.Delay(100);
            messageKeeper.Rekeep("Orders", storedOrder);
            storedOrder = messageKeeper.RetrieveMessage<Order>("Orders");
            var storeCount2 = storedOrder.StoreCount;
            var lastStoreTime2 = storedOrder.LastStoreTime;

            // ASSERT
            Assert.AreEqual(storeCount1 + 1, storeCount2);
            Assert.IsTrue(lastStoreTime2 > lastStoreTime1);
        }

        [TestMethod]
        public void IfSync_MultipleConcurrentOperations_NoDuplicates()
        {
            // ARRANGE
            var items = new ConcurrentDictionary<string, int>();
            var tasks = new List<Task>();
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(Task.Run(() => KeepRetrieveRekeepLoop(10, items)));
            }

            // ACT
            Task.WaitAll(tasks.ToArray());

            // ASSERT
            foreach (var key in items.Keys)
                Assert.AreEqual(1, items[key]);
        }

        private void KeepRetrieveRekeepLoop(int iterations, ConcurrentDictionary<string, int> items)
        {
            var messageKeeper = MessageKeeperFactory.GetMessageKeeper(ConnectionString);

            // create a number of different orders concurrently (with other running tasks)
            for (int i = 0; i < iterations; i++)
            {
                var order = CreateOrder();
                messageKeeper.Keep("Orders", order);
            }

            // then retrieve and rekeep from all running tasks concurrently
            for (int i = 0; i < iterations; i++)
            {
                var storedOrder = messageKeeper.RetrieveMessage<Order>("Orders");

                if (storedOrder != null)
                {
                    var key = storedOrder.Payload.OrderId + ":" + storedOrder.StoreCount;
                    if (items.ContainsKey(key))
                        items[key]++;
                    else
                        items.TryAdd(key, 1);
                    messageKeeper.Rekeep("Orders", storedOrder);
                }
            }

            // finally Retrieve all the orders to empty the keep
            IStoredMessage<Order> storedMessage = messageKeeper.RetrieveMessage<Order>("Orders");
            while (storedMessage != null)
            {
                var key = storedMessage.Payload.OrderId + ":" + storedMessage.StoreCount;
                if (items.ContainsKey(key))
                    items[key]++;
                else
                    items.TryAdd(key, 1);

                storedMessage = messageKeeper.RetrieveMessage<Order>("Orders");
            }
        }

        private Order CreateOrder()
        {
            var rand = new Random(Guid.NewGuid().GetHashCode());
            var order = new Order()
            {
                OrderId = rand.Next(100000),
                ClientId = rand.Next(1000000),
                OfferCode = "badgers",
                ProductCode = "HGDHGDF",
                Quantity = 10,
                UnitPrice = 9.99M
            };

            return order;
        }

        private Order CreateOrder(int orderId)
        {
            var rand = new Random(Guid.NewGuid().GetHashCode());
            var order = new Order()
            {
                OrderId = orderId,
                ClientId = rand.Next(1000000),
                OfferCode = "badgers",
                ProductCode = "HGDHGDF",
                Quantity = 10,
                UnitPrice = 9.99M
            };

            return order;
        }
    }
}
