using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.TestHost;
using HotChocolate.AspNetCore.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;
using HotChocolate.AspNetCore.Serialization;
using Microsoft.AspNetCore.Builder;
using HotChocolate.AspNetCore.Extensions;
using HotChocolate.Types;
using HotChocolate.Resolvers;
using System.Threading;
using System.Linq;

namespace HotChocolate.AspNetCore
{
    public class HttpPostMiddlewareTests : ServerTestBase
    {
        public HttpPostMiddlewareTests(TestServerFactory serverFactory)
            : base(serverFactory)
        {
        }

        [Fact]
        public async Task Simple_IsAlive_Test()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            ClientQueryResult result = await server.PostAsync(
                new ClientQueryRequest { Query = "{ __typename }" });

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task Serialize_Payload_With_Whitespaces()
        {
            // arrange
            TestServer server = CreateStarWarsServer(
                configureServices: sc => sc.AddHttpResultSerializer(indented: true));

            // act
            ClientRawResult result = await server.PostRawAsync(
                new ClientQueryRequest { Query = "{ __typename }" });

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task Serialize_Payload_Without_Extra_Whitespaces()
        {
            // arrange
            TestServer server = CreateStarWarsServer(
                configureServices: sc => sc.AddHttpResultSerializer(indented: false));

            // act
            ClientRawResult result = await server.PostRawAsync(
                new ClientQueryRequest { Query = "{ __typename }" });

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task Simple_IsAlive_Test_On_Non_GraphQL_Path()
        {
            // arrange
            TestServer server = CreateStarWarsServer("/foo");

            // act
            ClientQueryResult result = await server.PostAsync(
                    new ClientQueryRequest { Query = "{ __typename }" },
                    "/foo");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task SingleRequest_GetHeroName()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            ClientQueryResult result =
                await server.PostAsync(new ClientQueryRequest
                {
                    Query = @"
                    {
                        hero {
                            name
                        }
                    }"
                });

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task SingleRequest_GetHeroName_Casing_Is_Preserved()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            ClientQueryResult result =
                await server.PostAsync(new ClientQueryRequest
                {
                    Query = @"
                    {
                        HERO: hero {
                            name
                        }
                    }"
                });

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task SingleRequest_GetHeroName_With_EnumVariable()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            ClientQueryResult result =
                await server.PostAsync(new ClientQueryRequest
                {
                    Query = @"
                    query ($episode: Episode!) {
                        hero(episode: $episode) {
                            name
                        }
                    }",
                    Variables = new Dictionary<string, object>
                    {
                        { "episode", "NEW_HOPE" }
                    }
                });

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task SingleRequest_GetHumanName_With_StringVariable()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            ClientQueryResult result =
                await server.PostAsync(new ClientQueryRequest
                {
                    Query = @"
                    query h($id: String!) {
                        human(id: $id) {
                            name
                        }
                    }",
                    Variables = new Dictionary<string, object>
                    {
                        { "id", "1000" }
                    }
                });

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task SingleRequest_Defer_Results()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            ClientRawResult result =
                await server.PostRawAsync(new ClientQueryRequest
                {
                    Query = @"
                    {
                        hero(episode: NEW_HOPE)
                        {
                            name
                            ... on Droid @defer(label: ""my_id"")
                            {
                                id
                            }
                        }
                    }"
                });

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task SingleRequest_CreateReviewForEpisode_With_ObjectVariable()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            ClientQueryResult result =
                await server.PostAsync(new ClientQueryRequest
                {
                    Query = @"
                    mutation CreateReviewForEpisode(
                        $ep: Episode!
                        $review: ReviewInput!) {
                        createReview(episode: $ep, review: $review) {
                            stars
                            commentary
                        }
                    }",
                    Variables = new Dictionary<string, object>
                    {
                        { "ep", "EMPIRE" },
                        {
                            "review",
                            new Dictionary<string, object>
                            {
                                { "stars", 5 },
                                { "commentary", "This is a great movie!" },
                            }
                        }
                    }
                });

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task SingleRequest_CreateReviewForEpisode_Omit_NonNull_Variable()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            ClientQueryResult result =
                await server.PostAsync(new ClientQueryRequest
                {
                    Query = @"
                    mutation CreateReviewForEpisode(
                        $ep: Episode!
                        $review: ReviewInput!) {
                        createReview(episode: $ep, review: $review) {
                            stars
                            commentary
                        }
                    }",
                    Variables = new Dictionary<string, object>
                    {
                        {
                            "review",
                            new Dictionary<string, object>
                            {
                                { "stars", 5 },
                                { "commentary", "This is a great movie!" },
                            }
                        }
                    }
                });

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task SingleRequest_CreateReviewForEpisode_Variables_In_ObjectValue()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            ClientQueryResult result =
                await server.PostAsync(new ClientQueryRequest
                {
                    Query = @"
                    mutation CreateReviewForEpisode(
                        $ep: Episode!
                        $stars: Int!
                        $commentary: String!) {
                        createReview(episode: $ep, review: {
                            stars: $stars
                            commentary: $commentary
                        } ) {
                            stars
                            commentary
                        }
                    }",
                    Variables = new Dictionary<string, object>
                    {
                        { "ep", "EMPIRE" },
                        { "stars", 5 },
                        { "commentary", "This is a great movie!" }
                    }
                });

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task SingleRequest_CreateReviewForEpisode_Variables_Unused()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            ClientQueryResult result =
                await server.PostAsync(new ClientQueryRequest
                {
                    Query = @"
                    mutation CreateReviewForEpisode(
                        $ep: Episode!
                        $stars: Int!
                        $commentary: String!
                        $foo: Float) {
                        createReview(episode: $ep, review: {
                            stars: $stars
                            commentary: $commentary
                        } ) {
                            stars
                            commentary
                        }
                    }",
                    Variables = new Dictionary<string, object>
                    {
                        { "ep", "EMPIRE" },
                        { "stars", 5 },
                        { "commentary", "This is a great movie!" }
                    }
                });

            // assert
            result.MatchSnapshot();
        }

        [InlineData("a")]
        [InlineData("b")]
        [Theory]
        public async Task SingleRequest_Execute_Specific_Operation(
            string operationName)
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            ClientQueryResult result =
                await server.PostAsync(new ClientQueryRequest
                {
                    Query = @"
                    query a {
                        a: hero {
                            name
                        }
                    }

                    query b {
                        b: hero {
                            name
                        }
                    }",
                    OperationName = operationName
                });

            // assert
            result.MatchSnapshot(new SnapshotNameExtension(operationName));
        }

        [Fact]
        public async Task SingleRequest_ValidationError()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            ClientQueryResult result =
                await server.PostAsync(new ClientQueryRequest
                {
                    Query = @"
                    {
                        hero(episode: $episode) {
                            name
                        }
                    }",
                    Variables = new Dictionary<string, object>
                    {
                        { "episode", "NEW_HOPE" }
                    }
                });

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task SingleRequest_SyntaxError()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            ClientQueryResult result =
                await server.PostAsync(new ClientQueryRequest
                {
                    Query = @"
                    {
                        ähero {
                            name
                        }
                    }"
                });

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task SingleRequest_Double_Variable()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            ClientQueryResult result =
                await server.PostAsync(new ClientQueryRequest
                {
                    Query = @"
                        query ($d: Float) {
                             double_arg(d: $d)
                        }",
                    Variables = new Dictionary<string, object> { { "d", 1.539 } }
                },
                "/arguments");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task SingleRequest_Double_Max_Variable()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            ClientQueryResult result =
                await server.PostAsync(new ClientQueryRequest
                {
                    Query = @"
                        query ($d: Float) {
                             double_arg(d: $d)
                        }",
                    Variables = new Dictionary<string, object> { { "d", double.MaxValue } }
                },
                "/arguments");

            // assert
            new
            {
                double.MaxValue,
                result
            }.MatchSnapshot();
        }

