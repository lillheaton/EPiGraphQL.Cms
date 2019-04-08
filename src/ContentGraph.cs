using EPiGraphQL.Core;
using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using GraphQL;
using GraphQL.Language.AST;
using GraphQL.Types;

namespace EPiGraphQL.Cms
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
                        Name = Constants.Arguments.ARGUMENT_ID,
                        Description = "Default to Current site StartPage"                        
                    },
                    new QueryArgument<StringGraphType>()
                    {
                        Name = Constants.Arguments.ARGUMENT_LOCALE,
                        DefaultValue = Constants.Value.DefaultLocale
                    },
                    new QueryArgument<BooleanGraphType>()
                    {
                        Name = Constants.Arguments.ARGUMENT_ALLOWFALLBACK_LANG,
                        Description = "Allow Fallback Language",
                        DefaultValue = true
                    }
                ),
                resolve: context =>
                {
                    int id = context.HasArgument(Constants.Arguments.ARGUMENT_ID)
                        ? context.GetArgument<int>(Constants.Arguments.ARGUMENT_ID) 
                        : SiteDefinition.Current.StartPage.ID; 

                    var locale = context.GetLocaleFromArgument();

                    context.Variables.Add(new Variable { Name = Constants.Arguments.ARGUMENT_LOCALE, Value = locale.Name });
                    
                    if (_contentLoader
                        .TryGet(
                            new ContentReference(id),
                            context.CreateLoaderOptionsFromAgruments(),
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
