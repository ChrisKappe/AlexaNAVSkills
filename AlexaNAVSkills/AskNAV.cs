using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AlexaNAVSkills
{
    public static class AskNAV
    {
        [FunctionName("AskNAV")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");            

            var navOdataUrl = new System.Uri("<url to NAV odata>");
            var credentials = new NetworkCredential("<NAV user name>", "<NAV Web Access Key>");
            var handler = new HttpClientHandler { Credentials = credentials };

            using (var client = new HttpClient(handler))
            {
                // Get request body
                dynamic data = await req.Content.ReadAsAsync<object>();
                //log.Info($"Content={data}");

                JObject alexaRequest = JObject.Parse(Convert.ToString(data));
                JObject requestJson = new JObject();
                JProperty jProperty = new JProperty("Json", alexaRequest.ToString());
                requestJson.Add(jProperty);
                //log.Info($"Request={requestJson.ToString()}");
                var requestData = new StringContent(requestJson.ToString(), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(navOdataUrl,requestData);
                dynamic result = await response.Content.ReadAsStringAsync();
                //log.Info($"Result={result}");

                JObject responseJson = JObject.Parse(Convert.ToString(result));
                if (responseJson.TryGetValue("Json", out JToken responseJToken))
                {
                    jProperty = responseJson.Property("Json");
                    JObject alexaResponse = JObject.Parse(Convert.ToString(jProperty.Value));
                    log.Info("Response from NAV forwarded!");
                    return req.CreateResponse(HttpStatusCode.OK, alexaResponse );
                }
                else
                {
                    log.Info("Failed to get response from NAV!");
                    return req.CreateResponse(HttpStatusCode.OK, new
                    {
                        version = "1.1",
                        sessionAttributes = new { },
                        response = new
                        {
                            outputSpeech = new
                            {
                                type = "PlainText",
                                text = "Unable to speak to Navision!"
                            },
                            card = new
                            {
                                type = "Simple",
                                title = "NAVError",
                                content = "Navision missing!"
                            },
                            shouldEndSession = true
                        }
                    });
                }
            }
        }
    }
}
