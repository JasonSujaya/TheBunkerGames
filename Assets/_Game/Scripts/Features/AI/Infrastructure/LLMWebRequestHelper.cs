using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace TheBunkerGames
{
    /// <summary>
    /// Low-level HTTP utility for LLM API calls.
    /// Handles UnityWebRequest construction, headers, and error extraction.
    /// </summary>
    public static class LLMWebRequestHelper
    {
        /// <summary>
        /// Sends a POST request with JSON body. Use with StartCoroutine.
        /// </summary>
        /// <param name="url">Full endpoint URL</param>
        /// <param name="jsonBody">Serialized JSON request body</param>
        /// <param name="headers">Custom headers (Authorization, etc.)</param>
        /// <param name="timeoutSeconds">Request timeout in seconds</param>
        /// <param name="onComplete">Callback: (responseBody, httpStatusCode, isError)</param>
        public static IEnumerator Post(
            string url,
            string jsonBody,
            Dictionary<string, string> headers,
            float timeoutSeconds,
            Action<string, long, bool> onComplete)
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

            using var request = new UnityWebRequest(url, "POST");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = Mathf.Max(1, Mathf.RoundToInt(timeoutSeconds));
            request.SetRequestHeader("Content-Type", "application/json");

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    request.SetRequestHeader(header.Key, header.Value);
                }
            }

            yield return request.SendWebRequest();

            string responseBody = request.downloadHandler?.text ?? "";
            long statusCode = request.responseCode;
            bool isError = request.result != UnityWebRequest.Result.Success;

            onComplete?.Invoke(responseBody, statusCode, isError);
        }
    }
}
