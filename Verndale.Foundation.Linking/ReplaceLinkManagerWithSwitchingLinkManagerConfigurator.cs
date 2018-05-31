using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sitecore.Abstractions;
using Sitecore.DependencyInjection;

namespace Verndale.Foundation.Linking
{
	public class ReplaceLinkManagerWithSwitchingLinkManagerConfigurator : IServicesConfigurator
	{
		public void Configure(IServiceCollection serviceCollection)
		{
			var service = new ServiceDescriptor(typeof(BaseLinkManager), typeof(SwitchingLinkManager), ServiceLifetime.Singleton);
			serviceCollection.Replace(service);
		}
	}
}
