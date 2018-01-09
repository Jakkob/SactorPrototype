using System;
using System.Collections.Generic;
using System.Fabric;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using ShittyService.Interfaces;
using ShittyService.ModelStructs;

namespace ShittyService
{
    /// <inheritdoc cref="StatefulService" />
    internal sealed class ShittyService : StatefulService, IShittyService
    {
        private const string _cardDictName = "CardDictionary";

        public ShittyService(StatefulServiceContext context)
            : base(context)
        {
        }

        public ShittyService(StatefulServiceContext context, IReliableStateManagerReplica2 stateManager)
            : base(context, stateManager)
        {
        }

        public async Task<CardModel> GetCard(Guid cardId)
        {
            CardModel cardModel = null;

            var mDict = await StateManager.GetOrAddAsync<IReliableDictionary<Guid, CardStruct>>(_cardDictName);
            using (var tx = StateManager.CreateTransaction())
            {
                var result = await mDict.TryGetValueAsync(tx, cardId);

                if (!result.HasValue)
                    return null;

                cardModel = StructToModel(result.Value);
            }

            return cardModel;
        }

        public async Task<CardModel> CreateCard()
        {
            var newCardId = Guid.NewGuid();
            CardModel cardModel = null;

            var mDict = await StateManager.GetOrAddAsync<IReliableDictionary<Guid, CardStruct>>(_cardDictName);
            using (var tx = StateManager.CreateTransaction())
            {
                while (await mDict.ContainsKeyAsync(tx, newCardId)) newCardId = Guid.NewGuid();

                var cardStruct = new CardStruct
                {
                    Id = newCardId,
                    Value = 0.0m
                };

                await mDict.AddAsync(tx, newCardId, cardStruct);

                cardModel = StructToModel(cardStruct);

                // If an exception is thrown before calling CommitAsync, the transaction aborts, all changes are 
                // discarded, and nothing is saved to the secondary replicas.
                await tx.CommitAsync();
            }

            return cardModel;
        }

        public async Task<CardModel> AddCardValue(Guid cardId, decimal value)
        {
            CardModel cardModel = null;

            var mDict = await StateManager.GetOrAddAsync<IReliableDictionary<Guid, CardStruct>>(_cardDictName);
            using (var tx = StateManager.CreateTransaction())
            {
                var result = await mDict.TryGetValueAsync(tx, cardId);

                if (!result.HasValue)
                    throw new KeyNotFoundException();

                var card = result.Value;

                card.Value += value;

                await mDict.TryAddAsync(tx, cardId, card);

                cardModel = StructToModel(card);

                await tx.CommitAsync();
            }

            return cardModel;
        }

        public async Task<CardModel> RemoveCardValue(Guid cardId, decimal value)
        {
            CardModel cardModel = null;

            var mDict = await StateManager.GetOrAddAsync<IReliableDictionary<Guid, CardStruct>>(_cardDictName);
            using (var tx = StateManager.CreateTransaction())
            {
                var result = await mDict.TryGetValueAsync(tx, cardId);

                if (!result.HasValue)
                    throw new KeyNotFoundException();

                var card = result.Value;
                var diff = card.Value - value;

                if (diff < 0.0m)
                    throw new InvalidOperationException("Negtive card balances are not allowd.");

                card.Value = diff;

                await mDict.TryAddAsync(tx, cardId, card);
                cardModel = StructToModel(card);
                await tx.CommitAsync();
            }

            return cardModel;
        }

        private static CardModel StructToModel(CardStruct @struct)
        {
            return new CardModel
            {
                Id = @struct.Id,
                Value = @struct.Value
            };
        }

        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return this.CreateServiceRemotingReplicaListeners();
        }
    }
}