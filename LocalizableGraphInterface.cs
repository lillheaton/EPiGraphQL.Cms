using EPiServer.Core;
using EPiServer.ServiceLocation;
using GraphQL.Types;
using System.Linq;

namespace Eols.EPiGraphQL.Cms
{
    [ServiceConfiguration(typeof(IInterfaceGraphType), Lifecycle = ServiceInstanceScope.Singleton)]
    public class LocalizableGraphInterface : InterfaceGraphType<ILocalizable>
    {
        public LocalizableGraphInterface()
        {
            Field<StringGraphType>("Language", resolve: x => x.Source.Language.Name);
            Field<ListGraphType<StringGraphType>>("AvailableLanguage", resolve: x => x.Source.ExistingLanguages.Select(culture => culture.Name));
            Field<StringGraphType>("MasterLanguage", resolve: x => x.Source.MasterLanguage);
        }
    }
}