// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.BotBuilderSamples
{
    public class QnABot : ActivityHandler
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<QnABot> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public QnABot(IConfiguration configuration, ILogger<QnABot> logger, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var httpClient = _httpClientFactory.CreateClient();

            var qnaMaker = new QnAMaker(new QnAMakerEndpoint
            {
                KnowledgeBaseId = _configuration["QnAKnowledgebaseId"],
                EndpointKey = _configuration["QnAAuthKey"],
                Host = GetHostname()
            },
            null,
            httpClient);

            _logger.LogInformation("Calling QnA Maker");

            // The actual call to the QnA Maker service.
            // The response will contain all of the questions that provide the response.
            var response = await qnaMaker.GetAnswersAsync(turnContext, new QnAMakerOptions { Top = 3 });
            if (response != null && response.Length > 0)
            {
                // If multiple responses are received, the first element will be the most confident.

                // Append lack of confidence message if applicable.
                if (response[0].Score < 0.5)
                {
                    // Answer may not apply at all
                    await turnContext.SendActivityAsync(MessageFactory.Text("I couldn't find a good answer to your question. Here's the closest match:"), cancellationToken);
                }
                else if (response[0].Score < 0.7)
                {
                    // Express some uncertainty in the result
                    await turnContext.SendActivityAsync(MessageFactory.Text("This seems related to what you asked:"), cancellationToken);
                }

                await turnContext.SendActivityAsync(MessageFactory.Text($"Question: {response[0].Questions[0]}" + System.Environment.NewLine + $"Answer: {response[0].Answer}"), cancellationToken);

                // Handle the additional responses:
                if (response.Length > 1)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text("I also found these topics:"), cancellationToken);
                    for (int i = 1; i < response.Length; i++)
                    {
                        await turnContext.SendActivityAsync(MessageFactory.Text($"Question: {response[i].Questions[0]}" + System.Environment.NewLine + $"Answer: {response[i].Answer}"), cancellationToken);

                    }
                }
            }
            else
            {
                await turnContext.SendActivityAsync(MessageFactory.Text("No QnA Maker answers were found."), cancellationToken);
            }

            // TODOs: 
            // - better formatting of responses
        }

        private string GetHostname()
        {
            var hostname = _configuration["QnAEndpointHostName"];
            if (!hostname.StartsWith("https://"))
            {
                hostname = string.Concat("https://", hostname);
            }

            if (!hostname.EndsWith("/qnamaker"))
            {
                hostname = string.Concat(hostname, "/qnamaker");
            }

            return hostname;
        }
    }
}
