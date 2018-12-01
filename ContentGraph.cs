using Eols.EPiGraphQL.Core;
using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using GraphQL;
using GraphQL.Language.AST;
using GraphQL.Types;

namespace Eols.EPiGraphQL.Cms
{
    [ServiceConfiguration(typeof(IEPiServerGraph), Lifecycle = ServiceInstanceScope.Singleton)]
    public class ContentGraph : ObjectGraphType, IEPiServerGraph
    {
        private readonly IContentLoader _contentLoader;

        public ContentGraph(IContentLoader contentLoader)
        {
            _contentLoader = contentLoader;

            Name = "Content";

            Field<ContentGraphInterface>(
                "Item",
                "Get content by ContentReferenceID, default => \"current site\" start page",
                arguments: new QueryArguments(
                    new QueryArgument<IntGraphType>()
                    {
                        Name = "id",
                        DefaultValue = SiteDefinition.Current.StartPage.ID
                    },
                    new QueryArgument<StringGraphType>()
                    {
                        Name = "locale",
                        DefaultValue = Constants.Value.DefaultLocale
                    }
                ),
                resolve: context =>
                {
                    int id = context.GetArgument<int>("id");
                    var locale = context.GetLocaleFromArgument();

                    context.Variables.Add(new Variable { Name = "locale", Value = locale.Name });

                    if (_contentLoader
                        .TryGet<IContent>(
                            new ContentReference(id),
                            locale,
                            out IContent result))
                    {
                        return result;
                    }
                    else
                    {
                        context.Errors.Add(new ExecutionError($"Could not find content with id {id}"));
                        return null;
                    }
                }
            );
        }
    }
}
