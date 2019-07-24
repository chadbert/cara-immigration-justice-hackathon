// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QnAService;

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

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Calling QnA Maker");

            // make the query
            var response = await _qnaService.QueryQnAServiceAsync(turnContext.Activity.Text, null);

            if (response != null && response.Length > 0)
            {
                // if no answer could be found that meets the threshold, we'll get a response with score 0
                if (response[0].Score > 0)
                {
                    var prompts = response[0].Context.Prompts;
                    if (prompts != null && prompts.Length > 0)
                    {
                        // send the text as text message because teams doesn't support markdown in cards
                        await turnContext.SendActivityAsync(CreateHeroCard(response[0].Answer, prompts), cancellationToken);
                    }
                    else
                    {
                        var messageActivity = MessageFactory.Text(response[0].Answer);
                        messageActivity.TextFormat = "markdown";

                        await turnContext.SendActivityAsync(messageActivity, cancellationToken);
                    }

                    return;
                }
            }

            await turnContext.SendActivityAsync(MessageFactory.Text("..."), cancellationToken);
            await turnContext.SendActivityAsync(MessageFactory.Text("*(It looks like Maria didn't understand you. Try using different terms, and use very simple language. For example, instead of asking 'Who's your persecutor?', ask 'Who are you afraid of?')*"), cancellationToken);
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

        public static Activity CreateHeroCard(string text, QnAPrompts[] prompts)
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
                Text = text,
                Buttons = buttons,
            };

            var attachment = plCard.ToAttachment();

            chatActivity.Attachments.Add(attachment);

            return (Activity)chatActivity;
        }
    }
}