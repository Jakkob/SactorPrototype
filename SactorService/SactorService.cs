using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Client;
using SactorService.Interfaces;

namespace SactorService
{
    /// <remarks>
    ///     This class represents an actor.
    ///     Every ActorID maps to an instance of this class.
    ///     The StatePersistence attribute determines persistence and replication of actor state:
    ///     - Persisted: State is written to disk and replicated.
    ///     - Volatile: State is kept in memory only and replicated.
    ///     - None: State is kept in memory only and not replicated.
    /// </remarks>
    [StatePersistence(StatePersistence.Persisted)]
    internal class SactorService : Actor, ISactorService
    {
        private FabricTransportServiceRemotingClientFactory _clientFactory;
        private const string TransactionStatusKey = @"TransactionStatus";

        /// <summary>
        ///     Initializes a new instance of SactorService
        /// </summary>
        /// <param name="actorService">The Microsoft.ServiceFabric.Actors.Runtime.ActorService that will host this actor instance.</param>
        /// <param name="actorId">The Microsoft.ServiceFabric.Actors.ActorId for this actor instance.</param>
        public SactorService(ActorService actorService, ActorId actorId, FabricTransportServiceRemotingClientFactory clientFactory)
            : base(actorService, actorId)
        {
            _clientFactory = clientFactory;
        }

        /// <summary>
        ///     TODO: Replace with your own actor method.
        /// </summary>
        /// <returns></returns>
        Task<int> ISactorService.GetCountAsync(CancellationToken cancellationToken)
        {
            return StateManager.GetStateAsync<int>("count", cancellationToken);
        }

        /// <summary>
        ///     TODO: Replace with your own actor method.
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        Task ISactorService.SetCountAsync(int count, CancellationToken cancellationToken)
        {
            // Requests are not guaranteed to be processed in order nor at most once.
            // The update function here verifies that the incoming count is greater than the current count to preserve order.
            return StateManager.AddOrUpdateStateAsync("count", count, (key, value) => count > value ? count : value,
                cancellationToken);
        }

        private void InitializeActorState()
        {

        }

        /// <summary>
        ///     This method is called whenever an actor is activated.
        ///     An actor is activated the first time any of its methods are invoked.
        /// </summary>
        protected override async Task OnActivateAsync()
        {
            ActorEventSource.Current.ActorMessage(this, "Actor activated.");

            var previouslyActivated = await StateManager.ContainsStateAsync(TransactionStatusKey);

            if (!previouslyActivated)
                InitializeActorState();
        }
    }
}