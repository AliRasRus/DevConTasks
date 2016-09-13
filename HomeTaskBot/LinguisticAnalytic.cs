using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Description;
using Newtonsoft.Json;

namespace Microsoft.CognitiveServices.LinguisticsAPI
{
    public struct LingAnlRequest  //тело запроса к Linguistics API
    {
        public string Language { get; set; }    //язык текста
        public Guid[] AnalyzerIds { get; set; } //идентификаторы используемыех анализаторов
        public string Text { get; set; }        //текст для анализа
    }

    public struct LingAnlResult //ответ от сервиса Linguistics API
    {
        public Guid AnalyzerId { get; set; }    //идентификатор анализатора, выполнившего анализ текста
        public object Result   { get; set; }    //результат анализа
    }

    public struct LingAnlError
    {
        public string Code    { get; set; }
        public string Message { get; set; }
    }

    public enum AnalyzerKind { POSTags=1, PhraseStructure, Tokens };

    public class LinguisticAnalytic
    {
        private static string uri = "https://api.projectoxford.ai/linguistics/v1.0/analyze?";
        private HttpClient httpClient;

        private static Hashtable POSTags = new Hashtable();
        private static void FillPOSTags()
        {
            POSTags.Add("\"",   new string[] { "'", "\"" });
            POSTags.Add("(",    new string[] { "(", "[", "{" });
            POSTags.Add(")",    new string[] { ")", "]", "}" });
            POSTags.Add(".",    new string[] { ".", "!", "?" });
            POSTags.Add(":",    new string[] { ":", ";", "..." });
            POSTags.Add("CC",   new string[] { "and", "but", "or", "yet" });
            POSTags.Add("DT",   new string[] { "a", "an", "the", "all", "both", "neither" });
            POSTags.Add("IN",   new string[] { "in", "inside", "if", "upon", "whether" });
            POSTags.Add("MD",   new string[] { "can", "may", "shall", "will", "could", "would", "should", "might" });
            POSTags.Add("PDT",  new string[] { "all", "both", "many", "such", "half", "quite", "sure" });
            POSTags.Add("PRP",  new string[] { "he", "she", "it", "I", "we", "they", "you" });
            POSTags.Add("PRP$", new string[] { "his", "hers", "its", "my", "our", "their", "your" });
            POSTags.Add("RP",   new string[] { "on", "off", "up", "out", "about", "they", "you" });
            POSTags.Add("WDT",  new string[] { "that", "what", "which" });
            POSTags.Add("WRB",  new string[] { "how", "however", "whenever", "where" });
        }

        public LinguisticAnalytic(string subKey)
        {
            if (POSTags.Count == 0)
                FillPOSTags();

            httpClient = new HttpClient();
            // Request headers
            httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subKey);
        }

        public async Task<string> AnalyzeText(string text)
        {
            // Request body
            LingAnlRequest lReq = new LingAnlRequest()
            {
                Language = @"en",
                AnalyzerIds = new Guid[2] { new Guid("4fa79af1-f22c-408d-98bb-b7d7aeef7f04"),   //анализ по частям речи
                                            new Guid("08ea174b-bfdb-4e64-987e-602f85da7f72") }, //анализ по составу предложения

                Text = text
            };
            string bodyReq = JsonConvert.SerializeObject(lReq);

            HttpRequestMessage textRequest = new HttpRequestMessage(HttpMethod.Post, uri);
            textRequest.Content = new StringContent(bodyReq, System.Text.Encoding.UTF8, "application/json");

            HttpResponseMessage resp = await httpClient.SendAsync(textRequest);
           
            if (resp.IsSuccessStatusCode)
            {
                string responseContent = "";
                
                if (resp.Content != null)
                    responseContent = await resp.Content.ReadAsStringAsync();
                
                if (!string.IsNullOrWhiteSpace(responseContent))
                {
                    LingAnlResult[] lingRes = JsonConvert.DeserializeObject<LingAnlResult[]>(responseContent);

                    string POSTagsstring = lingRes[0].Result.ToString();
                    string[] posspl = POSTagsstring.Split(new char[] { '\"' });

                    string Tokenstring = lingRes[1].Result.ToString();
                    string[] txtspl = Tokenstring.Split(new char[] { '\"' });
                    List<string> txtTokens = new List<string>();
                    for(int i=0; i<txtspl.GetLength(0);i++)
                    {
                        string str = txtspl[i];
                        if (!str.Equals("NormalizedToken")) continue;
                        txtTokens.Add(txtspl[i + 2]);
                    }

                    int wrdindx = 0;
                    string res = "";
                    foreach (string str in posspl)
                    {
                        if (str.Contains("\r\n")) continue;
                        if (POSTags.ContainsKey(str))
                        {
                            string[] arr = (string[])POSTags[str];
                            Random rnd = new Random();
                            res += arr[rnd.Next(arr.GetLength(0))] + " ";
                        }
                        else
                        {
                            //await GenerateWords(str);
                            
                            res += txtTokens[wrdindx] + " ";
                        }
                        
                        wrdindx = wrdindx < txtTokens.Count ? wrdindx+1 : wrdindx;
                    }
                    return res;
                }
            }
            else
            {
                if (resp.Content != null && resp.Content.Headers.ContentType.MediaType.Contains("application/json"))
                {
                    string errorString = await resp.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<LingAnlError>(errorString).Message;
                }
            }

            return "We didn't analyze anything";
        }

        //private async Task<string> GenerateWords(string kind)
        //{
        //    HttpClient httpRndClient = new HttpClient();
        //    string uri = "";
        //    if (kind.Equals("NN"))
        //    {
        //        uri = @"http://www.randomwordgenerator.com/noun.php?&generate";
        //    }

        //    HttpRequestMessage txtRequest = new HttpRequestMessage(HttpMethod.Get, uri);
        //    HttpResponseMessage resp = await httpRndClient.SendAsync(txtRequest);
            
        //    if (resp.IsSuccessStatusCode)
        //    {
        //        string responseContent = "";

        //        if (resp.Content != null)
        //            responseContent = await resp.Content.ReadAsStringAsync();
        //    }
        //    return "";
        //}
    }
}