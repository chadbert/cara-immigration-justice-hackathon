// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QnAPrompting.Helpers;
using QnAPrompting.Models;

namespace Microsoft.BotBuilderSamples
{
    public class QnABot : ActivityHandler
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<QnABot> _logger;
        private readonly IQnAService _qnaService;

        public QnABot(IConfiguration configuration, ILogger<QnABot> logger, IQnAService qnaService)
        {
            _configuration = configuration;
            _logger = logger;
            _qnaService = qnaService;
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var memberList = new List<string>();

            foreach (var member in membersAdded)
            {
                memberList.Add(member.Name);
            }

            await turnContext.SendActivityAsync(MessageFactory.Text($"Hi {String.Join(", ", memberList)}"), cancellationToken);
            await turnContext.SendActivityAsync(MessageFactory.Text("Please ask me a question. For example: 'What is a CFI prep?'"), cancellationToken);
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            // TODO: bot name is esperansa
            // TODOs: 
            // - better formatting of responses
            // - Typing "Hello" provides multiple greetings.

            _logger.LogInformation("Calling QnA Maker");

            // make the query
            var response = await _qnaService.QueryQnAServiceAsync(turnContext.Activity.Text, null);
            // score, context, questions

            if (response != null && response.Length > 0)
            {
                // The first response will always be the most confident, so we use that as the main response
                var firstResponse = response.First();

                // if no answer could be found that meets the threshold, we'll get a response with score 0
                if (firstResponse.Score == 0)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text("Sorry, I couldn't find an answer to that question. Please try rephrasing the question."), cancellationToken);
                }
                else if (firstResponse.Score < 20)
                {
                    // Express some uncertainty in the result
                    // Add two new lines for markdown
                    await turnContext.SendActivityAsync(MessageFactory.Text("Did you mean to ask about:"), cancellationToken);

                    string message = string.Empty;

                    for (int i = 0; i < response.Length; i++)
                    {
                        message += CreateQuestionAndAnswerString(response[i].Questions[0], response[i].Answer);
                    }

                    var messageActivity = MessageFactory.Text(message);
                    messageActivity.TextFormat = "markdown";

                    await turnContext.SendActivityAsync(messageActivity, cancellationToken);
                }
                else
                {
                    // If the first response contains prompts, then we will show that experience. Otherwise, show additional responses as "related topics"
                    var answer = firstResponse.Answer;
                    var prompts = firstResponse.Context?.Prompts;

                    if (prompts != null && prompts.Length > 0)
                    {
                        // send the text as text message because teams doesn't support markdown in cards
                        await turnContext.SendActivityAsync(MessageFactory.Text(answer), cancellationToken);
                        await turnContext.SendActivityAsync(CardHelper.GetHeroCard(prompts), cancellationToken);
                    }
                    else
                    {
                        var messageActivity = MessageFactory.Text(firstResponse.Answer);
                        messageActivity.TextFormat = "markdown";

                        await turnContext.SendActivityAsync(messageActivity, cancellationToken);
                    }
                }
            }
            else
            {
                await turnContext.SendActivityAsync(MessageFactory.Text("Sorry, I couldn't find an answer to that question. Please try rephrasing the question."), cancellationToken);
            }
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

        private string CreateQuestionAndAnswerString(string question, string answer)
        {
            return $"**Question**:{System.Environment.NewLine}{System.Environment.NewLine}{question}{System.Environment.NewLine}{System.Environment.NewLine}**Answer**:{System.Environment.NewLine}{System.Environment.NewLine}{answer}{System.Environment.NewLine}{System.Environment.NewLine}";
        }
    }

    public class CardHelper
    {
        /// <summary>
        /// Get Hero card
        /// </summary>
        /// <param name="cardText">Text for the card</param>
        /// <param name="prompts">List of suggested prompts</param>
        /// <returns>Message activity</returns>
        public static Activity GetHeroCard(QnAPrompts[] prompts)
        {
            var chatActivity = Activity.CreateMessageActivity();
            var buttons = new List<CardAction>();

            var sortedPrompts = prompts.OrderBy(r => r.DisplayOrder);
            foreach (var prompt in sortedPrompts)
            {
                buttons.Add(
                    new CardAction()
                    {
                        Value = prompt.DisplayText,
                        Type = ActionTypes.ImBack,
                        Title = prompt.DisplayText,
                    });
            }

            var plCard = new HeroCard()
            {
                Title = "Related topics",
                Buttons = buttons,
            };

            var attachment = plCard.ToAttachment();

            chatActivity.Attachments.Add(attachment);

            return (Activity)chatActivity;
        }
    }
}
