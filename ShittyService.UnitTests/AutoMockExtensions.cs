using System;
using System.Collections.Generic;
using System.Fabric;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extras.Moq;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Moq;

namespace ShittyService.UnitTests
{
    public static class AutoMockExtensions
    {
        public static AutoMock MockServiceContext(this AutoMock mock)
        {
            mock.Provide(new NodeContext("Node0", new NodeId(0, 1), 0, "NodeType1", "TEST.MACHINE"));
            var serviceContext = new StatefulServiceContext(
                mock.Container.Resolve<NodeContext>(),
                mock.Mock<ICodePackageActivationContext>().Object,
                "ShittyServiceType",
                new Uri("fabric:/someapp/someservice"),
                null,
                Guid.NewGuid(),
                long.MaxValue);
            mock.Provide(serviceContext);

            return mock;
        }

        public static AutoMock MockStateManagerWithDictionary<TKey, TValue>(this AutoMock mock)
            where TKey : IComparable<TKey>, IEquatable<TKey>
        {
            // Register a dictionary to hide the IReliableDictionary state in.
            mock.Provide(new Dictionary<TKey, TValue>());

            // Mock GetOrAddAsync Method
            mock.Mock<IReliableDictionary<TKey, TValue>>()
                .Setup(m => m.GetOrAddAsync(It.IsAny<ITransaction>(), It.IsAny<TKey>(), It.IsAny<TValue>()))
                .ReturnsAsync((ITransaction tx, TKey key, TValue value) =>
                {
                    var dict = mock.Container.Resolve<Dictionary<TKey, TValue>>();

                    if (dict.TryGetValue(key, out var val)) return val;

                    dict.Add(key, value);

                    return value;
                });

            // Mock ContainsKeyAsync Method
            mock.Mock<IReliableDictionary<TKey, TValue>>()
                .Setup(m => m.ContainsKeyAsync(It.IsAny<ITransaction>(), It.IsAny<TKey>()))
                .ReturnsAsync((ITransaction tx, TKey key) =>
                {
                    var dict = mock.Container.Resolve<Dictionary<TKey, TValue>>();

                    return dict.ContainsKey(key);
                });

            // Mock TryGetValueAsync Method
            mock.Mock<IReliableDictionary<TKey, TValue>>()
                .Setup(m => m.TryGetValueAsync(It.IsAny<ITransaction>(), It.IsAny<TKey>()))
                .ReturnsAsync((ITransaction tx, TKey key) =>
                {
                    var dict = mock.Container.Resolve<Dictionary<TKey, TValue>>();

                    var response = new ConditionalValue<TValue>(dict.TryGetValue(key, out var val), val);

                    return response;
                });

            // Mock AddAsync Method
            mock.Mock<IReliableDictionary<TKey, TValue>>()
                .Setup(m => m.AddAsync(It.IsAny<ITransaction>(), It.IsAny<TKey>(), It.IsAny<TValue>()))
                .Returns((ITransaction tx, TKey key, TValue value) =>
                {
                    var dict = mock.Container.Resolve<Dictionary<TKey, TValue>>();

                    if (dict.ContainsKey(key))
                        throw new ArgumentException(
                            "A value with the same key already exists in the Reliable Dictionary.");

                    dict.Add(key, value);

                    return Task.CompletedTask;
                });

            // Mock TryAddAsync Method
            mock.Mock<IReliableDictionary<TKey, TValue>>()
                .Setup(m => m.TryAddAsync(It.IsAny<ITransaction>(), It.IsAny<TKey>(), It.IsAny<TValue>()))
                .ReturnsAsync((ITransaction tx, TKey key, TValue value) =>
                {
                    var dict = mock.Container.Resolve<Dictionary<TKey, TValue>>();

                    if (dict.ContainsKey(key))
                    {
                        dict.Remove(key);
                    }

                    dict.Add(key, value);

                    return true;
                });


            mock.Mock<IReliableStateManagerReplica2>()
                .Setup(m => m.CreateTransaction())
                .Returns(mock.Mock<ITransaction>().Object);

            mock.Mock<IReliableStateManagerReplica2>()
                .Setup(m => m.GetOrAddAsync<IReliableDictionary<TKey, TValue>>(It.IsAny<string>()))
                .ReturnsAsync(mock.Mock<IReliableDictionary<TKey, TValue>>().Object);

            mock.Mock<IReliableStateManagerReplica2>()
                .Setup(m => m.GetOrAddAsync<IReliableDictionary<TKey, TValue>>(It.IsAny<ITransaction>(),
                    It.IsAny<string>()))
                .ReturnsAsync(mock.Mock<IReliableDictionary<TKey, TValue>>().Object);

            return mock;
        }
    }
}