        [Fact]
        public async Task SingleRequest_Double_Min_Variable()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            ClientQueryResult result =
                await server.PostAsync(new ClientQueryRequest
                {
                    Query = @"
                        query ($d: Float) {
                             double_arg(d: $d)
                        }",
                    Variables = new Dictionary<string, object> { { "d", double.MinValue } }
                },
                "/arguments");

            // assert
            new
            {
                double.MinValue,
                result
            }.MatchSnapshot();
        }

        [Fact]
        public async Task SingleRequest_Decimal_Max_Variable()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            ClientQueryResult result =
                await server.PostAsync(new ClientQueryRequest
                {
                    Query = @"
                        query ($d: Decimal) {
                             decimal_arg(d: $d)
                        }",
                    Variables = new Dictionary<string, object> { { "d", decimal.MaxValue } }
                },
                "/arguments");

            // assert
            new
            {
                decimal.MaxValue,
                result
            }.MatchSnapshot();
        }

        [Fact]
        public async Task SingleRequest_Decimal_Min_Variable()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            ClientQueryResult result =
                await server.PostAsync(new ClientQueryRequest
                {
                    Query = @"
                        query ($d: Decimal) {
                             decimal_arg(d: $d)
                        }",
                    Variables = new Dictionary<string, object> { { "d", decimal.MinValue } }
                },
                "/arguments");

            // assert
            new
            {
                decimal.MinValue,
                result
            }.MatchSnapshot();
        }

        [Fact]
        public async Task SingleRequest_Incomplete()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            ClientQueryResult result = await server.PostAsync("{ \"query\":    ");

            // assert
            result.MatchSnapshot();
        }

        [InlineData("{}", 1)]
        [InlineData("{ }", 2)]
        [InlineData("{\n}", 3)]
        [InlineData("{\r\n}", 4)]
        [Theory]
        public async Task SingleRequest_Empty(string request, int id)
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            ClientQueryResult result = await server.PostAsync(request);

            // assert
            result.MatchSnapshot(new SnapshotNameExtension(id.ToString()));
        }

        [InlineData("[]", 1)]
        [InlineData("[ ]", 2)]
        [InlineData("[\n]", 3)]
        [InlineData("[\r\n]", 4)]
        [Theory]
        public async Task BatchRequest_Empty(string request, int id)
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            ClientQueryResult result = await server.PostAsync(request);

            // assert
            result.MatchSnapshot(new SnapshotNameExtension(id.ToString()));
        }

        [Fact]
        public async Task EmptyRequest()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            ClientQueryResult result = await server.PostAsync(string.Empty);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task Ensure_Middleware_Mapping()
        {
            // arrange
            TestServer server = CreateStarWarsServer("/foo");

            // act
            ClientQueryResult result = await server.PostAsync(string.Empty);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task BatchRequest_GetHero_And_GetHuman()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            IReadOnlyList<ClientQueryResult> result =
                await server.PostAsync(new List<ClientQueryRequest>
                {
                    new ClientQueryRequest
                    {
                        Query = @"
                            query getHero {
                                hero(episode: EMPIRE) {
                                    id @export
                                }
                            }"
                    },
                    new ClientQueryRequest
                    {
                        Query = @"
                            query getHuman {
                                human(id: $id) {
                                    name
                                }
                            }"
                    }
                });

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task OperationBatchRequest_GetHero_And_GetHuman()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            IReadOnlyList<ClientQueryResult> result =
                await server.PostOperationAsync(
                    new ClientQueryRequest
                    {
                        Query =
                            @"query getHero {
                                hero(episode: EMPIRE) {
                                    id @export
                                }
                            }

                            query getHuman {
                                human(id: $id) {
                                    name
                                }
                            }"
                    },
                    "getHero, getHuman");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task OperationBatchRequest_Invalid_BatchingParameter_1()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            IReadOnlyList<ClientQueryResult> result =
                await server.PostOperationAsync(
                    new ClientQueryRequest
                    {
                        Query =
                            @"
                        query getHero {
                            hero(episode: EMPIRE) {
                                id @export
                            }
                        }

                        query getHuman {
                            human(id: $id) {
                                name
                            }
                        }"
                    },
                    "getHero",
                    createOperationParameter: s => "batchOperations=" + s);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task OperationBatchRequest_Invalid_BatchingParameter_2()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            IReadOnlyList<ClientQueryResult> result =
                await server.PostOperationAsync(
                    new ClientQueryRequest
                    {
                        Query = @"
                            query getHero {
                                hero(episode: EMPIRE) {
                                    id @export
                                }
                            }

                            query getHuman {
                                human(id: $id) {
                                    name
                                }
                            }"
                    },
                    "getHero, getHuman",
                    createOperationParameter: s => "batchOperations=[" + s);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task OperationBatchRequest_Invalid_BatchingParameter_3()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            IReadOnlyList<ClientQueryResult> result =
                await server.PostOperationAsync(
                    new ClientQueryRequest
                    {
                        Query = @"
                            query getHero {
                                hero(episode: EMPIRE) {
                                    id @export
                                }
                            }

                            query getHuman {
                                human(id: $id) {
                                    name
                                }
                            }"
                    },
                    "getHero, getHuman",
                    createOperationParameter: s => "batchOperations=" + s);

            // assert
            result.MatchSnapshot();
        }


        private async Task OperationBatchRequest_AllowParallelExecution(bool allow)
        {
            Snapshot.FullName();

            // arrange
            int batchCount = 0;
            TestServer server = ServerFactory.Create(
                services => services
                    .AddRouting()
                    .AddHttpResultSerializer(HttpResultSerialization.JsonArray)
                    .AddGraphQLServer()
                        .AddQueryType(d => d.Name("Query")
                        .Field("foo")
                        .Argument("bar", a => a.Type<StringType>())
                        .Type<StringType>()
                        .Resolve(async c =>
                        {
                            var bar = c.ArgumentValue<string>("bar");
                            return await c.BatchDataLoader<string, string>((keys, ctxToken) =>
                            {
                                Interlocked.Increment(ref batchCount);
                                return Task.FromResult(keys.ToDictionary(x => x, x => $"{x}-{batchCount}") as IReadOnlyDictionary<string, string>);
                            }, "foo").LoadAsync(bar, CancellationToken.None);
                        }))
                    .AddExportDirectiveType()
                ,
                app => app
                    .UseRouting()
                    .UseEndpoints(endpoints =>
                    {
                        GraphQLEndpointConventionBuilder builder = endpoints.MapGraphQL("/graphql");
                        builder.WithOptions(new GraphQLServerOptions { AllowParallelBatchExecution = allow });
                    })
                );


            // act
            IReadOnlyList<ClientQueryResult> result =
                await server.PostOperationAsync(
                    new ClientQueryRequest
                    {
                        Query =
                            @"query q1 {
                                f1: foo(bar: ""A""),
                                f2: foo(bar: ""B"")
                            }

                            query q2 {
                                f3: foo(bar: ""A""),
                                f4: foo(bar: ""C"")
                            }"
                    },
                    "q1, q2");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public Task OperationBatchRequest_AllowParallelExecution_Off()
        {
            return OperationBatchRequest_AllowParallelExecution(false);
        }

        [Fact]
        public Task OperationBatchRequest_AllowParallelExecution_On()
        {
            return OperationBatchRequest_AllowParallelExecution(true);
        }
    }
}