using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.FabricTransport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[assembly: FabricTransportServiceRemotingProvider(RemotingListener = RemotingListener.V2Listener, RemotingClient = RemotingClient.V2Client)]
namespace ShittyService.Interfaces
{
    public interface IShittyService : IService
    {
        Task<CardModel> CreateCard();

        Task<CardModel> GetCard(Guid cardId);

        Task<CardModel> AddCardValue(Guid cardId, decimal value);

        Task<CardModel> RemoveCardValue(Guid cardId, decimal value);
    }
}
