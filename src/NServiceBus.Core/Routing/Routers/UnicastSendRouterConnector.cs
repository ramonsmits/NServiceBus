namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;
    using Routing;
    using Unicast.Queuing;

    class UnicastSendRouterConnector : StageConnector<IOutgoingSendContext, IOutgoingLogicalMessageContext>
    {
        public enum RouteOption
        {
            None,
            ExplicitDestination,
            RouteToThisInstance,
            RouteToAnyInstanceOfThisEndpoint,
            RouteToSpecificInstance
        }

        public UnicastSendRouterConnector(
            string sharedQueue,
            string instanceSpecificQueue,
            IUnicastSendRouter unicastSendRouter,
            DistributionPolicy distributionPolicy,
            Func<EndpointInstance, string> transportAddressTranslation)
        {
            this.sharedQueue = sharedQueue;
            this.instanceSpecificQueue = instanceSpecificQueue;
            this.unicastSendRouter = unicastSendRouter;
            defaultDistributionPolicy = distributionPolicy;
            this.transportAddressTranslation = transportAddressTranslation;
        }

        public override async Task Invoke(IOutgoingSendContext context, Func<IOutgoingLogicalMessageContext, Task> stage)
        {
            var messageType = context.Message.MessageType;

            var state = context.Extensions.GetOrCreate<State>();

            if (state.Option == RouteOption.RouteToThisInstance && instanceSpecificQueue == null)
            {
                throw new InvalidOperationException("Cannot route to a specific instance because an endpoint instance discriminator was not configured for the destination endpoint. It can be specified via EndpointConfiguration.MakeInstanceUniquelyAddressable(string discriminator).");
            }
            var thisEndpoint = state.Option == RouteOption.RouteToAnyInstanceOfThisEndpoint ? sharedQueue : null;
            var thisInstance = state.Option == RouteOption.RouteToThisInstance ? instanceSpecificQueue : null;
            var explicitDestination = state.Option == RouteOption.ExplicitDestination ? state.ExplicitDestination : null;
            var destination = explicitDestination ?? thisInstance ?? thisEndpoint;

            var distributionPolicy = state.Option == RouteOption.RouteToSpecificInstance ? new SpecificInstanceDistributionPolicy(state.SpecificInstance, transportAddressTranslation) : defaultDistributionPolicy;

            var routingStrategy = string.IsNullOrEmpty(destination)
                ? unicastSendRouter.Route(messageType, distributionPolicy)
                : RouteToDestination(destination);

            if (routingStrategy == null)
            {
                throw new Exception($"No destination specified for message: {messageType}");
            }

            context.Headers[Headers.MessageIntent] = MessageIntentEnum.Send.ToString();

            var logicalMessageContext = this.CreateOutgoingLogicalMessageContext(
                context.Message,
                new[]
                {
                    routingStrategy
                },
                context);

            try
            {
                await stage(logicalMessageContext).ConfigureAwait(false);
            }
            catch (QueueNotFoundException ex)
            {
                throw new Exception($"The destination queue '{ex.Queue}' could not be found. The destination may be misconfigured for this kind of message ({messageType}) in the MessageEndpointMappings of the UnicastBusConfig section in the configuration file. It may also be the case that the given queue hasn't been created yet, or has been deleted.", ex);
            }
        }

        static UnicastRoutingStrategy RouteToDestination(string physicalAddress)
        {
            return new UnicastRoutingStrategy(physicalAddress);
        }

        IDistributionPolicy defaultDistributionPolicy;
        Func<EndpointInstance, string> transportAddressTranslation;
        string instanceSpecificQueue;
        string sharedQueue;
        IUnicastSendRouter unicastSendRouter;

        public class State
        {
            public string ExplicitDestination { get; set; }
            public string SpecificInstance { get; set; }

            public RouteOption Option
            {
                get { return option; }
                set
                {
                    if (option != RouteOption.None)
                    {
                        throw new Exception("Already specified routing option for this message: " + option);
                    }
                    option = value;
                }
            }

            RouteOption option;
        }
    }
}