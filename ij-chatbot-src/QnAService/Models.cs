// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace QnAPrompting.Models
{
    //https://docs.microsoft.com/en-us/rest/api/cognitiveservices/qnamakerruntime/runtime/generateanswer
    public class GenerateAnswerRequest
    {
        QnAContext Context { get; set; }

        bool IsTest { get; set; }

        string QnaId { get; set; }

        string Question { get; set; }

        int ScoreThreshold { get; set; }

        int Top { get; set; }

        string UserId { get; set; }

        //strictFilters	MetadataDTO[]
    }

    public class QnAResultList
    {
        public QnAResult[] Answers { get; set; }
    }

    //https://docs.microsoft.com/en-us/rest/api/cognitiveservices/qnamakerruntime/runtime/generateanswer#qnasearchresult
    public class QnAResult
    {
        public string[] Questions { get; set; }

        public string Answer { get; set; }

        public double Score { get; set; }

        public int Id { get; set; }

        public string Source { get; set; }

        public QnAMetadata[] Metadata { get; }

        public QnAContext Context { get; set; }
    }

    public class QnAMetadata
    {
        public string Name { get; set; }

        public string Value { get; set; }
    }

    public class QnAContext
    {
        public QnAPrompts[] Prompts { get; set; }
    }

    public class QnABotState
    {
        public int PreviousQnaId { get; set; }

        public string PreviousUserQuery { get; set; }
    }

    public class QnAPrompts
    {
        public int DisplayOrder { get; set; }

        public int QnaId { get; set; }

        public string DisplayText { get; set; }
    }
}
