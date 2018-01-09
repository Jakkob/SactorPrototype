using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extras.Moq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShittyService.ModelStructs;

namespace ShittyService.UnitTests
{
    [TestClass]
    public class ShittyServiceTests
    {
        private Dictionary<Guid, CardStruct> _cardDict;
        private AutoMock _mock;
        private ShittyService _service;

        [TestInitialize]
        public void Initialize()
        {
            _mock = AutoMock.GetLoose();

            _mock.MockStateManagerWithDictionary<Guid, CardStruct>();
            _mock.MockServiceContext();

            _cardDict = _mock.Container.Resolve<Dictionary<Guid, CardStruct>>();

            _service = _mock.Create<ShittyService>();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _mock.Dispose();
        }

        private Guid AddNewCard(Guid? id = null, decimal? value = null)
        {
            var card = new CardStruct
            {
                Id = id ?? Guid.NewGuid(),
                Value = value ?? 0.0m
            };
            _cardDict.Add(card.Id, card);

            return card.Id;
        }

        [TestMethod]
        public async Task CreateCard()
        {
            var cardData = await _service.CreateCard();

            Assert.IsTrue(_cardDict.ContainsKey(cardData.Id));
        }

        [TestMethod]
        public async Task GetCard_Success()
        {
            var cardId = AddNewCard();

            var result = await _service.GetCard(cardId);

            Assert.AreEqual(cardId, result.Id);
        }

        [TestMethod]
        public async Task GetCard_ReturnsNull()
        {
            var result = await _service.GetCard(Guid.NewGuid());

            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task AddCardValue_Success()
        {
            var cardId = AddNewCard();
            var result = await _service.AddCardValue(cardId, 15.5m);

            _cardDict.TryGetValue(cardId, out var freshCard);

            Assert.AreEqual(15.5m, freshCard.Value);

            Assert.AreEqual(15.5m, result.Value);
        }

        [TestMethod]
        public async Task RemoveCardValue_Success()
        {
            var cardId = AddNewCard(value: 15.5m);
            var result = await _service.RemoveCardValue(cardId, 15.0m);

            _cardDict.TryGetValue(cardId, out var freshCard);

            Assert.AreEqual(0.5m, freshCard.Value);

            Assert.AreEqual(0.5m, result.Value);
        }
    }
}