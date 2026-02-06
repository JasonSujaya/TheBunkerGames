using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TheBunkerGames
{
    // -------------------------------------------------------------------------
    // Request DTOs
    // -------------------------------------------------------------------------

    [Serializable]
    public class LLMMessage
    {
        [JsonProperty("role")]
        public string role;

        [JsonProperty("content")]
        public string content;

        public static LLMMessage System(string content) =>
            new LLMMessage { role = "system", content = content };

        public static LLMMessage User(string content) =>
            new LLMMessage { role = "user", content = content };

        public static LLMMessage Assistant(string content) =>
            new LLMMessage { role = "assistant", content = content };
    }

    [Serializable]
    public class LLMChatRequest
    {
        [JsonProperty("model")]
        public string model;

        [JsonProperty("messages")]
        public List<LLMMessage> messages;

        [JsonProperty("temperature", NullValueHandling = NullValueHandling.Ignore)]
        public float? temperature;

        [JsonProperty("max_tokens", NullValueHandling = NullValueHandling.Ignore)]
        public int? max_tokens;
    }

    // -------------------------------------------------------------------------
    // Response DTOs
    // -------------------------------------------------------------------------

    [Serializable]
    public class LLMChatResponse
    {
        [JsonProperty("id")]
        public string id;

        [JsonProperty("model")]
        public string model;

        [JsonProperty("choices")]
        public List<LLMChoice> choices;

        [JsonProperty("usage")]
        public LLMUsage usage;

        public string FirstMessageContent =>
            choices != null && choices.Count > 0
                ? choices[0].message?.content
                : null;
    }

    [Serializable]
    public class LLMChoice
    {
        [JsonProperty("index")]
        public int index;

        [JsonProperty("message")]
        public LLMMessage message;

        [JsonProperty("finish_reason")]
        public string finish_reason;
    }

    [Serializable]
    public class LLMUsage
    {
        [JsonProperty("prompt_tokens")]
        public int prompt_tokens;

        [JsonProperty("completion_tokens")]
        public int completion_tokens;

        [JsonProperty("total_tokens")]
        public int total_tokens;
    }

    [Serializable]
    public class LLMErrorResponse
    {
        [JsonProperty("error")]
        public LLMError error;
    }

    [Serializable]
    public class LLMError
    {
        [JsonProperty("message")]
        public string message;

        [JsonProperty("type")]
        public string type;

        [JsonProperty("code")]
        public string code;
    }

    // -------------------------------------------------------------------------
    // Result Wrapper
    // -------------------------------------------------------------------------

    public class LLMResult<T>
    {
        public bool Success { get; private set; }
        public T Data { get; private set; }
        public string Error { get; private set; }
        public long HttpStatusCode { get; private set; }

        private LLMResult() { }

        public static LLMResult<T> Ok(T data, long statusCode)
        {
            return new LLMResult<T>
            {
                Success = true,
                Data = data,
                HttpStatusCode = statusCode
            };
        }

        public static LLMResult<T> Fail(string error, long statusCode = 0)
        {
            return new LLMResult<T>
            {
                Success = false,
                Error = error,
                HttpStatusCode = statusCode
            };
        }
    }
}
